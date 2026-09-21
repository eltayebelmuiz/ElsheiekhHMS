using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Configurations.Entities;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", "dbo");
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Id).ValueGeneratedOnAdd();

        builder.Property(department => department.Name)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(department => department.Description)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(2000);
        builder.Property(department => department.PhoneExtension)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(32);
        builder.Property(department => department.IsActive).IsRequired();

        builder.HasMany<Doctor>()
            .WithOne()
            .HasForeignKey(doctor => doctor.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<Appointment>()
            .WithOne()
            .HasForeignKey(appointment => appointment.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<WalkInQueueEntry>()
            .WithOne()
            .HasForeignKey(entry => entry.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Department> builder)
    {
        builder.Property(department => department.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(department => department.CreatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(department => department.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(department => department.UpdatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
    }
}
