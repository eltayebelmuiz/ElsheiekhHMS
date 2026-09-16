using Microsoft.Extensions.DependencyInjection;

namespace ElsheiekhHMS.Application;

/// <summary>
/// Registers all Application-layer services into the DI container.
/// Called from Program.cs as: builder.Services.AddApplication()
///
/// WHY: Keeps Program.cs clean. All application registrations live here.
/// WHAT goes here: Service interfaces, validators, mapping profiles.
/// WHAT does NOT go here: EF Core, Identity, connection strings — those are Infrastructure.
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Phase 07 onwards: register application services here
        // e.g. services.AddScoped<IPatientService, PatientService>();

        return services;
    }
}
