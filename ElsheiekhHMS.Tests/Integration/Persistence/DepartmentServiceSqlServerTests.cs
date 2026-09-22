using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Departments;
using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Infrastructure.Persistence.Departments;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class DepartmentServiceSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Create_persists_department_and_audit_in_one_save()
    {
        var currentUser = new TestCurrentUser("department-service-user", [RoleNames.SystemAdministrator]);
        var now = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext(currentUser, now);
        var service = CreateService(context, currentUser, now);
        var name = "Emergency " + Guid.NewGuid().ToString("N")[..8];

        var result = await service.CreateAsync(new CreateDepartmentRequest(name, "Care", "101"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Id > 0);
        Assert.True(await context.Departments.AnyAsync(department => department.Id == result.Value.Id));
        Assert.True(await context.AuditLogs.AnyAsync(log =>
            log.Action == AuditActions.DepartmentCreated &&
            log.TargetType == "Department" &&
            log.TargetId == name));
    }

    [Fact]
    public async Task Search_uses_server_filters_sorting_and_pagination_contract()
    {
        var currentUser = new TestCurrentUser("department-service-user", [RoleNames.SystemAdministrator]);
        var now = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext(currentUser, now);
        var service = CreateService(context, currentUser, now);
        var prefix = "Search " + Guid.NewGuid().ToString("N")[..8];
        var first = await service.CreateAsync(new CreateDepartmentRequest(prefix + " A", null, null));
        var second = await service.CreateAsync(new CreateDepartmentRequest(prefix + " B", null, null));
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var deactivated = await service.DeactivateAsync(second.Value!.Id);
        Assert.True(deactivated.IsSuccess);

        var result = await service.SearchAsync(new DepartmentSearchRequest(
            prefix, true, new(1, 1), DepartmentSortField.Name));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalCount);
        Assert.Single(result.Value.Items);
        Assert.Equal(first.Value!.Id, result.Value.Items[0].Id);
    }

    [Fact]
    public async Task Update_and_deactivate_preserve_department_history_and_audit()
    {
        var currentUser = new TestCurrentUser("department-service-user", [RoleNames.SystemAdministrator]);
        var now = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext(currentUser, now);
        var service = CreateService(context, currentUser, now);
        var name = "History " + Guid.NewGuid().ToString("N")[..8];
        var created = await service.CreateAsync(new CreateDepartmentRequest(name, "Original", null));
        Assert.True(created.IsSuccess);

        var updated = await service.UpdateAsync(
            created.Value!.Id,
            new UpdateDepartmentRequest(name + " Updated", "Changed", "205"));
        var deactivated = await service.DeactivateAsync(created.Value.Id);

        Assert.True(updated.IsSuccess);
        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value!.IsActive);
        Assert.Equal("Changed", deactivated.Value.Description);
        Assert.True(await context.AuditLogs.CountAsync(log =>
            log.TargetType == "Department" &&
            log.TargetId == created.Value.Id.ToString() &&
            (log.Action == AuditActions.DepartmentUpdated || log.Action == AuditActions.DepartmentDeactivated)) >= 2);
    }

    [Fact]
    public async Task Duplicate_department_names_remain_allowed_without_a_unique_model_rule()
    {
        var currentUser = new TestCurrentUser("department-service-user", [RoleNames.SystemAdministrator]);
        var now = new FixedTimeProvider(new DateTimeOffset(2026, 9, 22, 10, 0, 0, TimeSpan.Zero));
        await using var context = fixture.CreateContext(currentUser, now);
        var service = CreateService(context, currentUser, now);
        var name = "Shared " + Guid.NewGuid().ToString("N")[..8];

        var first = await service.CreateAsync(new CreateDepartmentRequest(name, null, null));
        var second = await service.CreateAsync(new CreateDepartmentRequest(name, null, null));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, await context.Departments.CountAsync(department => department.Name == name));
    }

    private static DepartmentService CreateService(
        ElsheiekhHmsDbContext context,
        ICurrentUser currentUser,
        TimeProvider timeProvider) =>
        new(
            new DepartmentPersistence(context),
            new AuditEventWriter(context, currentUser, timeProvider),
            currentUser,
            timeProvider);

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
