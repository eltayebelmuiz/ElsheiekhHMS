using ElsheiekhHMS.Application.Common.Security;

namespace ElsheiekhHMS.Tests.Unit.Application.Security;

public sealed class SecurityVocabularyTests
{
    [Fact]
    public void Role_names_are_the_five_approved_persisted_identity_names()
    {
        Assert.Equal(
            ["SystemAdministrator", "Administrator", "Receptionist", "Provider", "Patient"],
            RoleNames.All);
    }

    [Fact]
    public void Role_names_do_not_include_specialty_or_deferred_roles()
    {
        Assert.DoesNotContain("Doctor", RoleNames.All);
        Assert.DoesNotContain("Dentist", RoleNames.All);
        Assert.DoesNotContain("Nurse", RoleNames.All);
        Assert.DoesNotContain("Cashier", RoleNames.All);
        Assert.DoesNotContain("Pharmacist", RoleNames.All);
        Assert.DoesNotContain("LabTechnician", RoleNames.All);
        Assert.DoesNotContain("Accountant", RoleNames.All);
    }

    [Fact]
    public void Policy_names_are_the_eight_approved_capability_names()
    {
        Assert.Equal(
            [
                "CanManageUsers",
                "CanManageUserSecurity",
                "CanViewAuditTrail",
                "CanManagePatients",
                "CanManageAppointments",
                "CanAccessClinicalRecords",
                "CanManageBilling",
                "CanConfigureSystem"
            ],
            PolicyNames.All);
    }

    [Fact]
    public void Role_and_policy_constants_preserve_the_approved_values()
    {
        Assert.Equal("SystemAdministrator", RoleNames.SystemAdministrator);
        Assert.Equal("Administrator", RoleNames.Administrator);
        Assert.Equal("Receptionist", RoleNames.Receptionist);
        Assert.Equal("Provider", RoleNames.Provider);
        Assert.Equal("Patient", RoleNames.Patient);

        Assert.Equal("CanManageUsers", PolicyNames.CanManageUsers);
        Assert.Equal("CanManageUserSecurity", PolicyNames.CanManageUserSecurity);
        Assert.Equal("CanViewAuditTrail", PolicyNames.CanViewAuditTrail);
        Assert.Equal("CanManagePatients", PolicyNames.CanManagePatients);
        Assert.Equal("CanManageAppointments", PolicyNames.CanManageAppointments);
        Assert.Equal("CanAccessClinicalRecords", PolicyNames.CanAccessClinicalRecords);
        Assert.Equal("CanManageBilling", PolicyNames.CanManageBilling);
        Assert.Equal("CanConfigureSystem", PolicyNames.CanConfigureSystem);
    }

    [Fact]
    public void Policy_role_membership_matches_the_approved_capability_matrix()
    {
        var memberships = new Dictionary<string, string[]>
        {
            [PolicyNames.CanManageUsers] = [RoleNames.Administrator, RoleNames.SystemAdministrator],
            [PolicyNames.CanManageUserSecurity] = [RoleNames.Administrator, RoleNames.SystemAdministrator],
            [PolicyNames.CanViewAuditTrail] = [RoleNames.Administrator, RoleNames.SystemAdministrator],
            [PolicyNames.CanManagePatients] = [RoleNames.Administrator, RoleNames.Receptionist],
            [PolicyNames.CanManageAppointments] = [RoleNames.Administrator, RoleNames.Receptionist, RoleNames.Provider],
            [PolicyNames.CanAccessClinicalRecords] = [RoleNames.Provider, RoleNames.Patient],
            [PolicyNames.CanManageBilling] = [RoleNames.Administrator, RoleNames.Receptionist],
            [PolicyNames.CanConfigureSystem] = [RoleNames.SystemAdministrator]
        };

        Assert.Equal(8, memberships.Count);
        Assert.DoesNotContain(RoleNames.Administrator, memberships[PolicyNames.CanAccessClinicalRecords]);
        Assert.DoesNotContain(RoleNames.SystemAdministrator, memberships[PolicyNames.CanAccessClinicalRecords]);
        Assert.DoesNotContain(RoleNames.Receptionist, memberships[PolicyNames.CanAccessClinicalRecords]);
        Assert.Equal(
            [RoleNames.Provider, RoleNames.Patient],
            memberships[PolicyNames.CanAccessClinicalRecords]);
    }
}
