using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Appointments.Persistence;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Appointments;

public sealed class AppointmentServiceTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_allocates_code_stages_audit_and_saves_once()
    {
        var persistence = new FakePersistence { AllocatedCode = "AP-2026-00001" };
        var audit = new RecordingAuditWriter();

        var result = await CreateService(persistence, audit).CreateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("AP-2026-00001", result.Value!.AppointmentCode);
        Assert.Equal(1, persistence.SaveChangesCalls);
        Assert.Equal(AuditActions.AppointmentScheduled, Assert.Single(audit.Events).Action);
        Assert.Equal("AP-2026-00001", audit.Events[0].TargetId);
    }

    [Fact]
    public async Task Create_rejects_a_booking_before_the_current_Kigali_instant()
    {
        var persistence = new FakePersistence();

        var result = await CreateService(persistence, new RecordingAuditWriter()).CreateAsync(
            ValidRequest(scheduledDate: new DateOnly(2026, 9, 22), scheduledTime: new TimeOnly(11, 59)));

        Assert.False(result.IsSuccess);
        Assert.Equal("appointment.scheduled_time.past", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Create_maps_a_concurrent_slot_collision()
    {
        var persistence = new FakePersistence { SaveStatus = AppointmentPersistenceSaveStatus.Collision };

        var result = await CreateService(persistence, new RecordingAuditWriter()).CreateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("appointment.collision", Assert.Single(result.Errors).Code);
        Assert.Equal(1, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Create_rejects_inactive_department_and_missing_patient()
    {
        var persistence = new FakePersistence { DepartmentActive = false };
        var result = await CreateService(persistence, new RecordingAuditWriter()).CreateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("appointment.department.inactive", Assert.Single(result.Errors).Code);

        persistence.DepartmentActive = true;
        persistence.PatientExists = false;
        result = await CreateService(persistence, new RecordingAuditWriter()).CreateAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("appointment.patient.not_found", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Cancel_requires_token_calls_core_and_audits_once()
    {
        var appointment = NewAppointment();
        appointment.RowVersion = [1, 2, 3];
        var persistence = new FakePersistence { TrackedAppointment = appointment };
        var audit = new RecordingAuditWriter();

        var result = await CreateService(persistence, audit).CancelAsync(
            new CancelAppointmentRequest(7, "Patient request", "AQID"));

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(1, persistence.SaveChangesCalls);
        Assert.Equal(AuditActions.AppointmentCancelled, Assert.Single(audit.Events).Action);
    }

    [Fact]
    public async Task Lifecycle_actions_use_core_transitions_and_audit_actions()
    {
        var appointment = NewAppointment();
        appointment.RowVersion = [1, 2, 3];
        var persistence = new FakePersistence { TrackedAppointment = appointment };
        var audit = new RecordingAuditWriter();
        var service = CreateService(persistence, audit);

        Assert.True((await service.CheckInAsync(new AppointmentActionRequest(7, "AQID"))).IsSuccess);
        Assert.True((await service.CompleteAsync(new AppointmentActionRequest(7, "AQID"))).IsSuccess);

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal([AuditActions.AppointmentCheckedIn, AuditActions.AppointmentCompleted], audit.Events.Select(e => e.Action));
        Assert.Equal(2, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task No_show_uses_the_core_transition_and_audit_action()
    {
        var appointment = NewAppointment();
        appointment.RowVersion = [1, 2, 3];
        var persistence = new FakePersistence { TrackedAppointment = appointment };
        var audit = new RecordingAuditWriter();

        var result = await CreateService(persistence, audit).MarkNoShowAsync(new AppointmentActionRequest(7, "AQID"));

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
        Assert.Equal(AuditActions.AppointmentNoShow, Assert.Single(audit.Events).Action);
    }

    [Fact]
    public async Task Invalid_lifecycle_transition_is_returned_without_saving()
    {
        var appointment = NewAppointment();
        appointment.RowVersion = [1, 2, 3];
        var persistence = new FakePersistence { TrackedAppointment = appointment };

        var result = await CreateService(persistence, new RecordingAuditWriter()).CompleteAsync(new AppointmentActionRequest(7, "AQID"));

        Assert.False(result.IsSuccess);
        Assert.Equal("appointment.domain_rule", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Provider_and_missing_user_are_rejected_before_persistence()
    {
        var persistence = new FakePersistence();
        var providerResult = await CreateService(persistence, new RecordingAuditWriter(), [RoleNames.Provider]).CreateAsync(ValidRequest());
        var anonymousResult = await new AppointmentService(
            persistence,
            new RecordingAuditWriter(),
            new TestCurrentUser(null, []),
            new FixedTimeProvider(NowUtc)).CreateAsync(ValidRequest());

        Assert.Equal("appointment.forbidden", Assert.Single(providerResult.Errors).Code);
        Assert.Equal("appointment.forbidden", Assert.Single(anonymousResult.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Search_cancellation_is_propagated_before_persistence()
    {
        var persistence = new FakePersistence();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(persistence, new RecordingAuditWriter()).SearchAsync(
                new AppointmentSearchRequest(new DateOnly(2026, 9, 22), new DateOnly(2026, 9, 22)), cancellation.Token));
        Assert.Equal(0, persistence.SearchCalls);
    }

    private static AppointmentService CreateService(
        FakePersistence persistence,
        RecordingAuditWriter audit,
        IReadOnlyCollection<string>? roles = null) => new(
            persistence,
            audit,
            new TestCurrentUser("stable-user", roles ?? [RoleNames.Receptionist]),
            new FixedTimeProvider(NowUtc));

    private static CreateAppointmentRequest ValidRequest(
        DateOnly? scheduledDate = null,
        TimeOnly? scheduledTime = null) => new(
        10,
        20,
        30,
        scheduledDate ?? new DateOnly(2026, 9, 23),
        scheduledTime ?? new TimeOnly(9, 0),
        AppointmentType.General,
        "Routine visit");

    private static Appointment NewAppointment() => new(
        "AP-2026-00001", 10, 20, 30,
        new DateOnly(2026, 9, 23), new TimeOnly(9, 0), AppointmentType.General,
        null, NowUtc, "stable-user");

    private sealed class TestCurrentUser(string? userId, IReadOnlyCollection<string> roles) : ICurrentUser
    {
        public bool IsAuthenticated => userId is not null;
        public string? UserId => userId;
        public string? UserName => "operator";
        public IReadOnlyCollection<string> Roles => roles;
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

    private sealed class FakePersistence : IAppointmentPersistence
    {
        public string AllocatedCode { get; init; } = "AP-2026-00001";
        public bool PatientExists { get; set; } = true;
        public bool DepartmentActive { get; set; } = true;
        public bool DoctorActive { get; init; } = true;
        public Appointment? TrackedAppointment { get; init; }
        public AppointmentPersistenceSaveStatus SaveStatus { get; init; } = AppointmentPersistenceSaveStatus.Saved;
        public int SaveChangesCalls { get; private set; }
        public int SearchCalls { get; private set; }
        public Appointment? AddedAppointment { get; private set; }

        public Task<AppointmentDetailsDto?> GetDetailsAsync(int appointmentId, CancellationToken cancellationToken) => Task.FromResult<AppointmentDetailsDto?>(null);
        public Task<PagedResult<AppointmentSummaryDto>> SearchAsync(AppointmentSearchRequest request, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(new PagedResult<AppointmentSummaryDto>([], 0, request.Page.PageNumber, request.Page.PageSize));
        }
        public Task<string> AllocateAppointmentCodeAsync(CancellationToken cancellationToken) => Task.FromResult(AllocatedCode);
        public Task<bool> PatientExistsAsync(int patientId, CancellationToken cancellationToken) => Task.FromResult(PatientExists);
        public Task<bool> DepartmentIsActiveAsync(int departmentId, CancellationToken cancellationToken) => Task.FromResult(DepartmentActive);
        public Task<bool> DoctorIsActiveInDepartmentAsync(int doctorId, int departmentId, CancellationToken cancellationToken) => Task.FromResult(DoctorActive);
        public Task<Appointment?> LoadTrackedAsync(int appointmentId, CancellationToken cancellationToken) => Task.FromResult(TrackedAppointment);
        public void Add(Appointment appointment) => AddedAppointment = appointment;
        public Task<AppointmentPersistenceSaveStatus> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.FromResult(SaveStatus);
        }
    }
}
