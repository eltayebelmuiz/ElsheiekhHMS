using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Queue;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Application.Workflows.AppointmentArrival;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Application.Common.Security;

namespace ElsheiekhHMS.Tests.Unit.Application.Workflows;

public sealed class AppointmentArrivalQueueServiceTests
{
    private static readonly AppointmentDetailsDto Scheduled = Appointment(AppointmentStatus.Scheduled);
    private static readonly AppointmentDetailsDto CheckedIn = Appointment(AppointmentStatus.CheckedIn);
    private static readonly QueueEntryDetailsDto Queue = new(
        91, "A-007", 17, 4, 33, null, new DateOnly(2026, 9, 23),
        QueuePriority.Urgent, QueueStatus.Waiting, Utc(2), "arrival", null, null, "AQ");

    [Theory]
    [InlineData(RoleNames.SystemAdministrator)]
    [InlineData(RoleNames.Provider)]
    [InlineData(RoleNames.Patient)]
    public async Task Non_staff_roles_are_denied_without_loading_appointment(string role)
    {
        var appointments = new FakeAppointmentService { Details = Scheduled };
        var service = Create(appointments, new FakeQueueService(), [role]);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.Equal("arrival.forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, appointments.GetCalls);
    }

    [Fact]
    public async Task Missing_stable_user_is_denied()
    {
        var appointments = new FakeAppointmentService { Details = Scheduled };
        var service = Create(appointments, new FakeQueueService(), [], userId: null);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.Equal("arrival.forbidden", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Scheduled_appointment_is_checked_in_then_handed_to_queue_with_appointment_values()
    {
        var appointments = new FakeAppointmentService { Details = Scheduled, CheckInResult = Success(CheckedIn) };
        var queue = new FakeQueueService { AddResult = Success(Queue) };
        var service = Create(appointments, queue);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentArrivalQueueOutcome.CompleteSuccess, result.Value!.Outcome);
        Assert.Equal(CheckedIn, result.Value.Appointment);
        Assert.Equal(Queue, result.Value.Queue);
        Assert.Equal(Scheduled.PatientId, queue.AddRequest!.PatientId);
        Assert.Equal(Scheduled.DepartmentId, queue.AddRequest.DepartmentId);
        Assert.Equal(Scheduled.Id, queue.AddRequest.AppointmentId);
        Assert.Equal(1, appointments.CheckInCalls);
    }

    [Fact]
    public async Task Checked_in_appointment_with_existing_link_returns_idempotent_handoff_without_checkin()
    {
        var appointments = new FakeAppointmentService { Details = CheckedIn };
        var queue = new FakeQueueService { LookupResult = Success(Queue) };
        var service = Create(appointments, queue);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentArrivalQueueOutcome.ExistingHandoff, result.Value!.Outcome);
        Assert.Equal(Queue, result.Value.Queue);
        Assert.Equal(0, appointments.CheckInCalls);
        Assert.Equal(0, queue.AddCalls);
    }

    [Fact]
    public async Task Checked_in_appointment_without_link_creates_only_queue_entry()
    {
        var appointments = new FakeAppointmentService { Details = CheckedIn };
        var queue = new FakeQueueService { AddResult = Success(Queue) };
        var service = Create(appointments, queue);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.Equal(AppointmentArrivalQueueOutcome.CompleteSuccess, result.Value!.Outcome);
        Assert.Equal(0, appointments.CheckInCalls);
        Assert.Equal(1, queue.AddCalls);
    }

    [Fact]
    public async Task Queue_failure_returns_partial_success_and_keeps_checked_in_appointment()
    {
        var appointments = new FakeAppointmentService { Details = Scheduled, CheckInResult = Success(CheckedIn) };
        var queue = new FakeQueueService
        {
            AddResult = Failure<QueueEntryDetailsDto>("queue.duplicate_active", "The patient already has an active queue entry for today.")
        };
        var service = Create(appointments, queue);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentArrivalQueueOutcome.PartialSuccess, result.Value!.Outcome);
        Assert.Equal(CheckedIn, result.Value.Appointment);
        Assert.Equal("queue.duplicate_active", result.Value.QueueError!.Code);
    }

    [Fact]
    public async Task Duplicate_link_race_recovers_winning_queue_entry()
    {
        var appointments = new FakeAppointmentService { Details = Scheduled, CheckInResult = Success(CheckedIn) };
        var queue = new FakeQueueService
        {
            AddResult = Failure<QueueEntryDetailsDto>("queue.duplicate_appointment", "The appointment already has a queue entry."),
            LookupResults = [Failure<QueueEntryDetailsDto>("queue.not_found", "Queue entry was not found."), Success(Queue)]
        };
        var service = Create(appointments, queue);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.Equal(AppointmentArrivalQueueOutcome.ExistingHandoff, result.Value!.Outcome);
        Assert.Equal(Queue, result.Value.Queue);
        Assert.Equal(2, queue.LookupCalls);
    }

    [Theory]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.Cancelled)]
    [InlineData(AppointmentStatus.NoShow)]
    public async Task Terminal_appointment_state_is_rejected_without_queue_access(AppointmentStatus status)
    {
        var appointments = new FakeAppointmentService { Details = Appointment(status) };
        var queue = new FakeQueueService();
        var service = Create(appointments, queue);

        var result = await service.CheckInAndQueueAsync(Request());

        Assert.Equal("arrival.appointment.not_eligible", Assert.Single(result.Errors).Code);
        Assert.Equal(0, queue.LookupCalls);
        Assert.Equal(0, queue.AddCalls);
    }

    [Fact]
    public async Task Cancellation_is_propagated_before_workflow_calls()
    {
        var appointments = new FakeAppointmentService { Details = Scheduled };
        var service = Create(appointments, new FakeQueueService());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.CheckInAndQueueAsync(Request(), cancellation.Token));
    }

    private static AppointmentArrivalQueueService Create(
        FakeAppointmentService appointments,
        FakeQueueService queue,
        IReadOnlyCollection<string>? roles = null,
        string? userId = "arrival-user") =>
        new(appointments, queue, new TestCurrentUser(userId, roles ?? [RoleNames.Receptionist]));

    private static AppointmentArrivalQueueRequest Request() => new(33, "token", QueuePriority.Urgent, "arrival");

    private static AppointmentDetailsDto Appointment(AppointmentStatus status) => new(
        33, "AP-2026-00033", 17, 8, 4, new DateOnly(2026, 9, 24), new TimeOnly(9, 0),
        AppointmentType.General, status, null, null, null, "token");

    private static DateTimeOffset Utc(int minute) => new(2026, 9, 23, 7, minute, 0, TimeSpan.Zero);

    private static ServiceResult<T> Success<T>(T value) => ServiceResult<T>.Success(value);
    private static ServiceResult<T> Failure<T>(string code, string message) => ServiceResult<T>.Failure(new[] { new ServiceError(code, message) });

    private sealed record TestCurrentUser(string? UserId, IReadOnlyCollection<string> Roles) : ICurrentUser
    {
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
        public string? UserName => "operator";
    }

    private sealed class FakeAppointmentService : IAppointmentService
    {
        public AppointmentDetailsDto? Details { get; init; }
        public ServiceResult<AppointmentDetailsDto>? CheckInResult { get; init; }
        public int GetCalls { get; private set; }
        public int CheckInCalls { get; private set; }

        public Task<ServiceResult<AppointmentDetailsDto>> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetCalls++;
            return Task.FromResult(Details is null
                ? Failure<AppointmentDetailsDto>("appointment.not_found", "Appointment was not found.")
                : Success(Details));
        }

        public Task<ServiceResult<AppointmentDetailsDto>> CheckInAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckInCalls++;
            return Task.FromResult(CheckInResult ?? Failure<AppointmentDetailsDto>("appointment.persistence_failure", "failed"));
        }

        public Task<ServiceResult<PagedResult<AppointmentSummaryDto>>> SearchAsync(AppointmentSearchRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<AppointmentDetailsDto>> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<AppointmentDetailsDto>> ScheduleAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<AppointmentDetailsDto>> CancelAsync(CancelAppointmentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<AppointmentDetailsDto>> MarkNoShowAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<AppointmentDetailsDto>> CompleteAsync(AppointmentActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeQueueService : IQueueService
    {
        public ServiceResult<QueueEntryDetailsDto>? LookupResult { get; init; }
        public ServiceResult<QueueEntryDetailsDto>? AddResult { get; init; }
        public IReadOnlyList<ServiceResult<QueueEntryDetailsDto>>? LookupResults { get; init; }
        public AddAppointmentQueueEntryRequest? AddRequest { get; private set; }
        public int LookupCalls { get; private set; }
        public int AddCalls { get; private set; }

        public Task<ServiceResult<QueueEntryDetailsDto>> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = LookupResults is not null && LookupCalls < LookupResults.Count
                ? LookupResults[LookupCalls]
                : LookupResult ?? Failure<QueueEntryDetailsDto>("queue.not_found", "Queue entry was not found.");
            LookupCalls++;
            return Task.FromResult(result);
        }

        public Task<ServiceResult<QueueEntryDetailsDto>> AddAppointmentAsync(AddAppointmentQueueEntryRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddRequest = request;
            AddCalls++;
            return Task.FromResult(AddResult ?? Failure<QueueEntryDetailsDto>("queue.persistence_failure", "failed"));
        }

        public Task<ServiceResult<QueueEntryDetailsDto>> GetByIdAsync(int queueEntryId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<PagedResult<QueueEntrySummaryDto>>> SearchAsync(QueueSearchRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<QueueEntryDetailsDto>> AddAsync(AddWalkInQueueEntryRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<QueueEntryDetailsDto>> CallToNurseAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<QueueEntryDetailsDto>> SendToDoctorAsync(SendToDoctorRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<QueueEntryDetailsDto>> HoldAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<QueueEntryDetailsDto>> ResumeAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<QueueEntryDetailsDto>> CompleteAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ServiceResult<QueueEntryDetailsDto>> CancelAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
