using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Patients;
using ElsheiekhHMS.Application.Patients.Contracts;

namespace ElsheiekhHMS.Application.Workflows.PatientIntake;

public sealed class PatientIntakeAppointmentService(
    IPatientService patientService,
    IAppointmentService appointmentService,
    ICurrentUser currentUser) : IPatientIntakeAppointmentService
{
    public async Task<ServiceResult<PatientIntakeAppointmentResult>> IntakeAndScheduleAsync(
        PatientIntakeAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Failure("workflow.intake.forbidden", "The current user is not authorized to perform patient intake.");
        cancellationToken.ThrowIfCancellationRequested();

        var validation = Validate(request);
        if (validation.Count > 0) return ServiceResult<PatientIntakeAppointmentResult>.Failure(validation.ToArray());

        if (request.ExistingPatientId.HasValue)
        {
            return await ScheduleForExistingPatientAsync(request, cancellationToken);
        }

        return await RegisterThenScheduleAsync(request, cancellationToken);
    }

    private async Task<ServiceResult<PatientIntakeAppointmentResult>> ScheduleForExistingPatientAsync(
        PatientIntakeAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<AppointmentDetailsDto> appointmentResult;
        try
        {
            appointmentResult = await appointmentService.ScheduleAsync(request.Appointment!, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return Failure("workflow.appointment_stage_failure", "The appointment could not be scheduled."); }

        if (!appointmentResult.IsSuccess) return Forward(appointmentResult.Errors);
        if (appointmentResult.Value is null) return Failure("workflow.appointment_stage_failure", "The appointment service returned no appointment.");

        return ServiceResult<PatientIntakeAppointmentResult>.Success(
            Completed(request.ExistingPatientId!.Value, null, appointmentResult.Value));
    }

    private async Task<ServiceResult<PatientIntakeAppointmentResult>> RegisterThenScheduleAsync(
        PatientIntakeAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<PatientDetailsDto> patientResult;
        try
        {
            patientResult = await patientService.RegisterAsync(request.NewPatient!, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return Failure("workflow.patient_stage_failure", "The patient registration could not be completed."); }

        if (!patientResult.IsSuccess) return Forward(patientResult.Errors);
        if (patientResult.Value is null || patientResult.Value.Id <= 0)
        {
            return Failure("workflow.patient_stage_failure", "The patient registration returned no valid patient.");
        }

        var appointmentRequest = request.Appointment! with { PatientId = patientResult.Value.Id };
        ServiceResult<AppointmentDetailsDto> appointmentResult;
        try
        {
            appointmentResult = await appointmentService.ScheduleAsync(appointmentRequest, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            return PartialAppointmentFailure(
                patientResult.Value.Id,
                patientResult.Value,
                [new ServiceError("workflow.appointment_stage_failure", "The appointment could not be scheduled.")]);
        }

        if (!appointmentResult.IsSuccess)
        {
            return PartialAppointmentFailure(patientResult.Value.Id, patientResult.Value, appointmentResult.Errors);
        }

        if (appointmentResult.Value is null)
        {
            return PartialAppointmentFailure(
                patientResult.Value.Id,
                patientResult.Value,
                [new ServiceError("workflow.appointment_stage_failure", "The appointment service returned no appointment.")]);
        }

        return ServiceResult<PatientIntakeAppointmentResult>.Success(
            Completed(patientResult.Value.Id, patientResult.Value, appointmentResult.Value));
    }

    private static List<ServiceError> Validate(PatientIntakeAppointmentRequest request)
    {
        var errors = new List<ServiceError>();
        if (request is null)
        {
            errors.Add(new("workflow.request.required", "An intake request is required."));
            return errors;
        }

        var hasExisting = request.ExistingPatientId.HasValue;
        var hasNew = request.NewPatient is not null;
        if (hasExisting == hasNew)
        {
            errors.Add(new("workflow.patient_mode.conflict", "Select exactly one existing or new patient mode."));
        }

        if (hasExisting && !hasNew && request.ExistingPatientId <= 0)
        {
            errors.Add(new("workflow.patient_id.positive", "Existing patient ID must be greater than zero.", nameof(request.ExistingPatientId)));
        }

        if (request.Appointment is null)
        {
            errors.Add(new("workflow.appointment.required", "An appointment request is required.", nameof(request.Appointment)));
        }
        else if (hasExisting && !hasNew && request.Appointment.PatientId != request.ExistingPatientId)
        {
            errors.Add(new("workflow.appointment.patient_id.match", "Appointment PatientId must match the existing patient.", nameof(request.Appointment)));
        }
        else if (hasNew && !hasExisting && request.Appointment.PatientId != 0)
        {
            errors.Add(new("workflow.appointment.patient_id.zero", "New-patient appointment PatientId must be zero until registration succeeds.", nameof(request.Appointment)));
        }

        return errors;
    }

    private bool HasAccess() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.Roles.Any(role =>
            string.Equals(role, RoleNames.Administrator, StringComparison.Ordinal) ||
            string.Equals(role, RoleNames.Receptionist, StringComparison.Ordinal));

    private static PatientIntakeAppointmentResult Completed(
        int patientId,
        PatientDetailsDto? patient,
        AppointmentDetailsDto appointment) =>
        new(PatientIntakeOutcome.Completed, patientId, patient, appointment, Array.Empty<ServiceError>());

    private static ServiceResult<PatientIntakeAppointmentResult> PartialAppointmentFailure(
        int patientId,
        PatientDetailsDto patient,
        IEnumerable<ServiceError> errors) =>
        ServiceResult<PatientIntakeAppointmentResult>.Success(
            new(
                PatientIntakeOutcome.AppointmentSchedulingFailed,
                patientId,
                patient,
                null,
                errors.ToArray()));

    private static ServiceResult<PatientIntakeAppointmentResult> Forward(
        IReadOnlyList<ServiceError> errors) =>
        ServiceResult<PatientIntakeAppointmentResult>.Failure(errors.ToArray());

    private static ServiceResult<PatientIntakeAppointmentResult> Failure(
        string code,
        string message,
        string? field = null) =>
        ServiceResult<PatientIntakeAppointmentResult>.Failure(new ServiceError(code, message, field));
}
