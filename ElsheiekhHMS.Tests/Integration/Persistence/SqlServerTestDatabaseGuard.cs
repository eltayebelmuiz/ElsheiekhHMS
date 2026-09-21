namespace ElsheiekhHMS.Tests.Integration.Persistence;

internal static class SqlServerTestDatabaseGuard
{
    public const string LocalDbInstanceName = @"(localdb)\MSSQLLocalDB";
    public const string IntegrationDatabaseName = "ElsheiekhHMS_IntegrationTests";
    public const string DevelopmentDatabaseName = "ElsheiekhHMS_Dev";

    public static string ConnectionString =>
        $"Server={LocalDbInstanceName};Database={IntegrationDatabaseName};" +
        "Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public static bool IsAllowedDatabase(string? databaseName) =>
        string.Equals(databaseName, IntegrationDatabaseName, StringComparison.Ordinal);

    public static void ValidateDestructiveTarget(string connectionString)
    {
        if (!string.Equals(connectionString, ConnectionString, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Destructive integration-database operations require the exact approved LocalDB connection.");
        }

        if (!IsAllowedDatabase(IntegrationDatabaseName) ||
            string.Equals(IntegrationDatabaseName, DevelopmentDatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The integration database safety allow-list is invalid.");
        }
    }
}
