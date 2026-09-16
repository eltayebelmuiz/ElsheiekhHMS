#!/usr/bin/env pwsh
# =============================================================================
# Elshiekh Hospital Management System
# PHASE 01 — Solution & Architecture Setup
# Run from: any empty folder e.g. C:\Projects\
# Requires: .NET 9 SDK (latest stable — .NET 10 preview not yet stable)
# =============================================================================

$Solution   = "ElsheiekhHMS"
$Core       = "ElsheiekhHMS.Core"
$App        = "ElsheiekhHMS.Application"
$Infra      = "ElsheiekhHMS.Infrastructure"
$Web        = "ElsheiekhHMS.Web"
$Tests      = "ElsheiekhHMS.Tests"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Elshiekh Hospital Management System                      " -ForegroundColor Cyan
Write-Host "  PHASE 01 — Solution & Architecture                       " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# ── Create root folder & solution ────────────────────────────────────────────
Write-Host "[1/6] Creating solution..." -ForegroundColor Yellow
New-Item -ItemType Directory -Name $Solution -Force | Out-Null
Set-Location $Solution
dotnet new sln --name $Solution --output .
Write-Host "  Solution: $Solution.sln" -ForegroundColor Green

# ── Create projects ───────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/6] Creating projects..." -ForegroundColor Yellow

# Core — pure domain, zero external dependencies
dotnet new classlib -n $Core -f net9.0 -o $Core
Remove-Item "$Core/Class1.cs" -Force
Write-Host "  Created: $Core (classlib)" -ForegroundColor Green

# Application — use cases, DTOs, service interfaces
dotnet new classlib -n $App -f net9.0 -o $App
Remove-Item "$App/Class1.cs" -Force
Write-Host "  Created: $App (classlib)" -ForegroundColor Green

# Infrastructure — EF Core, Identity, repositories, external services
dotnet new classlib -n $Infra -f net9.0 -o $Infra
Remove-Item "$Infra/Class1.cs" -Force
Write-Host "  Created: $Infra (classlib)" -ForegroundColor Green

# Web — Blazor Interactive Server
dotnet new blazor `
    -n $Web `
    -f net9.0 `
    --interactivity Server `
    --no-https false `
    -o $Web
Write-Host "  Created: $Web (Blazor Interactive Server)" -ForegroundColor Green

# Tests — xUnit
dotnet new xunit -n $Tests -f net9.0 -o $Tests
Remove-Item "$Tests/UnitTest1.cs" -Force
Write-Host "  Created: $Tests (xUnit)" -ForegroundColor Green

# ── Add all projects to solution ──────────────────────────────────────────────
Write-Host ""
Write-Host "[3/6] Adding projects to solution..." -ForegroundColor Yellow
dotnet sln add "$Core/$Core.csproj"
dotnet sln add "$App/$App.csproj"
dotnet sln add "$Infra/$Infra.csproj"
dotnet sln add "$Web/$Web.csproj"
dotnet sln add "$Tests/$Tests.csproj"
Write-Host "  All 5 projects added." -ForegroundColor Green

# ── Project references (dependency direction enforced) ────────────────────────
Write-Host ""
Write-Host "[4/6] Wiring project references (dependency direction)..." -ForegroundColor Yellow

# Application → Core  (uses domain entities and interfaces)
dotnet add "$App/$App.csproj" reference "$Core/$Core.csproj"

# Infrastructure → Core + Application
dotnet add "$Infra/$Infra.csproj" reference "$Core/$Core.csproj"
dotnet add "$Infra/$Infra.csproj" reference "$App/$App.csproj"

# Web → Application + Infrastructure
# Web knows Application (calls services) and Infrastructure (DI registration only)
dotnet add "$Web/$Web.csproj" reference "$App/$App.csproj"
dotnet add "$Web/$Web.csproj" reference "$Infra/$Infra.csproj"

# Tests → Core + Application + Infrastructure (tests all layers)
dotnet add "$Tests/$Tests.csproj" reference "$Core/$Core.csproj"
dotnet add "$Tests/$Tests.csproj" reference "$App/$App.csproj"
dotnet add "$Tests/$Tests.csproj" reference "$Infra/$Infra.csproj"

Write-Host "  References wired." -ForegroundColor Green

# ── NuGet packages — PHASE 01 only (minimal bootstrap) ───────────────────────
Write-Host ""
Write-Host "[5/6] Installing Phase 01 NuGet packages..." -ForegroundColor Yellow

# Core — intentionally empty.
# Core must NOT depend on EF Core, Identity, or any infrastructure concern.
# If you are tempted to add a package to Core, think twice.
Write-Host "  Core: no packages (intentional — pure domain)" -ForegroundColor Gray

# Application
$appPkgs = @(
    "Microsoft.Extensions.Logging.Abstractions",  # ILogger<T> interface only — no impl
    "Microsoft.Extensions.DependencyInjection.Abstractions" # IServiceCollection for DI extension
)
foreach ($pkg in $appPkgs) {
    Write-Host "  App: $pkg" -ForegroundColor Gray
    dotnet add "$App/$App.csproj" package $pkg --no-restore
}

# Infrastructure
$infraPkgs = @(
    "Microsoft.EntityFrameworkCore",
    "Microsoft.EntityFrameworkCore.SqlServer",
    "Microsoft.EntityFrameworkCore.Tools",          # EF CLI migrations
    "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
    "Microsoft.Extensions.Logging.Abstractions",
    "Microsoft.Extensions.Configuration.Abstractions",
    "Microsoft.Extensions.DependencyInjection.Abstractions"
)
foreach ($pkg in $infraPkgs) {
    Write-Host "  Infra: $pkg" -ForegroundColor Gray
    dotnet add "$Infra/$Infra.csproj" package $pkg --no-restore
}

# Web (Blazor) — only design-time EF and diagnostics for now
$webPkgs = @(
    "Microsoft.EntityFrameworkCore.Design",
    "Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"
)
foreach ($pkg in $webPkgs) {
    Write-Host "  Web: $pkg" -ForegroundColor Gray
    dotnet add "$Web/$Web.csproj" package $pkg --no-restore
}

# Tests
$testPkgs = @(
    "Microsoft.NET.Test.Sdk",
    "xunit",
    "xunit.runner.visualstudio",
    "FluentAssertions",            # readable assertions: result.Should().BeTrue()
    "Moq",                         # mocking interfaces for unit tests
    "Microsoft.EntityFrameworkCore.InMemory" # in-memory DB for integration tests
)
foreach ($pkg in $testPkgs) {
    Write-Host "  Tests: $pkg" -ForegroundColor Gray
    dotnet add "$Tests/$Tests.csproj" package $pkg --no-restore
}

# Restore all at once
Write-Host "  Restoring all packages..." -ForegroundColor Gray
dotnet restore

# ── Folder structure ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[6/6] Creating folder structure..." -ForegroundColor Yellow

# Core folders
$coreFolders = @(
    "$Core/Entities",
    "$Core/Enums",
    "$Core/Constants",
    "$Core/Interfaces",
    "$Core/Exceptions",
    "$Core/Common"
)

# Application folders
$appFolders = @(
    "$App/DTOs/Patient",
    "$App/DTOs/Staff",
    "$App/DTOs/Appointment",
    "$App/DTOs/Clinical",
    "$App/DTOs/Laboratory",
    "$App/DTOs/Pharmacy",
    "$App/DTOs/Billing",
    "$App/DTOs/Dashboard",
    "$App/Interfaces/Services",
    "$App/Interfaces/Repositories",
    "$App/Services",
    "$App/Validators",
    "$App/Mappings",
    "$App/Models",
    "$App/Features/Patients",
    "$App/Features/Appointments",
    "$App/Features/Clinical",
    "$App/Features/Laboratory",
    "$App/Features/Billing",
    "$App/Common"
)

# Infrastructure folders
$infraFolders = @(
    "$Infra/Data",
    "$Infra/Configurations/Entities",
    "$Infra/Identity",
    "$Infra/Repositories",
    "$Infra/Services",
    "$Infra/Persistence",
    "$Infra/Migrations",
    "$Infra/Seed",
    "$Infra/BackgroundJobs",
    "$Infra/Extensions"
)

# Web folders
$webFolders = @(
    "$Web/Components/Pages/Dashboard",
    "$Web/Components/Pages/Patients",
    "$Web/Components/Pages/Doctors",
    "$Web/Components/Pages/Nurses",
    "$Web/Components/Pages/Appointments",
    "$Web/Components/Pages/Clinical",
    "$Web/Components/Pages/Laboratory",
    "$Web/Components/Pages/Pharmacy",
    "$Web/Components/Pages/Admissions",
    "$Web/Components/Pages/WalkInQueue",
    "$Web/Components/Pages/Billing",
    "$Web/Components/Pages/Reports",
    "$Web/Components/Pages/Settings",
    "$Web/Components/Pages/Users",
    "$Web/Components/Shared",
    "$Web/Components/Layout",
    "$Web/Services",               # Blazor-specific UI services (state, navigation helpers)
    "$Web/Extensions"
)

# Test folders
$testFolders = @(
    "$Tests/Unit/Domain",
    "$Tests/Unit/Application",
    "$Tests/Integration/Repositories",
    "$Tests/Integration/Services",
    "$Tests/Integration/Workflows",
    "$Tests/Helpers"
)

$allFolders = $coreFolders + $appFolders + $infraFolders + $webFolders + $testFolders
foreach ($folder in $allFolders) {
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
}

# Add .gitkeep to empty folders so git tracks them
foreach ($folder in $allFolders) {
    $keep = Join-Path $folder ".gitkeep"
    if (-not (Test-Path $keep)) {
        New-Item -ItemType File -Path $keep -Force | Out-Null
    }
}

Write-Host "  All folders created." -ForegroundColor Green

# ── DI extension stubs ────────────────────────────────────────────────────────
# These are the extension method files that Program.cs will call.
# They start empty and fill up as we add services in later phases.

@"
using Microsoft.Extensions.DependencyInjection;

namespace $App;

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
"@ | Set-Content "$App/ApplicationServiceExtensions.cs"

@"
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace $Infra;

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
"@ | Set-Content "$Infra/InfrastructureServiceExtensions.cs"

# ── Minimal Program.cs ────────────────────────────────────────────────────────
@"
using $App;
using $Infra;
using $Web.Components;

var builder = WebApplication.CreateBuilder(args);

// ── Layer registrations ───────────────────────────────────────────────────────
// Each layer registers itself. Program.cs stays clean.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Blazor ────────────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Health Checks ─────────────────────────────────────────────────────────────
// Phase 09: add database, storage, external service checks here
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// ── Health check endpoint ─────────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ── Blazor ────────────────────────────────────────────────────────────────────
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
"@ | Set-Content "$Web/Program.cs"

# ── .gitignore ────────────────────────────────────────────────────────────────
@"
## .NET
bin/
obj/
*.user
*.suo
.vs/
*.userprefs
.DS_Store

## EF Migrations (keep — do not ignore)
# Migrations/ → tracked

## Secrets (NEVER commit)
**/appsettings.Production.json
**/appsettings.Secrets.json
**/secrets.json
**/*.pfx
**/*.p12

## Logs
logs/
*.log

## Test results
TestResults/
coverage/
"@ | Set-Content ".gitignore"

# ── appsettings stubs ─────────────────────────────────────────────────────────
@"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "HospitalSettings": {
    "Name": "Elshiekh Medical Complex",
    "ShortName": "EMC",
    "City": "Khartoum",
    "Country": "Sudan",
    "Phone": "",
    "Email": "",
    "PatientCodePrefix": "PT",
    "EmployeeCodePrefix": "EMP",
    "InvoicePrefix": "INV",
    "PaymentPrefix": "PAY"
  },
  "AllowedHosts": "*"
}
"@ | Set-Content "$Web/appsettings.json"

@"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ElsheiekhHMSDb_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
"@ | Set-Content "$Web/appsettings.Development.json"

# ── Build verification ────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Building solution to verify everything compiles..." -ForegroundColor Yellow
dotnet build --no-restore -v minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host "  PHASE 01 COMPLETE — Solution builds successfully          " -ForegroundColor Green
    Write-Host "============================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Build failed. Check errors above." -ForegroundColor Red
    exit 1
}

# ── Solution tree ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Solution structure:" -ForegroundColor Cyan
Write-Host ""
Write-Host "$Solution/"
Write-Host "├── $Core/                  ← Domain layer. Zero dependencies."
Write-Host "│   ├── Entities/           ← Patient, Doctor, Appointment, etc."
Write-Host "│   ├── Enums/              ← Gender, BloodGroup, Status enums"
Write-Host "│   ├── Constants/          ← Roles, Permissions, PolicyNames"
Write-Host "│   ├── Interfaces/         ← IRepository<T>, IUnitOfWork (domain contracts)"
Write-Host "│   ├── Exceptions/         ← NotFoundException, BusinessRuleException, etc."
Write-Host "│   └── Common/             ← BaseEntity, Result<T>, PagedResult<T>"
Write-Host "│"
Write-Host "├── $App/             ← Use cases, DTOs, service contracts"
Write-Host "│   ├── DTOs/               ← PatientListDto, CreatePatientDto, etc."
Write-Host "│   ├── Interfaces/         ← IPatientService, IAppointmentService, etc."
Write-Host "│   ├── Services/           ← PatientService, AppointmentService, etc."
Write-Host "│   ├── Validators/         ← Input validation logic"
Write-Host "│   ├── Mappings/           ← Entity ↔ DTO mapping"
Write-Host "│   ├── Features/           ← Feature-grouped use cases"
Write-Host "│   └── ApplicationServiceExtensions.cs"
Write-Host "│"
Write-Host "├── $Infra/       ← EF Core, Identity, repos, external services"
Write-Host "│   ├── Data/               ← ApplicationDbContext"
Write-Host "│   ├── Configurations/     ← IEntityTypeConfiguration<T> per entity"
Write-Host "│   ├── Identity/           ← ApplicationUser, role/permission setup"
Write-Host "│   ├── Repositories/       ← Concrete repository implementations"
Write-Host "│   ├── Services/           ← File storage, email, SMS, audit impl"
Write-Host "│   ├── Seed/               ← Roles, departments, settings bootstrap"
Write-Host "│   ├── Migrations/         ← EF Core generated migrations"
Write-Host "│   └── InfrastructureServiceExtensions.cs"
Write-Host "│"
Write-Host "├── $Web/                   ← Blazor Interactive Server (UI only)"
Write-Host "│   ├── Components/"
Write-Host "│   │   ├── Pages/          ← One subfolder per module"
Write-Host "│   │   ├── Shared/         ← Reusable Blazor components"
Write-Host "│   │   └── Layout/         ← MainLayout, NavMenu, etc."
Write-Host "│   ├── Services/           ← UI-only state/nav helpers"
Write-Host "│   └── Program.cs          ← App entry point"
Write-Host "│"
Write-Host "└── $Tests/                 ← xUnit tests"
Write-Host "    ├── Unit/               ← Pure logic, no DB"
Write-Host "    ├── Integration/        ← EF InMemory, service tests"
Write-Host "    └── Helpers/            ← Test builders, fakes, fixtures"
Write-Host ""

# ── Dependency rules ──────────────────────────────────────────────────────────
Write-Host "Dependency direction (enforced by project references):" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Web  →  Application  →  Core"
Write-Host "  Web  →  Infrastructure"
Write-Host "  Infrastructure  →  Application  →  Core"
Write-Host ""
Write-Host "  Core knows NOTHING about EF Core, Identity, or Blazor."
Write-Host "  Application knows NOTHING about SQL Server or HTTP."
Write-Host "  Web calls Application services only — never DbContext directly."
Write-Host ""

# ── Migration commands reference ──────────────────────────────────────────────
Write-Host "EF Core migration commands (Phase 04):" -ForegroundColor Cyan
Write-Host ""
Write-Host "  # Create migration"
Write-Host "  dotnet ef migrations add InitialCreate ``"
Write-Host "    --project $Infra ``"
Write-Host "    --startup-project $Web ``"
Write-Host "    --output-dir Migrations"
Write-Host ""
Write-Host "  # Apply migration"
Write-Host "  dotnet ef database update ``"
Write-Host "    --project $Infra ``"
Write-Host "    --startup-project $Web"
Write-Host ""
Write-Host "  # Remove last migration (before applying)"
Write-Host "  dotnet ef migrations remove ``"
Write-Host "    --project $Infra ``"
Write-Host "    --startup-project $Web"
Write-Host ""
Write-Host "  # Generate SQL script (for production review)"
Write-Host "  dotnet ef migrations script ``"
Write-Host "    --project $Infra ``"
Write-Host "    --startup-project $Web ``"
Write-Host "    --output migration.sql"
Write-Host ""

# ── Phase 01 completion checklist ────────────────────────────────────────────
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  PHASE 01 — Completion Checklist                          " -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  [x] Solution created: $Solution.sln"
Write-Host "  [x] ElsheiekhHMS.Core — classlib, zero dependencies"
Write-Host "  [x] ElsheiekhHMS.Application — classlib, refs Core"
Write-Host "  [x] ElsheiekhHMS.Infrastructure — classlib, refs Core + App"
Write-Host "  [x] ElsheiekhHMS.Web — Blazor Interactive Server, refs App + Infra"
Write-Host "  [x] ElsheiekhHMS.Tests — xUnit, refs Core + App + Infra"
Write-Host "  [x] All project references wired in correct direction"
Write-Host "  [x] Phase 01 NuGet packages installed"
Write-Host "  [x] Folder structure created (all modules)"
Write-Host "  [x] AddApplication() DI extension stub"
Write-Host "  [x] AddInfrastructure() DI extension stub"
Write-Host "  [x] Program.cs wired to both extension methods"
Write-Host "  [x] appsettings.json with HospitalSettings section"
Write-Host "  [x] appsettings.Development.json"
Write-Host "  [x] .gitignore"
Write-Host "  [x] Solution builds successfully (dotnet build)"
Write-Host ""
Write-Host "  Ready for PHASE 02 — Core Foundation" -ForegroundColor Green
Write-Host "  (BaseEntity, enums, exceptions, Result<T>, constants)"
Write-Host ""
