using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Application.Patients.Persistence;
using ElsheiekhHMS.Application.Patients.Validation;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Exceptions;

namespace ElsheiekhHMS.Application.Patients;

public sealed class PatientService(
    IPatientPersistence persistence,
    IAuditEventWriter auditEventWriter,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IPatientService
{
    public async Task<ServiceResult<PatientDetailsDto>> GetByIdAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<PatientDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (patientId <= 0)
        {
            return Failure<PatientDetailsDto>("patient.id.positive", "Patient ID must be greater than zero.", nameof(patientId));
        }

        try
        {
            var patient = await persistence.GetDetailsAsync(patientId, cancellationToken);
            return patient is null
                ? Failure<PatientDetailsDto>("patient.not_found", "Patient was not found.")
                : ServiceResult<PatientDetailsDto>.Success(patient);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<PatientDetailsDto>(); }
    }

    public async Task<ServiceResult<PagedResult<PatientSummaryDto>>> SearchAsync(
        PatientSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<PagedResult<PatientSummaryDto>>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new PatientSearchRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<PagedResult<PatientSummaryDto>>(validation);

        try
        {
            var page = await persistence.SearchAsync(request, cancellationToken);
            return ServiceResult<PagedResult<PatientSummaryDto>>.Success(page);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<PagedResult<PatientSummaryDto>>(); }
    }

    public async Task<ServiceResult<PatientDetailsDto>> RegisterAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<PatientDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new RegisterPatientRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<PatientDetailsDto>(validation);

        try
        {
            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            var patientCode = await persistence.AllocatePatientCodeAsync(utcNow.Year, cancellationToken);
            var patient = new Patient(
                patientCode,
                request.FirstName,
                request.MiddleName,
                request.ThirdName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.BloodGroup,
                request.NationalId,
                request.PassportNumber,
                request.Phone,
                request.Address,
                request.City,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.EmergencyContactRelationship,
                request.InsuranceProvider,
                DateOnly.FromDateTime(utcNow.UtcDateTime),
                utcNow,
                currentUser.UserId);

            persistence.Add(patient);
            await auditEventWriter.RecordAsync(
                new AuditEventRequest(
                    AuditCategories.Business,
                    AuditActions.PatientRegistered,
                    "Patient",
                    patient.PatientCode),
                cancellationToken);

            return MapSaveStatus(
                await persistence.SaveChangesAsync(cancellationToken),
                patient);
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<PatientDetailsDto>("patient.domain_validation", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<PatientDetailsDto>(); }
    }

    public Task<ServiceResult<PatientDetailsDto>> UpdateDemographicsAsync(
        int patientId,
        UpdatePatientDemographicsRequest request,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(
            patientId,
            request,
            new UpdatePatientDemographicsRequestValidator().Validate,
            (patient, utcNow) => patient.UpdateDemographics(
                request.FirstName,
                request.MiddleName,
                request.ThirdName,
                request.LastName,
                request.DateOfBirth,
                request.Gender,
                request.BloodGroup,
                DateOnly.FromDateTime(utcNow.UtcDateTime),
                utcNow,
                currentUser.UserId),
            AuditActions.PatientDemographicsUpdated,
            request.ExpectedConcurrencyToken,
            cancellationToken);

    public Task<ServiceResult<PatientDetailsDto>> UpdateContactDetailsAsync(
        int patientId,
        UpdatePatientContactDetailsRequest request,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(
            patientId,
            request,
            new UpdatePatientContactDetailsRequestValidator().Validate,
            (patient, utcNow) => patient.UpdateContactDetails(
                request.Phone,
                request.Address,
                request.City,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.EmergencyContactRelationship,
                request.InsuranceProvider,
                utcNow,
                currentUser.UserId),
            AuditActions.PatientContactUpdated,
            request.ExpectedConcurrencyToken,
            cancellationToken);

    public async Task<ServiceResult<PatientDetailsDto>> UpdateIdentifiersAsync(
        int patientId,
        UpdatePatientIdentifiersRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<PatientDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new UpdatePatientIdentifiersRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<PatientDetailsDto>(validation);
        if (!HasExpectedToken(request.ExpectedConcurrencyToken))
        {
            return Failure<PatientDetailsDto>("patient.concurrency_token.required", "An expected concurrency token is required.", nameof(request.ExpectedConcurrencyToken));
        }

        try
        {
            if (patientId <= 0)
            {
                return Failure<PatientDetailsDto>("patient.id.positive", "Patient ID must be greater than zero.", nameof(patientId));
            }

            var patient = await persistence.LoadTrackedAsync(patientId, cancellationToken);
            if (patient is null) return Failure<PatientDetailsDto>("patient.not_found", "Patient was not found.");
            if (!MatchesConcurrencyToken(patient, request.ExpectedConcurrencyToken!))
            {
                return Failure<PatientDetailsDto>("patient.concurrency_conflict", "The patient was changed by another operation.");
            }

            var conflicts = new List<ServiceError>();
            if (request.NationalId is not null && await persistence.ExistsByNationalIdAsync(request.NationalId, patientId, cancellationToken))
            {
                conflicts.Add(new("patient.national_id.conflict", "The national ID is already assigned."));
            }
            if (request.PassportNumber is not null && await persistence.ExistsByPassportNumberAsync(request.PassportNumber, patientId, cancellationToken))
            {
                conflicts.Add(new("patient.passport_number.conflict", "The passport number is already assigned."));
            }
            if (conflicts.Count > 0) return ServiceResult<PatientDetailsDto>.Failure(conflicts.ToArray());

            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            patient.UpdateIdentifiers(request.NationalId, request.PassportNumber, utcNow, currentUser.UserId);
            await RecordAuditAsync(AuditActions.PatientIdentifiersUpdated, patient.Id, cancellationToken);
            return MapSaveStatus(await persistence.SaveChangesAsync(cancellationToken), patient);
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<PatientDetailsDto>("patient.domain_validation", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<PatientDetailsDto>(); }
    }

    private async Task<ServiceResult<PatientDetailsDto>> UpdateAsync<TRequest>(
        int patientId,
        TRequest request,
        Func<TRequest, ValidationResult> validate,
        Action<Patient, DateTimeOffset> update,
        string auditAction,
        string? expectedConcurrencyToken,
        CancellationToken cancellationToken)
    {
        if (!HasAccess()) return Forbidden<PatientDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = validate(request);
        if (!validation.IsValid) return ValidationFailure<PatientDetailsDto>(validation);
        if (!HasExpectedToken(expectedConcurrencyToken))
        {
            return Failure<PatientDetailsDto>("patient.concurrency_token.required", "An expected concurrency token is required.", "ExpectedConcurrencyToken");
        }

        try
        {
            if (patientId <= 0)
            {
                return Failure<PatientDetailsDto>("patient.id.positive", "Patient ID must be greater than zero.", nameof(patientId));
            }

            var patient = await persistence.LoadTrackedAsync(patientId, cancellationToken);
            if (patient is null) return Failure<PatientDetailsDto>("patient.not_found", "Patient was not found.");
            if (!MatchesConcurrencyToken(patient, expectedConcurrencyToken!))
            {
                return Failure<PatientDetailsDto>("patient.concurrency_conflict", "The patient was changed by another operation.");
            }

            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            update(patient, utcNow);
            await RecordAuditAsync(auditAction, patient.Id, cancellationToken);
            return MapSaveStatus(await persistence.SaveChangesAsync(cancellationToken), patient);
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<PatientDetailsDto>("patient.domain_validation", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<PatientDetailsDto>(); }
    }

    private Task RecordAuditAsync(string action, int patientId, CancellationToken cancellationToken) =>
        auditEventWriter.RecordAsync(
            new AuditEventRequest(
                AuditCategories.Business,
                action,
                "Patient",
                patientId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            cancellationToken);

    private static ServiceResult<PatientDetailsDto> MapSaveStatus(
        PatientPersistenceSaveStatus status,
        Patient patient) => status switch
        {
            PatientPersistenceSaveStatus.Saved => ServiceResult<PatientDetailsDto>.Success(ToDetails(patient)),
            PatientPersistenceSaveStatus.ConcurrencyConflict => Failure<PatientDetailsDto>("patient.concurrency_conflict", "The patient was changed by another operation."),
            PatientPersistenceSaveStatus.NationalIdConflict => Failure<PatientDetailsDto>("patient.national_id.conflict", "The national ID is already assigned."),
            PatientPersistenceSaveStatus.PassportNumberConflict => Failure<PatientDetailsDto>("patient.passport_number.conflict", "The passport number is already assigned."),
            _ => PersistenceFailure<PatientDetailsDto>()
        };

    private bool HasAccess() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.Roles.Any(role =>
            string.Equals(role, RoleNames.Administrator, StringComparison.Ordinal) ||
            string.Equals(role, RoleNames.Receptionist, StringComparison.Ordinal));

    private static bool HasExpectedToken(string? token) => !string.IsNullOrWhiteSpace(token);

    private static bool MatchesConcurrencyToken(Patient patient, string token)
    {
        try { return Convert.FromBase64String(token).AsSpan().SequenceEqual(patient.RowVersion); }
        catch (FormatException) { return false; }
    }

    private static PatientDetailsDto ToDetails(Patient patient) => new(
        patient.Id,
        patient.PatientCode,
        patient.FullName,
        patient.DateOfBirth,
        patient.Gender,
        patient.Phone,
        patient.MiddleName,
        patient.ThirdName,
        patient.BloodGroup,
        patient.NationalId,
        patient.PassportNumber,
        patient.Address,
        patient.City,
        patient.EmergencyContactName,
        patient.EmergencyContactPhone,
        patient.EmergencyContactRelationship,
        patient.InsuranceProvider,
        patient.RowVersion.Length == 0 ? null : Convert.ToBase64String(patient.RowVersion));

    private static ServiceResult<T> Forbidden<T>() => Failure<T>("patient.forbidden", "The current user is not authorized to manage patients.");
    private static ServiceResult<T> PersistenceFailure<T>() => Failure<T>("patient.persistence_failure", "The patient operation could not be completed.");

    private static ServiceResult<T> ValidationFailure<T>(ValidationResult validation) =>
        ServiceResult<T>.Failure(validation.Errors.Select(error => new ServiceError(error.Code, error.Message, error.Field)).ToArray());

    private static ServiceResult<T> Failure<T>(string code, string message, string? field = null) =>
        ServiceResult<T>.Failure(new ServiceError(code, message, field));
}
