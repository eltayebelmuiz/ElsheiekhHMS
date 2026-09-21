using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ElsheiekhHMS.Infrastructure.Auditing;

public sealed class EntityAuditSaveChangesInterceptor(
    ICurrentUser currentUser,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> AuditPropertyNames =
    [
        nameof(AuditableEntity.CreatedAt),
        nameof(AuditableEntity.CreatedBy),
        nameof(AuditableEntity.UpdatedAt),
        nameof(AuditableEntity.UpdatedBy),
        nameof(SoftDeletableEntity.IsDeleted),
        nameof(SoftDeletableEntity.DeletedAt),
        nameof(SoftDeletableEntity.DeletedBy)
    ];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditMetadata(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditMetadata(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditMetadata(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actor = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ApplyCreation(entry, now, actor);
                    break;
                case EntityState.Modified:
                    ApplyUpdate(entry, now, actor);
                    break;
            }
        }
    }

    private static void ApplyCreation(
        EntityEntry<AuditableEntity> entry,
        DateTimeOffset now,
        string? actor)
    {
        Set(entry, nameof(AuditableEntity.CreatedAt), now, modified: false);
        Set(entry, nameof(AuditableEntity.CreatedBy), actor, modified: false);
        Set(entry, nameof(AuditableEntity.UpdatedAt), null, modified: false);
        Set(entry, nameof(AuditableEntity.UpdatedBy), null, modified: false);

        if (entry.Entity is SoftDeletableEntity)
        {
            Set(entry, nameof(SoftDeletableEntity.DeletedAt), null, modified: false);
            Set(entry, nameof(SoftDeletableEntity.DeletedBy), null, modified: false);
        }
    }

    private static void ApplyUpdate(
        EntityEntry<AuditableEntity> entry,
        DateTimeOffset now,
        string? actor)
    {
        RestoreOriginal(entry, nameof(AuditableEntity.CreatedAt));
        RestoreOriginal(entry, nameof(AuditableEntity.CreatedBy));

        var businessChanged = entry.Properties.Any(property =>
            property.IsModified && !AuditPropertyNames.Contains(property.Metadata.Name));

        if (entry.Entity is SoftDeletableEntity)
        {
            var deleted = entry.Property(nameof(SoftDeletableEntity.IsDeleted));
            var wasDeleted = (bool)(deleted.OriginalValue ?? false);
            var isDeleted = (bool)(deleted.CurrentValue ?? false);

            if (!wasDeleted && isDeleted)
            {
                Set(entry, nameof(SoftDeletableEntity.DeletedAt), now, modified: true);
                Set(entry, nameof(SoftDeletableEntity.DeletedBy), actor, modified: true);
                businessChanged = true;
            }
            else if (wasDeleted && !isDeleted)
            {
                Set(entry, nameof(SoftDeletableEntity.DeletedAt), null, modified: true);
                Set(entry, nameof(SoftDeletableEntity.DeletedBy), null, modified: true);
                businessChanged = true;
            }
            else
            {
                RestoreOriginal(entry, nameof(SoftDeletableEntity.DeletedAt));
                RestoreOriginal(entry, nameof(SoftDeletableEntity.DeletedBy));
            }
        }

        if (businessChanged)
        {
            Set(entry, nameof(AuditableEntity.UpdatedAt), now, modified: true);
            Set(entry, nameof(AuditableEntity.UpdatedBy), actor, modified: true);
        }
        else
        {
            RestoreOriginal(entry, nameof(AuditableEntity.UpdatedAt));
            RestoreOriginal(entry, nameof(AuditableEntity.UpdatedBy));
        }
    }

    private static void RestoreOriginal(EntityEntry<AuditableEntity> entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        property.CurrentValue = property.OriginalValue;
        property.IsModified = false;
    }

    private static void Set(
        EntityEntry<AuditableEntity> entry,
        string propertyName,
        object? value,
        bool modified)
    {
        var property = entry.Property(propertyName);
        property.CurrentValue = value;
        property.IsModified = modified;
    }
}
