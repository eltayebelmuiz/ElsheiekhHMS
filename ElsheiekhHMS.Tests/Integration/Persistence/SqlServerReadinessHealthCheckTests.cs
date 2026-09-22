using ElsheiekhHMS.Infrastructure.Health;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class SqlServerReadinessHealthCheckTests(
    SqlServerTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Readiness_succeeds_for_the_isolated_database_without_migrations_or_writes()
    {
        await using var beforeContext = fixture.CreateContext();
        var migrationsBefore = (await beforeContext.Database.GetAppliedMigrationsAsync()).ToArray();

        using var services = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ElsheiekhHmsDbContext>(options =>
                options.UseSqlServer(fixture.ConnectionString))
            .AddSingleton<SqlServerReadinessHealthCheck>()
            .BuildServiceProvider();

        var check = services.GetRequiredService<SqlServerReadinessHealthCheck>();
        var result = await check.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);

        await using var afterContext = fixture.CreateContext();
        var migrationsAfter = (await afterContext.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(migrationsBefore, migrationsAfter);
        Assert.Empty(afterContext.ChangeTracker.Entries());
    }
}
