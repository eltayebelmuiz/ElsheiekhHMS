using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Patients.Contracts;

namespace ElsheiekhHMS.Application.Workflows.PatientIntake;

/// <summary>
/// Selects exactly one intake mode. Existing patients must use their identity;
/// new-patient requests use a zero PatientId placeholder until registration succeeds.
/// </summary>
public sealed record PatientIntakeAppointmentRequest(
    int? ExistingPatientId,
    RegisterPatientRequest? NewPatient,
    CreateAppointmentRequest? Appointment);
