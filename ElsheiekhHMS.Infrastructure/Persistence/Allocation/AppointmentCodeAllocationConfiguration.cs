using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class AppointmentCodeAllocationConfiguration
    : IEntityTypeConfiguration<AppointmentCodeAllocation>
{
    public void Configure(EntityTypeBuilder<AppointmentCodeAllocation> builder)
    {
        builder.ToTable("AppointmentCodeAllocations", "dbo", table =>
            table.HasCheckConstraint(
                "CK_AppointmentCodeAllocations_NextSequenceNumber",
                "[NextSequenceNumber] >= 1"));
        builder.HasKey(allocation => allocation.AllocationYear);
        builder.Property(allocation => allocation.AllocationYear)
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(allocation => allocation.NextSequenceNumber)
            .ValueGeneratedNever()
            .IsRequired();
    }
}
