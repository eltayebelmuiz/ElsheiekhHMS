using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;

namespace ElsheiekhHMS.Tests.Unit.Web.Security;

public sealed class AuthorizationConfigurationTests
{
    [Fact]
    public async Task Fallback_policy_requires_an_authenticated_user()
    {
        using var provider = BuildProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var fallback = await policies.GetFallbackPolicyAsync();

        Assert.NotNull(fallback);
        Assert.Contains(fallback!.Requirements, requirement =>
            requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task Patient_and_appointment_policies_preserve_the_approved_roles()
    {
        using var provider = BuildProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var patient = await policies.GetPolicyAsync(PolicyNames.CanManagePatients);
        var appointment = await policies.GetPolicyAsync(PolicyNames.CanManageAppointments);

        Assert.Equal(
            [RoleNames.Administrator, RoleNames.Receptionist],
            GetAllowedRoles(patient));
        Assert.Equal(
            [RoleNames.Administrator, RoleNames.Receptionist, RoleNames.Provider],
            GetAllowedRoles(appointment));
    }

    [Fact]
    public async Task System_administrator_is_not_granted_clinical_or_patient_policy_access()
    {
        using var provider = BuildProvider();
        var policies = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var patient = await policies.GetPolicyAsync(PolicyNames.CanManagePatients);
        var appointment = await policies.GetPolicyAsync(PolicyNames.CanManageAppointments);
        var clinical = await policies.GetPolicyAsync(PolicyNames.CanAccessClinicalRecords);

        Assert.DoesNotContain(RoleNames.SystemAdministrator, GetAllowedRoles(patient));
        Assert.DoesNotContain(RoleNames.SystemAdministrator, GetAllowedRoles(appointment));
        Assert.DoesNotContain(RoleNames.SystemAdministrator, GetAllowedRoles(clinical));
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddHmsAuthorization();
        return services.BuildServiceProvider();
    }

    private static string[] GetAllowedRoles(AuthorizationPolicy? policy)
    {
        Assert.NotNull(policy);
        var requirement = Assert.Single(
            policy!.Requirements.OfType<RolesAuthorizationRequirement>());
        return requirement.AllowedRoles.ToArray();
    }
}
