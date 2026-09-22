using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Queue;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
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

        await using var verification = fixture.CreateContext();
        var queueDate = new DateOnly(2026, 9, 23);
        var active = await verification.WalkInQueueEntries
            .Where(entry =>
                entry.PatientId == setup.PatientId &&
                entry.QueueDate == queueDate &&
                entry.Status != QueueStatus.Completed &&
                entry.Status != QueueStatus.Cancelled)
            .ToListAsync();

        var winner = Assert.Single(active);
        Assert.InRange(winner.SequenceNumber, 1, 999);
        Assert.Equal($"A-{winner.SequenceNumber:D3}", winner.QueueNumber);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.QueueEntryCreated &&
            log.TargetId == winner.QueueNumber));
    }

    [Fact]
    public async Task Concurrent_appointment_link_requests_leave_one_durable_link_and_audit()
    {
        var setup = await CreateRecordsAsync();
        int appointmentId;
        await using (var seed = fixture.CreateContext(setup.User, setup.Clock))
        {
            var doctor = new Doctor(
                "DR-" + Guid.NewGuid().ToString("N")[..8],
                "Queue Link Doctor",
                "General",
                false,
                100m,
                setup.DepartmentId,
                NowUtc,
                setup.User.UserId);
            seed.Doctors.Add(doctor);
            await seed.SaveChangesAsync();

            var appointment = new Appointment(
                "AP-" + Guid.NewGuid().ToString("N")[..8],
                setup.PatientId,
                doctor.Id,
                setup.DepartmentId,
                new DateOnly(2026, 9, 24),
                new TimeOnly(9, 0),
                AppointmentType.General,
                null,
                NowUtc,
                setup.User.UserId);
            seed.Appointments.Add(appointment);
            await seed.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        await using var firstContext = fixture.CreateContext(setup.User, setup.Clock);
        await using var secondContext = fixture.CreateContext(setup.User, setup.Clock);
        var firstService = CreateService(firstContext, setup.User, setup.Clock);
        var secondService = CreateService(secondContext, setup.User, setup.Clock);
        var request = new AddAppointmentQueueEntryRequest(
            setup.PatientId,
            setup.DepartmentId,
            appointmentId,
            QueuePriority.Normal,
            "scheduled arrival");

        var results = await Task.WhenAll(
            firstService.AddAppointmentAsync(request),
            secondService.AddAppointmentAsync(request));
        var diagnostic = string.Join(" | ", results.Select(result => result.IsSuccess
            ? "success"
            : string.Join(",", result.Errors.Select(error => error.Code))));

        Assert.True(results.Count(result => result.IsSuccess) == 1, diagnostic);
        Assert.True(results.Count(result => !result.IsSuccess && result.Errors.Any(error =>
            error.Code is "queue.duplicate_active" or "queue.duplicate_appointment")) == 1, diagnostic);

        await using var verification = fixture.CreateContext();
        var persistedAppointment = await verification.Appointments.SingleAsync(item => item.Id == appointmentId);
        var linked = await verification.WalkInQueueEntries
            .Where(entry => entry.AppointmentId == appointmentId)
            .ToListAsync();

        var winner = Assert.Single(linked);
        Assert.Equal(persistedAppointment.PatientId, winner.PatientId);
        Assert.Equal(persistedAppointment.DepartmentId, winner.DepartmentId);
        Assert.Equal(new DateOnly(2026, 9, 23), winner.QueueDate);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.QueueEntryCreated &&
            log.TargetId == winner.QueueNumber));
    }

    [Fact]
    public async Task Filtered_appointment_link_index_rejects_duplicate_and_allows_multiple_null_links()
    {
        var setup = await CreateRecordsAsync();
        int appointmentId;
        await using (var seed = fixture.CreateContext(setup.User, setup.Clock))
        {
            var doctor = new Doctor(
                "DR-" + Guid.NewGuid().ToString("N")[..8],
                "Index Doctor",
                "General",
                false,
                100m,
                setup.DepartmentId,
                NowUtc,
                setup.User.UserId);
            seed.Doctors.Add(doctor);
            await seed.SaveChangesAsync();
            var appointment = new Appointment(
                "AP-" + Guid.NewGuid().ToString("N")[..8],
                setup.PatientId,
                doctor.Id,
                setup.DepartmentId,
                new DateOnly(2026, 9, 25),
                new TimeOnly(9, 0),
                AppointmentType.General,
                null,
                NowUtc,
                setup.User.UserId);
            seed.Appointments.Add(appointment);
            await seed.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        await using (var first = fixture.CreateContext(setup.User, setup.Clock))
        {
            var service = CreateService(first, setup.User, setup.Clock);
            var result = await service.AddAppointmentAsync(new AddAppointmentQueueEntryRequest(
                setup.PatientId,
                setup.DepartmentId,
                appointmentId,
                QueuePriority.Normal,
                "index seed"));
            Assert.True(result.IsSuccess, string.Join(";", result.Errors.Select(error => error.Code)));
        }

        await using (var duplicate = fixture.CreateContext(setup.User, setup.Clock))
        {
            duplicate.WalkInQueueEntries.Add(new WalkInQueueEntry(
                setup.PatientId,
                setup.DepartmentId,
                new DateOnly(2026, 9, 26),
                1,
                "A-001",
                QueuePriority.Normal,
                null,
                NowUtc,
                setup.User.UserId,
                appointmentId));

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
            Assert.Contains("UX_WalkInQueueEntries_AppointmentId", exception.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        await using (var nullLinks = fixture.CreateContext(setup.User, setup.Clock))
        {
            nullLinks.WalkInQueueEntries.AddRange(
                new WalkInQueueEntry(
                    setup.PatientId,
                    setup.DepartmentId,
                    new DateOnly(2026, 9, 27),
                    1,
                    "A-001",
                    QueuePriority.Normal,
                    null,
                    NowUtc,
                    setup.User.UserId),
                new WalkInQueueEntry(
                    setup.PatientId,
                    setup.DepartmentId,
                    new DateOnly(2026, 9, 28),
                    1,
                    "A-001",
                    QueuePriority.Normal,
                    null,
                    NowUtc,
                    setup.User.UserId));

            await nullLinks.SaveChangesAsync();
            Assert.Equal(2, await nullLinks.WalkInQueueEntries.CountAsync(entry =>
                entry.AppointmentId == null && entry.QueueDate >= new DateOnly(2026, 9, 27)));
        }
    }

    [Fact]
    public async Task Completed_history_allows_a_new_queue_entry_for_the_same_patient_and_date()
    {
        var setup = await CreateRecordsAsync();
        int doctorId;
        await using (var seed = fixture.CreateContext(setup.User, setup.Clock))
        {
            var doctor = new Doctor(
                "DR-" + Guid.NewGuid().ToString("N")[..8],
                "Queue Lifecycle Doctor",
                "General",
                false,
                100m,
                setup.DepartmentId,
                NowUtc,
                setup.User.UserId);
            seed.Doctors.Add(doctor);
            await seed.SaveChangesAsync();
            doctorId = doctor.Id;
        }

        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);
        var first = await service.AddAsync(new AddWalkInQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, QueuePriority.Normal, "first"));
        Assert.True(first.IsSuccess);

        var atNurse = await service.CallToNurseAsync(new QueueEntryActionRequest(
            first.Value!.Id, first.Value.ConcurrencyToken));
        var atDoctor = await service.SendToDoctorAsync(new SendToDoctorRequest(
            first.Value.Id, doctorId, atNurse.Value!.ConcurrencyToken));
        var completed = await service.CompleteAsync(new QueueEntryActionRequest(
            first.Value.Id, atDoctor.Value!.ConcurrencyToken));
        Assert.True(atNurse.IsSuccess);
        Assert.True(atDoctor.IsSuccess);
        Assert.True(completed.IsSuccess);

        var replacement = await service.AddAsync(new AddWalkInQueueEntryRequest(
            setup.PatientId, setup.DepartmentId, QueuePriority.Urgent, "replacement"));
        Assert.True(replacement.IsSuccess, string.Join(";", replacement.Errors.Select(error => error.Code)));
        Assert.NotEqual(first.Value.QueueNumber, replacement.Value!.QueueNumber);

        await using var verification = fixture.CreateContext();
        var entries = await verification.WalkInQueueEntries
            .Where(entry => entry.PatientId == setup.PatientId && entry.QueueDate == new DateOnly(2026, 9, 23))
            .OrderBy(entry => entry.Id)
            .ToListAsync();
        Assert.Equal(2, entries.Count);
        Assert.Equal(QueueStatus.Completed, entries[0].Status);
        Assert.Equal(QueueStatus.Waiting, entries[1].Status);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.QueueEntryCompleted &&
            log.TargetId == entries[0].QueueNumber));
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.QueueEntryCreated &&
            log.TargetId == entries[1].QueueNumber));
    }

    [Fact]
    public async Task Queue_date_follows_Kigali_calendar_on_both_sides_of_utc_midnight()
    {
        var before = await CreateRecordsAsync(new DateTimeOffset(2026, 9, 22, 21, 59, 0, TimeSpan.Zero));
        await using (var beforeContext = fixture.CreateContext(before.User, before.Clock))
        {
            var result = await CreateService(beforeContext, before.User, before.Clock).AddAsync(
                new AddWalkInQueueEntryRequest(before.PatientId, before.DepartmentId, QueuePriority.Normal, null));
            Assert.True(result.IsSuccess);
            Assert.Equal(new DateOnly(2026, 9, 22), result.Value!.QueueDate);
        }

        var after = await CreateRecordsAsync(new DateTimeOffset(2026, 9, 22, 22, 0, 0, TimeSpan.Zero));
        await using (var afterContext = fixture.CreateContext(after.User, after.Clock))
        {
            var result = await CreateService(afterContext, after.User, after.Clock).AddAsync(
                new AddWalkInQueueEntryRequest(after.PatientId, after.DepartmentId, QueuePriority.Normal, null));
            Assert.True(result.IsSuccess);
            Assert.Equal(new DateOnly(2026, 9, 23), result.Value!.QueueDate);
        }
    }

    private QueueService CreateService(
        ElsheiekhHmsDbContext context,
        ICurrentUser user,
        TimeProvider clock) => new(
        new QueuePersistence(context, new QueueTicketAllocator(context)),
        new AuditEventWriter(context, user, clock),
        user,
        clock);

    private async Task<SetupRecords> CreateRecordsAsync(DateTimeOffset? nowOverride = null)
    {
        var user = new TestCurrentUser("queue-service-user", [RoleNames.Receptionist]);
        var now = nowOverride ?? NowUtc;
        var clock = new FixedTimeProvider(now);
        await using var context = fixture.CreateContext(user, clock);
        var department = new Department("Queue " + Guid.NewGuid().ToString("N")[..8], null, null, now, user.UserId);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var patient = new Patient(
            $"PT-2026-{Random.Shared.Next(1, 100000):D5}", "Amina", "M", null, "Hassan",
            new DateOnly(1990, 1, 1), Gender.Female, BloodGroup.OPositive, null, null,
            "0900" + Guid.NewGuid().ToString("N")[..6], "Main street", "City", null, null, null, null,
            new DateOnly(2026, 9, 22), now, user.UserId);
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
