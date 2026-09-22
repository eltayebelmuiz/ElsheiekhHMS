using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;

namespace ElsheiekhHMS.Application.Appointments;

public interface IAppointmentService
{
    Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<AppointmentSummaryDto>>> SearchAsync(AppointmentSearchRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AppointmentDetailsDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AppointmentDetailsDto>> ScheduleAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AppointmentDetailsDto>> CancelAsync(CancelAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AppointmentDetailsDto>> MarkNoShowAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AppointmentDetailsDto>> CheckInAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AppointmentDetailsDto>> CompleteAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default);
}
