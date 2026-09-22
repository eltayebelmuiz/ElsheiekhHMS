using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class AppointmentCodeAllocatorSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Allocates_sequential_codes_within_the_utc_year()
    {
        var time = new FixedTimeProvider(new DateTimeOffset(2040, 4, 5, 10, 0, 0, TimeSpan.Zero));
        await using var firstContext = fixture.CreateContext();
        var first = await new AppointmentCodeAllocator(firstContext, time).AllocateAsync();

        await using var secondContext = fixture.CreateContext();
        var second = await new AppointmentCodeAllocator(secondContext, time).AllocateAsync();

        Assert.Equal("AP-2040-00001", first);
        Assert.Equal("AP-2040-00002", second);
    }

    [Fact]
    public async Task A_new_utc_year_starts_at_sequence_one_and_previous_year_persists()
    {
        await using var firstContext = fixture.CreateContext();
        var first = await new AppointmentCodeAllocator(
            firstContext,
            new FixedTimeProvider(new DateTimeOffset(2041, 1, 1, 0, 0, 0, TimeSpan.Zero)))
            .AllocateAsync();

        await using var secondContext = fixture.CreateContext();
        var second = await new AppointmentCodeAllocator(
            secondContext,
            new FixedTimeProvider(new DateTimeOffset(2042, 1, 1, 0, 0, 0, TimeSpan.Zero)))
            .AllocateAsync();

        Assert.Equal("AP-2041-00001", first);
        Assert.Equal("AP-2042-00001", second);
    }

    [Fact]
    public async Task Concurrent_contexts_receive_distinct_codes()
    {
        var time = new FixedTimeProvider(new DateTimeOffset(2043, 6, 1, 0, 0, 0, TimeSpan.Zero));
        var tasks = Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var context = fixture.CreateContext();
            return await new AppointmentCodeAllocator(context, time).AllocateAsync();
        });

        var codes = await Task.WhenAll(tasks);

        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            Enumerable.Range(1, codes.Length)
                .Select(sequence => AppointmentCodeAllocator.Format(2043, sequence)),
            codes.OrderBy(code => code, StringComparer.Ordinal));
    }

    [Fact]
    public async Task Cancellation_before_allocation_does_not_create_a_row()
    {
        var time = new FixedTimeProvider(new DateTimeOffset(2044, 1, 1, 0, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext();
        var allocator = new AppointmentCodeAllocator(context, time);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            allocator.AllocateAsync(cancellation.Token));

        Assert.False(await context.AppointmentCodeAllocations
            .AnyAsync(allocation => allocation.AllocationYear == 2044));
    }

    [Fact]
    public async Task Duplicate_appointment_code_is_rejected_by_the_database()
    {
        var now = new DateTimeOffset(2045, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await using var context = fixture.CreateContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var department = new Department($"Allocator-{suffix}", null, null, now, "allocator-test");
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        var doctor = new Doctor($"DR-{suffix}", "Allocator Doctor", "General", true, 0, department.Id, now, "allocator-test");
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();

        var patient = new Patient(
            "PT-2045-00001",
            "Allocator",
            null,
            null,
            "Patient",
            new DateOnly(1990, 1, 1),
            Gender.Female,
            BloodGroup.OPositive,
            null,
            null,
            $"0900{suffix}",
            "Test address",
            null,
            null,
            null,
            null,
            null,
            new DateOnly(2045, 1, 1),
            now,
            "allocator-test");
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var first = new Appointment(
            "AP-2045-00001", patient.Id, doctor.Id, department.Id,
            new DateOnly(2045, 2, 1), new TimeOnly(9, 0), AppointmentType.General,
            null, now, "allocator-test");
        context.Appointments.Add(first);
        await context.SaveChangesAsync();

        var duplicate = new Appointment(
            "AP-2045-00001", patient.Id, doctor.Id, department.Id,
            new DateOnly(2045, 2, 2), new TimeOnly(9, 0), AppointmentType.General,
            null, now, "allocator-test");
        context.Appointments.Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
