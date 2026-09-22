using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ElsheiekhHMS.Infrastructure.Health;

public sealed class HmsLivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("HMS process is running."));
}

public sealed class SqlServerReadinessHealthCheck(
    IServiceScopeFactory scopeFactory,
    ILogger<SqlServerReadinessHealthCheck> logger) : IHealthCheck
{
    private const string UnavailableDescription = "SQL Server dependency is unavailable.";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ElsheiekhHmsDbContext>();
            var reachable = await dbContext.Database.CanConnectAsync(cancellationToken);
            return reachable
                ? HealthCheckResult.Healthy("SQL Server dependency is available.")
                : HealthCheckResult.Unhealthy(UnavailableDescription);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            logger.LogWarning("SQL Server readiness check failed for the configured dependency.");
            return HealthCheckResult.Unhealthy(UnavailableDescription);
        }
    }
}

public static class HmsHealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapHmsHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", CreateOptions("live"))
            .AllowAnonymous();
        endpoints.MapHealthChecks("/health/ready", CreateOptions("ready"))
            .AllowAnonymous();
        endpoints.MapHealthChecks("/health", CreateOptions("live"))
            .AllowAnonymous();
        return endpoints;
    }

    private static HealthCheckOptions CreateOptions(string tag) => new()
    {
        Predicate = check => check.Tags.Contains(tag, StringComparer.Ordinal),
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        },
        ResponseWriter = WriteMinimalResponse
    };

    private static Task WriteMinimalResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        return context.Response.WriteAsync(report.Status.ToString());
    }
}
