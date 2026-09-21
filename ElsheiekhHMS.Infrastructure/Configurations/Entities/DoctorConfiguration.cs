using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Configurations.Entities;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors", "dbo");
        builder.HasKey(doctor => doctor.Id);
        builder.Property(doctor => doctor.Id).ValueGeneratedOnAdd();

        builder.Property(doctor => doctor.DoctorCode)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(32);
        builder.Property(doctor => doctor.FullName)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(doctor => doctor.Specialization)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(doctor => doctor.IsGeneralPractitioner).IsRequired();
        builder.Property(doctor => doctor.ConsultationFee)
            .IsRequired()
            .HasPrecision(12, 3);
        builder.Property(doctor => doctor.Status)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(doctor => doctor.DepartmentId).IsRequired();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(doctor => doctor.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(doctor => doctor.Schedules)
            .WithOne()
            .HasForeignKey("DoctorId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(doctor => doctor.Schedules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany<Appointment>()
            .WithOne()
            .HasForeignKey(appointment => appointment.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<WalkInQueueEntry>()
            .WithOne()
            .HasForeignKey(entry => entry.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(doctor => doctor.DepartmentId);

        ConfigureAuditAndDeletion(builder);
    }

    private static void ConfigureAuditAndDeletion(EntityTypeBuilder<Doctor> builder)
    {
        builder.Property(doctor => doctor.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(doctor => doctor.CreatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(doctor => doctor.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(doctor => doctor.UpdatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(doctor => doctor.IsDeleted).IsRequired();
        builder.Property(doctor => doctor.DeletedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(doctor => doctor.DeletedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
    }
}
