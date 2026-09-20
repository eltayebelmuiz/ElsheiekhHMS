# ElsheiekhHMS Architecture Graph Report

## Snapshot

- **generatedAtUtc:** `2026-09-20T13:34:34.7722575+00:00`
- **gitBranch:** `main`
- **gitCommit:** `70433a65532516376ba76a6db45a6475ed4d1ebc`
- **workingTreeDirty:** `True`
- **sourceFingerprint:** `d6eaf90da98f7a4bae3857758e23847458dd62df13c6d1efd3cbbd593985914b`
- Framework: net10.0
- Structural extraction only: **0 LLM API calls / 0 LLM tokens**. No statistical community clustering is claimed; project ownership supplies the visual groups.

## Solution

`ElsheiekhHMS.slnx`

- [ElsheiekhHMS.Application](../ElsheiekhHMS.Application/ElsheiekhHMS.Application.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Core](../ElsheiekhHMS.Core/ElsheiekhHMS.Core.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Infrastructure](../ElsheiekhHMS.Infrastructure/ElsheiekhHMS.Infrastructure.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Tests](../ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Web](../ElsheiekhHMS.Web/ElsheiekhHMS.Web.csproj) — net10.0; solution member: True

## Project Dependencies

Arrows mean references; extracted from evaluated ProjectReference items.

```text
ElsheiekhHMS.Application -> ElsheiekhHMS.Core
ElsheiekhHMS.Core -> (none)
ElsheiekhHMS.Infrastructure -> ElsheiekhHMS.Application, ElsheiekhHMS.Core
ElsheiekhHMS.Tests -> ElsheiekhHMS.Application, ElsheiekhHMS.Core, ElsheiekhHMS.Infrastructure
ElsheiekhHMS.Web -> ElsheiekhHMS.Application, ElsheiekhHMS.Infrastructure
```

## Core Foundation

- [ElsheiekhHMS.Core.Common.AuditableEntity](../ElsheiekhHMS.Core/Common/AuditableEntity.cs) — abstract-class, public
- [ElsheiekhHMS.Core.Common.BaseEntity](../ElsheiekhHMS.Core/Common/BaseEntity.cs) — abstract-class, public
- [ElsheiekhHMS.Core.Common.SoftDeletableEntity](../ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs) — abstract-class, public
- [ElsheiekhHMS.Core.Exceptions.BusinessRuleException](../ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs) — class, public
- [ElsheiekhHMS.Core.Exceptions.DomainException](../ElsheiekhHMS.Core/Exceptions/DomainException.cs) — class, public
- [ElsheiekhHMS.Core.Exceptions.DomainValidationException](../ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs) — class, public
- [ElsheiekhHMS.Core.Interfaces.IHasConcurrencyToken](../ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs) — interface, public

## Inheritance and Exceptions

Derived -> base (includes private test helper types and resolved external bases):

- `ElsheiekhHMS.Core.Common.AuditableEntity` -> `ElsheiekhHMS.Core.Common.BaseEntity` — ElsheiekhHMS.Core/Common/AuditableEntity.cs:L3
- `ElsheiekhHMS.Core.Common.SoftDeletableEntity` -> `ElsheiekhHMS.Core.Common.AuditableEntity` — ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs:L3
- `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` -> `ElsheiekhHMS.Core.Exceptions.DomainException` — ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs:L4
- `ElsheiekhHMS.Core.Exceptions.DomainException` -> `System.Exception` — ElsheiekhHMS.Core/Exceptions/DomainException.cs:L3
- `ElsheiekhHMS.Core.Exceptions.DomainValidationException` -> `ElsheiekhHMS.Core.Exceptions.DomainException` — ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs:L4
- `ElsheiekhHMS.Tests.Unit.Domain.Common.AuditableEntityTests.TestEntity` -> `ElsheiekhHMS.Core.Common.AuditableEntity` — ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs:L28
- `ElsheiekhHMS.Tests.Unit.Domain.Common.BaseEntityTests.TestEntity` -> `ElsheiekhHMS.Core.Common.BaseEntity` — ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs:L15
- `ElsheiekhHMS.Tests.Unit.Domain.Common.SoftDeletableEntityTests.TestEntity` -> `ElsheiekhHMS.Core.Common.SoftDeletableEntity` — ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs:L18

## Interfaces

- `ElsheiekhHMS.Core.Interfaces.IHasConcurrencyToken`
  - Implementations: none discovered

## Tests

4 classes; 6 methods; 10 statically enumerable cases. This is discovery from source, not an execution result.

- [ElsheiekhHMS.Tests.Unit.Domain.Common.AuditableEntityTests](../ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs) — 2 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Common.BaseEntityTests](../ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs) — 1 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Common.SoftDeletableEntityTests](../ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs) — 1 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.SoftDeletableEntity` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Exceptions.DomainExceptionTests](../ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs) — 6 cases
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainValidationException` (source type/member binding; includes inherited foundation dependencies)

## Packages

Evaluated direct PackageReference items only. Framework/SDK auto-references and transitives are outside this list.

- **ElsheiekhHMS.Application:** Microsoft.Extensions.DependencyInjection.Abstractions 10.0.12
- **ElsheiekhHMS.Core:** none
- **ElsheiekhHMS.Infrastructure:** Microsoft.Extensions.Configuration.Abstractions 10.0.12; Microsoft.Extensions.DependencyInjection.Abstractions 10.0.12
- **ElsheiekhHMS.Tests:** coverlet.collector 6.0.4; Microsoft.NET.Test.Sdk 17.14.1; xunit 2.9.3; xunit.runner.visualstudio 3.1.4
- **ElsheiekhHMS.Web:** none

## Architecture Checks

- coreIndependent: `True`
- circularProjectDependenciesDetected: `False`
- unexpectedDependencies: `[]`
- forbiddenCoreUsings: `[]`
- ruleSource: `Documented HMS layer policy; observed edges extracted separately`
- efCorePresent: `False`
- sqlServerPresent: `False`
- identityPackagePresent: `False`
- Identity source references: `[]`

## Current Development Phase

Declared by README; not inferred from the existence of classes:

- > **Current development stage:** Phase 02 — Core Foundation   — README.md:L5
- > **Phase 01:** ✅ Complete   — README.md:L6
- > **Phase 02 Setup:** ✅ Complete and verified   — README.md:L7
- > **Next action:** Final Phase 02 review/checkpoint, then Phase 03 — Domain Entities — README.md:L8
-  /  01  /  Solution & Architecture  /  ✅ Complete  /  — README.md:L325
-  /  02  /  Core Foundation  /  🟡 Setup complete / final review pending  /  — README.md:L326
-  /  03  /  Domain Entities  /  ⏳ Not started  /  — README.md:L327
-  /  04  /  EF Core & Database  /  ⏳ Not started  /  — README.md:L328
-  /  05  /  Identity & Security  /  ⏳ Not started  /  — README.md:L329
-  /  06  /  DTOs & Validation  /  ⏳ Not started  /  — README.md:L330
-  /  07  /  Application Services  /  ⏳ Not started  /  — README.md:L331
-  /  08  /  Business Workflows  /  ⏳ Not started  /  — README.md:L332
-  /  09  /  Enterprise Infrastructure  /  ⏳ Not started  /  — README.md:L333

## Important Files

- [ElsheiekhHMS.Application/ApplicationServiceExtensions.cs](../ElsheiekhHMS.Application/ApplicationServiceExtensions.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Core/Common/AuditableEntity.cs](../ElsheiekhHMS.Core/Common/AuditableEntity.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Common/BaseEntity.cs](../ElsheiekhHMS.Core/Common/BaseEntity.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs](../ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs](../ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Exceptions/DomainException.cs](../ElsheiekhHMS.Core/Exceptions/DomainException.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs](../ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs](../ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs](../ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Web/Components/App.razor](../ElsheiekhHMS.Web/Components/App.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Layout/MainLayout.razor](../ElsheiekhHMS.Web/Components/Layout/MainLayout.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Layout/NavMenu.razor](../ElsheiekhHMS.Web/Components/Layout/NavMenu.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Layout/ReconnectModal.razor](../ElsheiekhHMS.Web/Components/Layout/ReconnectModal.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Counter.razor](../ElsheiekhHMS.Web/Components/Pages/Counter.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Error.razor](../ElsheiekhHMS.Web/Components/Pages/Error.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Home.razor](../ElsheiekhHMS.Web/Components/Pages/Home.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/NotFound.razor](../ElsheiekhHMS.Web/Components/Pages/NotFound.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Weather.razor](../ElsheiekhHMS.Web/Components/Pages/Weather.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Routes.razor](../ElsheiekhHMS.Web/Components/Routes.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/_Imports.razor](../ElsheiekhHMS.Web/Components/_Imports.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Program.cs](../ElsheiekhHMS.Web/Program.cs) — ElsheiekhHMS.Web

## Graph Freshness

`generatedAtUtc` and Git metadata describe generation time. Dirty status includes unrelated local changes. Git commit alone cannot describe uncommitted source.
`sourceManifest` hashes source, project, solution, Razor, build configuration and README inputs. Each sorted record is relative path + NUL + SHA-256; records are joined with LF and hashed again. All graphify-out directories and build output are excluded. `generatorFingerprint` also detects tooling changes.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\graphify.ps1 -Check
powershell -ExecutionPolicy Bypass -File .\tools\graphify.ps1
```

## Usage and Limits

- `graph.json`: machine-readable Codex architecture context. `edges` is canonical; `links` is an identical node-link compatibility alias.
- `graph.html`: offline, self-contained interactive explorer with project, Core, test and full-structure views.
- `GRAPH_REPORT.md`: human navigation index generated from the same JSON data.
- Read README and this index first. Graphify is an architectural index, NOT authoritative source code. Read the relevant source before reasoning about implementation or editing it. Regenerate if fingerprints differ.
- Refresh these outputs with tools/graphify.ps1. The generic Graphify CLI updater uses another schema and may replace this custom metadata. Existing CLI caches and project-local snapshots have not been refreshed or removed.
- Single combined C# analysis compilation; not a replacement for project-by-project compilation.
- Only resolved bases and test targets become edges; unresolved relationships are omitted with warnings.
- testCount is static Fact + InlineData case count, not a test execution result; dynamic theories reported separately.
- Package list is evaluated direct PackageReference only; transitive and SDK auto-references are not claimed as installed direct packages.
- Architecture checks are structural observations, not a full semantic security or dependency audit.
