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
        // Phase 04 onwards: register DbContext, Identity, repositories
        // e.g. services.AddDbContext<ApplicationDbContext>(...);

        return services;
    }
}
