using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Identity.Configurations;

public sealed class DoctorApplicationUserLinkConfiguration
    : IEntityTypeConfiguration<DoctorApplicationUserLink>
{
    public void Configure(EntityTypeBuilder<DoctorApplicationUserLink> builder)
    {
        builder.ToTable("DoctorApplicationUserLinks", "dbo");
        builder.HasKey(link => link.DoctorId);
        builder.Property(link => link.DoctorId).IsRequired();
        builder.Property(link => link.UserId)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(450);

        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(link => link.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(link => link.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => link.UserId)
            .IsUnique()
            .HasDatabaseName("UX_DoctorApplicationUserLinks_UserId");
    }
}
