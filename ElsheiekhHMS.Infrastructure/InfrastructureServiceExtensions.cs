using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElsheiekhHMS.Infrastructure;

/// <summary>
/// Registers all Infrastructure-layer services into the DI container.
/// Called from Program.cs as: builder.Services.AddInfrastructure(configuration)
///
/// WHY: Keeps Program.cs clean. All infrastructure registrations live here.
/// WHAT goes here: DbContext, Identity, repositories, file storage, email, SMS.
/// WHAT does NOT go here: Business logic, DTOs, Blazor services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ElsheiekhHmsDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'ElsheiekhHmsDatabase' is required.");

        services.AddDbContext<ElsheiekhHmsDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
