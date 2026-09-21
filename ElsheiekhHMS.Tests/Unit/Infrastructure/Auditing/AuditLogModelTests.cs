using ElsheiekhHMS.Infrastructure.Auditing.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Auditing;

public sealed class AuditLogModelTests
{
    private static readonly DbContextOptions<ElsheiekhHmsDbContext> Options =
        new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_ModelOnly")
            .Options;

    [Fact]
    public void AuditLog_is_not_an_entity_lifecycle_type()
    {
        using var context = new ElsheiekhHmsDbContext(Options);
        var entity = context.Model.FindEntityType(typeof(AuditLog));

        Assert.NotNull(entity);
        Assert.Equal(typeof(object), typeof(AuditLog).BaseType);
        Assert.DoesNotContain("CreatedAt", entity!.GetProperties().Select(property => property.Name));
        Assert.DoesNotContain("IsDeleted", entity.GetProperties().Select(property => property.Name));
        Assert.Empty(entity.GetNavigations());
        Assert.All(typeof(AuditLog).GetProperties(), property =>
            Assert.False(property.SetMethod?.IsPublic == true));
    }

    [Fact]
    public void AuditLog_has_bounded_columns_and_expected_indexes()
    {
        using var context = new ElsheiekhHmsDbContext(Options);
        var entity = context.Model.FindEntityType(typeof(AuditLog))!;

        Assert.False(entity.FindProperty(nameof(AuditLog.OccurredAtUtc))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(AuditLog.ActorKind))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(AuditLog.Category))!.IsNullable);
        Assert.False(entity.FindProperty(nameof(AuditLog.Action))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(AuditLog.ActorUserId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(AuditLog.MetadataJson))!.IsNullable);
        Assert.Equal(64, entity.FindProperty(nameof(AuditLog.Action))!.GetMaxLength());
        Assert.Equal(32, entity.FindProperty(nameof(AuditLog.Category))!.GetMaxLength());
        Assert.Equal(1000, entity.FindProperty(nameof(AuditLog.Reason))!.GetMaxLength());
        Assert.Equal(4000, entity.FindProperty(nameof(AuditLog.MetadataJson))!.GetMaxLength());
        Assert.Equal(128, entity.FindProperty(nameof(AuditLog.CorrelationId))!.GetMaxLength());

        var indexes = entity.GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            [
                "Action,OccurredAtUtc",
                "ActorUserId,OccurredAtUtc",
                "OccurredAtUtc",
                "TargetType,TargetId,OccurredAtUtc"
            ],
            indexes.OrderBy(value => value, StringComparer.Ordinal));
    }

    [Fact]
    public void AuditLog_key_is_long_and_no_relationships_exist()
    {
        using var context = new ElsheiekhHmsDbContext(Options);
        var entity = context.Model.FindEntityType(typeof(AuditLog))!;

        Assert.Equal(typeof(long), entity.FindProperty(nameof(AuditLog.Id))!.ClrType);
        Assert.Empty(entity.GetForeignKeys());
    }
}
