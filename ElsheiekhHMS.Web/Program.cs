using ElsheiekhHMS.Application;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Infrastructure;
using ElsheiekhHMS.Web.Components;
using ElsheiekhHMS.Web.Security;
using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Infrastructure.Development;
using ElsheiekhHMS.Infrastructure.Health;
using ElsheiekhHMS.Infrastructure.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Application layers.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHmsAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    })
    .AddIdentityCookies();
// Identity stores and options remain owned by Infrastructure. Web adds only the
// framework sign-in coordinator required by the approved login presentation.
builder.Services.AddScoped<SignInManager<ApplicationUser>>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.Events.OnValidatePrincipal = async context =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ElsheiekhHMS.Authentication");
        if (context.Principal is null)
        {
            logger.LogWarning("Authentication cookie rejected: missing principal.");
            context.RejectPrincipal();
            return;
        }

        var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<ApplicationUser>>();
        var eligibility = context.HttpContext.RequestServices
            .GetRequiredService<AccountLoginEligibility>();
        var user = await userManager.GetUserAsync(context.Principal);
        var stampClaimType = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<IdentityOptions>>().Value
            .ClaimsIdentity.SecurityStampClaimType;
        var expectedStamp = user is null ? null : await userManager.GetSecurityStampAsync(user);
        var actualStamp = context.Principal?.FindFirst(stampClaimType)?.Value;

        if (user is null || !eligibility.CanContinueSession(user) ||
            string.IsNullOrEmpty(expectedStamp) ||
            !string.Equals(expectedStamp, actualStamp, StringComparison.Ordinal))
        {
            logger.LogWarning("Authentication cookie rejected by account or security-stamp validation.");
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentUser>(services =>
    services.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddScoped<AuthenticationStateProvider, HmsRevalidatingAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

// Blazor.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Basic application health check.
builder.Services.AddHealthChecks()
    .AddCheck<HmsLivenessHealthCheck>("hms_liveness", tags: ["live"])
    .AddCheck<SqlServerReadinessHealthCheck>("sql_server", tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
{
    await using var seedScope = app.Services.CreateAsyncScope();
    await seedScope.ServiceProvider
        .GetRequiredService<DevelopmentDataSeeder>()
        .SeedAsync();
}

// Configure the HTTP request pipeline.
app.UseMiddleware<RequestObservabilityMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
    context.Response.Headers["Content-Security-Policy-Report-Only"] =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; connect-src 'self' ws: wss:; font-src 'self' data:; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    await next(context);
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.MapPost("/account/login", async (
    HttpContext context,
    IAntiforgery antiforgery,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    AccountLoginEligibility eligibility,
    ILoggerFactory loggerFactory) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    var logger = loggerFactory.CreateLogger("ElsheiekhHMS.Authentication");
    var form = await context.Request.ReadFormAsync(context.RequestAborted);
    var username = form["username"].ToString().Trim();
    var password = form["password"].ToString();
    var returnUrl = SafeLocalReturnUrl(form["returnUrl"].ToString());

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect(LoginFailureUrl("required", returnUrl));
    }

    var user = await userManager.FindByNameAsync(username);
    if (user is null || !eligibility.CanEstablishSession(user))
    {
        logger.LogInformation("Login attempt was not accepted for the supplied username.");
        return Results.Redirect(LoginFailureUrl("invalid", returnUrl));
    }

    var result = await signInManager.PasswordSignInAsync(
        user,
        password,
        isPersistent: false,
        lockoutOnFailure: true);
    if (!result.Succeeded)
    {
        logger.LogInformation("Login attempt was not accepted by Identity.");
        return Results.Redirect(LoginFailureUrl("invalid", returnUrl));
    }

    return Results.LocalRedirect(returnUrl);
})
    .AllowAnonymous();

app.MapPost("/account/logout", async (HttpContext context, IAntiforgery antiforgery, SignInManager<ApplicationUser> signInManager) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest();
    }

    await signInManager.SignOutAsync();
    return Results.Redirect("/login?loggedOut=true");
})
    .RequireAuthorization();

// Static assets are public presentation resources; the fallback policy protects
// application routes while leaving CSS, scripts, and the favicon available to
// the anonymous login and error surfaces.
app.MapStaticAssets().AllowAnonymous();
app.MapHmsHealthEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string SafeLocalReturnUrl(string? value) =>
    !string.IsNullOrWhiteSpace(value) && value.StartsWith('/') && !value.StartsWith("//")
        ? value
        : "/";

static string LoginFailureUrl(string reason, string returnUrl) =>
    $"/login?error={Uri.EscapeDataString(reason)}&returnUrl={Uri.EscapeDataString(returnUrl)}";
