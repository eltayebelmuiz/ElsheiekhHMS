using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Queue;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Application.Queue.Persistence;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;

namespace ElsheiekhHMS.Tests.Unit.Application.Queue;

public sealed class QueueServiceTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 22, 22, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Add_uses_Kigali_queue_date_allocates_ticket_and_audits_once()
    {
        var persistence = new FakePersistence();
        var audit = new RecordingAuditWriter();

        var result = await CreateService(persistence, audit).AddAsync(
            new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Emergency, "  chest pain  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 9, 23), result.Value!.QueueDate);
        Assert.Equal("A-007", result.Value.QueueNumber);
        Assert.Equal(1, persistence.SaveChangesCalls);
        Assert.Equal(AuditActions.QueueEntryCreated, Assert.Single(audit.Events).Action);
        Assert.Equal("A-007", audit.Events[0].TargetId);
    }

    [Fact]
    public async Task Add_rejects_missing_or_inactive_references_without_saving()
    {
        var persistence = new FakePersistence { PatientExists = false };
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.AddAsync(new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Normal, null));
        Assert.Equal("queue.patient.not_found", Assert.Single(result.Errors).Code);

        persistence.PatientExists = true;
        persistence.DepartmentExists = false;
        result = await service.AddAsync(new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Normal, null));
        Assert.Equal("queue.department.not_found", Assert.Single(result.Errors).Code);

        persistence.DepartmentExists = true;
        persistence.DepartmentActive = false;
        result = await service.AddAsync(new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Normal, null));
        Assert.Equal("queue.department.inactive", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Add_rejects_duplicate_active_patient_and_maps_ticket_exhaustion()
    {
        var persistence = new FakePersistence { DuplicateActive = true };
        var service = CreateService(persistence, new RecordingAuditWriter());

        var result = await service.AddAsync(new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Normal, null));
        Assert.Equal("queue.duplicate_active", Assert.Single(result.Errors).Code);

        persistence.DuplicateActive = false;
        persistence.SaveStatus = QueuePersistenceSaveStatus.TicketAllocationFailure;
        result = await service.AddAsync(new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Normal, null));
        Assert.Equal("queue.ticket_allocation_failed", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Appointment_link_duplicate_is_translated_without_a_second_write()
    {
        var persistence = new FakePersistence { SaveStatus = QueuePersistenceSaveStatus.DuplicateAppointmentLink };
        var result = await CreateService(persistence, new RecordingAuditWriter()).AddAppointmentAsync(
            new AddAppointmentQueueEntryRequest(10, 20, 77, QueuePriority.Normal, null));

        Assert.Equal("queue.duplicate_appointment", Assert.Single(result.Errors).Code);
        Assert.Equal(1, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Lifecycle_actions_use_core_transitions_and_audit_actions()
    {
        var entry = NewEntry();
        entry.RowVersion = [1, 2, 3];
        var persistence = new FakePersistence { TrackedEntry = entry };
        var audit = new RecordingAuditWriter();
        var service = CreateService(persistence, audit);

        Assert.True((await service.CallToNurseAsync(Action("AQID"))).IsSuccess);
        Assert.True((await service.SendToDoctorAsync(new SendToDoctorRequest(7, 42, "AQID"))).IsSuccess);
        Assert.True((await service.CompleteAsync(Action("AQID"))).IsSuccess);

        Assert.Equal(QueueStatus.Completed, entry.Status);
        Assert.Equal(
            [AuditActions.QueueEntryCalled, AuditActions.QueueEntryStarted, AuditActions.QueueEntryCompleted],
            audit.Events.Select(item => item.Action));
        Assert.Equal(3, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Hold_resume_and_cancel_are_supported_and_invalid_transitions_are_returned()
    {
        var entry = NewEntry();
        entry.RowVersion = [1, 2, 3];
        var persistence = new FakePersistence { TrackedEntry = entry };
        var audit = new RecordingAuditWriter();
        var service = CreateService(persistence, audit);

        Assert.True((await service.HoldAsync(Action("AQID"))).IsSuccess);
        Assert.True((await service.ResumeAsync(Action("AQID"))).IsSuccess);
        Assert.True((await service.CancelAsync(Action("AQID", "patient request"))).IsSuccess);
        Assert.Equal(QueueStatus.Cancelled, entry.Status);

        var invalid = await service.CompleteAsync(Action("AQID"));
        Assert.Equal("queue.domain_rule", Assert.Single(invalid.Errors).Code);
        Assert.Equal(3, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Authorization_requires_stable_user_and_denies_provider_system_admin_and_patient()
    {
        var persistence = new FakePersistence();
        foreach (var role in new[] { RoleNames.Provider, RoleNames.SystemAdministrator, RoleNames.Patient })
        {
            var result = await CreateService(persistence, new RecordingAuditWriter(), [role])
                .AddAsync(new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Normal, null));
            Assert.Equal("queue.forbidden", Assert.Single(result.Errors).Code);
        }

        var anonymous = await new QueueService(
            persistence,
            new RecordingAuditWriter(),
            new TestCurrentUser(null, []),
            new FixedTimeProvider(NowUtc))
            .AddAsync(new AddWalkInQueueEntryRequest(10, 20, QueuePriority.Normal, null));
        Assert.Equal("queue.forbidden", Assert.Single(anonymous.Errors).Code);
    }

    [Fact]
    public async Task Search_propagates_cancellation_before_persistence()
    {
        var persistence = new FakePersistence();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(persistence, new RecordingAuditWriter()).SearchAsync(
                new QueueSearchRequest(new DateOnly(2026, 9, 23)), cancellation.Token));
        Assert.Equal(0, persistence.SearchCalls);
    }

    private static QueueService CreateService(
        FakePersistence persistence,
        RecordingAuditWriter audit,
        IReadOnlyCollection<string>? roles = null) => new(
        persistence,
        audit,
        new TestCurrentUser("stable-user", roles ?? [RoleNames.Receptionist]),
        new FixedTimeProvider(NowUtc));

    private static QueueEntryActionRequest Action(string token, string? reason = null) =>
        new(7, token, reason);

    private static WalkInQueueEntry NewEntry() => new(
        10,
        20,
        new DateOnly(2026, 9, 23),
        7,
        "A-007",
        QueuePriority.Normal,
        null,
        NowUtc,
        "stable-user");

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

    private sealed class FakePersistence : IQueuePersistence
    {
        public bool PatientExists { get; set; } = true;
        public bool DepartmentExists { get; set; } = true;
        public bool DepartmentActive { get; set; } = true;
        public bool DoctorActive { get; set; } = true;
        public bool DuplicateActive { get; set; }
        public int SequenceNumber { get; set; } = 7;
        public QueuePersistenceSaveStatus SaveStatus { get; set; } = QueuePersistenceSaveStatus.Saved;
        public WalkInQueueEntry? TrackedEntry { get; init; }
        public int SaveChangesCalls { get; private set; }
        public int SearchCalls { get; private set; }

        public Task<QueueEntryDetailsDto?> GetDetailsAsync(int queueEntryId, CancellationToken cancellationToken) =>
            Task.FromResult<QueueEntryDetailsDto?>(null);

        public Task<QueueEntryDetailsDto?> GetDetailsByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken) =>
            Task.FromResult<QueueEntryDetailsDto?>(null);

        public Task<PagedResult<QueueEntrySummaryDto>> SearchAsync(QueueSearchRequest request, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(new PagedResult<QueueEntrySummaryDto>([], 0, request.Page.PageNumber, request.Page.PageSize));
        }

        public Task<bool> PatientExistsAsync(int patientId, CancellationToken cancellationToken) => Task.FromResult(PatientExists);
        public Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken) => Task.FromResult(DepartmentExists);
        public Task<bool> DepartmentIsActiveAsync(int departmentId, CancellationToken cancellationToken) => Task.FromResult(DepartmentActive);
        public Task<bool> DoctorIsActiveInDepartmentAsync(int doctorId, int departmentId, CancellationToken cancellationToken) => Task.FromResult(DoctorActive);
        public Task<bool> HasActiveEntryAsync(int patientId, DateOnly queueDate, CancellationToken cancellationToken) => Task.FromResult(DuplicateActive);
        public Task<(DateOnly QueueDate, int SequenceNumber, string QueueNumber)> AllocateTicketAsync(DateOnly queueDate, CancellationToken cancellationToken) =>
            Task.FromResult((queueDate, SequenceNumber, QueueTicketAllocator.Format(SequenceNumber)));
        public Task<WalkInQueueEntry?> LoadTrackedAsync(int queueEntryId, CancellationToken cancellationToken) => Task.FromResult(TrackedEntry);
        public void Add(WalkInQueueEntry entry) { }
        public Task<QueuePersistenceSaveStatus> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.FromResult(SaveStatus);
        }
    }
}
