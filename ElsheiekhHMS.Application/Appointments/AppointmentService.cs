using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Appointments.Persistence;
using ElsheiekhHMS.Application.Appointments.Validation;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Exceptions;

namespace ElsheiekhHMS.Application.Appointments;

public sealed class AppointmentService(
    IAppointmentPersistence persistence,
    IAuditEventWriter auditEventWriter,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IAppointmentService
{
    private const string HospitalTimeZoneId = "Africa/Kigali";

    public async Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<AppointmentDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (appointmentId <= 0)
        {
            return Failure<AppointmentDetailsDto>("appointment.id.positive", "Appointment ID must be greater than zero.", nameof(appointmentId));
        }

        try
        {
            var appointment = await persistence.GetDetailsAsync(appointmentId, cancellationToken);
            return appointment is null
                ? Failure<AppointmentDetailsDto>("appointment.not_found", "Appointment was not found.")
                : ServiceResult<AppointmentDetailsDto>.Success(appointment);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<AppointmentDetailsDto>(); }
    }

    public async Task<ServiceResult<PagedResult<AppointmentSummaryDto>>> SearchAsync(
        AppointmentSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<PagedResult<AppointmentSummaryDto>>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new AppointmentSearchRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<PagedResult<AppointmentSummaryDto>>(validation);

        try
        {
            var page = await persistence.SearchAsync(request, cancellationToken);
            return ServiceResult<PagedResult<AppointmentSummaryDto>>.Success(page);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<PagedResult<AppointmentSummaryDto>>(); }
    }

    public Task<ServiceResult<AppointmentDetailsDto>> ScheduleAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default) => CreateAsync(request, cancellationToken);

    public async Task<ServiceResult<AppointmentDetailsDto>> CreateAsync(
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<AppointmentDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new CreateAppointmentRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<AppointmentDetailsDto>(validation);

        var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
        if (!TryGetScheduledUtc(request.ScheduledDate, request.ScheduledTime, utcNow, out _, out var timeError))
        {
            return Failure<AppointmentDetailsDto>(timeError.Code, timeError.Message, timeError.Field);
        }

        try
        {
            if (!await persistence.PatientExistsAsync(request.PatientId, cancellationToken))
            {
                return Failure<AppointmentDetailsDto>("appointment.patient.not_found", "The patient was not found.", nameof(request.PatientId));
            }

            if (!await persistence.DepartmentIsActiveAsync(request.DepartmentId, cancellationToken))
            {
                return Failure<AppointmentDetailsDto>("appointment.department.inactive", "The department is not active.", nameof(request.DepartmentId));
            }

            if (!await persistence.DoctorIsActiveInDepartmentAsync(request.DoctorId, request.DepartmentId, cancellationToken))
            {
                return Failure<AppointmentDetailsDto>("appointment.doctor.unavailable", "The doctor is not active in the selected department.", nameof(request.DoctorId));
            }

            var appointmentCode = await persistence.AllocateAppointmentCodeAsync(cancellationToken);
            var appointment = new Appointment(
                appointmentCode,
                request.PatientId,
                request.DoctorId,
                request.DepartmentId,
                request.ScheduledDate,
                request.ScheduledTime,
                request.Type,
                request.Notes,
                utcNow,
                currentUser.UserId);

            persistence.Add(appointment);
            await RecordAuditAsync(AuditActions.AppointmentScheduled, appointment.AppointmentCode, null, cancellationToken);
            return MapSaveStatus(await persistence.SaveChangesAsync(cancellationToken), appointment);
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<AppointmentDetailsDto>("appointment.domain_rule", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<AppointmentDetailsDto>(); }
    }

    public Task<ServiceResult<AppointmentDetailsDto>> CancelAsync(
        CancelAppointmentRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.AppointmentId,
            request.ExpectedConcurrencyToken,
            new CancelAppointmentRequestValidator().Validate(request),
            (appointment, utcNow) => appointment.Cancel(request.Reason, utcNow, currentUser.UserId),
            AuditActions.AppointmentCancelled,
            request.Reason,
            cancellationToken);

    public Task<ServiceResult<AppointmentDetailsDto>> MarkNoShowAsync(
        AppointmentActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.AppointmentId,
            request.ExpectedConcurrencyToken,
            new AppointmentActionRequestValidator().Validate(request),
            (appointment, utcNow) => appointment.MarkNoShow(utcNow, currentUser.UserId),
            AuditActions.AppointmentNoShow,
            null,
            cancellationToken);

    public Task<ServiceResult<AppointmentDetailsDto>> CheckInAsync(
        AppointmentActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.AppointmentId,
            request.ExpectedConcurrencyToken,
            new AppointmentActionRequestValidator().Validate(request),
            (appointment, utcNow) => appointment.CheckIn(utcNow, currentUser.UserId),
            AuditActions.AppointmentCheckedIn,
            null,
            cancellationToken);

    public Task<ServiceResult<AppointmentDetailsDto>> CompleteAsync(
        AppointmentActionRequest request,
        CancellationToken cancellationToken = default) =>
        MutateAsync(
            request.AppointmentId,
            request.ExpectedConcurrencyToken,
            new AppointmentActionRequestValidator().Validate(request),
            (appointment, utcNow) => appointment.Complete(utcNow, currentUser.UserId),
            AuditActions.AppointmentCompleted,
            null,
            cancellationToken);

    private async Task<ServiceResult<AppointmentDetailsDto>> MutateAsync(
        int appointmentId,
        string? expectedConcurrencyToken,
        ValidationResult validation,
        Action<Appointment, DateTimeOffset> mutation,
        string auditAction,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (!HasAccess()) return Forbidden<AppointmentDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (!validation.IsValid) return ValidationFailure<AppointmentDetailsDto>(validation);
        if (string.IsNullOrWhiteSpace(expectedConcurrencyToken))
        {
            return Failure<AppointmentDetailsDto>(
                "appointment.concurrency_token.required",
                "An expected concurrency token is required.",
                nameof(expectedConcurrencyToken));
        }

        try
        {
            var appointment = await persistence.LoadTrackedAsync(appointmentId, cancellationToken);
            if (appointment is null)
            {
                return Failure<AppointmentDetailsDto>("appointment.not_found", "Appointment was not found.");
            }

            if (!MatchesConcurrencyToken(appointment, expectedConcurrencyToken))
            {
                return Failure<AppointmentDetailsDto>("appointment.concurrency_conflict", "The appointment was changed by another operation.");
            }

            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            mutation(appointment, utcNow);
            await RecordAuditAsync(auditAction, appointment.AppointmentCode, reason, cancellationToken);
            return MapSaveStatus(await persistence.SaveChangesAsync(cancellationToken), appointment);
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<AppointmentDetailsDto>("appointment.domain_rule", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<AppointmentDetailsDto>(); }
    }

    private Task RecordAuditAsync(string action, string appointmentCode, string? reason, CancellationToken cancellationToken) =>
        auditEventWriter.RecordAsync(
            new AuditEventRequest(AuditCategories.Business, action, "Appointment", appointmentCode, reason),
            cancellationToken);

    private static bool TryGetScheduledUtc(
        DateOnly scheduledDate,
        TimeOnly scheduledTime,
        DateTimeOffset utcNow,
        out DateTimeOffset scheduledUtc,
        out ServiceError error)
    {
        scheduledUtc = default;
        error = new ServiceError("appointment.timezone.unavailable", "The hospital booking timezone is unavailable.");
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(HospitalTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }

        var local = DateTime.SpecifyKind(scheduledDate.ToDateTime(scheduledTime), DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local))
        {
            error = new ServiceError("appointment.scheduled_time.invalid", "The scheduled time does not exist in the hospital timezone.", "ScheduledTime");
            return false;
        }

        if (zone.IsAmbiguousTime(local))
        {
            error = new ServiceError("appointment.scheduled_time.ambiguous", "The scheduled time is ambiguous in the hospital timezone.", "ScheduledTime");
            return false;
        }

        scheduledUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
        if (scheduledUtc <= utcNow)
        {
            error = new ServiceError("appointment.scheduled_time.past", "The scheduled time must be in the future.", "ScheduledTime");
            return false;
        }

        return true;
    }

    private bool HasAccess() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.Roles.Any(role =>
            string.Equals(role, RoleNames.Administrator, StringComparison.Ordinal) ||
            string.Equals(role, RoleNames.Receptionist, StringComparison.Ordinal));

    private static bool MatchesConcurrencyToken(Appointment appointment, string token)
    {
        try { return Convert.FromBase64String(token).AsSpan().SequenceEqual(appointment.RowVersion); }
        catch (FormatException) { return false; }
    }

    private static ServiceResult<AppointmentDetailsDto> MapSaveStatus(
        AppointmentPersistenceSaveStatus status,
        Appointment appointment) => status switch
        {
            AppointmentPersistenceSaveStatus.Saved => ServiceResult<AppointmentDetailsDto>.Success(ToDetails(appointment)),
            AppointmentPersistenceSaveStatus.ConcurrencyConflict => Failure<AppointmentDetailsDto>("appointment.concurrency_conflict", "The appointment was changed by another operation."),
            AppointmentPersistenceSaveStatus.Collision => Failure<AppointmentDetailsDto>("appointment.collision", "The selected department and time are already occupied."),
            _ => PersistenceFailure<AppointmentDetailsDto>()
        };

    private static AppointmentDetailsDto ToDetails(Appointment appointment) => new(
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
        appointment.RowVersion.Length == 0 ? null : Convert.ToBase64String(appointment.RowVersion));

    private static ServiceResult<T> Forbidden<T>() => Failure<T>("appointment.forbidden", "The current user is not authorized to manage appointments.");
    private static ServiceResult<T> PersistenceFailure<T>() => Failure<T>("appointment.persistence_failure", "The appointment operation could not be completed.");
    private static ServiceResult<T> ValidationFailure<T>(ValidationResult validation) =>
        ServiceResult<T>.Failure(validation.Errors.Select(error => new ServiceError(error.Code, error.Message, error.Field)).ToArray());
    private static ServiceResult<T> Failure<T>(string code, string message, string? field = null) =>
        ServiceResult<T>.Failure(new ServiceError(code, message, field));
}
