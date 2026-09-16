using ElsheiekhHMS.Application;
using ElsheiekhHMS.Infrastructure;
using ElsheiekhHMS.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// -- Layer registrations -------------------------------------------------------
// Each layer registers itself. Program.cs stays clean.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// -- Blazor --------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// -- Health Checks -------------------------------------------------------------
// Phase 09: add database, storage, external service checks here
builder.Services.AddHealthChecks();

var app = builder.Build();

// -- Middleware ----------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// -- Health check endpoint -----------------------------------------------------
app.MapHealthChecks("/health");

// -- Blazor --------------------------------------------------------------------
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
