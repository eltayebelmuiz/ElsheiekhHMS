using ElsheiekhHMS.Infrastructure.Identity.Entities;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Identity;

public sealed class ApplicationUserTests
{
    [Fact]
    public void New_user_has_approved_account_lifecycle_defaults()
    {
        var user = new ApplicationUser();

        Assert.True(user.LoginAllowed);
        Assert.Equal(AccountSecurityState.Active, user.SecurityState);
        Assert.Null(user.DisabledAt);
        Assert.Null(user.DisabledBy);
        Assert.Equal(default, user.CreatedAt);
        Assert.Null(user.DisplayName);
    }

    [Fact]
    public void Account_security_state_has_stable_byte_values()
    {
        Assert.Equal(typeof(byte), Enum.GetUnderlyingType(typeof(AccountSecurityState)));
        Assert.Equal(0, (byte)AccountSecurityState.Active);
        Assert.Equal(1, (byte)AccountSecurityState.Suspended);
        Assert.Equal(2, (byte)AccountSecurityState.Banned);
    }

    [Fact]
    public void Active_user_can_block_login_without_changing_account_state()
    {
        var user = new ApplicationUser
        {
            SecurityState = AccountSecurityState.Active,
            LoginAllowed = false
        };

        Assert.Equal(AccountSecurityState.Active, user.SecurityState);
        Assert.False(user.LoginAllowed);
    }

    [Fact]
    public void User_uses_the_default_string_identity_key()
    {
        var user = new ApplicationUser { Id = "user-1" };

        Assert.Equal("user-1", user.Id);
        Assert.Equal(typeof(string), typeof(ApplicationUser).BaseType!.GetProperty(nameof(user.Id))!.PropertyType);
    }
}
