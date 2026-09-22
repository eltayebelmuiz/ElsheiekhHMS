using ElsheiekhHMS.Infrastructure.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Health;

public sealed class HealthCheckTests
{
    [Fact]
    public async Task Liveness_is_healthy_without_resolving_a_database()
    {
        var result = await new HmsLivenessHealthCheck()
            .CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task Readiness_is_unhealthy_when_the_database_dependency_cannot_be_resolved()
    {
        var check = new SqlServerReadinessHealthCheck(
            new ThrowingScopeFactory(),
            NullLogger<SqlServerReadinessHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("SQL Server dependency is unavailable.", result.Description);
    }

    private sealed class ThrowingScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new ThrowingScope();
    }

    private sealed class ThrowingScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new ThrowingServiceProvider();
        public void Dispose() { }
    }

    private sealed class ThrowingServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            throw new InvalidOperationException("database unavailable");
    }
}
