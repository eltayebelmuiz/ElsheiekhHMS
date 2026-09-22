using System.Reflection;
using System.Security.Claims;
using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Web.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ElsheiekhHMS.Tests.Unit.Web.Security;

public sealed class AuthenticationStateValidationTests
{
    [Fact]
    public async Task A_changed_security_stamp_invalidates_the_existing_state()
    {
        var user = ActiveUser();
        var currentUser = new CurrentUserAccessor(new HttpContextAccessor());
        var manager = new StubUserManager(user, "new-stamp");
        var provider = CreateProvider(manager, currentUser);
        var stalePrincipal = Principal(user.Id, "old-stamp");

        var valid = await ValidateAsync(provider, stalePrincipal);

        Assert.False(valid);
        Assert.False(currentUser.IsAuthenticated);
    }

    [Theory]
    [InlineData(AccountSecurityState.Suspended)]
    [InlineData(AccountSecurityState.Banned)]
    public async Task A_restricted_account_invalidates_the_existing_state(AccountSecurityState state)
    {
        var user = ActiveUser();
        user.SecurityState = state;
        user.LoginAllowed = false;
        var currentUser = new CurrentUserAccessor(new HttpContextAccessor());
        var manager = new StubUserManager(user, "stamp");
        var provider = CreateProvider(manager, currentUser);

        var valid = await ValidateAsync(provider, Principal(user.Id, "stamp"));

        Assert.False(valid);
        Assert.False(currentUser.IsAuthenticated);
    }

    [Fact]
    public async Task An_active_account_with_the_current_stamp_remains_valid()
    {
        var user = ActiveUser();
        var currentUser = new CurrentUserAccessor(new HttpContextAccessor());
        var manager = new StubUserManager(user, "stamp");
        var provider = CreateProvider(manager, currentUser);

        var valid = await ValidateAsync(provider, Principal(user.Id, "stamp"));

        Assert.True(valid);
        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(user.Id, currentUser.UserId);
    }

    private static HmsRevalidatingAuthenticationStateProvider CreateProvider(
        StubUserManager manager,
        CurrentUserAccessor currentUser) =>
        new(
            NullLoggerFactory.Instance,
            new SingleServiceScopeFactory(manager),
            currentUser,
            Options.Create(new IdentityOptions()));

    private static async Task<bool> ValidateAsync(
        HmsRevalidatingAuthenticationStateProvider provider,
        ClaimsPrincipal principal)
    {
        var method = provider.GetType().GetMethod(
            "ValidateAuthenticationStateAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var task = Assert.IsAssignableFrom<Task<bool>>(method!.Invoke(
            provider,
            [new AuthenticationState(principal), CancellationToken.None]));
        return await task;
    }

    private static ApplicationUser ActiveUser() => new()
    {
        Id = "security-user",
        UserName = "security-user",
        LoginAllowed = true,
        SecurityState = AccountSecurityState.Active
    };

    private static ClaimsPrincipal Principal(string userId, string securityStamp) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(new IdentityOptions().ClaimsIdentity.SecurityStampClaimType, securityStamp)
        ],
        "test"));

    private sealed class StubUserManager(ApplicationUser user, string securityStamp)
        : UserManager<ApplicationUser>(
            new EmptyUserStore(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance)
    {
        public override Task<ApplicationUser?> GetUserAsync(ClaimsPrincipal principal) =>
            Task.FromResult<ApplicationUser?>(
                principal.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id ? user : null);

        public override Task<string> GetSecurityStampAsync(ApplicationUser user) =>
            Task.FromResult(securityStamp);
    }

    private sealed class SingleServiceScopeFactory(StubUserManager manager) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new SingleScope(manager);
    }

    private sealed class SingleScope(StubUserManager manager) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new SingleServiceProvider(manager);
        public void Dispose() { }
    }

    private sealed class SingleServiceProvider(StubUserManager manager) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(UserManager<ApplicationUser>) ? manager :
            serviceType == typeof(AccountLoginEligibility) ? new AccountLoginEligibility() : null;
    }

    private sealed class EmptyUserStore : IUserStore<ApplicationUser>
    {
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public void Dispose() { }

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);
    }
}
