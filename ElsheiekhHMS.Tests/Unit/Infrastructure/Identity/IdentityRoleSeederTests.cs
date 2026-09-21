using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Identity;

public sealed class IdentityRoleSeederTests
{
    [Fact]
    public async Task SeedAsync_creates_exactly_the_five_canonical_roles()
    {
        var store = new InMemoryRoleStore();
        var manager = CreateRoleManager(store);
        var seeder = new IdentityRoleSeeder(manager);

        await seeder.SeedAsync();

        Assert.Equal(RoleNamesForAssertion, store.Roles.Select(role => role.Name));
        Assert.Empty(store.Users);
    }

    [Fact]
    public async Task SeedAsync_is_idempotent_when_called_repeatedly()
    {
        var store = new InMemoryRoleStore();
        var manager = CreateRoleManager(store);
        var seeder = new IdentityRoleSeeder(manager);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(RoleNamesForAssertion, store.Roles.Select(role => role.Name));
        Assert.Equal(5, store.CreateCount);
    }

    [Fact]
    public async Task SeedAsync_does_not_create_passwords_or_privileged_users()
    {
        var store = new InMemoryRoleStore();
        var manager = CreateRoleManager(store);
        var seeder = new IdentityRoleSeeder(manager);

        await seeder.SeedAsync();

        Assert.Empty(store.Users);
    }

    [Fact]
    public void Infrastructure_registers_the_role_seeder_without_invoking_it()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElsheiekhHmsDatabase"] = "Server=unused;Database=ShapeOnly;"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IdentityRoleSeeder));
    }

    private static RoleManager<IdentityRole> CreateRoleManager(InMemoryRoleStore store) =>
        new(
            store,
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new NullLogger<RoleManager<IdentityRole>>());

    private static readonly string[] RoleNamesForAssertion =
    [
        "SystemAdministrator",
        "Administrator",
        "Receptionist",
        "Provider",
        "Patient"
    ];

    private sealed class NullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }
    }

    private sealed class InMemoryRoleStore : IRoleStore<IdentityRole>
    {
        public List<IdentityRole> Roles { get; } = [];
        public List<IdentityUser> Users { get; } = [];
        public int CreateCount { get; private set; }

        public Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            Roles.Add(role);
            CreateCount++;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken) =>
            Task.FromResult(role.Id);

        public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken) =>
            Task.FromResult(role.Name);

        public Task SetRoleNameAsync(
            IdentityRole role,
            string? roleName,
            CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedRoleNameAsync(
            IdentityRole role,
            CancellationToken cancellationToken) =>
            Task.FromResult(role.NormalizedName);

        public Task SetNormalizedRoleNameAsync(
            IdentityRole role,
            string? normalizedName,
            CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityRole?> FindByIdAsync(
            string roleId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Roles.SingleOrDefault(role => role.Id == roleId));

        public Task<IdentityRole?> FindByNameAsync(
            string normalizedRoleName,
            CancellationToken cancellationToken) =>
            Task.FromResult(Roles.SingleOrDefault(role => role.NormalizedName == normalizedRoleName));

        public void Dispose()
        {
        }
    }
}
