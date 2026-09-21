using ElsheiekhHMS.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ElsheiekhHMS.Infrastructure.Identity.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.DisplayName)
            .IsRequired()
            .IsUnicode()
            .HasMaxLength(200);
        builder.Property(user => user.SecurityState)
            .IsRequired()
            .HasColumnType("tinyint")
            .HasDefaultValue(AccountSecurityState.Active);
        builder.Property(user => user.LoginAllowed)
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(true);
        builder.Property(user => user.CreatedAt)
            .IsRequired()
            .HasColumnType("datetimeoffset(7)");
        builder.Property(user => user.DisabledAt)
            .IsRequired(false)
            .HasColumnType("datetimeoffset(7)");
        builder.Property(user => user.DisabledBy)
            .IsRequired(false)
            .IsUnicode()
            .HasMaxLength(200);
    }
}
