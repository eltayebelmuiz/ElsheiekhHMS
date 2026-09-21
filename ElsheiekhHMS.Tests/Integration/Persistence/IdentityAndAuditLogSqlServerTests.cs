using System.Data.Common;
using System.Diagnostics;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Infrastructure.Auditing.Entities;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class IdentityAndAuditLogSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Identity_and_audit_tables_exist_after_migrations()
    {
        await using var context = fixture.CreateContext();

        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "AspNetUsers",
            "AspNetRoles",
            "AspNetUserRoles",
            "AspNetUserClaims",
            "AspNetUserLogins",
            "AspNetUserTokens",
            "AspNetRoleClaims",
            "AuditLogs"
        };

        var actual = await ReadTableNamesAsync(context, expected);

        Assert.True(expected.SetEquals(actual));
    }

    [Fact]
    public async Task AuditLog_writer_persists_approved_fields_and_server_context()
    {
        var occurredAt = Utc(40);
        var correlationId = string.Empty;
        var currentUser = new TestCurrentUser("audit-user", "audit-name", ["Administrator"]);
        long auditId;

        await using (var context = fixture.CreateContext())
        {
            var writer = new AuditEventWriter(context, currentUser, new FixedTimeProvider(occurredAt));
            var activity = new Activity("audit-log-test").Start();
            try
            {
                await writer.RecordAsync(new AuditEventRequest(
                    AuditCategories.Security,
                    AuditActions.AccountSuspended,
                    "ApplicationUser",
                    "target-user",
                    "Administrative review",
                    new Dictionary<string, string?>
                    {
                        ["source"] = "integration-test",
                        ["reasonCode"] = "SECURITY_REVIEW"
                    }));

                await context.SaveChangesAsync();
                var entry = Assert.Single(context.AuditLogs.Local);
                auditId = entry.Id;
                correlationId = entry.CorrelationId!;
            }
            finally
            {
                activity?.Stop();
            }
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.AuditLogs.SingleAsync(log => log.Id == auditId);

        Assert.Equal(occurredAt, persisted.OccurredAtUtc);
        Assert.Equal(AuditActorKinds.Human, persisted.ActorKind);
        Assert.Equal("audit-user", persisted.ActorUserId);
        Assert.Equal("audit-name", persisted.ActorUserNameSnapshot);
        Assert.Equal(AuditCategories.Security, persisted.Category);
        Assert.Equal(AuditActions.AccountSuspended, persisted.Action);
        Assert.Equal("ApplicationUser", persisted.TargetType);
        Assert.Equal("target-user", persisted.TargetId);
        Assert.Equal("Administrative review", persisted.Reason);
        Assert.Contains("integration-test", persisted.MetadataJson, StringComparison.Ordinal);
        Assert.Equal(correlationId, persisted.CorrelationId);
        Assert.Null(typeof(AuditLog).GetProperty("Outcome"));
        Assert.Equal(0, await ReadScalarAsync(readContext,
            "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.AuditLogs') AND name = N'Outcome'"));
    }

    [Fact]
    public async Task AuditLog_transaction_rolls_back_without_leaving_a_row()
    {
        var correlationId = $"rollback-{Guid.NewGuid():N}";
        var request = new AuditEventRequest(
            AuditCategories.Business,
            AuditActions.RoleAssigned,
            "ApplicationUser",
            "rollback-user",
            Metadata: new Dictionary<string, string?> { ["operation"] = "rollback" });

        await using (var context = fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var writer = new AuditEventWriter(
                context,
                new TestCurrentUser("rollback-actor", "rollback-name", []),
                new FixedTimeProvider(Utc(41)));

            await writer.RecordAsync(request with { Metadata = new Dictionary<string, string?>
            {
                ["operation"] = "rollback",
                ["correlation"] = correlationId
            }});
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var readContext = fixture.CreateContext();
        Assert.False(await readContext.AuditLogs.AnyAsync(log => log.MetadataJson!.Contains(correlationId)));
    }

    [Fact]
    public async Task Protected_operation_and_audit_commit_together()
    {
        var departmentName = $"Audit transaction {Guid.NewGuid():N}";

        await using (var context = fixture.CreateContext())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            var department = new Department(departmentName, "Transaction test", null, Utc(42), "transaction-actor");
            context.Departments.Add(department);

            var writer = new AuditEventWriter(
                context,
                new TestCurrentUser("transaction-actor", "transaction-name", []),
                new FixedTimeProvider(Utc(42)));
            await writer.RecordAsync(new AuditEventRequest(
                AuditCategories.Administration,
                AuditActions.RoleAssigned,
                "Department",
                departmentName,
                "Same transaction"));

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var readContext = fixture.CreateContext();
        Assert.True(await readContext.Departments.AnyAsync(department => department.Name == departmentName));
        Assert.True(await readContext.AuditLogs.AnyAsync(log =>
            log.TargetType == "Department" && log.TargetId == departmentName));
    }

    [Fact]
    public async Task ApplicationUser_persists_security_state_and_login_control()
    {
        var normalizer = new UpperInvariantLookupNormalizer();
        var userName = $"staff-{Guid.NewGuid():N}";
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = userName,
            NormalizedUserName = normalizer.NormalizeName(userName),
            DisplayName = "Integration Staff",
            SecurityState = AccountSecurityState.Suspended,
            LoginAllowed = false,
            CreatedAt = Utc(43)
        };

        await using (var context = fixture.CreateContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Users.SingleAsync(item => item.Id == user.Id);
        Assert.Equal(userName, persisted.UserName);
        Assert.Equal(normalizer.NormalizeName(userName), persisted.NormalizedUserName);
        Assert.Equal(AccountSecurityState.Suspended, persisted.SecurityState);
        Assert.False(persisted.LoginAllowed);
        Assert.Equal(Utc(43), persisted.CreatedAt);
    }

    [Fact]
    public async Task Normalized_usernames_are_unique()
    {
        var normalizer = new UpperInvariantLookupNormalizer();
        var suffix = Guid.NewGuid().ToString("N");
        var first = CreateUser($"case-{suffix}", normalizer);
        var second = CreateUser($"CASE-{suffix}", normalizer);

        await using var context = fixture.CreateContext();
        context.Users.Add(first);
        await context.SaveChangesAsync();
        context.Users.Add(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Roles_and_user_role_assignment_persist()
    {
        var normalizer = new UpperInvariantLookupNormalizer();
        var suffix = Guid.NewGuid().ToString("N");
        var user = CreateUser($"role-user-{suffix}", normalizer);
        var roleName = $"IntegrationRole-{suffix}";
        var role = new IdentityRole
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = roleName,
            NormalizedName = normalizer.NormalizeName(roleName)
        };

        await using (var context = fixture.CreateContext())
        {
            context.Users.Add(user);
            context.Roles.Add(role);
            context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        Assert.True(await readContext.Roles.AnyAsync(item => item.Id == role.Id));
        Assert.True(await readContext.UserRoles.AnyAsync(item =>
            item.UserId == user.Id && item.RoleId == role.Id));
    }

    [Fact]
    public async Task No_default_privileged_user_is_created_by_migration()
    {
        await using var context = fixture.CreateContext();

        Assert.False(await context.Users.AnyAsync(user =>
            user.UserName == "SystemAdministrator" ||
            user.UserName == "Administrator" ||
            user.NormalizedUserName == "SYSTEMADMINISTRATOR" ||
            user.NormalizedUserName == "ADMINISTRATOR"));
    }

    private static ApplicationUser CreateUser(
        string userName,
        UpperInvariantLookupNormalizer normalizer) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            UserName = userName,
            NormalizedUserName = normalizer.NormalizeName(userName),
            DisplayName = "Integration User",
            CreatedAt = Utc(44)
        };

    private static async Task<HashSet<string>> ReadTableNamesAsync(
        DbContext context,
        IReadOnlySet<string> expected)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'";
        await using var reader = await command.ExecuteReaderAsync();
        var actual = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            var tableName = reader.GetString(0);
            if (expected.Contains(tableName))
            {
                actual.Add(tableName);
            }
        }

        return actual;
    }

    private static async Task<int> ReadScalarAsync(DbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static DateTimeOffset Utc(int second) =>
        new(2026, 1, 1, 12, 0, second, TimeSpan.Zero);

    private sealed record TestCurrentUser(
        string? UserId,
        string? UserName,
        IReadOnlyCollection<string> Roles) : ICurrentUser
    {
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
