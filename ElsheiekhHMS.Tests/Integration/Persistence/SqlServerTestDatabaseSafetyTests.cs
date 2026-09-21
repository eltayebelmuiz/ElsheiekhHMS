namespace ElsheiekhHMS.Tests.Integration.Persistence;

public sealed class SqlServerTestDatabaseSafetyTests
{
    [Theory]
    [InlineData("ElsheiekhHMS_Dev")]
    [InlineData("master")]
    [InlineData("tempdb")]
    [InlineData("")]
    [InlineData("AnotherDatabase")]
    public void Destructive_operations_reject_non_allowlisted_database(string databaseName)
    {
        Assert.False(SqlServerTestDatabaseGuard.IsAllowedDatabase(databaseName));
    }

    [Fact]
    public void Destructive_operations_accept_only_the_exact_integration_database()
    {
        Assert.True(SqlServerTestDatabaseGuard.IsAllowedDatabase(
            SqlServerTestDatabaseGuard.IntegrationDatabaseName));
    }

    [Fact]
    public void Destructive_operations_reject_a_different_server_or_database_connection()
    {
        var wrongConnection =
            "Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMS_Dev;Trusted_Connection=True";

        Assert.Throws<InvalidOperationException>(() =>
            SqlServerTestDatabaseGuard.ValidateDestructiveTarget(wrongConnection));
    }
}
