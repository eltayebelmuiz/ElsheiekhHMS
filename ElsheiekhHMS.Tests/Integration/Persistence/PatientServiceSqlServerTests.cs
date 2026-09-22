using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Patients;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using ElsheiekhHMS.Infrastructure.Persistence.Patients;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class PatientServiceSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Registration_persists_patient_and_audit_in_one_save()
    {
        var currentUser = new TestCurrentUser("patient-service-user", [RoleNames.Administrator]);
        var now = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext(currentUser, now);
        var service = CreateService(context, currentUser, now);

        var result = await service.RegisterAsync(Request("0900" + Guid.NewGuid().ToString("N")[..6]));

        Assert.True(result.IsSuccess);
        Assert.StartsWith("PT-2026-", result.Value!.PatientCode, StringComparison.Ordinal);
        Assert.True(result.Value.Id > 0);
        Assert.True(await context.AuditLogs.AnyAsync(log =>
            log.Action == AuditActions.PatientRegistered &&
            log.TargetType == "Patient" &&
            log.TargetId == result.Value.PatientCode));
    }

    [Fact]
    public async Task Registration_allows_duplicate_phone_numbers()
    {
        var phone = "0900" + Guid.NewGuid().ToString("N")[..6];
        var currentUser = new TestCurrentUser("patient-service-user", [RoleNames.Receptionist]);
        var now = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext(currentUser, now);
        var service = CreateService(context, currentUser, now);

        var first = await service.RegisterAsync(Request(phone));
        var second = await service.RegisterAsync(Request(phone));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, await context.Patients.CountAsync(patient => patient.Phone == phone));
    }

    [Fact]
    public async Task Search_returns_bounded_projected_results()
    {
        var currentUser = new TestCurrentUser("patient-service-user", [RoleNames.Administrator]);
        var now = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext(currentUser, now);
        var service = CreateService(context, currentUser, now);
        var phone = "0900" + Guid.NewGuid().ToString("N")[..6];
        var registration = await service.RegisterAsync(Request(phone));
        Assert.True(registration.IsSuccess);

        var result = await service.SearchAsync(new PatientSearchRequest(phone: phone));

        Assert.True(result.IsSuccess);
        var page = result.Value!;
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(registration.Value!.PatientCode, page.Items[0].PatientCode);
    }

    private PatientService CreateService(
        ElsheiekhHmsDbContext context,
        ICurrentUser currentUser,
        TimeProvider timeProvider) =>
        new(
            new PatientPersistence(context, new PatientCodeAllocator(context)),
            new AuditEventWriter(context, currentUser, timeProvider),
            currentUser,
            timeProvider);

    private static RegisterPatientRequest Request(string phone) =>
        new(
            "Amina", "M", null, "Hassan", new DateOnly(1990, 1, 1),
            Core.Domain.Patients.Enums.Gender.Female,
            Core.Domain.Patients.Enums.BloodGroup.OPositive,
            null, null, phone, "Main street", "City", null, null, null, null);

    private sealed record TestCurrentUser(
        string? UserId,
        IReadOnlyCollection<string> Roles) : ICurrentUser
    {
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
        public string? UserName => "operator";
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
