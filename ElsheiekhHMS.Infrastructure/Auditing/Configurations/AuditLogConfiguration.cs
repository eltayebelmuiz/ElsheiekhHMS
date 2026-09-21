using ElsheiekhHMS.Infrastructure.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Auditing.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs", "dbo");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id)
            .ValueGeneratedOnAdd();
        builder.Property(log => log.OccurredAtUtc)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(log => log.ActorKind)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(16);
        builder.Property(log => log.ActorUserId)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(log => log.ActorUserNameSnapshot)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(log => log.Category)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(32);
        builder.Property(log => log.Action)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(64);
        builder.Property(log => log.TargetType)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(128);
        builder.Property(log => log.TargetId)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(log => log.Reason)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(1000);
        builder.Property(log => log.MetadataJson)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(4000);
        builder.Property(log => log.CorrelationId)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(128);

        builder.HasIndex(log => log.OccurredAtUtc)
            .HasDatabaseName("IX_AuditLogs_OccurredAtUtc");
        builder.HasIndex(log => new { log.ActorUserId, log.OccurredAtUtc })
            .HasDatabaseName("IX_AuditLogs_ActorUserId_OccurredAtUtc");
        builder.HasIndex(log => new { log.Action, log.OccurredAtUtc })
            .HasDatabaseName("IX_AuditLogs_Action_OccurredAtUtc");
        builder.HasIndex(log => new { log.TargetType, log.TargetId, log.OccurredAtUtc })
            .HasDatabaseName("IX_AuditLogs_TargetType_TargetId_OccurredAtUtc");
    }
}
