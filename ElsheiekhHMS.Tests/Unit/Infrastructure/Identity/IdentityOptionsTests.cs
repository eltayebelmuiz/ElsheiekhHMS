using ElsheiekhHMS.Infrastructure;
using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Identity;

public sealed class IdentityOptionsTests
{
    [Fact]
    public void Infrastructure_uses_the_standard_identity_username_normalizer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElsheiekhHmsDatabase"] = "Server=unused;Database=ShapeOnly;"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<UpperInvariantLookupNormalizer>(
            provider.GetRequiredService<ILookupNormalizer>());
    }

    [Fact]
    public void Infrastructure_registers_user_and_role_stores_for_the_existing_context()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElsheiekhHmsDatabase"] = "Server=unused;Database=ShapeOnly;"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IUserStore<ApplicationUser>));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IRoleStore<IdentityRole>));
    }

    [Fact]
    public void Infrastructure_registers_explicit_administrator_bootstrapper()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElsheiekhHmsDatabase"] = "Server=unused;Database=ShapeOnly;"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(AdministratorBootstrapper));
    }

    [Fact]
    public void Infrastructure_registers_the_approved_password_and_lockout_policy()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElsheiekhHmsDatabase"] = "Server=unused;Database=ShapeOnly;"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.Equal(8, options.Password.RequiredLength);
        Assert.True(options.Password.RequireUppercase);
        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireNonAlphanumeric);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), options.Lockout.DefaultLockoutTimeSpan);
        Assert.True(options.Lockout.AllowedForNewUsers);
    }
}
