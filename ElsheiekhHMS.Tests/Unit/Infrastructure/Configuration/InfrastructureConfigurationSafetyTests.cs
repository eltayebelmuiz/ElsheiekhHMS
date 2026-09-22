using ElsheiekhHMS.Infrastructure;
using ElsheiekhHMS.Infrastructure.Health;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Configuration;

public sealed class InfrastructureConfigurationSafetyTests
{
    [Fact]
    public void Infrastructure_requires_the_approved_connection_string_without_a_fallback()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddInfrastructure(configuration));

        Assert.Equal(
            "Connection string 'ElsheiekhHmsDatabase' is required.",
            exception.Message);
    }

    [Fact]
    public async Task Malformed_connection_configuration_returns_safe_unhealthy_readiness()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ElsheiekhHmsDbContext>(options =>
                options.UseSqlServer("NotARealKeyword=value"))
            .AddSingleton<SqlServerReadinessHealthCheck>()
            .BuildServiceProvider();

        var result = await services
            .GetRequiredService<SqlServerReadinessHealthCheck>()
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("SQL Server dependency is unavailable.", result.Description);
        Assert.DoesNotContain("NotARealKeyword", result.Description, StringComparison.Ordinal);
    }
}
