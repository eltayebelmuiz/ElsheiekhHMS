using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class QueueTicketAllocationConfiguration : IEntityTypeConfiguration<QueueTicketAllocation>
{
    public void Configure(EntityTypeBuilder<QueueTicketAllocation> builder)
    {
        builder.ToTable("QueueTicketAllocations", "dbo", table =>
            table.HasCheckConstraint(
                "CK_QueueTicketAllocations_NextSequenceNumber",
                "[NextSequenceNumber] >= 1 AND [NextSequenceNumber] <= 1000"));
        builder.HasKey(allocation => allocation.QueueDate);
        builder.Property(allocation => allocation.QueueDate)
            .HasColumnType("date")
            .ValueGeneratedNever()
            .IsRequired();
        builder.Property(allocation => allocation.NextSequenceNumber)
            .ValueGeneratedNever()
            .IsRequired();
    }
}
