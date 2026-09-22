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
    }

    private AppointmentService CreateService(
        ElsheiekhHmsDbContext context,
        ICurrentUser user,
        TimeProvider clock) => new(
        new AppointmentPersistence(context, new AppointmentCodeAllocator(context, clock)),
        new AuditEventWriter(context, user, clock),
        user,
        clock);

    private static CreateAppointmentRequest Request(int patientId, int doctorId, int departmentId) => new(
        patientId,
        doctorId,
        departmentId,
        new DateOnly(2026, 9, 23),
        new TimeOnly(9, 0),
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
