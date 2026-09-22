using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using ElsheiekhHMS.Infrastructure.Persistence.Appointments;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class AppointmentServiceSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Schedule_persists_appointment_and_audit_with_allocator_code()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);

        var result = await service.ScheduleAsync(Request(setup.PatientId, setup.DoctorId, setup.DepartmentId));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("AP-2026-", result.Value!.AppointmentCode, StringComparison.Ordinal);
        Assert.True(await context.Appointments.AnyAsync(appointment => appointment.AppointmentCode == result.Value.AppointmentCode));
        Assert.True(await context.AuditLogs.AnyAsync(log =>
            log.Action == AuditActions.AppointmentScheduled && log.TargetId == result.Value.AppointmentCode));
    }

    [Fact]
    public async Task Search_and_get_return_bounded_projected_appointment_data()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);
        var created = await service.CreateAsync(Request(setup.PatientId, setup.DoctorId, setup.DepartmentId));
        Assert.True(created.IsSuccess);

        var details = await service.GetByIdAsync(created.Value!.Id);
        var search = await service.SearchAsync(new AppointmentSearchRequest(
            new DateOnly(2026, 9, 23), new DateOnly(2026, 9, 23),
            departmentId: setup.DepartmentId, page: new(1, 1)));

        Assert.True(details.IsSuccess);
        Assert.Equal(created.Value.AppointmentCode, details.Value!.AppointmentCode);
        Assert.True(search.IsSuccess);
        Assert.Equal(1, search.Value!.TotalCount);
        Assert.Single(search.Value.Items);
    }

    [Fact]
    public async Task Same_department_and_instant_collision_is_rejected_but_cancelled_history_does_not_block()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);
        var request = Request(setup.PatientId, setup.DoctorId, setup.DepartmentId);

        var first = await service.CreateAsync(request);
        var second = await service.CreateAsync(request);
        Assert.True(first.IsSuccess);
        Assert.False(second.IsSuccess);
        Assert.Equal("appointment.collision", Assert.Single(second.Errors).Code);

        var cancelled = await service.CancelAsync(new CancelAppointmentRequest(
            first.Value!.Id,
            "Rescheduled by patient",
            first.Value.ConcurrencyToken));
        Assert.True(cancelled.IsSuccess, string.Join(";", cancelled.Errors.Select(error => error.Code + ":" + error.Message)));

        var replacement = await service.CreateAsync(request);
        Assert.True(replacement.IsSuccess);
    }

    [Fact]
    public async Task Concurrent_same_department_and_instant_requests_allow_only_one_slot()
    {
        var setup = await CreateRecordsAsync();
        await using var firstContext = fixture.CreateContext(setup.User, setup.Clock);
        await using var secondContext = fixture.CreateContext(setup.User, setup.Clock);
        var firstService = CreateService(firstContext, setup.User, setup.Clock);
        var secondService = CreateService(secondContext, setup.User, setup.Clock);
        var request = Request(setup.PatientId, setup.DoctorId, setup.DepartmentId);

        var results = await Task.WhenAll(
            firstService.CreateAsync(request),
            secondService.CreateAsync(request));

        var diagnostic = string.Join(" | ", results.Select(result => result.IsSuccess ? "success" : string.Join(",", result.Errors.Select(error => error.Code + ":" + error.Message))));
        Assert.True(results.Count(result => result.IsSuccess) == 1, diagnostic);
        Assert.True(results.Count(result => !result.IsSuccess && result.Errors.Any(error => error.Code == "appointment.collision")) == 1, diagnostic);

        await using var verification = fixture.CreateContext();
        var persisted = await verification.Appointments
            .Where(appointment =>
                appointment.DepartmentId == setup.DepartmentId &&
                appointment.ScheduledDate == request.ScheduledDate &&
                appointment.ScheduledTime == request.ScheduledTime &&
                appointment.Status != AppointmentStatus.Cancelled &&
                appointment.Status != AppointmentStatus.NoShow &&
                appointment.Status != AppointmentStatus.Completed)
            .ToListAsync();

        var winner = Assert.Single(persisted);
        Assert.Equal(AppointmentStatus.Scheduled, winner.Status);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentScheduled &&
            log.TargetId == winner.AppointmentCode));
    }

    [Fact]
    public async Task No_show_check_in_and_complete_persist_lifecycle_state_and_audits()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);

        var noShow = await service.CreateAsync(Request(
            setup.PatientId, setup.DoctorId, setup.DepartmentId,
            new DateOnly(2026, 9, 24), new TimeOnly(9, 0)));
        var checkedIn = await service.CreateAsync(Request(
            setup.PatientId, setup.DoctorId, setup.DepartmentId,
            new DateOnly(2026, 9, 24), new TimeOnly(10, 0)));
        Assert.True(noShow.IsSuccess);
        Assert.True(checkedIn.IsSuccess);

        var noShowResult = await service.MarkNoShowAsync(new AppointmentActionRequest(
            noShow.Value!.Id, noShow.Value.ConcurrencyToken));
        var checkInResult = await service.CheckInAsync(new AppointmentActionRequest(
            checkedIn.Value!.Id, checkedIn.Value.ConcurrencyToken));
        Assert.True(noShowResult.IsSuccess);
        Assert.True(checkInResult.IsSuccess);
        Assert.Equal(AppointmentStatus.NoShow, noShowResult.Value!.Status);
        Assert.Equal(AppointmentStatus.CheckedIn, checkInResult.Value!.Status);

        await using (var checkedInVerification = fixture.CreateContext())
        {
            var persisted = await checkedInVerification.Appointments.SingleAsync(item => item.Id == checkedIn.Value.Id);
            Assert.Equal(AppointmentStatus.CheckedIn, persisted.Status);
            Assert.Null(persisted.CancellationReason);
            Assert.Null(persisted.CancelledAt);
            Assert.Equal(1, await checkedInVerification.AuditLogs.CountAsync(log =>
                log.Action == AuditActions.AppointmentCheckedIn &&
                log.TargetId == persisted.AppointmentCode &&
                log.ActorUserId == setup.User.UserId));
        }

        var completed = await service.CompleteAsync(new AppointmentActionRequest(
            checkInResult.Value.Id, checkInResult.Value.ConcurrencyToken));
        Assert.True(completed.IsSuccess);
        Assert.Equal(AppointmentStatus.Completed, completed.Value!.Status);

        await using var verification = fixture.CreateContext();
        var noShowPersisted = await verification.Appointments.SingleAsync(item => item.Id == noShow.Value.Id);
        var completedPersisted = await verification.Appointments.SingleAsync(item => item.Id == checkedIn.Value.Id);
        Assert.Equal(AppointmentStatus.NoShow, noShowPersisted.Status);
        Assert.Equal(AppointmentStatus.Completed, completedPersisted.Status);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentNoShow &&
            log.TargetId == noShowPersisted.AppointmentCode &&
            log.ActorUserId == setup.User.UserId));
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentCompleted &&
            log.TargetId == completedPersisted.AppointmentCode &&
            log.ActorUserId == setup.User.UserId));
        Assert.False(await verification.WalkInQueueEntries.AnyAsync(entry =>
            entry.PatientId == setup.PatientId && entry.AppointmentId == checkedIn.Value.Id));
    }

    [Fact]
    public async Task No_show_and_completed_history_allow_physical_slot_reuse()
    {
        var setup = await CreateRecordsAsync();
        await using var context = fixture.CreateContext(setup.User, setup.Clock);
        var service = CreateService(context, setup.User, setup.Clock);

        var noShow = await service.CreateAsync(Request(
            setup.PatientId, setup.DoctorId, setup.DepartmentId,
            new DateOnly(2026, 9, 25), new TimeOnly(9, 0)));
        Assert.True(noShow.IsSuccess);
        var noShowTransition = await service.MarkNoShowAsync(new AppointmentActionRequest(
            noShow.Value!.Id, noShow.Value.ConcurrencyToken));
        Assert.True(noShowTransition.IsSuccess);

        var noShowReplacement = await service.CreateAsync(Request(
            setup.PatientId, setup.DoctorId, setup.DepartmentId,
            new DateOnly(2026, 9, 25), new TimeOnly(9, 0)));
        Assert.True(noShowReplacement.IsSuccess);

        var completed = await service.CreateAsync(Request(
            setup.PatientId, setup.DoctorId, setup.DepartmentId,
            new DateOnly(2026, 9, 25), new TimeOnly(10, 0)));
        Assert.True(completed.IsSuccess);
        var checkedIn = await service.CheckInAsync(new AppointmentActionRequest(
            completed.Value!.Id, completed.Value.ConcurrencyToken));
        var completedResult = await service.CompleteAsync(new AppointmentActionRequest(
            completed.Value.Id, checkedIn.Value!.ConcurrencyToken));
        Assert.True(checkedIn.IsSuccess);
        Assert.True(completedResult.IsSuccess);

        var completedReplacement = await service.CreateAsync(Request(
            setup.PatientId, setup.DoctorId, setup.DepartmentId,
            new DateOnly(2026, 9, 25), new TimeOnly(10, 0)));
        Assert.True(completedReplacement.IsSuccess);

        await using var verification = fixture.CreateContext();
        var sameSlot = await verification.Appointments
            .Where(item => item.DepartmentId == setup.DepartmentId &&
                           item.ScheduledDate == new DateOnly(2026, 9, 25))
            .OrderBy(item => item.ScheduledTime)
            .ToListAsync();
        Assert.Equal(4, sameSlot.Count);
        Assert.Equal(1, sameSlot.Count(item => item.Status == AppointmentStatus.NoShow));
        Assert.Equal(1, sameSlot.Count(item => item.Status == AppointmentStatus.Completed));
        Assert.Equal(2, sameSlot.Count(item => item.Status == AppointmentStatus.Scheduled));
        Assert.Equal(4, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentScheduled &&
            sameSlot.Select(item => item.AppointmentCode).Contains(log.TargetId!)));
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentNoShow &&
            log.TargetId == noShow.Value.AppointmentCode));
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentCompleted &&
            log.TargetId == completed.Value.AppointmentCode));
    }

    private AppointmentService CreateService(
        ElsheiekhHmsDbContext context,
        ICurrentUser user,
        TimeProvider clock) => new(
        new AppointmentPersistence(context, new AppointmentCodeAllocator(context, clock)),
        new AuditEventWriter(context, user, clock),
        user,
        clock);

    private static CreateAppointmentRequest Request(
        int patientId,
        int doctorId,
        int departmentId,
        DateOnly? scheduledDate = null,
        TimeOnly? scheduledTime = null) => new(
        patientId,
        doctorId,
        departmentId,
        scheduledDate ?? new DateOnly(2026, 9, 23),
        scheduledTime ?? new TimeOnly(9, 0),
        AppointmentType.General,
        "Routine visit");

    private async Task<SetupRecords> CreateRecordsAsync()
    {
        var user = new TestCurrentUser("appointment-service-user", [RoleNames.Receptionist]);
        var clock = new FixedTimeProvider(NowUtc);
        await using var context = fixture.CreateContext(user, clock);
        var department = new Department("Appointments " + Guid.NewGuid().ToString("N")[..8], null, null, NowUtc, user.UserId);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var patient = new Patient(
            $"PT-2026-{Random.Shared.Next(1, 100000):D5}", "Amina", "M", null, "Hassan",
            new DateOnly(1990, 1, 1), Gender.Female, BloodGroup.OPositive, null, null,
            "0900" + Guid.NewGuid().ToString("N")[..6], "Main street", "City", null, null, null, null,
            new DateOnly(2026, 9, 22), NowUtc, user.UserId);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var doctor = new Doctor(
            "DR-" + Guid.NewGuid().ToString("N")[..10], "Doctor A", "General Medicine", true,
            0, department.Id, NowUtc, user.UserId);
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();
        return new SetupRecords(department.Id, patient.Id, doctor.Id, user, clock);
    }

    private sealed record SetupRecords(
        int DepartmentId,
        int PatientId,
        int DoctorId,
        TestCurrentUser User,
        FixedTimeProvider Clock);

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
