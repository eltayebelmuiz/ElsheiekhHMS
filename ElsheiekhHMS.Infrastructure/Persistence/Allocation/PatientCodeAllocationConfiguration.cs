using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class PatientCodeAllocationConfiguration : IEntityTypeConfiguration<PatientCodeAllocation>
{
    public void Configure(EntityTypeBuilder<PatientCodeAllocation> builder)
    {
        builder.ToTable("PatientCodeAllocations", "dbo", table =>
            table.HasCheckConstraint(
                "CK_PatientCodeAllocations_NextSequenceNumber",
                "[NextSequenceNumber] >= 1 AND [NextSequenceNumber] <= 100000"));
        builder.HasKey(allocation => allocation.CodeYear);
        builder.Property(allocation => allocation.CodeYear)
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(allocation => allocation.NextSequenceNumber)
            .ValueGeneratedNever()
            .IsRequired();
    }
}
