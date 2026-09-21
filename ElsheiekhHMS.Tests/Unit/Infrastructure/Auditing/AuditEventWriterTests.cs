using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Infrastructure.Auditing.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Auditing;

public sealed class AuditEventWriterTests
{
    [Fact]
    public async Task Writer_stages_server_actor_time_and_safe_event_data_without_saving()
    {
        var occurredAt = new DateTimeOffset(2026, 9, 21, 12, 30, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_WriterOnly")
            .Options;
        await using var context = new ElsheiekhHmsDbContext(options);
        var writer = new AuditEventWriter(
            context,
            new TestCurrentUser("stable-user", "mutable-name"),
            new FixedTimeProvider(occurredAt));

        await writer.RecordAsync(new AuditEventRequest(
            AuditCategories.Security,
            AuditActions.AccountSuspended,
            "ApplicationUser",
            "user-42",
            "Approved administrative action",
            new Dictionary<string, string?> { ["previousState"] = "Active" }));

        var entry = Assert.Single(context.ChangeTracker.Entries<AuditLog>());
        Assert.Equal(EntityState.Added, entry.State);
        Assert.Equal("stable-user", entry.Entity.ActorUserId);
        Assert.Equal("mutable-name", entry.Entity.ActorUserNameSnapshot);
        Assert.Equal(occurredAt, entry.Entity.OccurredAtUtc);
        Assert.Equal(AuditActorKinds.Human, entry.Entity.ActorKind);
        Assert.Equal(AuditCategories.Security, entry.Entity.Category);
        Assert.Equal(AuditActions.AccountSuspended, entry.Entity.Action);
        Assert.Equal("ApplicationUser", entry.Entity.TargetType);
        Assert.Equal("user-42", entry.Entity.TargetId);
        Assert.Contains("previousState", entry.Entity.MetadataJson);
        Assert.Equal(0, context.ChangeTracker.Entries().Count(entry => entry.State != EntityState.Added));
    }

    [Fact]
    public async Task Writer_does_not_allow_caller_actor_or_timestamp_values()
    {
        var occurredAt = new DateTimeOffset(2026, 9, 21, 12, 30, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_WriterOnly")
            .Options;
        await using var context = new ElsheiekhHmsDbContext(options);
        var writer = new AuditEventWriter(
            context,
            new TestCurrentUser("server-user", "server-name"),
            new FixedTimeProvider(occurredAt));

        await writer.RecordAsync(new AuditEventRequest(
            AuditCategories.Identity,
            AuditActions.UsernameChanged,
            "ApplicationUser",
            "user-42",
            Metadata: null));

        var entity = Assert.Single(context.ChangeTracker.Entries<AuditLog>()).Entity;
        Assert.Equal("server-user", entity.ActorUserId);
        Assert.NotEqual("spoofed-user", entity.ActorUserId);
        Assert.Equal(occurredAt, entity.OccurredAtUtc);
    }

    [Fact]
    public async Task Writer_rejects_prohibited_metadata_and_unbounded_values()
    {
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_WriterOnly")
            .Options;
        await using var context = new ElsheiekhHmsDbContext(options);
        var writer = new AuditEventWriter(
            context,
            new TestCurrentUser("server-user", "server-name"),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ArgumentException>(() => writer.RecordAsync(new AuditEventRequest(
            AuditCategories.Identity,
            AuditActions.PasswordResetByAdmin,
            Metadata: new Dictionary<string, string?> { ["password"] = "never-store" })));

        await Assert.ThrowsAsync<ArgumentException>(() => writer.RecordAsync(new AuditEventRequest(
            AuditCategories.Identity,
            AuditActions.UsernameChanged,
            Metadata: new Dictionary<string, string?> { ["context"] = new string('x', 5000) })));
    }

    [Fact]
    public async Task Writer_preserves_anonymous_semantics_without_inventing_an_actor()
    {
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_WriterOnly")
            .Options;
        await using var context = new ElsheiekhHmsDbContext(options);
        var writer = new AuditEventWriter(
            context,
            new TestCurrentUser(null, null, isAuthenticated: false),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        await writer.RecordAsync(new AuditEventRequest(
            AuditCategories.Security,
            AuditActions.AccountLoginBlocked));

        var entity = Assert.Single(context.ChangeTracker.Entries<AuditLog>()).Entity;
        Assert.Equal(AuditActorKinds.Anonymous, entity.ActorKind);
        Assert.Null(entity.ActorUserId);
    }

    [Fact]
    public async Task Writer_preserves_explicit_system_actor_semantics()
    {
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_WriterOnly")
            .Options;
        await using var context = new ElsheiekhHmsDbContext(options);
        var writer = new AuditEventWriter(
            context,
            new TestCurrentUser("system", null, isAuthenticated: false),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        await writer.RecordAsync(new AuditEventRequest(
            AuditCategories.Administration,
            AuditActions.SessionsRevoked));

        var entity = Assert.Single(context.ChangeTracker.Entries<AuditLog>()).Entity;
        Assert.Equal(AuditActorKinds.System, entity.ActorKind);
        Assert.Null(entity.ActorUserId);
    }

    [Fact]
    public async Task Writer_uses_existing_activity_for_correlation_without_http_context()
    {
        using var activity = new Activity("audit-test").Start();
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_WriterOnly")
            .Options;
        await using var context = new ElsheiekhHmsDbContext(options);
        var writer = new AuditEventWriter(
            context,
            new TestCurrentUser("server-user", "server-name"),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        await writer.RecordAsync(new AuditEventRequest(
            AuditCategories.Security,
            AuditActions.SessionsRevoked));

        var entity = Assert.Single(context.ChangeTracker.Entries<AuditLog>()).Entity;
        Assert.Equal(activity.Id, entity.CorrelationId);
    }

    [Fact]
    public async Task Writer_honors_cancellation_before_staging()
    {
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_WriterOnly")
            .Options;
        await using var context = new ElsheiekhHmsDbContext(options);
        var writer = new AuditEventWriter(
            context,
            new TestCurrentUser("server-user", "server-name"),
            new FixedTimeProvider(DateTimeOffset.UtcNow));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => writer.RecordAsync(
            new AuditEventRequest(AuditCategories.Security, AuditActions.SessionsRevoked),
            cancellation.Token));
        Assert.Empty(context.ChangeTracker.Entries<AuditLog>());
    }

    private sealed class TestCurrentUser(
        string? userId,
        string? userName,
        bool isAuthenticated = true) : ICurrentUser
    {
        public bool IsAuthenticated => isAuthenticated;
        public string? UserId { get; } = userId;
        public string? UserName { get; } = userName;
        public IReadOnlyCollection<string> Roles => [];
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
