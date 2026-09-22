using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Configurations.Entities;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", "dbo");
        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.Id).ValueGeneratedOnAdd();

        builder.Property(appointment => appointment.AppointmentCode)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(64);
        builder.Property(appointment => appointment.PatientId).IsRequired();
        builder.Property(appointment => appointment.DoctorId).IsRequired();
        builder.Property(appointment => appointment.DepartmentId).IsRequired();
        builder.Property(appointment => appointment.ScheduledDate)
            .IsRequired()
            .HasColumnType("date");
        builder.Property(appointment => appointment.ScheduledTime)
            .IsRequired()
            .HasColumnType("time(0)");
        builder.Property(appointment => appointment.Type)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(appointment => appointment.Status)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(appointment => appointment.Notes)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(2000);
        builder.Property(appointment => appointment.CancellationReason)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(1000);
        builder.Property(appointment => appointment.CancelledAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");

        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(appointment => appointment.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(appointment => appointment.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(appointment => appointment.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(appointment => new
        {
            appointment.DoctorId,
            appointment.ScheduledDate,
            appointment.ScheduledTime,
            appointment.Status
        });
        builder.HasIndex(appointment => new
        {
            appointment.PatientId,
            appointment.ScheduledDate
        });
        builder.HasIndex(appointment => appointment.DepartmentId);
        builder.HasIndex(appointment => appointment.AppointmentCode)
            .IsUnique()
            .HasDatabaseName("UX_Appointments_AppointmentCode");

        builder.HasQueryFilter(appointment => !appointment.IsDeleted);
        builder.Property(appointment => appointment.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        ConfigureAuditAndDeletion(builder);
    }

    private static void ConfigureAuditAndDeletion(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(appointment => appointment.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(appointment => appointment.CreatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(appointment => appointment.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(appointment => appointment.UpdatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(appointment => appointment.IsDeleted).IsRequired();
        builder.Property(appointment => appointment.DeletedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(appointment => appointment.DeletedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
    }
}
