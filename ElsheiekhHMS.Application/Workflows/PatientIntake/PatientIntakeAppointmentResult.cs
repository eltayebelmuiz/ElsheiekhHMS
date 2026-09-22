using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Patients.Contracts;

namespace ElsheiekhHMS.Application.Workflows.PatientIntake;

public enum PatientIntakeOutcome
{
    Completed,
    PatientRegistrationFailed,
    AppointmentSchedulingFailed
}

/// <summary>
/// A completed workflow result. AppointmentSchedulingFailed is an intentional
/// partial-success outcome: Patient is committed, Appointment is null, and
/// AppointmentErrors contain the safe retryable scheduling errors.
/// </summary>
public sealed record PatientIntakeAppointmentResult(
    PatientIntakeOutcome Outcome,
    int PatientId,
    PatientDetailsDto? Patient,
    AppointmentDetailsDto? Appointment,
    IReadOnlyList<ServiceError> AppointmentErrors);
