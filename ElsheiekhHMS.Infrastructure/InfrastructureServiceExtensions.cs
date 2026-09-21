using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Infrastructure.Auditing;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.AspNetCore.Identity;
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

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<EntityAuditSaveChangesInterceptor>();
        services.AddScoped<IAuditEventWriter, AuditEventWriter>();
        services.AddScoped<AccountLoginEligibility>();
        services.AddScoped<AdministratorBootstrapper>();
        services.AddScoped<PatientCodeAllocator>();
        services.AddScoped<QueueTicketAllocator>();
        services.AddDbContext<ElsheiekhHmsDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(
                serviceProvider.GetRequiredService<EntityAuditSaveChangesInterceptor>());
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ElsheiekhHmsDbContext>();

        services.AddScoped<IdentityRoleSeeder>();

        return services;
    }
}
