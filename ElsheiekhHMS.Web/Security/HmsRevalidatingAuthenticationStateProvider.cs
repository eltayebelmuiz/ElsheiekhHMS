using System.Security.Claims;
using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ElsheiekhHMS.Web.Security;

/// <summary>
/// Revalidates the server circuit against the current Identity account state.
/// A circuit is invalidated when its user no longer exists, its security stamp
/// changes, or administrative login eligibility is withdrawn.
/// </summary>
public sealed class HmsRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    CurrentUserAccessor currentUser,
    IOptions<IdentityOptions> identityOptions)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    private readonly string _securityStampClaimType =
        identityOptions.Value.ClaimsIdentity.SecurityStampClaimType;

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(5);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        var principal = authenticationState.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            currentUser.SetPrincipal(new ClaimsPrincipal());
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();
        // A circuit keeps its DI scope alive for its lifetime. Resolve Identity
        // from a fresh scope so a tracked user cannot hide a later state/stamp
        // change from the periodic revalidation boundary.
        await using var validationScope = scopeFactory.CreateAsyncScope();
        var userManager = validationScope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var eligibility = validationScope.ServiceProvider
            .GetRequiredService<AccountLoginEligibility>();
        var user = await userManager.GetUserAsync(principal);
        if (user is null || !eligibility.CanContinueSession(user))
        {
            currentUser.SetPrincipal(new ClaimsPrincipal());
            return false;
        }

        var expectedStamp = await userManager.GetSecurityStampAsync(user);
        var actualStamp = principal.FindFirstValue(_securityStampClaimType);
        if (string.IsNullOrEmpty(expectedStamp) ||
            !string.Equals(expectedStamp, actualStamp, StringComparison.Ordinal))
        {
            currentUser.SetPrincipal(new ClaimsPrincipal());
            return false;
        }

        currentUser.SetPrincipal(principal);
        return true;
    }
}
