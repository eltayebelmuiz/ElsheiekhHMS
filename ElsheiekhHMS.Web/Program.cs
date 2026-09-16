using ElsheiekhHMS.Application;
using ElsheiekhHMS.Infrastructure;
using ElsheiekhHMS.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Application layers.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Blazor.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Basic application health check.
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
