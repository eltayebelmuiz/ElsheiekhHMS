using System.Data;
using System.Globalization;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Appointments.Persistence;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Enums;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElsheiekhHMS.Infrastructure.Persistence.Appointments;

public sealed class AppointmentPersistence(
    ElsheiekhHmsDbContext context,
    AppointmentCodeAllocator codeAllocator) : IAppointmentPersistence
{
    public async Task<AppointmentDetailsDto?> GetDetailsAsync(
        int appointmentId,
        CancellationToken cancellationToken)
    {
        var projection = await context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.Id == appointmentId)
            .Select(appointment => new AppointmentDetailsProjection(
                appointment.Id,
                appointment.AppointmentCode,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.DepartmentId,
                appointment.ScheduledDate,
                appointment.ScheduledTime,
                appointment.Type,
                appointment.Status,
                appointment.Notes,
                appointment.CancellationReason,
                appointment.CancelledAt,
                appointment.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);

        return projection?.ToDto();
    }

    public async Task<PagedResult<AppointmentSummaryDto>> SearchAsync(
        AppointmentSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.Appointments
            .AsNoTracking()
            .Where(appointment => appointment.ScheduledDate >= request.FromDate && appointment.ScheduledDate <= request.ToDate);

        if (request.PatientId.HasValue) query = query.Where(appointment => appointment.PatientId == request.PatientId.Value);
        if (request.DoctorId.HasValue) query = query.Where(appointment => appointment.DoctorId == request.DoctorId.Value);
        if (request.DepartmentId.HasValue) query = query.Where(appointment => appointment.DepartmentId == request.DepartmentId.Value);
        if (request.Status.HasValue) query = query.Where(appointment => appointment.Status == request.Status.Value);

        var ordered = ApplyOrdering(query, request);
        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((request.Page.PageNumber - 1) * request.Page.PageSize)
            .Take(request.Page.PageSize)
            .Select(appointment => new AppointmentSummaryDto(
                appointment.Id,
                appointment.AppointmentCode,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.DepartmentId,
                appointment.ScheduledDate,
                appointment.ScheduledTime,
                appointment.Type,
                appointment.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<AppointmentSummaryDto>(
            items,
            totalCount,
            request.Page.PageNumber,
            request.Page.PageSize);
    }

    public Task<string> AllocateAppointmentCodeAsync(CancellationToken cancellationToken) =>
        codeAllocator.AllocateAsync(cancellationToken);

    public Task<bool> PatientExistsAsync(int patientId, CancellationToken cancellationToken) =>
        context.Patients.AnyAsync(patient => patient.Id == patientId, cancellationToken);

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

    public Task<Appointment?> LoadTrackedAsync(
        int appointmentId,
        CancellationToken cancellationToken) =>
        context.Appointments.SingleOrDefaultAsync(
            appointment => appointment.Id == appointmentId,
            cancellationToken);

    public void Add(Appointment appointment) => context.Appointments.Add(appointment);

    public async Task<AppointmentPersistenceSaveStatus> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            foreach (var added in context.ChangeTracker.Entries<Appointment>()
                         .Where(entry => entry.State == EntityState.Added)
                         .Select(entry => entry.Entity))
            {
                await AcquireSlotLockAsync(added, transaction, cancellationToken);
                var collision = await context.Appointments.AnyAsync(
                    existing =>
                        existing.DepartmentId == added.DepartmentId &&
                        existing.ScheduledDate == added.ScheduledDate &&
                        existing.ScheduledTime == added.ScheduledTime &&
                        existing.Status != AppointmentStatus.Cancelled &&
                        existing.Status != AppointmentStatus.NoShow &&
                        existing.Status != AppointmentStatus.Completed,
                    cancellationToken);

                if (collision)
                {
                    DetachAddedEntries();
                    await transaction.RollbackAsync(cancellationToken);
                    return AppointmentPersistenceSaveStatus.Collision;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AppointmentPersistenceSaveStatus.Saved;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return AppointmentPersistenceSaveStatus.ConcurrencyConflict;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
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

    private async Task AcquireSlotLockAsync(
        Appointment appointment,
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
            "ElsheiekhHMS:AppointmentSlot:",
            appointment.DepartmentId.ToString(CultureInfo.InvariantCulture),
            ":",
            appointment.ScheduledDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            ":",
            appointment.ScheduledTime.ToString("HHmmss", CultureInfo.InvariantCulture));
        command.Parameters.Add(parameter);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (result < 0)
        {
            throw new InvalidOperationException("Unable to acquire the appointment slot lock.");
        }
    }

    private static IQueryable<Appointment> ApplyOrdering(
        IQueryable<Appointment> query,
        AppointmentSearchRequest request)
    {
        return request.SortBy switch
        {
            AppointmentSortField.Status => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(appointment => appointment.Status)
                    .ThenBy(appointment => appointment.ScheduledDate)
                    .ThenBy(appointment => appointment.ScheduledTime)
                : query.OrderByDescending(appointment => appointment.Status)
                    .ThenByDescending(appointment => appointment.ScheduledDate)
                    .ThenByDescending(appointment => appointment.ScheduledTime),
            _ => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(appointment => appointment.ScheduledDate)
                    .ThenBy(appointment => appointment.ScheduledTime)
                : query.OrderByDescending(appointment => appointment.ScheduledDate)
                    .ThenByDescending(appointment => appointment.ScheduledTime)
        };
    }

    private sealed record AppointmentDetailsProjection(
        int Id,
        string AppointmentCode,
        int PatientId,
        int DoctorId,
        int DepartmentId,
        DateOnly ScheduledDate,
        TimeOnly ScheduledTime,
        AppointmentType Type,
        AppointmentStatus Status,
        string? Notes,
        string? CancellationReason,
        DateTimeOffset? CancelledAt,
        byte[] RowVersion)
    {
        public AppointmentDetailsDto ToDto() => new(
            Id,
            AppointmentCode,
            PatientId,
            DoctorId,
            DepartmentId,
            ScheduledDate,
            ScheduledTime,
            Type,
            Status,
            Notes,
            CancellationReason,
            CancelledAt,
            RowVersion.Length == 0 ? null : Convert.ToBase64String(RowVersion));
    }
}
