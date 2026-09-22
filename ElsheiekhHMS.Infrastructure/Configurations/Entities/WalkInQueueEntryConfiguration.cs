using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Configurations.Entities;

public sealed class WalkInQueueEntryConfiguration : IEntityTypeConfiguration<WalkInQueueEntry>
{
    public void Configure(EntityTypeBuilder<WalkInQueueEntry> builder)
    {
        builder.ToTable("WalkInQueueEntries", "dbo");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedOnAdd();

        builder.Property(entry => entry.PatientId).IsRequired();
        builder.Property(entry => entry.DepartmentId).IsRequired();
        builder.Property(entry => entry.AppointmentId).IsRequired(false);
        builder.Property(entry => entry.DoctorId).IsRequired(false);
        builder.Property(entry => entry.QueueDate)
            .IsRequired()
            .HasColumnType("date");
        builder.Property(entry => entry.SequenceNumber).IsRequired();
        builder.Property(entry => entry.QueueNumber)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(5);
        builder.Property(entry => entry.Priority)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(entry => entry.Status)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(entry => entry.Notes)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(2000);
        builder.Property(entry => entry.RegisteredAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(entry => entry.CalledAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(entry => entry.CompletedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(entry => entry.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(entry => entry.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Appointment>()
            .WithMany()
            .HasForeignKey(entry => entry.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(entry => entry.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => entry.PatientId);
        builder.HasIndex(entry => entry.DoctorId);
        builder.HasIndex(entry => entry.AppointmentId)
            .IsUnique()
            .HasFilter("[AppointmentId] IS NOT NULL")
            .HasDatabaseName("UX_WalkInQueueEntries_AppointmentId");
        builder.HasIndex(entry => new
        {
            entry.DepartmentId,
            entry.QueueDate,
            entry.Status,
            entry.Priority,
            entry.RegisteredAt
        });
        builder.HasIndex(entry => new
        {
            entry.QueueDate,
            entry.RegisteredAt
        });
        builder.HasIndex(entry => new { entry.QueueDate, entry.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("UX_WalkInQueueEntries_QueueDate_SequenceNumber");

        builder.HasQueryFilter(entry => !entry.IsDeleted);
        builder.Property(entry => entry.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        ConfigureAuditAndDeletion(builder);
    }

    private static void ConfigureAuditAndDeletion(EntityTypeBuilder<WalkInQueueEntry> builder)
    {
        builder.Property(entry => entry.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(entry => entry.CreatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(entry => entry.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(entry => entry.UpdatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(entry => entry.IsDeleted).IsRequired();
        builder.Property(entry => entry.DeletedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(entry => entry.DeletedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
    }
}
