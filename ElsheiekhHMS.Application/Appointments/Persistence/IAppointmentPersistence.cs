using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;

namespace ElsheiekhHMS.Application.Appointments.Persistence;

public interface IAppointmentPersistence
{
    Task<AppointmentDetailsDto?> GetDetailsAsync(int appointmentId, CancellationToken cancellationToken);
    Task<PagedResult<AppointmentSummaryDto>> SearchAsync(AppointmentSearchRequest request, CancellationToken cancellationToken);
    Task<string> AllocateAppointmentCodeAsync(CancellationToken cancellationToken);
    Task<bool> PatientExistsAsync(int patientId, CancellationToken cancellationToken);
    Task<bool> DepartmentIsActiveAsync(int departmentId, CancellationToken cancellationToken);
    Task<bool> DoctorIsActiveInDepartmentAsync(int doctorId, int departmentId, CancellationToken cancellationToken);
    Task<Appointment?> LoadTrackedAsync(int appointmentId, CancellationToken cancellationToken);
    void Add(Appointment appointment);
    Task<AppointmentPersistenceSaveStatus> SaveChangesAsync(CancellationToken cancellationToken);
}
