using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Patients;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Application.Workflows.PatientIntake;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Workflows;

public sealed class PatientIntakeAppointmentServiceTests
{
    [Fact]
    public async Task Existing_patient_is_scheduled_without_registering_another_patient()
    {
        var patient = Patient(7);
        var patients = new FakePatientService { Details = patient };
        var appointment = Appointment(11, patient.Id);
        var appointments = new FakeAppointmentService { Details = appointment };

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            ExistingRequest(patient.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(PatientIntakeOutcome.Completed, result.Value!.Outcome);
        Assert.Equal(patient.Id, result.Value.PatientId);
        Assert.Null(result.Value.Patient);
        Assert.Equal(appointment, result.Value.Appointment);
        Assert.Equal(0, patients.GetByIdCalls);
        Assert.Equal(0, patients.RegisterCalls);
        Assert.Equal(1, appointments.CreateCalls);
        Assert.Equal(patient.Id, appointments.LastRequest!.PatientId);
    }

    [Fact]
    public async Task New_patient_is_registered_then_scheduled()
    {
        var patient = Patient(7);
        var patients = new FakePatientService { Registered = patient };
        var appointments = new FakeAppointmentService { Details = Appointment(11, patient.Id) };

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            NewRequest(patient.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(PatientIntakeOutcome.Completed, result.Value!.Outcome);
        Assert.Equal(1, patients.RegisterCalls);
        Assert.Equal(1, appointments.CreateCalls);
        Assert.Equal(patient.Id, appointments.LastRequest!.PatientId);
    }

    [Fact]
    public async Task Patient_registration_failure_stops_before_appointment()
    {
        var patients = new FakePatientService
        {
            RegisterResult = Failure<PatientDetailsDto>("patient.validation")
        };
        var appointments = new FakeAppointmentService();

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            NewRequest(7));

        Assert.False(result.IsSuccess);
        Assert.Equal("patient.validation", Assert.Single(result.Errors).Code);
        Assert.Equal(1, patients.RegisterCalls);
        Assert.Equal(0, appointments.CreateCalls);
    }

    [Fact]
    public async Task New_patient_is_retained_when_appointment_scheduling_fails()
    {
        var patient = Patient(7);
        var patients = new FakePatientService { Registered = patient };
        var appointments = new FakeAppointmentService
        {
            CreateResult = Failure<AppointmentDetailsDto>("appointment.collision")
        };

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            NewRequest(patient.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(PatientIntakeOutcome.AppointmentSchedulingFailed, result.Value!.Outcome);
        Assert.Equal(patient, result.Value.Patient);
        Assert.Null(result.Value.Appointment);
        Assert.Equal("appointment.collision", Assert.Single(result.Value.AppointmentErrors).Code);
    }

    [Fact]
    public async Task Existing_patient_appointment_failure_does_not_register_or_mutate_patient()
    {
        var patient = Patient(7);
        var patients = new FakePatientService { Details = patient };
        var appointments = new FakeAppointmentService
        {
            CreateResult = Failure<AppointmentDetailsDto>("appointment.department.inactive")
        };

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            ExistingRequest(patient.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("appointment.department.inactive", Assert.Single(result.Errors).Code);
        Assert.Equal(0, patients.RegisterCalls);
        Assert.Equal(1, appointments.CreateCalls);
    }

    [Fact]
    public async Task Retry_uses_existing_patient_without_duplicate_registration()
    {
        var patient = Patient(7);
        var patients = new FakePatientService { Details = patient };
        var appointments = new FakeAppointmentService
        {
            CreateResult = Failure<AppointmentDetailsDto>("appointment.collision")
        };
        var service = CreateService(patients, appointments);

        var first = await service.IntakeAndScheduleAsync(ExistingRequest(patient.Id));
        appointments.CreateResult = ServiceResult<AppointmentDetailsDto>.Success(Appointment(12, patient.Id));
        var retry = await service.IntakeAndScheduleAsync(ExistingRequest(patient.Id));

        Assert.False(first.IsSuccess);
        Assert.True(retry.IsSuccess);
        Assert.Equal(0, patients.RegisterCalls);
        Assert.Equal(2, appointments.CreateCalls);
    }

    [Fact]
    public async Task Conflicting_patient_modes_are_rejected_before_writes()
    {
        var patients = new FakePatientService();
        var appointments = new FakeAppointmentService();
        var request = new PatientIntakeAppointmentRequest(
            7,
            ValidRegistration(),
            AppointmentRequest(7));

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("workflow.patient_mode.conflict", Assert.Single(result.Errors).Code);
        Assert.Equal(0, patients.GetByIdCalls);
        Assert.Equal(0, patients.RegisterCalls);
        Assert.Equal(0, appointments.CreateCalls);
    }

    [Fact]
    public async Task New_patient_mode_requires_zero_placeholder_patient_id()
    {
        var patients = new FakePatientService();
        var appointments = new FakeAppointmentService();

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            new PatientIntakeAppointmentRequest(null, ValidRegistration(), AppointmentRequest(7)));

        Assert.False(result.IsSuccess);
        Assert.Equal("workflow.appointment.patient_id.zero", Assert.Single(result.Errors).Code);
        Assert.Equal(0, patients.RegisterCalls);
    }

    [Fact]
    public async Task Missing_existing_patient_is_propagated_by_appointment_service()
    {
        var patients = new FakePatientService
        {
            DetailsResult = Failure<PatientDetailsDto>("patient.not_found")
        };
        var appointments = new FakeAppointmentService
        {
            CreateResult = Failure<AppointmentDetailsDto>("appointment.patient.not_found")
        };

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            ExistingRequest(7));

        Assert.False(result.IsSuccess);
        Assert.Equal("appointment.patient.not_found", Assert.Single(result.Errors).Code);
        Assert.Equal(0, patients.GetByIdCalls);
        Assert.Equal(1, appointments.CreateCalls);
    }

    [Fact]
    public async Task Appointment_errors_are_propagated_without_reimplementation()
    {
        var patient = Patient(7);
        var patients = new FakePatientService { Details = patient };
        var appointments = new FakeAppointmentService
        {
            CreateResult = Failure<AppointmentDetailsDto>("appointment.scheduled_time.past")
        };

        var result = await CreateService(patients, appointments).IntakeAndScheduleAsync(
            ExistingRequest(patient.Id));

        Assert.Equal("appointment.scheduled_time.past", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Unauthorized_and_SystemAdministrator_users_are_rejected()
    {
        var request = ExistingRequest(7);
        var patients = new FakePatientService { Details = Patient(7) };
        var appointments = new FakeAppointmentService();

        var provider = await CreateService(patients, appointments, [RoleNames.Provider])
            .IntakeAndScheduleAsync(request);
        var systemAdministrator = await CreateService(patients, appointments, [RoleNames.SystemAdministrator])
            .IntakeAndScheduleAsync(request);

        Assert.Equal("workflow.intake.forbidden", Assert.Single(provider.Errors).Code);
        Assert.Equal("workflow.intake.forbidden", Assert.Single(systemAdministrator.Errors).Code);
        Assert.Equal(0, patients.GetByIdCalls);
        Assert.Equal(0, appointments.CreateCalls);
    }

    [Theory]
    [InlineData(RoleNames.Patient, "stable-user")]
    [InlineData("", null)]
    public async Task Patient_and_anonymous_users_are_rejected_without_service_calls(
        string role,
        string? userId)
    {
        var patients = new FakePatientService { Details = Patient(7) };
        var appointments = new FakeAppointmentService();

        var result = await CreateService(patients, appointments, [role], userId)
            .IntakeAndScheduleAsync(ExistingRequest(7));

        Assert.Equal("workflow.intake.forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, patients.GetByIdCalls);
        Assert.Equal(0, patients.RegisterCalls);
        Assert.Equal(0, appointments.CreateCalls);
    }

    [Fact]
    public async Task Cancellation_is_propagated_to_the_first_service()
    {
        var patients = new FakePatientService { Details = Patient(7) };
        var appointments = new FakeAppointmentService();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(patients, appointments).IntakeAndScheduleAsync(
                ExistingRequest(7), cancellation.Token));

        Assert.Equal(0, patients.GetByIdCalls);
        Assert.Equal(0, appointments.CreateCalls);
    }

    [Fact]
    public async Task Cancellation_from_appointment_service_is_rethrown()
    {
        var patient = Patient(7);
        var patients = new FakePatientService { Details = patient };
        var appointments = new FakeAppointmentService { ThrowCancellation = true };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(patients, appointments).IntakeAndScheduleAsync(
                ExistingRequest(7)));

        Assert.Equal(1, appointments.CreateCalls);
    }

    private static PatientIntakeAppointmentService CreateService(
        FakePatientService patients,
        FakeAppointmentService appointments,
        IReadOnlyCollection<string>? roles = null,
        string? userId = "stable-user") =>
        new(patients, appointments, new TestCurrentUser(userId, roles ?? [RoleNames.Receptionist]));

    private static PatientIntakeAppointmentRequest ExistingRequest(int patientId) =>
        new(patientId, null, AppointmentRequest(patientId));

    private static PatientIntakeAppointmentRequest NewRequest(int expectedPatientId) =>
        new(null, ValidRegistration(), AppointmentRequest(0));

    private static CreateAppointmentRequest AppointmentRequest(int patientId) =>
        new(patientId, 20, 30, new DateOnly(2026, 10, 1), new TimeOnly(9, 0), AppointmentType.General, "Routine");

    private static RegisterPatientRequest ValidRegistration() =>
        new("Amina", "M", null, "Hassan", new DateOnly(1990, 1, 1), Gender.Female,
            BloodGroup.OPositive, null, null, "0900000000", "Main street", "City", null, null, null, null);

    private static PatientDetailsDto Patient(int id) => new(
        id, "PT-2026-00001", "Amina M Hassan", new DateOnly(1990, 1, 1), Gender.Female,
        "0900000000", "M", null, BloodGroup.OPositive, null, null, "Main street", "City", null, null, null, null, "AQID");

    private static AppointmentDetailsDto Appointment(int id, int patientId) => new(
        id, "AP-2026-00001", patientId, 20, 30, new DateOnly(2026, 10, 1), new TimeOnly(9, 0),
        AppointmentType.General, AppointmentStatus.Scheduled, "Routine", null, null, "AQID");

    private static ServiceResult<T> Failure<T>(string code) =>
        ServiceResult<T>.Failure(new ServiceError(code, code));

    private sealed class TestCurrentUser(string? userId, IReadOnlyCollection<string> roles) : ICurrentUser
    {
        public bool IsAuthenticated => userId is not null;
        public string? UserId => userId;
        public string? UserName => "operator";
        public IReadOnlyCollection<string> Roles => roles;
    }

    private sealed class FakePatientService : IPatientService
    {
        public PatientDetailsDto? Details { get; init; }
        public PatientDetailsDto? Registered { get; init; }
        public ServiceResult<PatientDetailsDto>? DetailsResult { get; init; }
        public ServiceResult<PatientDetailsDto>? RegisterResult { get; init; }
        public int GetByIdCalls { get; private set; }
        public int RegisterCalls { get; private set; }

        public Task<ServiceResult<PatientDetailsDto>> GetByIdAsync(int patientId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetByIdCalls++;
            return Task.FromResult(DetailsResult ?? (Details is null
                ? Failure<PatientDetailsDto>("patient.not_found")
                : ServiceResult<PatientDetailsDto>.Success(Details)));
        }

        public Task<ServiceResult<PatientDetailsDto>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RegisterCalls++;
            return Task.FromResult(RegisterResult ?? (Registered is null
                ? Failure<PatientDetailsDto>("patient.registration_failed")
                : ServiceResult<PatientDetailsDto>.Success(Registered)));
        }

        public Task<ServiceResult<PagedResult<PatientSummaryDto>>> SearchAsync(PatientSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult<PagedResult<PatientSummaryDto>>.Success(new PagedResult<PatientSummaryDto>([], 0, 1, 20)));

        public Task<ServiceResult<PatientDetailsDto>> UpdateDemographicsAsync(int patientId, UpdatePatientDemographicsRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<PatientDetailsDto>("unused"));

        public Task<ServiceResult<PatientDetailsDto>> UpdateContactDetailsAsync(int patientId, UpdatePatientContactDetailsRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<PatientDetailsDto>("unused"));

        public Task<ServiceResult<PatientDetailsDto>> UpdateIdentifiersAsync(int patientId, UpdatePatientIdentifiersRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<PatientDetailsDto>("unused"));
    }

    private sealed class FakeAppointmentService : IAppointmentService
    {
        public AppointmentDetailsDto? Details { get; init; }
        public ServiceResult<AppointmentDetailsDto>? CreateResult { get; set; }
        public bool ThrowCancellation { get; init; }
        public int CreateCalls { get; private set; }
        public CreateAppointmentRequest? LastRequest { get; private set; }

        public Task<ServiceResult<AppointmentDetailsDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CreateCalls++;
            LastRequest = request;
            if (ThrowCancellation) throw new OperationCanceledException(cancellationToken);
            return Task.FromResult(CreateResult ?? (Details is null
                ? Failure<AppointmentDetailsDto>("appointment.failed")
                : ServiceResult<AppointmentDetailsDto>.Success(Details)));
        }

        public Task<ServiceResult<AppointmentDetailsDto>> ScheduleAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default) =>
            CreateAsync(request, cancellationToken);

        public Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<AppointmentDetailsDto>("unused"));

        public Task<ServiceResult<PagedResult<AppointmentSummaryDto>>> SearchAsync(AppointmentSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceResult<PagedResult<AppointmentSummaryDto>>.Success(new PagedResult<AppointmentSummaryDto>([], 0, 1, 20)));

        public Task<ServiceResult<AppointmentDetailsDto>> CancelAsync(CancelAppointmentRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<AppointmentDetailsDto>("unused"));

        public Task<ServiceResult<AppointmentDetailsDto>> MarkNoShowAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<AppointmentDetailsDto>("unused"));

        public Task<ServiceResult<AppointmentDetailsDto>> CheckInAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<AppointmentDetailsDto>("unused"));

        public Task<ServiceResult<AppointmentDetailsDto>> CompleteAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(Failure<AppointmentDetailsDto>("unused"));
    }
}
