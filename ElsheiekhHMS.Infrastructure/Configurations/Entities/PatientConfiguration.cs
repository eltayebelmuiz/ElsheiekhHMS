using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Configurations.Entities;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", "dbo");
        builder.HasKey(patient => patient.Id);
        builder.Property(patient => patient.Id).ValueGeneratedOnAdd();

        builder.Property(patient => patient.PatientCode)
            .HasField("<PatientCode>k__BackingField")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(13);
        builder.Property(patient => patient.FirstName)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(100);
        builder.Property(patient => patient.MiddleName)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(100);
        builder.Property(patient => patient.ThirdName)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(100);
        builder.Property(patient => patient.LastName)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(100);
        builder.Ignore(patient => patient.FullName);
        builder.Ignore(patient => patient.ShortName);
        builder.Property(patient => patient.DateOfBirth)
            .IsRequired()
            .HasColumnType("date");
        builder.Property(patient => patient.Gender)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(patient => patient.BloodGroup)
            .IsRequired(false)
            .HasConversion<int?>();
        builder.Property(patient => patient.NationalId)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(100);
        builder.Property(patient => patient.PassportNumber)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(100);
        builder.Property(patient => patient.Phone)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(32);
        builder.Property(patient => patient.Address)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(500);
        builder.Property(patient => patient.City)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(100);
        builder.Property(patient => patient.EmergencyContactName)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(patient => patient.EmergencyContactPhone)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(32);
        builder.Property(patient => patient.EmergencyContactRelationship)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(100);
        builder.Property(patient => patient.InsuranceProvider)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);

        builder.HasMany<Appointment>()
            .WithOne()
            .HasForeignKey(appointment => appointment.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany<WalkInQueueEntry>()
            .WithOne()
            .HasForeignKey(entry => entry.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(patient => patient.PatientCode).IsUnique();
        builder.HasIndex(patient => patient.NationalId)
            .IsUnique()
            .HasFilter("[NationalId] IS NOT NULL");
        builder.HasIndex(patient => patient.PassportNumber)
            .IsUnique()
            .HasFilter("[PassportNumber] IS NOT NULL");
        builder.HasIndex(patient => patient.Phone);

        builder.HasQueryFilter(patient => !patient.IsDeleted);
        builder.Property(patient => patient.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        ConfigureAuditAndDeletion(builder);
    }

    private static void ConfigureAuditAndDeletion(EntityTypeBuilder<Patient> builder)
    {
        builder.Property(patient => patient.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(patient => patient.CreatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(patient => patient.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(patient => patient.UpdatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(patient => patient.IsDeleted).IsRequired();
        builder.Property(patient => patient.DeletedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(patient => patient.DeletedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
    }
}
