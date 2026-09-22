using System.Data;
using System.Globalization;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Application.Queue.Persistence;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Enums;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElsheiekhHMS.Infrastructure.Persistence.Queue;

public sealed class QueuePersistence(
    ElsheiekhHmsDbContext context,
    QueueTicketAllocator ticketAllocator) : IQueuePersistence
{
    public async Task<QueueEntryDetailsDto?> GetDetailsAsync(
        int queueEntryId,
        CancellationToken cancellationToken)
    {
        var projection = await context.WalkInQueueEntries
            .AsNoTracking()
            .Where(entry => entry.Id == queueEntryId)
            .Select(entry => new QueueEntryDetailsProjection(
                entry.Id,
                entry.QueueNumber,
                entry.PatientId,
                entry.DepartmentId,
                entry.AppointmentId,
                entry.DoctorId,
                entry.QueueDate,
                entry.Priority,
                entry.Status,
                entry.RegisteredAt,
                entry.Notes,
                entry.CalledAt,
                entry.CompletedAt,
                entry.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);

        return projection?.ToDto();
    }

    public async Task<QueueEntryDetailsDto?> GetDetailsByAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken)
    {
        var projection = await context.WalkInQueueEntries
            .AsNoTracking()
            .Where(entry => entry.AppointmentId == appointmentId)
            .Select(entry => new QueueEntryDetailsProjection(
                entry.Id,
                entry.QueueNumber,
                entry.PatientId,
                entry.DepartmentId,
                entry.AppointmentId,
                entry.DoctorId,
                entry.QueueDate,
                entry.Priority,
                entry.Status,
                entry.RegisteredAt,
                entry.Notes,
                entry.CalledAt,
                entry.CompletedAt,
                entry.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);

        return projection?.ToDto();
    }

    public async Task<PagedResult<QueueEntrySummaryDto>> SearchAsync(
        QueueSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.WalkInQueueEntries
            .AsNoTracking()
            .Where(entry => entry.QueueDate == request.QueueDate);

        if (request.DepartmentId.HasValue) query = query.Where(entry => entry.DepartmentId == request.DepartmentId.Value);
        if (request.DoctorId.HasValue) query = query.Where(entry => entry.DoctorId == request.DoctorId.Value);
        if (request.Status.HasValue) query = query.Where(entry => entry.Status == request.Status.Value);
        if (request.Priority.HasValue) query = query.Where(entry => entry.Priority == request.Priority.Value);

        var ordered = ApplyOrdering(query, request);
        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((request.Page.PageNumber - 1) * request.Page.PageSize)
            .Take(request.Page.PageSize)
            .Select(entry => new QueueEntrySummaryDto(
                entry.Id,
                entry.QueueNumber,
                entry.PatientId,
                entry.DepartmentId,
                entry.AppointmentId,
                entry.DoctorId,
                entry.QueueDate,
                entry.Priority,
                entry.Status,
                entry.RegisteredAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<QueueEntrySummaryDto>(
            items,
            totalCount,
            request.Page.PageNumber,
            request.Page.PageSize);
    }

    public Task<bool> PatientExistsAsync(int patientId, CancellationToken cancellationToken) =>
        context.Patients.AnyAsync(patient => patient.Id == patientId, cancellationToken);

    public Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken) =>
        context.Departments.AnyAsync(department => department.Id == departmentId, cancellationToken);

    public Task<bool> DepartmentIsActiveAsync(int departmentId, CancellationToken cancellationToken) =>
        context.Departments.AnyAsync(
            department => department.Id == departmentId && department.IsActive,
            cancellationToken);

    public Task<bool> DoctorIsActiveInDepartmentAsync(
        int doctorId,
        int departmentId,
        CancellationToken cancellationToken) =>
        context.Doctors.AnyAsync(
            doctor => doctor.Id == doctorId &&
                      doctor.DepartmentId == departmentId &&
                      doctor.Status == DoctorStatus.Active,
            cancellationToken);

    public Task<bool> HasActiveEntryAsync(
        int patientId,
        DateOnly queueDate,
        CancellationToken cancellationToken) =>
        context.WalkInQueueEntries.AnyAsync(
            entry => entry.PatientId == patientId &&
                     entry.QueueDate == queueDate &&
                     entry.Status != QueueStatus.Completed &&
                     entry.Status != QueueStatus.Cancelled,
            cancellationToken);

    public Task<(DateOnly QueueDate, int SequenceNumber, string QueueNumber)> AllocateTicketAsync(
        DateOnly queueDate,
        CancellationToken cancellationToken) =>
        ticketAllocator.AllocateAsync(queueDate, cancellationToken);

    public Task<WalkInQueueEntry?> LoadTrackedAsync(
        int queueEntryId,
        CancellationToken cancellationToken) =>
        context.WalkInQueueEntries.SingleOrDefaultAsync(
            entry => entry.Id == queueEntryId,
            cancellationToken);

    public void Add(WalkInQueueEntry entry) => context.WalkInQueueEntries.Add(entry);

    public async Task<QueuePersistenceSaveStatus> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            foreach (var added in context.ChangeTracker.Entries<WalkInQueueEntry>()
                         .Where(entry => entry.State == EntityState.Added)
                         .Select(entry => entry.Entity))
            {
                await AcquireDuplicateLockAsync(added, transaction, cancellationToken);
                var duplicate = await context.WalkInQueueEntries.AnyAsync(
                    existing => existing.PatientId == added.PatientId &&
                                existing.QueueDate == added.QueueDate &&
                                existing.Status != QueueStatus.Completed &&
                                existing.Status != QueueStatus.Cancelled,
                    cancellationToken);
                if (duplicate)
                {
                    DetachAddedEntries();
                    await transaction.RollbackAsync(cancellationToken);
                    return QueuePersistenceSaveStatus.DuplicateActive;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return QueuePersistenceSaveStatus.Saved;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return QueuePersistenceSaveStatus.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (ContainsIndex(exception, "UX_WalkInQueueEntries_AppointmentId"))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return QueuePersistenceSaveStatus.DuplicateAppointmentLink;
        }
        catch (DbUpdateException exception) when (ContainsIndex(exception, "UX_WalkInQueueEntries_QueueDate_SequenceNumber"))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return QueuePersistenceSaveStatus.TicketAllocationFailure;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task AcquireDuplicateLockAsync(
        WalkInQueueEntry entry,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            SELECT @result;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.DbType = DbType.String;
        parameter.Value = string.Concat(
            "ElsheiekhHMS:QueuePatient:",
            entry.PatientId.ToString(CultureInfo.InvariantCulture),
            ":",
            entry.QueueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        command.Parameters.Add(parameter);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (result < 0) throw new InvalidOperationException("Unable to acquire the queue duplicate lock.");
    }

    private void DetachAddedEntries()
    {
        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(entry => entry.State == EntityState.Added)
                     .ToArray())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool ContainsIndex(DbUpdateException exception, string indexName) =>
        exception.ToString().Contains(indexName, StringComparison.OrdinalIgnoreCase);

    private static IQueryable<WalkInQueueEntry> ApplyOrdering(
        IQueryable<WalkInQueueEntry> query,
        QueueSearchRequest request) => request.SortBy switch
        {
            QueueSortField.Priority => request.SortDirection == SortDirection.Ascending
                ? query.OrderByDescending(entry => entry.Priority)
                    .ThenBy(entry => entry.RegisteredAt)
                    .ThenBy(entry => entry.SequenceNumber)
                : query.OrderBy(entry => entry.Priority)
                    .ThenByDescending(entry => entry.RegisteredAt)
                    .ThenByDescending(entry => entry.SequenceNumber),
            QueueSortField.QueueNumber => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(entry => entry.SequenceNumber).ThenBy(entry => entry.RegisteredAt)
                : query.OrderByDescending(entry => entry.SequenceNumber).ThenByDescending(entry => entry.RegisteredAt),
            _ => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(entry => entry.RegisteredAt).ThenBy(entry => entry.SequenceNumber)
                : query.OrderByDescending(entry => entry.RegisteredAt).ThenByDescending(entry => entry.SequenceNumber)
        };

    private sealed record QueueEntryDetailsProjection(
        int Id,
        string QueueNumber,
        int PatientId,
        int DepartmentId,
        int? AppointmentId,
        int? DoctorId,
        DateOnly QueueDate,
        QueuePriority Priority,
        QueueStatus Status,
        DateTimeOffset RegisteredAt,
        string? Notes,
        DateTimeOffset? CalledAt,
        DateTimeOffset? CompletedAt,
        byte[] RowVersion)
    {
        public QueueEntryDetailsDto ToDto() => new(
            Id,
            QueueNumber,
            PatientId,
            DepartmentId,
            AppointmentId,
            DoctorId,
            QueueDate,
            Priority,
            Status,
            RegisteredAt,
            Notes,
            CalledAt,
            CompletedAt,
            RowVersion.Length == 0 ? null : Convert.ToBase64String(RowVersion));
    }
}
