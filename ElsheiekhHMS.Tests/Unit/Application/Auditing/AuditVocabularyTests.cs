using ElsheiekhHMS.Application.Common.Auditing;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Application.Auditing;

public sealed class AuditVocabularyTests
{
    [Fact]
    public void Categories_have_stable_persisted_values()
    {
        Assert.Equal(
            ["SECURITY", "IDENTITY", "AUTHORIZATION", "ADMINISTRATION", "BUSINESS"],
            AuditCategories.All);
    }

    [Fact]
    public void Actions_have_stable_persisted_values()
    {
        Assert.Equal(
            [
                "ACCOUNT_LOGIN_BLOCKED",
                "ACCOUNT_LOGIN_UNBLOCKED",
                "ACCOUNT_SUSPENDED",
                "ACCOUNT_RESTORED",
                "ACCOUNT_BANNED",
                "ACCOUNT_BAN_REVERSED",
                "SESSIONS_REVOKED",
                "USERNAME_CHANGED",
                "PASSWORD_RESET_BY_ADMIN",
                "ROLE_ASSIGNED",
                "ROLE_REMOVED",
                "PRIVILEGED_ROLE_ASSIGNED",
                "PRIVILEGED_ROLE_REMOVED"
            ],
            AuditActions.All);
    }

    [Fact]
    public void Actor_kinds_are_stable()
    {
        Assert.Equal("Human", AuditActorKinds.Human);
        Assert.Equal("System", AuditActorKinds.System);
        Assert.Equal("Anonymous", AuditActorKinds.Anonymous);
    }
}
