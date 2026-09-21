using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Identity;

public sealed class AccountLoginEligibilityTests
{
    [Fact]
    public void Active_allowed_user_can_establish_a_session()
    {
        var user = NewUser();

        Assert.True(new AccountLoginEligibility().CanEstablishSession(user));
    }

    [Theory]
    [InlineData(AccountSecurityState.Suspended)]
    [InlineData(AccountSecurityState.Banned)]
    public void Restricted_state_cannot_establish_a_session(AccountSecurityState state)
    {
        var user = NewUser();
        user.SecurityState = state;
        user.LoginAllowed = false;

        Assert.False(new AccountLoginEligibility().CanEstablishSession(user));
    }

    [Fact]
    public void Explicit_login_block_cannot_establish_a_session()
    {
        var user = NewUser();
        user.LoginAllowed = false;

        Assert.False(new AccountLoginEligibility().CanEstablishSession(user));
    }

    [Fact]
    public void Identity_lockout_is_distinct_from_administrative_account_state()
    {
        var user = NewUser();
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);

        Assert.True(new AccountLoginEligibility().CanContinueSession(user));
        Assert.False(new AccountLoginEligibility().CanEstablishSession(user));
    }

    private static ApplicationUser NewUser() => new()
    {
        Id = "user-1",
        UserName = "operator",
        DisplayName = "Operator",
        LoginAllowed = true,
        SecurityState = AccountSecurityState.Active
    };
}
