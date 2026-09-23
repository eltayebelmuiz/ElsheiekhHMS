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
                "PRIVILEGED_ROLE_REMOVED",
                "PATIENT_REGISTERED",
                "PATIENT_DEMOGRAPHICS_UPDATED",
                "PATIENT_CONTACT_UPDATED",
                "PATIENT_IDENTIFIERS_UPDATED",
                "DEPARTMENT_CREATED",
                "DEPARTMENT_UPDATED",
                "DEPARTMENT_DEACTIVATED",
                "APPOINTMENT_SCHEDULED",
                "APPOINTMENT_CANCELLED",
                "APPOINTMENT_NO_SHOW",
                "APPOINTMENT_CHECKED_IN",
                "APPOINTMENT_COMPLETED",
                "QUEUE_ENTRY_CREATED",
                "QUEUE_ENTRY_CALLED",
                "QUEUE_ENTRY_STARTED",
                "QUEUE_ENTRY_SKIPPED",
                "QUEUE_ENTRY_HELD",
                "QUEUE_ENTRY_RESUMED",
                "QUEUE_ENTRY_COMPLETED",
                "QUEUE_ENTRY_CANCELLED",
                "PROVIDER_OWNERSHIP_ASSIGNED",
                "PROVIDER_OWNERSHIP_UNASSIGNED"
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
