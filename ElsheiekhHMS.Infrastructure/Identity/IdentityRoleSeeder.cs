using ElsheiekhHMS.Application.Common.Security;
using Microsoft.AspNetCore.Identity;

namespace ElsheiekhHMS.Infrastructure.Identity;

public sealed class IdentityRoleSeeder(RoleManager<IdentityRole> roleManager)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in RoleNames.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Code));
                throw new InvalidOperationException($"Unable to seed Identity role '{roleName}': {errors}");
            }
        }
    }
}
