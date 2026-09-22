using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Queue;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using ElsheiekhHMS.Infrastructure.Persistence.Queue;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class QueueServiceSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 22, 22, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Add_persists_queue_entry_and_audit_with_Kigali_date_and_ticket()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);

        var result = await service.AddAsync(new AddWalkInQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, QueuePriority.Urgent, "Needs review"));

        Assert.True(result.IsSuccess, string.Join(";", result.Errors.Select(error => error.Code)));
        Assert.Equal(new DateOnly(2026, 9, 23), result.Value!.QueueDate);
        Assert.StartsWith("A-", result.Value.QueueNumber, StringComparison.Ordinal);
        Assert.True(await context.WalkInQueueEntries.AnyAsync(entry => entry.QueueNumber == result.Value.QueueNumber));
        Assert.True(await context.AuditLogs.AnyAsync(log =>
            log.Action == AuditActions.QueueEntryCreated && log.TargetId == result.Value.QueueNumber));
    }

    [Fact]
    public async Task Search_and_get_return_projected_bounded_queue_data()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);
        var created = await service.AddAsync(new AddWalkInQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, QueuePriority.Normal, null));
        Assert.True(created.IsSuccess);

        var details = await service.GetByIdAsync(created.Value!.Id);
        var search = await service.SearchAsync(new QueueSearchRequest(
            new DateOnly(2026, 9, 23), departmentId: setup.DepartmentId, page: new(1, 1)));

        Assert.True(details.IsSuccess);
        Assert.Equal(created.Value.QueueNumber, details.Value!.QueueNumber);
        Assert.True(search.IsSuccess);
        Assert.Equal(1, search.Value!.TotalCount);
        Assert.Single(search.Value.Items);
    }

    [Fact]
    public async Task Appointment_linked_add_and_lookup_roundtrip_through_queue_service()
    {
        var setup = await CreateRecordsAsync();
        int appointmentId;
        await using (var seed = fixture.CreateContext(setup.User, setup.Clock))
        {
            var department = await seed.Departments.SingleAsync(item => item.Id == setup.DepartmentId);
            var doctor = new ElsheiekhHMS.Core.Domain.Staff.Entities.Doctor(
                "DR-" + Guid.NewGuid().ToString("N")[..8], "Queue Doctor", "General", false,
                100m, department.Id, NowUtc, setup.User.UserId);
            seed.Doctors.Add(doctor);
            await seed.SaveChangesAsync();
            var appointment = new ElsheiekhHMS.Core.Domain.Scheduling.Entities.Appointment(
                "AP-" + Guid.NewGuid().ToString("N")[..8], setup.PatientId, doctor.Id, department.Id,
                new DateOnly(2026, 9, 24), new TimeOnly(9, 0),
                ElsheiekhHMS.Core.Domain.Scheduling.Enums.AppointmentType.General,
                null, NowUtc, setup.User.UserId);
            seed.Appointments.Add(appointment);
            await seed.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);
        var linked = await service.AddAppointmentAsync(new AddAppointmentQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, appointmentId, QueuePriority.Normal, "scheduled arrival"));
        var found = await service.GetByAppointmentIdAsync(appointmentId);

        Assert.True(linked.IsSuccess, string.Join(";", linked.Errors.Select(error => error.Code)));
        Assert.Equal(appointmentId, linked.Value!.AppointmentId);
        Assert.True(found.IsSuccess);
        Assert.Equal(linked.Value.Id, found.Value!.Id);
    }

    [Fact]
    public async Task Cancelled_history_does_not_block_a_future_queue_entry()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);
        var first = await service.AddAsync(new AddWalkInQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, QueuePriority.Normal, null));
        Assert.True(first.IsSuccess);

        var cancelled = await service.CancelAsync(new QueueEntryActionRequest(
            first.Value!.Id, first.Value.ConcurrencyToken, "Patient left"));
        Assert.True(cancelled.IsSuccess, string.Join(";", cancelled.Errors.Select(error => error.Code)));

        var replacement = await service.AddAsync(new AddWalkInQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, QueuePriority.Normal, null));
        Assert.True(replacement.IsSuccess, string.Join(";", replacement.Errors.Select(error => error.Code)));
    }

    [Fact]
    public async Task Concurrent_add_requests_allow_only_one_active_entry_for_a_patient_day()
    {
        var setup = await CreateRecordsAsync();
        await using var firstContext = fixture.CreateContext(setup.User, setup.Clock);
        await using var secondContext = fixture.CreateContext(setup.User, setup.Clock);
        var firstService = CreateService(firstContext, setup.User, setup.Clock);
        var secondService = CreateService(secondContext, setup.User, setup.Clock);
        var request = new AddWalkInQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, QueuePriority.Normal, null);

        var results = await Task.WhenAll(firstService.AddAsync(request), secondService.AddAsync(request));
        var diagnostic = string.Join(" | ", results.Select(result => result.IsSuccess
            ? "success"
            : string.Join(",", result.Errors.Select(error => error.Code))));

        Assert.True(results.Count(result => result.IsSuccess) == 1, diagnostic);
        Assert.True(results.Count(result => !result.IsSuccess && result.Errors.Any(error => error.Code == "queue.duplicate_active")) == 1, diagnostic);
    }

    private QueueService CreateService(
        ElsheiekhHmsDbContext context,
        ICurrentUser user,
        TimeProvider clock) => new(
        new QueuePersistence(context, new QueueTicketAllocator(context)),
        new AuditEventWriter(context, user, clock),
        user,
        clock);

    private async Task<SetupRecords> CreateRecordsAsync()
    {
        var user = new TestCurrentUser("queue-service-user", [RoleNames.Receptionist]);
        var clock = new FixedTimeProvider(NowUtc);
        await using var context = fixture.CreateContext(user, clock);
        var department = new Department("Queue " + Guid.NewGuid().ToString("N")[..8], null, null, NowUtc, user.UserId);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var patient = new Patient(
            $"PT-2026-{Random.Shared.Next(1, 100000):D5}", "Amina", "M", null, "Hassan",
            new DateOnly(1990, 1, 1), Gender.Female, BloodGroup.OPositive, null, null,
            "0900" + Guid.NewGuid().ToString("N")[..6], "Main street", "City", null, null, null, null,
            new DateOnly(2026, 9, 22), NowUtc, user.UserId);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return new SetupRecords(department.Id, patient.Id, user, clock);
    }

    private sealed record SetupRecords(int DepartmentId, int PatientId, TestCurrentUser User, FixedTimeProvider Clock);

    private sealed record TestCurrentUser(string? UserId, IReadOnlyCollection<string> Roles) : ICurrentUser
    {
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
        public string? UserName => "operator";
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
