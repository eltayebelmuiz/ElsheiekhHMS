using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Queue;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Workflows.AppointmentArrival;

public sealed class AppointmentArrivalQueueService(
    IAppointmentService appointmentService,
    IQueueService queueService,
    ICurrentUser currentUser) : IAppointmentArrivalQueueService
{
    public async Task<ServiceResult<AppointmentArrivalQueueResult>> CheckInAndQueueAsync(
        AppointmentArrivalQueueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Failure("arrival.forbidden", "The current user is not authorized to process appointment arrivals.");
        cancellationToken.ThrowIfCancellationRequested();

        var validation = Validate(request);
        if (validation is not null) return ServiceResult<AppointmentArrivalQueueResult>.Failure(validation);

        var loaded = await appointmentService.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (!loaded.IsSuccess || loaded.Value is null)
        {
            return ServiceResult<AppointmentArrivalQueueResult>.Failure(loaded.Errors.ToArray());
        }

        var appointment = loaded.Value;
        if (appointment.Status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed)
        {
            var checkIn = await appointmentService.CheckInAsync(
                new AppointmentActionRequest(appointment.Id, request.ExpectedConcurrencyToken),
                cancellationToken);
            if (!checkIn.IsSuccess || checkIn.Value is null)
            {
                return ServiceResult<AppointmentArrivalQueueResult>.Failure(checkIn.Errors.ToArray());
            }

            appointment = checkIn.Value;
        }
        else if (appointment.Status is not AppointmentStatus.CheckedIn)
        {
            return Failure(
                "arrival.appointment.not_eligible",
                "Only scheduled, confirmed, or already checked-in appointments can be handed to the queue.");
        }

        var existing = await queueService.GetByAppointmentIdAsync(appointment.Id, cancellationToken);
        if (existing.IsSuccess && existing.Value is not null)
        {
            return Success(new(
                AppointmentArrivalQueueOutcome.ExistingHandoff,
                appointment,
                existing.Value,
                null));
        }

        if (!IsNotFound(existing))
        {
            return PartialOrFailure(appointment, existing.Errors);
        }

        var queued = await queueService.AddAppointmentAsync(
            new AddAppointmentQueueEntryRequest(
                appointment.PatientId,
                appointment.DepartmentId,
                appointment.Id,
                request.Priority,
                request.Notes),
            cancellationToken);
        if (queued.IsSuccess && queued.Value is not null)
        {
            return Success(new(
                AppointmentArrivalQueueOutcome.CompleteSuccess,
                appointment,
                queued.Value,
                null));
        }

        if (queued.Errors.Any(error => error.Code == "queue.duplicate_appointment"))
        {
            var winner = await queueService.GetByAppointmentIdAsync(appointment.Id, cancellationToken);
            if (winner.IsSuccess && winner.Value is not null)
            {
                return Success(new(
                    AppointmentArrivalQueueOutcome.ExistingHandoff,
                    appointment,
                    winner.Value,
                    null));
            }
        }

        return PartialOrFailure(appointment, queued.Errors);
    }

    private static ServiceResult<AppointmentArrivalQueueResult> PartialOrFailure(
        AppointmentDetailsDto appointment,
        IReadOnlyList<ServiceError> errors)
    {
        var queueError = errors.FirstOrDefault() ??
            new ServiceError("queue.persistence_failure", "The queue handoff could not be completed.");
        return Success(new(
            AppointmentArrivalQueueOutcome.PartialSuccess,
            appointment,
            null,
            queueError));
    }

    private static bool IsNotFound(ServiceResult<QueueEntryDetailsDto> result) =>
        !result.IsSuccess && result.Errors.Any(error => error.Code == "queue.not_found");

    private bool HasAccess() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.Roles.Any(role =>
            string.Equals(role, RoleNames.Administrator, StringComparison.Ordinal) ||
            string.Equals(role, RoleNames.Receptionist, StringComparison.Ordinal));

    private static ServiceError? Validate(AppointmentArrivalQueueRequest request)
    {
        if (request.AppointmentId <= 0)
            return new("arrival.appointment_id.positive", "Appointment ID must be positive.", nameof(request.AppointmentId));
        if (string.IsNullOrWhiteSpace(request.ExpectedConcurrencyToken))
            return new("arrival.concurrency_token.required", "An expected concurrency token is required.", nameof(request.ExpectedConcurrencyToken));
        if (!Enum.IsDefined(request.Priority))
            return new("arrival.priority.invalid", "Queue priority is invalid.", nameof(request.Priority));
        if (request.Notes is not null && request.Notes.Length > 2000)
            return new("arrival.notes.maximum", "Notes cannot exceed 2000 characters.", nameof(request.Notes));
        return null;
    }

    private static ServiceResult<AppointmentArrivalQueueResult> Success(AppointmentArrivalQueueResult result) =>
        ServiceResult<AppointmentArrivalQueueResult>.Success(result);

    private static ServiceResult<AppointmentArrivalQueueResult> Failure(string code, string message, string? field = null) =>
        ServiceResult<AppointmentArrivalQueueResult>.Failure(new ServiceError(code, message, field));
}
