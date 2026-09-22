using System.Net;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Infrastructure;
using ElsheiekhHMS.Infrastructure.Health;
using ElsheiekhHMS.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class HmsHealthEndpointHostTests(SqlServerTestDatabaseFixture fixture)
{
    [Fact]
    public async Task Health_endpoints_are_anonymous_and_return_minimal_statuses()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElsheiekhHmsDatabase"] = fixture.ConnectionString
            })
            .Build();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(HmsHealthEndpointHostTests).Assembly.GetName().Name
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddInfrastructure(configuration);
        builder.Services.AddHmsAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<CurrentUserAccessor>();
        builder.Services.AddScoped<ICurrentUser>(services =>
            services.GetRequiredService<CurrentUserAccessor>());
        builder.Services
            .AddHealthChecks()
            .AddCheck<HmsLivenessHealthCheck>("hms_liveness", tags: ["live"])
            .AddCheck<SqlServerReadinessHealthCheck>("sql_server", tags: ["ready"]);

        await using var app = builder.Build();
        app.UseAuthorization();
        app.MapHmsHealthEndpoints();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

            await AssertHealthyAsync(client, "/health/live");
            await AssertHealthyAsync(client, "/health");
            await AssertHealthyAsync(client, "/health/ready");
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static async Task AssertHealthyAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
        Assert.StartsWith("text/plain", response.Content.Headers.ContentType?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
