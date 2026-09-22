using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Patients;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Application.Patients.Persistence;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Patients;

public sealed class PatientServiceTests
{
    [Fact]
    public async Task Register_allocates_code_stages_business_audit_and_saves_once()
    {
        var persistence = new FakePatientPersistence { AllocatedCode = "PT-2026-00001" };
        var audit = new RecordingAuditWriter();
        var service = CreateService(persistence, audit);

        var result = await service.RegisterAsync(ValidRegistration());

        Assert.True(result.IsSuccess);
        Assert.Equal("PT-2026-00001", result.Value!.PatientCode);
        Assert.Equal(1, persistence.SaveChangesCalls);
        Assert.NotNull(persistence.AddedPatient);
        var eventRequest = Assert.Single(audit.Events);
        Assert.Equal(AuditActions.PatientRegistered, eventRequest.Action);
        Assert.Equal(AuditCategories.Business, eventRequest.Category);
        Assert.Equal("Patient", eventRequest.TargetType);
        Assert.Equal("PT-2026-00001", eventRequest.TargetId);
    }

    [Fact]
    public async Task Register_allows_duplicate_phone_without_phone_conflict()
    {
        var persistence = new FakePatientPersistence { AllocatedCode = "PT-2026-00002" };
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.RegisterAsync(ValidRegistration(phone: "0900000000"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Unauthorized_caller_is_rejected_before_persistence()
    {
        var persistence = new FakePatientPersistence();
        var service = CreateService(persistence, new RecordingAuditWriter(), roles: [RoleNames.Provider]);

        var result = await service.RegisterAsync(ValidRegistration());

        Assert.False(result.IsSuccess);
        Assert.Equal("patient.forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Invalid_request_returns_validator_errors()
    {
        var persistence = new FakePatientPersistence();
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.RegisterAsync(ValidRegistration(firstName: ""));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "patient.FirstName.required");
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_requires_the_expected_concurrency_token()
    {
        var persistence = new FakePatientPersistence
        {
            TrackedPatient = CreatePatient()
        };
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.UpdateContactDetailsAsync(
            7,
            new UpdatePatientContactDetailsRequest(
                "0900000000", "Updated address", null, null, null, null, null, null));

        Assert.False(result.IsSuccess);
        Assert.Equal("patient.concurrency_token.required", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_maps_stale_token_to_concurrency_conflict()
    {
        var patient = CreatePatient();
        patient.RowVersion = [1, 2, 3];
        var persistence = new FakePatientPersistence { TrackedPatient = patient };
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.UpdateContactDetailsAsync(
            7,
            new UpdatePatientContactDetailsRequest(
                "0900000000", "Updated address", null, null, null, null, null, "AQI="));

        Assert.False(result.IsSuccess);
        Assert.Equal("patient.concurrency_conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_contact_saves_once_and_stages_audit()
    {
        var patient = CreatePatient();
        patient.RowVersion = [1, 2, 3];
        var persistence = new FakePatientPersistence { TrackedPatient = patient };
        var audit = new RecordingAuditWriter();
        var service = CreateService(persistence, audit);

        var result = await service.UpdateContactDetailsAsync(
            7,
            new UpdatePatientContactDetailsRequest(
                "0900000001", "Updated address", "City", null, null, null, null, "AQID"));

        Assert.True(result.IsSuccess);
        Assert.Equal("0900000001", result.Value!.Phone);
        Assert.Equal(1, persistence.SaveChangesCalls);
        Assert.Equal(AuditActions.PatientContactUpdated, Assert.Single(audit.Events).Action);
    }

    [Fact]
    public async Task Identifier_update_maps_existing_national_id_to_conflict()
    {
        var patient = CreatePatient();
        patient.RowVersion = [1, 2, 3];
        var persistence = new FakePatientPersistence { TrackedPatient = patient, NationalIdExists = true };
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.UpdateIdentifiersAsync(
            7,
            new UpdatePatientIdentifiersRequest("N-1", null, "AQID"));

        Assert.False(result.IsSuccess);
        Assert.Equal("patient.national_id.conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Search_cancellation_is_enforced_before_persistence()
    {
        var persistence = new FakePatientPersistence();
        var service = CreateService(persistence, new RecordingAuditWriter());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.SearchAsync(new PatientSearchRequest(), cancellation.Token));
        Assert.Equal(0, persistence.SearchCalls);
    }

    [Fact]
    public async Task Save_failure_is_translated_without_exposing_infrastructure_details()
    {
        var persistence = new FakePatientPersistence
        {
            AllocatedCode = "PT-2026-00003",
            SaveException = new InvalidOperationException("database details")
        };
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.RegisterAsync(ValidRegistration());

        Assert.False(result.IsSuccess);
        Assert.Equal("patient.persistence_failure", Assert.Single(result.Errors).Code);
        Assert.DoesNotContain("database details", result.Errors[0].Message, StringComparison.Ordinal);
    }

    private static PatientService CreateService(
        FakePatientPersistence persistence,
        RecordingAuditWriter audit,
        IReadOnlyCollection<string>? roles = null) =>
        new(
            persistence,
            audit,
            new TestCurrentUser("stable-user", roles ?? [RoleNames.Administrator]),
            new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero)));

    private static RegisterPatientRequest ValidRegistration(
        string firstName = "Amina",
        string phone = "0900000000") =>
        new(
            firstName, "M", null, "Hassan", new DateOnly(1990, 1, 1), Gender.Female,
            BloodGroup.OPositive, null, null, phone, "Main street", "City", null, null, null, null);

    private static Patient CreatePatient() =>
        new(
            patientCode: "PT-2026-00007",
            firstName: "Amina",
            middleName: null,
            thirdName: null,
            lastName: "Hassan",
            dateOfBirth: new DateOnly(1990, 1, 1),
            gender: Gender.Female,
            bloodGroup: BloodGroup.OPositive,
            nationalId: null,
            passportNumber: null,
            phone: "0900000000",
            address: "Main street",
            city: null,
            emergencyContactName: null,
            emergencyContactPhone: null,
            emergencyContactRelationship: null,
            insuranceProvider: null,
            asOfDate: new DateOnly(2026, 9, 22),
            createdAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            createdBy: "stable-user");

    private sealed class TestCurrentUser(
        string userId,
        IReadOnlyCollection<string> roles) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? UserId { get; } = userId;
        public string? UserName => "operator";
        public IReadOnlyCollection<string> Roles { get; } = roles;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEventRequest> Events { get; } = [];

        public Task RecordAsync(AuditEventRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePatientPersistence : IPatientPersistence
    {
        public string AllocatedCode { get; init; } = "PT-2026-00001";
        public Patient? AddedPatient { get; private set; }
        public Patient? TrackedPatient { get; init; }
        public int SaveChangesCalls { get; private set; }
        public int SearchCalls { get; private set; }
        public bool NationalIdExists { get; init; }
        public Exception? SaveException { get; init; }

        public Task<string> AllocatePatientCodeAsync(int year, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(AllocatedCode);
        }

        public Task<PatientDetailsDto?> GetDetailsAsync(int patientId, CancellationToken cancellationToken) =>
            Task.FromResult<PatientDetailsDto?>(null);

        public Task<PagedResult<PatientSummaryDto>> SearchAsync(PatientSearchRequest request, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(new PagedResult<PatientSummaryDto>([], 0, request.Page.PageNumber, request.Page.PageSize));
        }

        public Task<Patient?> LoadTrackedAsync(int patientId, CancellationToken cancellationToken) =>
            Task.FromResult(TrackedPatient);

        public Task<bool> ExistsByNationalIdAsync(string value, int? excludingPatientId, CancellationToken cancellationToken) =>
            Task.FromResult(NationalIdExists);

        public Task<bool> ExistsByPassportNumberAsync(string value, int? excludingPatientId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public void Add(Patient patient) => AddedPatient = patient;

        public Task<PatientPersistenceSaveStatus> SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveChangesCalls++;
            if (SaveException is not null) throw SaveException;
            return Task.FromResult(PatientPersistenceSaveStatus.Saved);
        }
    }
}
