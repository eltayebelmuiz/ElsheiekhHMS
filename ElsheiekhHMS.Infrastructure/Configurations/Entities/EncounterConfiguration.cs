using ElsheiekhHMS.Core.Domain.Clinical.Entities;
using ElsheiekhHMS.Core.Domain.Clinical.Enums;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Configurations.Entities;

public sealed class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.ToTable("Encounters", "dbo");
        builder.HasKey(encounter => encounter.Id);
        builder.Property(encounter => encounter.Id).ValueGeneratedOnAdd();

        builder.Property(encounter => encounter.PatientId).IsRequired();
        builder.Property(encounter => encounter.DepartmentId).IsRequired();
        builder.Property(encounter => encounter.DoctorId).IsRequired();
        builder.Property(encounter => encounter.QueueEntryId).IsRequired();
        builder.Property(encounter => encounter.Status)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(encounter => encounter.StartedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(encounter => encounter.CompletedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(encounter => encounter.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Property(encounter => encounter.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(encounter => encounter.CreatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(encounter => encounter.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(encounter => encounter.UpdatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(encounter => encounter.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(encounter => encounter.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(encounter => encounter.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WalkInQueueEntry>()
            .WithMany()
            .HasForeignKey(encounter => encounter.QueueEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(encounter => new { encounter.PatientId, encounter.StartedAt });
        builder.HasIndex(encounter => new { encounter.DoctorId, encounter.StartedAt });
        builder.HasIndex(encounter => new { encounter.DepartmentId, encounter.StartedAt });
        builder.HasIndex(encounter => new { encounter.Status, encounter.StartedAt });
        builder.HasIndex(encounter => encounter.QueueEntryId)
            .IsUnique()
            .HasDatabaseName("UX_Encounters_QueueEntryId");
    }
}
