using ElsheiekhHMS.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ElsheiekhHMS.Web.Security;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddHmsAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy(PolicyNames.CanManageUsers, policy =>
                policy.RequireRole(RoleNames.Administrator, RoleNames.SystemAdministrator))
            .AddPolicy(PolicyNames.CanManageUserSecurity, policy =>
                policy.RequireRole(RoleNames.Administrator, RoleNames.SystemAdministrator))
            .AddPolicy(PolicyNames.CanViewAuditTrail, policy =>
                policy.RequireRole(RoleNames.Administrator, RoleNames.SystemAdministrator))
            .AddPolicy(PolicyNames.CanManagePatients, policy =>
                policy.RequireRole(RoleNames.Administrator, RoleNames.Receptionist))
            .AddPolicy(PolicyNames.CanManageAppointments, policy =>
                policy.RequireRole(
                    RoleNames.Administrator,
                    RoleNames.Receptionist,
                    RoleNames.Provider))
            .AddPolicy(PolicyNames.CanAccessClinicalRecords, policy =>
                policy.RequireRole(RoleNames.Provider, RoleNames.Patient))
            .AddPolicy(PolicyNames.CanManageBilling, policy =>
                policy.RequireRole(RoleNames.Administrator, RoleNames.Receptionist))
            .AddPolicy(PolicyNames.CanConfigureSystem, policy =>
                policy.RequireRole(RoleNames.SystemAdministrator));

        return services;
    }
}
