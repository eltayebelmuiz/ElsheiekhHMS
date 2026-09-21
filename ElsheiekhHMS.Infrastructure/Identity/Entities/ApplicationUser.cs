using Microsoft.AspNetCore.Identity;

namespace ElsheiekhHMS.Infrastructure.Identity.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = null!;
    public AccountSecurityState SecurityState { get; set; } = AccountSecurityState.Active;
    public bool LoginAllowed { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
    public string? DisabledBy { get; set; }
}
