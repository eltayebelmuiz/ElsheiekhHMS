using ElsheiekhHMS.Core.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Configurations.Entities;

public sealed class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules", "dbo");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Id).ValueGeneratedOnAdd();
        builder.Property<int>("DoctorId").IsRequired();

        builder.Property(schedule => schedule.DayOfWeek)
            .IsRequired()
            .HasConversion<int>();
        builder.Property(schedule => schedule.StartTime)
            .IsRequired()
            .HasColumnType("time(0)");
        builder.Property(schedule => schedule.EndTime)
            .IsRequired()
            .HasColumnType("time(0)");
        builder.Property(schedule => schedule.SlotDurationMinutes)
            .IsRequired();
        builder.Property(schedule => schedule.IsActive).IsRequired();

        builder.Property(schedule => schedule.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(schedule => schedule.CreatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(schedule => schedule.UpdatedAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(schedule => schedule.UpdatedBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);

        builder.HasIndex("DoctorId", nameof(DoctorSchedule.DayOfWeek), nameof(DoctorSchedule.StartTime));
    }
}
