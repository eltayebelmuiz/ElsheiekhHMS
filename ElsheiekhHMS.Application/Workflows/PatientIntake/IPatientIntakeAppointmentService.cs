using ElsheiekhHMS.Application.Common.Results;

namespace ElsheiekhHMS.Application.Workflows.PatientIntake;

public interface IPatientIntakeAppointmentService
{
    Task<ServiceResult<PatientIntakeAppointmentResult>> IntakeAndScheduleAsync(
        PatientIntakeAppointmentRequest request,
        CancellationToken cancellationToken = default);
}
