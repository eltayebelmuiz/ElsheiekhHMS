using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[CollectionDefinition("SQL Server persistence")]
public sealed class SqlServerPersistenceCollection
    : ICollectionFixture<SqlServerTestDatabaseFixture>;

public sealed class SqlServerTestDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString => SqlServerTestDatabaseGuard.ConnectionString;

    public async Task InitializeAsync()
    {
        SqlServerTestDatabaseGuard.ValidateDestructiveTarget(ConnectionString);

        await using (var cleanupContext = CreateContext())
        {
            await cleanupContext.Database.EnsureDeletedAsync();
        }

        await using var migrationContext = CreateContext();
        await migrationContext.Database.MigrateAsync();

        var applied = await migrationContext.Database.GetAppliedMigrationsAsync();
        if (!applied.Contains("20260921111137_InitialCreate", StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "The isolated integration database did not apply InitialCreate.");
        }
    }

    public ElsheiekhHmsDbContext CreateContext()
    {
        SqlServerTestDatabaseGuard.ValidateDestructiveTarget(ConnectionString);

        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new ElsheiekhHmsDbContext(options);
    }

    public ElsheiekhHmsDbContext CreateContext(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        SqlServerTestDatabaseGuard.ValidateDestructiveTarget(ConnectionString);

        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new EntityAuditSaveChangesInterceptor(currentUser, timeProvider))
            .Options;

        return new ElsheiekhHmsDbContext(options);
    }

    public async Task DisposeAsync()
    {
        SqlServerTestDatabaseGuard.ValidateDestructiveTarget(ConnectionString);

        try
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Unable to remove the isolated database '{SqlServerTestDatabaseGuard.IntegrationDatabaseName}'.",
                exception);
        }
    }
}
