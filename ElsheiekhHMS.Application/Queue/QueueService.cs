using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Application.Queue.Persistence;
using ElsheiekhHMS.Application.Queue.Validation;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Exceptions;

namespace ElsheiekhHMS.Application.Queue;

public sealed class QueueService(
    IQueuePersistence persistence,
    IAuditEventWriter auditEventWriter,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IQueueService
{
    private const string HospitalTimeZoneId = "Africa/Kigali";

    public async Task<ServiceResult<QueueEntryDetailsDto>> GetByIdAsync(
        int queueEntryId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<QueueEntryDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (queueEntryId <= 0)
        {
            return Failure<QueueEntryDetailsDto>(
                "queue.entry_id.positive",
                "Queue entry ID must be positive.",
                nameof(queueEntryId));
        }

        try
        {
            var entry = await persistence.GetDetailsAsync(queueEntryId, cancellationToken);
            return entry is null
                ? Failure<QueueEntryDetailsDto>("queue.not_found", "Queue entry was not found.")
                : ServiceResult<QueueEntryDetailsDto>.Success(entry);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<QueueEntryDetailsDto>(); }
    }

    public async Task<ServiceResult<PagedResult<QueueEntrySummaryDto>>> SearchAsync(
        QueueSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<PagedResult<QueueEntrySummaryDto>>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new QueueSearchRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return ValidationFailure<PagedResult<QueueEntrySummaryDto>>(validation);
        }

        try
        {
            var page = await persistence.SearchAsync(request, cancellationToken);
            return ServiceResult<PagedResult<QueueEntrySummaryDto>>.Success(page);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<PagedResult<QueueEntrySummaryDto>>(); }
    }

    public async Task<ServiceResult<QueueEntryDetailsDto>> AddAsync(
        AddWalkInQueueEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<QueueEntryDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new AddWalkInQueueEntryRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<QueueEntryDetailsDto>(validation);

        var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
        if (!TryGetQueueDate(utcNow, out var queueDate, out var dateError))
        {
            return Failure<QueueEntryDetailsDto>(dateError.Code, dateError.Message, dateError.Field);
        }

        try { return await AddCoreAsync(request.PatientId, request.DepartmentId, null, request.Priority, request.Notes, utcNow, queueDate, cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<QueueEntryDetailsDto>("queue.domain_validation", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure<QueueEntryDetailsDto>("queue.ticket_allocation_failed", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<QueueEntryDetailsDto>(); }
    }

    public async Task<ServiceResult<QueueEntryDetailsDto>> AddAppointmentAsync(
        AddAppointmentQueueEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<QueueEntryDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new AddAppointmentQueueEntryRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<QueueEntryDetailsDto>(validation);

        var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
        if (!TryGetQueueDate(utcNow, out var queueDate, out var dateError))
        {
            return Failure<QueueEntryDetailsDto>(dateError.Code, dateError.Message, dateError.Field);
        }

        try { return await AddCoreAsync(request.PatientId, request.DepartmentId, request.AppointmentId, request.Priority, request.Notes, utcNow, queueDate, cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception) { return Failure<QueueEntryDetailsDto>("queue.domain_validation", exception.Message); }
        catch (InvalidOperationException exception) { return Failure<QueueEntryDetailsDto>("queue.ticket_allocation_failed", exception.Message); }
        catch (Exception) { return PersistenceFailure<QueueEntryDetailsDto>(); }
    }

    public async Task<ServiceResult<QueueEntryDetailsDto>> GetByAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<QueueEntryDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (appointmentId <= 0) return Failure<QueueEntryDetailsDto>("queue.appointment_id.positive", "Appointment ID must be positive.", nameof(appointmentId));
        try
        {
            var entry = await persistence.GetDetailsByAppointmentIdAsync(appointmentId, cancellationToken);
            return entry is null
                ? Failure<QueueEntryDetailsDto>("queue.not_found", "Queue entry was not found.")
                : ServiceResult<QueueEntryDetailsDto>.Success(entry);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<QueueEntryDetailsDto>(); }
    }

    private async Task<ServiceResult<QueueEntryDetailsDto>> AddCoreAsync(
        int patientId,
        int departmentId,
        int? appointmentId,
        QueuePriority priority,
        string? notes,
        DateTimeOffset utcNow,
        DateOnly queueDate,
        CancellationToken cancellationToken)
    {
        if (!await persistence.PatientExistsAsync(patientId, cancellationToken))
            return Failure<QueueEntryDetailsDto>("queue.patient.not_found", "The patient was not found.", nameof(patientId));
        if (!await persistence.DepartmentExistsAsync(departmentId, cancellationToken))
            return Failure<QueueEntryDetailsDto>("queue.department.not_found", "The department was not found.", nameof(departmentId));
        if (!await persistence.DepartmentIsActiveAsync(departmentId, cancellationToken))
            return Failure<QueueEntryDetailsDto>("queue.department.inactive", "The department is not active.", nameof(departmentId));
        if (await persistence.HasActiveEntryAsync(patientId, queueDate, cancellationToken))
            return Failure<QueueEntryDetailsDto>("queue.duplicate_active", "The patient already has an active queue entry for today.", nameof(patientId));

        var ticket = await persistence.AllocateTicketAsync(queueDate, cancellationToken);
        var entry = new WalkInQueueEntry(
            patientId,
            departmentId,
            ticket.QueueDate,
            ticket.SequenceNumber,
            ticket.QueueNumber,
            priority,
            notes,
            utcNow,
            currentUser.UserId,
            appointmentId);

        persistence.Add(entry);
        await RecordAuditAsync(AuditActions.QueueEntryCreated, entry.QueueNumber, null, cancellationToken);
        return MapSaveStatus(await persistence.SaveChangesAsync(cancellationToken), entry);
    }

    public Task<ServiceResult<QueueEntryDetailsDto>> CallToNurseAsync(
        QueueEntryActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.QueueEntryId,
            request.ExpectedConcurrencyToken,
            new QueueEntryActionRequestValidator().Validate(request),
            (entry, utcNow) =>
            {
                entry.CallToNurse(utcNow, currentUser.UserId);
                return AuditActions.QueueEntryCalled;
            },
            request.Reason,
            cancellationToken);

    public async Task<ServiceResult<QueueEntryDetailsDto>> SendToDoctorAsync(
        SendToDoctorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<QueueEntryDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new SendToDoctorRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<QueueEntryDetailsDto>(validation);
        if (string.IsNullOrWhiteSpace(request.ExpectedConcurrencyToken))
        {
            return Failure<QueueEntryDetailsDto>(
                "queue.concurrency_token.required",
                "An expected concurrency token is required.",
                nameof(request.ExpectedConcurrencyToken));
        }

        try
        {
            var entry = await persistence.LoadTrackedAsync(request.QueueEntryId, cancellationToken);
            if (entry is null) return Failure<QueueEntryDetailsDto>("queue.not_found", "Queue entry was not found.");
            if (!MatchesConcurrencyToken(entry, request.ExpectedConcurrencyToken)) return ConcurrencyFailure();
            if (!await persistence.DoctorIsActiveInDepartmentAsync(request.DoctorId, entry.DepartmentId, cancellationToken))
            {
                return Failure<QueueEntryDetailsDto>("queue.doctor.unavailable", "The doctor is not active in the queue department.", nameof(request.DoctorId));
            }

            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            var action = entry.Status == QueueStatus.Waiting
                ? AuditActions.QueueEntrySkipped
                : AuditActions.QueueEntryStarted;
            entry.SendToDoctor(request.DoctorId, utcNow, currentUser.UserId);
            await RecordAuditAsync(action, entry.QueueNumber, null, cancellationToken);
            return MapSaveStatus(await persistence.SaveChangesAsync(cancellationToken), entry);
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception) { return Failure<QueueEntryDetailsDto>("queue.domain_rule", exception.Message); }
        catch (Exception) { return PersistenceFailure<QueueEntryDetailsDto>(); }
    }

    public Task<ServiceResult<QueueEntryDetailsDto>> HoldAsync(
        QueueEntryActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.QueueEntryId,
            request.ExpectedConcurrencyToken,
            new QueueEntryActionRequestValidator().Validate(request),
            (entry, utcNow) =>
            {
                entry.Hold(utcNow, currentUser.UserId);
                return AuditActions.QueueEntryHeld;
            },
            request.Reason,
            cancellationToken);

    public Task<ServiceResult<QueueEntryDetailsDto>> ResumeAsync(
        QueueEntryActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.QueueEntryId,
            request.ExpectedConcurrencyToken,
            new QueueEntryActionRequestValidator().Validate(request),
            (entry, utcNow) =>
            {
                entry.Resume(utcNow, currentUser.UserId);
                return AuditActions.QueueEntryResumed;
            },
            request.Reason,
            cancellationToken);

    public Task<ServiceResult<QueueEntryDetailsDto>> CompleteAsync(
        QueueEntryActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.QueueEntryId,
            request.ExpectedConcurrencyToken,
            new QueueEntryActionRequestValidator().Validate(request),
            (entry, utcNow) =>
            {
                entry.CompleteQueue(utcNow, currentUser.UserId);
                return AuditActions.QueueEntryCompleted;
            },
            request.Reason,
            cancellationToken);

    public Task<ServiceResult<QueueEntryDetailsDto>> CancelAsync(
        QueueEntryActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.QueueEntryId,
            request.ExpectedConcurrencyToken,
            new QueueEntryActionRequestValidator().Validate(request),
            (entry, utcNow) =>
            {
                entry.Cancel(utcNow, currentUser.UserId);
                return AuditActions.QueueEntryCancelled;
            },
            request.Reason,
            cancellationToken);

    private async Task<ServiceResult<QueueEntryDetailsDto>> MutateAsync(
        int queueEntryId,
        string? expectedConcurrencyToken,
        ValidationResult validation,
        Func<WalkInQueueEntry, DateTimeOffset, string> mutation,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (!HasAccess()) return Forbidden<QueueEntryDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (!validation.IsValid) return ValidationFailure<QueueEntryDetailsDto>(validation);
        if (string.IsNullOrWhiteSpace(expectedConcurrencyToken))
        {
            return Failure<QueueEntryDetailsDto>(
                "queue.concurrency_token.required",
                "An expected concurrency token is required.",
                nameof(expectedConcurrencyToken));
        }

        try
        {
            var entry = await persistence.LoadTrackedAsync(queueEntryId, cancellationToken);
            if (entry is null) return Failure<QueueEntryDetailsDto>("queue.not_found", "Queue entry was not found.");
            if (!MatchesConcurrencyToken(entry, expectedConcurrencyToken)) return ConcurrencyFailure();

            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            var auditAction = mutation(entry, utcNow);
            await RecordAuditAsync(auditAction, entry.QueueNumber, reason, cancellationToken);
            return MapSaveStatus(await persistence.SaveChangesAsync(cancellationToken), entry);
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception) { return Failure<QueueEntryDetailsDto>("queue.domain_rule", exception.Message); }
        catch (Exception) { return PersistenceFailure<QueueEntryDetailsDto>(); }
    }

    private Task RecordAuditAsync(string action, string queueNumber, string? reason, CancellationToken cancellationToken) =>
        auditEventWriter.RecordAsync(
            new AuditEventRequest(AuditCategories.Business, action, "WalkInQueueEntry", queueNumber, reason),
            cancellationToken);

    private static bool TryGetQueueDate(DateTimeOffset utcNow, out DateOnly queueDate, out ServiceError error)
    {
        queueDate = default;
        error = new ServiceError("queue.timezone.unavailable", "The hospital queue timezone is unavailable.");
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(HospitalTimeZoneId);
            queueDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).Date);
            return true;
        }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    private bool HasAccess() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.Roles.Any(role =>
            string.Equals(role, RoleNames.Administrator, StringComparison.Ordinal) ||
            string.Equals(role, RoleNames.Receptionist, StringComparison.Ordinal));

    private static bool MatchesConcurrencyToken(WalkInQueueEntry entry, string token)
    {
        try { return Convert.FromBase64String(token).AsSpan().SequenceEqual(entry.RowVersion); }
        catch (FormatException) { return false; }
    }

    private static ServiceResult<QueueEntryDetailsDto> MapSaveStatus(
        QueuePersistenceSaveStatus status,
        WalkInQueueEntry entry) => status switch
        {
            QueuePersistenceSaveStatus.Saved => ServiceResult<QueueEntryDetailsDto>.Success(ToDetails(entry)),
            QueuePersistenceSaveStatus.DuplicateActive => Failure<QueueEntryDetailsDto>("queue.duplicate_active", "The patient already has an active queue entry for today."),
            QueuePersistenceSaveStatus.DuplicateAppointmentLink => Failure<QueueEntryDetailsDto>("queue.duplicate_appointment", "The appointment already has a queue entry."),
            QueuePersistenceSaveStatus.ConcurrencyConflict => ConcurrencyFailure(),
            QueuePersistenceSaveStatus.TicketAllocationFailure => Failure<QueueEntryDetailsDto>("queue.ticket_allocation_failed", "A queue ticket could not be allocated."),
            _ => PersistenceFailure<QueueEntryDetailsDto>()
        };

    private static ServiceResult<QueueEntryDetailsDto> ConcurrencyFailure() =>
        Failure<QueueEntryDetailsDto>("queue.concurrency_conflict", "The queue entry was changed by another operation.");

    private static QueueEntryDetailsDto ToDetails(WalkInQueueEntry entry) => new(
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
        entry.RowVersion.Length == 0 ? null : Convert.ToBase64String(entry.RowVersion));

    private static ServiceResult<T> Forbidden<T>() => Failure<T>("queue.forbidden", "The current user is not authorized to manage the queue.");
    private static ServiceResult<T> PersistenceFailure<T>() => Failure<T>("queue.persistence_failure", "The queue operation could not be completed.");
    private static ServiceResult<T> ValidationFailure<T>(ValidationResult validation) =>
        ServiceResult<T>.Failure(validation.Errors.Select(error => new ServiceError(error.Code, error.Message, error.Field)).ToArray());
    private static ServiceResult<T> Failure<T>(string code, string message, string? field = null) =>
        ServiceResult<T>.Failure(new ServiceError(code, message, field));
}
