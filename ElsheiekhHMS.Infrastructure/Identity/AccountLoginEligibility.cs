using ElsheiekhHMS.Infrastructure.Identity.Entities;

namespace ElsheiekhHMS.Infrastructure.Identity;

/// <summary>
/// Centralizes the account restrictions that apply before authorization policies.
/// Identity lockout remains a sign-in restriction and does not revoke an existing
/// session by itself.
/// </summary>
public sealed class AccountLoginEligibility
{
    public bool CanEstablishSession(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return CanContinueSession(user)
            && (!user.LockoutEnabled || user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow);
    }

    public bool CanContinueSession(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.LoginAllowed && user.SecurityState == AccountSecurityState.Active;
    }
}
