using ElsheiekhHMS.Application.Common.Results;

namespace ElsheiekhHMS.Application.Workflows.AppointmentArrival;

public interface IAppointmentArrivalQueueService
{
    Task<ServiceResult<AppointmentArrivalQueueResult>> CheckInAndQueueAsync(
        AppointmentArrivalQueueRequest request,
        CancellationToken cancellationToken = default);
}
