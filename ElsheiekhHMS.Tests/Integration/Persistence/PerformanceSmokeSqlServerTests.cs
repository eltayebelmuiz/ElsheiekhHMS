using System.Data.Common;
using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Departments;
using ElsheiekhHMS.Application.Patients;
using ElsheiekhHMS.Application.Patients.Contracts;
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
using ElsheiekhHMS.Infrastructure.Persistence.Appointments;
using ElsheiekhHMS.Infrastructure.Persistence.Patients;
using ElsheiekhHMS.Infrastructure.Persistence.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class PerformanceSmokeSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    private static readonly DateTimeOffset NowUtc =
        new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Patient_search_returns_a_bounded_page_with_bounded_sql_commands()
    {
        var setup = await SeedAsync();
        var counter = new CommandCountingInterceptor();
        var user = setup.User;
        await using var context = CreateObservedContext(counter);
        var service = new PatientService(
            new PatientPersistence(context, new PatientCodeAllocator(context)),
            new AuditEventWriter(context, user, setup.Clock),
            user,
            setup.Clock);

        var result = await service.SearchAsync(new PatientSearchRequest(
            searchText: setup.PatientSearchText,
            page: new PageRequest(2, 5),
            sortBy: PatientSortField.Name));

        Assert.True(result.IsSuccess, string.Join(";", result.Errors.Select(error => error.Code)));
        Assert.Equal(setup.PatientCount, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Items.Count);
        Assert.InRange(counter.CommandCount, 1, 3);
    }

    [Fact]
    public async Task Appointment_search_returns_a_bounded_page_with_bounded_sql_commands()
    {
        var setup = await SeedAsync();
        var counter = new CommandCountingInterceptor();
        var user = setup.User;
        await using var context = CreateObservedContext(counter);
        var service = new AppointmentService(
            new AppointmentPersistence(context, new AppointmentCodeAllocator(context, setup.Clock)),
            new AuditEventWriter(context, user, setup.Clock),
            user,
            setup.Clock);

        var result = await service.SearchAsync(new AppointmentSearchRequest(
            setup.AppointmentDate,
            setup.AppointmentDate,
            departmentId: setup.DepartmentId,
            page: new PageRequest(2, 4)));

        Assert.True(result.IsSuccess, string.Join(";", result.Errors.Select(error => error.Code)));
        Assert.Equal(setup.AppointmentCount, result.Value!.TotalCount);
        Assert.Equal(4, result.Value.Items.Count);
        Assert.True(result.Value.Items[0].ScheduledTime < result.Value.Items[^1].ScheduledTime);
        Assert.InRange(counter.CommandCount, 1, 3);
    }

    [Fact]
    public async Task Queue_search_returns_a_bounded_page_with_bounded_sql_commands()
    {
        var setup = await SeedAsync();
        var counter = new CommandCountingInterceptor();
        var user = setup.User;
        await using var context = CreateObservedContext(counter);
        var service = new QueueService(
            new QueuePersistence(context, new QueueTicketAllocator(context)),
            new AuditEventWriter(context, user, setup.Clock),
            user,
            setup.Clock);

        var result = await service.SearchAsync(new QueueSearchRequest(
            setup.QueueDate,
            departmentId: setup.DepartmentId,
            page: new PageRequest(2, 4)));

        Assert.True(result.IsSuccess, string.Join(";", result.Errors.Select(error => error.Code)));
        Assert.Equal(setup.QueueCount, result.Value!.TotalCount);
        Assert.Equal(4, result.Value.Items.Count);
        Assert.True(result.Value.Items[0].RegisteredAt < result.Value.Items[^1].RegisteredAt);
        Assert.InRange(counter.CommandCount, 1, 3);
    }

    private ElsheiekhHmsDbContext CreateObservedContext(CommandCountingInterceptor counter) =>
        new(new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .AddInterceptors(counter)
            .Options);

    private async Task<SmokeData> SeedAsync()
    {
        var user = new TestCurrentUser(
            "phase10d-smoke-user",
            [RoleNames.Administrator, RoleNames.Receptionist, RoleNames.SystemAdministrator]);
        var clock = new FixedTimeProvider(NowUtc);
        var patientSearchText = "PerfSmoke-" + Guid.NewGuid().ToString("N")[..8];
        var appointmentDate = new DateOnly(2026, 9, 23);

        await using var context = fixture.CreateContext(user, clock);
        var department = new Department(
            "Performance " + Guid.NewGuid().ToString("N")[..8],
            null,
            null,
            NowUtc,
            user.UserId);
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var existingPatientCodes = await context.Patients
            .IgnoreQueryFilters()
            .Select(patient => patient.PatientCode)
            .ToHashSetAsync();
        var patientCodeStart = 90_000;
        while (patientCodeStart <= 99_967 && Enumerable.Range(patientCodeStart, 32)
                   .Any(index => existingPatientCodes.Contains($"PT-2026-{index:D5}")))
        {
            patientCodeStart++;
        }

        var patients = Enumerable.Range(1, 32)
            .Select(index => new Patient(
                $"PT-2026-{patientCodeStart + index - 1:D5}",
                $"{patientSearchText}-{index:D2}",
                null,
                null,
                "Patient",
                new DateOnly(1990, 1, 1),
                Gender.Female,
                BloodGroup.OPositive,
                null,
                null,
                "0900123456",
                "Main street",
                "City",
                null,
                null,
                null,
                null,
                new DateOnly(2026, 9, 22),
                NowUtc,
                user.UserId))
            .ToArray();
        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();

        var doctor = new Doctor(
            "DR-" + Guid.NewGuid().ToString("N")[..8],
            "Performance Doctor",
            null,
            true,
            100m,
            department.Id,
            NowUtc,
            user.UserId);
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var queueDate = new DateOnly(2099, 1, 1);
        while (await context.WalkInQueueEntries
                   .IgnoreQueryFilters()
                   .AnyAsync(entry => entry.QueueDate == queueDate))
        {
            queueDate = queueDate.AddDays(1);
        }

        var appointments = Enumerable.Range(0, 12)
            .Select(index => new Appointment(
                $"AP-10D-{Guid.NewGuid():N}-{index:D2}",
                patients[index % patients.Length].Id,
                doctor.Id,
                department.Id,
                appointmentDate,
                new TimeOnly(8, 0).AddMinutes(index * 15),
                AppointmentType.General,
                null,
                NowUtc.AddMinutes(index),
                user.UserId))
            .ToArray();
        context.Appointments.AddRange(appointments);

        var queueEntries = Enumerable.Range(1, 12)
            .Select(sequence => new WalkInQueueEntry(
                patients[(sequence - 1) % patients.Length].Id,
                department.Id,
                queueDate,
                sequence,
                $"A-{sequence:D3}",
                sequence % 2 == 0 ? QueuePriority.Urgent : QueuePriority.Normal,
                null,
                NowUtc.AddMinutes(sequence),
                user.UserId))
            .ToArray();
        context.WalkInQueueEntries.AddRange(queueEntries);
        await context.SaveChangesAsync();

        return new(
            department.Id,
            patientSearchText,
            patients.Length,
            appointmentDate,
            appointments.Length,
            queueDate,
            queueEntries.Length,
            user,
            clock);
    }

    private sealed record SmokeData(
        int DepartmentId,
        string PatientSearchText,
        int PatientCount,
        DateOnly AppointmentDate,
        int AppointmentCount,
        DateOnly QueueDate,
        int QueueCount,
        TestCurrentUser User,
        FixedTimeProvider Clock);

    private sealed record TestCurrentUser(
        string? UserId,
        IReadOnlyCollection<string> Roles) : ICurrentUser
    {
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
        public string? UserName => "phase10d-smoke";
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CommandCountingInterceptor : DbCommandInterceptor
    {
        public int CommandCount { get; private set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CommandCount++;
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CommandCount++;
            return ValueTask.FromResult(result);
        }
    }
}
