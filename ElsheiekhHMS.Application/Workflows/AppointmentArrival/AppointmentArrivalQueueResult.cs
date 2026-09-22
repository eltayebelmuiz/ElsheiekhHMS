using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Queue.Contracts;

namespace ElsheiekhHMS.Application.Workflows.AppointmentArrival;

public sealed record AppointmentArrivalQueueResult(
    AppointmentArrivalQueueOutcome Outcome,
    AppointmentDetailsDto Appointment,
    QueueEntryDetailsDto? Queue,
    ServiceError? QueueError);
