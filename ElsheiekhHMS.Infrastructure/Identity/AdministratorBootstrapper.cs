using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace ElsheiekhHMS.Infrastructure.Identity;

/// <summary>
/// Explicit, secret-backed provisioning primitive for the first administrator.
/// This service is never invoked automatically during Web startup.
/// </summary>
public sealed class AdministratorBootstrapper(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    TimeProvider timeProvider)
{
    public async Task BootstrapAsync(
        string username,
        string displayName,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("A username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("A password is required.", nameof(password));
        }

        foreach (var privilegedRole in new[] { RoleNames.SystemAdministrator, RoleNames.Administrator })
        {
            if (!await roleManager.RoleExistsAsync(privilegedRole))
            {
                throw new InvalidOperationException(
                    $"The required Identity role '{privilegedRole}' has not been seeded.");
            }
        }

        var systemAdministrators = await userManager.GetUsersInRoleAsync(RoleNames.SystemAdministrator);
        var administrators = await userManager.GetUsersInRoleAsync(RoleNames.Administrator);
        if (systemAdministrators.Count > 0 || administrators.Count > 0)
        {
            throw new InvalidOperationException(
                "An administrator account already exists; bootstrap will not overwrite it.");
        }

        var user = new ApplicationUser
        {
            UserName = username.Trim(),
            DisplayName = displayName.Trim(),
            CreatedAt = timeProvider.GetUtcNow(),
            SecurityState = AccountSecurityState.Active,
            LoginAllowed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to create the bootstrap administrator: " +
                string.Join(", ", createResult.Errors.Select(error => error.Code)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, RoleNames.SystemAdministrator);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Unable to assign the bootstrap administrator role: " +
                string.Join(", ", roleResult.Errors.Select(error => error.Code)));
        }
    }
}
