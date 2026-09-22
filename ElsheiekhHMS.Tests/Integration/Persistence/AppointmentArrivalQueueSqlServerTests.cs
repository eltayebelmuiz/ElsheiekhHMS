using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Queue;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Application.Workflows.AppointmentArrival;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using ElsheiekhHMS.Infrastructure.Persistence.Appointments;
using ElsheiekhHMS.Infrastructure.Persistence.Queue;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class AppointmentArrivalQueueSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    private static readonly DateTimeOffset NowUtc = new(2026, 9, 22, 22, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Arrival_checks_in_and_creates_a_linked_queue_entry_using_operational_date()
    {
        var user = new TestCurrentUser("arrival-user", [RoleNames.Receptionist]);
        var clock = new FixedTimeProvider(NowUtc);
        var department = new Department("Arrival " + Guid.NewGuid().ToString("N")[..8], null, null, NowUtc, user.UserId);
        var patient = new Patient(
            $"PT-2026-{Random.Shared.Next(1, 100000):D5}", "Amina", "M", null, "Hassan",
            new DateOnly(1990, 1, 1), Gender.Female, BloodGroup.OPositive, null, null,
            "0900" + Guid.NewGuid().ToString("N")[..6], "Main street", "Kigali", null, null, null, null,
            new DateOnly(2026, 9, 22), NowUtc, user.UserId);

        await using var seed = fixture.CreateContext(user, clock);
        seed.Departments.Add(department);
        seed.Patients.Add(patient);
        await seed.SaveChangesAsync();
        var doctor = new Doctor("DR-" + Guid.NewGuid().ToString("N")[..8], "Arrival Doctor", "General", false, 100m, department.Id, NowUtc, user.UserId);
        seed.Doctors.Add(doctor);
        await seed.SaveChangesAsync();
        var appointment = new Appointment(
            "AP-" + Guid.NewGuid().ToString("N")[..8], patient.Id, doctor.Id, department.Id,
            new DateOnly(2026, 9, 24), new TimeOnly(9, 0), AppointmentType.General, null, NowUtc, user.UserId);
        seed.Appointments.Add(appointment);
        await seed.SaveChangesAsync();
        var token = Convert.ToBase64String(appointment.RowVersion);

        var appointmentService = new AppointmentService(
            new AppointmentPersistence(seed, new AppointmentCodeAllocator(seed, clock)),
            new AuditEventWriter(seed, user, clock), user, clock);
        var queueService = new QueueService(
            new QueuePersistence(seed, new QueueTicketAllocator(seed)),
            new AuditEventWriter(seed, user, clock), user, clock);
        var workflow = new AppointmentArrivalQueueService(appointmentService, queueService, user);

        var result = await workflow.CheckInAndQueueAsync(
            new AppointmentArrivalQueueRequest(appointment.Id, token, QueuePriority.Urgent, "arrival"));

        Assert.True(result.IsSuccess, string.Join(";", result.Errors.Select(error => error.Code)));
        Assert.Equal(AppointmentArrivalQueueOutcome.CompleteSuccess, result.Value!.Outcome);
        Assert.Equal(patient.Id, result.Value.Queue!.PatientId);
        Assert.Equal(department.Id, result.Value.Queue.DepartmentId);
        Assert.Equal(appointment.Id, result.Value.Queue.AppointmentId);
        Assert.Equal(new DateOnly(2026, 9, 23), result.Value.Queue.QueueDate);
        Assert.Equal(AppointmentStatus.CheckedIn, result.Value.Appointment.Status);
        Assert.Equal(1, await seed.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentCheckedIn && log.TargetId == appointment.AppointmentCode));
        Assert.Equal(1, await seed.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.QueueEntryCreated && log.TargetId == result.Value.Queue.QueueNumber));
        Assert.Equal(0, await seed.AuditLogs.CountAsync(log => log.Action == "APPOINTMENT_ARRIVAL_QUEUE_HANDOFF"));

        var called = await queueService.CallToNurseAsync(new QueueEntryActionRequest(
            result.Value.Queue.Id,
            result.Value.Queue.ConcurrencyToken));
        Assert.True(called.IsSuccess, string.Join(";", called.Errors.Select(error => error.Code)));
        var sentToDoctor = await queueService.SendToDoctorAsync(new SendToDoctorRequest(
            result.Value.Queue.Id,
            doctor.Id,
            called.Value!.ConcurrencyToken));
        Assert.True(sentToDoctor.IsSuccess, string.Join(";", sentToDoctor.Errors.Select(error => error.Code)));
        var completed = await queueService.CompleteAsync(new QueueEntryActionRequest(
            result.Value.Queue.Id,
            sentToDoctor.Value!.ConcurrencyToken));
        Assert.True(completed.IsSuccess, string.Join(";", completed.Errors.Select(error => error.Code)));

        var retry = await workflow.CheckInAndQueueAsync(
            new AppointmentArrivalQueueRequest(appointment.Id, token, QueuePriority.Urgent, "arrival retry"));
        Assert.True(retry.IsSuccess, string.Join(";", retry.Errors.Select(error => error.Code)));
        Assert.Equal(AppointmentArrivalQueueOutcome.ExistingHandoff, retry.Value!.Outcome);
        Assert.Equal(completed.Value!.Id, retry.Value.Queue!.Id);
        Assert.Equal(QueueStatus.Completed, retry.Value.Queue.Status);

        await using var verification = fixture.CreateContext();
        var persistedAppointment = await verification.Appointments.SingleAsync(item => item.Id == appointment.Id);
        var linked = await verification.WalkInQueueEntries
            .Where(entry => entry.AppointmentId == appointment.Id)
            .ToListAsync();
        Assert.Equal(AppointmentStatus.CheckedIn, persistedAppointment.Status);
        Assert.Single(linked);
        Assert.Equal(QueueStatus.Completed, linked[0].Status);
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.AppointmentCheckedIn && log.TargetId == appointment.AppointmentCode));
        Assert.Equal(1, await verification.AuditLogs.CountAsync(log =>
            log.Action == AuditActions.QueueEntryCreated && log.TargetId == linked[0].QueueNumber));
    }

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
