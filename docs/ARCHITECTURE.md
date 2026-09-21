# ElsheiekhHMS — Architecture

## 1. Purpose and evidence

This is the stable reference for the verified ElsheiekhHMS structure and the boundaries implementations must preserve. It separates **current implementation** from **planned capabilities**; it does not declare phase completion.

| Reference | Responsibility |
|---|---|
| PRD.md | Product requirements: what to build |
| DEVELOPMENT_ROADMAP.md | Implementation phases: when to build |
| README.md | Development progress and resume checkpoint |
| DECISIONS.md | Reasons for settled architectural choices |
| AGENTS.md | Repository operating instructions |
| graphify-out/ | Generated snapshot of repository structure |
| This document | Architecture, responsibilities, and boundaries |

Evidence was reviewed on 2026-09-20: the root solution, all five project files, Core foundation and tests, Web startup, layer registration extensions, README, PRD, roadmap, graph report, and SpecKit constitution.

Resolve information conflicts in this order: actual source/project files; current approved SpecKit specification; PRD; development roadmap; AGENTS; README; graph JSON; graph report. Source describes what exists, while specifications describe intent. This evidence order does not override system/developer/user instructions or justify retaining a defect.

## 2. Architectural principles

- Build and audit the backend before substantial HMS UI development.
- Separate domain rules, application orchestration, technical implementation, and presentation.
- Keep Core independent of persistence, presentation, and identity implementation.
- Use dependency inversion at technical boundaries; do not add abstractions before a concrete use case needs them.
- Enforce business rules and security on the backend, independently of UI visibility.
- Preserve clinical and financial history; make behavior testable and maintainable.
- Introduce infrastructure in its authorized phase, not because a library or plugin is available.

These principles are supported by README architecture rules, AGENTS, and the SpecKit constitution. They do not imply that future services already exist.

## 3. Solution and dependencies

All five members of [ElsheiekhHMS.slnx](../ElsheiekhHMS.slnx) target `net10.0`.

| Project | Responsibility | Current implementation | Direct project references |
|---|---|---|---|
| Core | Domain foundations and, later, domain behavior | Base classes, concurrency contract, exceptions | None |
| Application | Use-case contracts and orchestration | `AddApplication()` registration entry point; no service registrations | Core |
| Infrastructure | Persistence and external technical implementations | `ElsheiekhHmsDbContext`, SQL Server registration, 04B scalar Fluent mappings, and 04C relational metadata | Application, Core |
| Web | ASP.NET Core host, composition root, Blazor presentation | Interactive Server template, startup pipeline, health endpoint | Application, Infrastructure |
| Tests | Automated verification | Core foundation, 04A context-contract, and 04B/04C model-metadata xUnit tests | Core, Application, Infrastructure |

Arrows below mean **direct project references**, not runtime execution:

```text
Web -------------> Application -----> Core
 |                      ^              ^
 +--> Infrastructure ---+--------------+

Tests -------> Core
      +------> Application
      +------> Infrastructure
```

The table is the exact reference list.

Core has no PackageReference, FrameworkReference, or ProjectReference. Application uses DI abstractions; Infrastructure uses EF Core, the SQL Server provider, DI, and configuration abstractions. Tests use xUnit, its Visual Studio runner, Microsoft.NET.Test.Sdk, and coverlet.collector. Identity packages are not installed.

Forbidden directions include Core to any outer layer, Application to Infrastructure/Web, Infrastructure to Web, and production projects to Tests. Web has no direct Core project reference. Core must not depend on Blazor, EF Core, SQL Server implementation, or ASP.NET Core Identity implementation.

## 4. Conceptual application flow

The intended HMS operation is:

```text
Blazor presentation
    -> Application use case
        -> Core domain rules
        -> technical contract implemented by Infrastructure
            -> EF Core -> SQL Server
```

This is a **planned runtime collaboration**, not a Core-to-Infrastructure project dependency. Application orchestrates domain behavior and technical calls; Core does not invoke an EF implementation. Concrete contracts are introduced with justified use cases.

The host, layer registration entry points, Core foundations, approved Phase 04A–04C EF Core persistence model, inspected Phase 04D-B migration/snapshot, verified Phase 04D-C local SQL Server schema, and Phase 04E isolated persistence integration tests now exist. Application use cases remain future work. The frontend interacts with the application and must not become the business layer.

## 5. Core foundation

Verified in [Core/Common](../ElsheiekhHMS.Core/Common/), [Core/Interfaces](../ElsheiekhHMS.Core/Interfaces/), and [Core/Exceptions](../ElsheiekhHMS.Core/Exceptions/).

```text
BaseEntity
    +-- AuditableEntity
            +-- SoftDeletableEntity

System.Exception
    +-- DomainException
            +-- BusinessRuleException
            +-- DomainValidationException
```

| Type | Actual contract |
|---|---|
| BaseEntity | Abstract; integer `Id` with protected setter |
| AuditableEntity | Abstract; `CreatedAt` as DateTimeOffset, nullable `CreatedBy`, `UpdatedAt`, `UpdatedBy`; protected setters |
| SoftDeletableEntity | Abstract; `IsDeleted`, nullable `DeletedAt` and `DeletedBy`; protected setters |
| IHasConcurrencyToken | Independent opt-in interface: `byte[] RowVersion { get; set; }` |
| DomainException | Exception subclass with message and message/inner-exception constructors |
| BusinessRuleException | Sealed domain exception for business-rule violations |
| DomainValidationException | Sealed domain exception for invalid domain values |

Callers/derived domain behavior supply audit metadata. Comments require UTC timestamps, but these properties do not enforce UTC, populate themselves, or record an audit trail. Soft-delete flags do not implement deletion operations or database filtering.

The concurrency interface is **retained**. It is not inherited by the base classes. The approved Phase 03D `Appointment` and `WalkInQueueEntry` aggregates implement it; Phase 04 supplies EF Core/SQL Server conflict detection and mapping. No EF attributes or persistence behavior are embedded in Core. See ADR-008.

The implemented domain model currently includes the completed Department, Doctor, DoctorSchedule, and Patient types plus the Phase 03D Appointment and WalkInQueueEntry scheduling roots. Other HMS capabilities remain future work; folder presence alone does not establish an aggregate.

## 6. Application, Infrastructure, and Web boundaries

**Application:** the existing registration method returns the service collection without adding services. DTOs, validation, mapping, queries, pagination, use cases, and workflows are planned responsibilities, primarily Phases 06–08. Business orchestration belongs here; domain invariants belong in Core. No repository or result-wrapper API is established by roadmap examples alone.

**Infrastructure:** Phase 04A registers `ElsheiekhHmsDbContext` with SQL Server using the `ElsheiekhHmsDatabase` connection key. Phase 04B owns scalar Fluent mappings, Phase 04C owns explicit historical-safe relationships, approved indexes/uniqueness, soft-delete filters, and rowversion metadata for the opted-in entities, Phase 04D-B/04D-C contain the inspected migration and verified local schema, and Phase 04E provides isolated LocalDB persistence integration tests. Repositories and Unit of Work remain deferred to later gates; Identity implementation is Phase 05. Patient retains its validated public creation path plus a private EF-only materialization constructor, with getter-only `PatientCode` mapped through its compiler-generated backing field. Storage, in-app notifications, integrations, background processing, and logging integrations are later technical concerns only where product requirements justify them. An example comment mentioning email/SMS is not authorization to add them; PRD excludes external SMS/email delivery from current scope.

**Web:** [Program.cs](../ElsheiekhHMS.Web/Program.cs) calls both layer registration methods, configures Razor components with Interactive Server support, and maps static assets and `/health`. It configures HTTPS redirection, antiforgery, non-development exception handling/HSTS, and status-code re-execution. It is the composition root.

Future Blazor pages own presentation, navigation, input, and loading/error states. They call Application contracts. Important rules must not exist only in Razor components; hidden controls are not authorization. Current template middleware does not establish HMS authentication or authorization.

## 7. Testing architecture

[ElsheiekhHMS.Tests](../ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj) currently contains:

- `Unit/Domain/Common`: identity assignment, audit metadata defaults/supplied values, and soft-delete defaults.
- `Unit/Domain/Exceptions`: message and inner-exception preservation for the three exception types.
- `Unit/Domain/Organization`, `Unit/Domain/Patients`, and `Unit/Domain/Staff`: completed 03A–03C domain behavior.
- `Unit/Domain/Scheduling`: Phase 03D appointment and walk-in queue invariants and lifecycle transitions.
- `Unit/Infrastructure`: Phase 04A context contract plus 04B/04C EF model metadata, relationship, index, filter, uniqueness, concurrency, and shadow-FK assertions.
- `Integration/Persistence`: Phase 04E isolated SQL Server migration, materialization, relationship, uniqueness, concurrency, audit, and soft-delete verification.

The current source contains the foundation, completed 03A–03C, Phase 03D domain test groups, and the Phase 04E persistence integration suite. Test counts are reported from actual test runs rather than treated as architecture guarantees.

Testing evolves with implementation: foundation tests -> domain invariants -> Application services -> persistence/integration -> authorization/security and workflows -> UI/E2E. Earlier tests continue throughout the roadmap; Phase 10 expands and hardens testing rather than introducing xUnit for the first time. No future integration, security, or E2E suite is claimed as present.

## 8. Cross-cutting concerns

| Concern | Current foundation | Planned enforcement |
|---|---|---|
| Auditing | Creation/update metadata properties | Transactional actor/action/history recording; persistence foundation in Phase 04, workflows/services and audit implementation in Phases 07–09 |
| Soft deletion | Flag and deletion metadata | Phase 04 query filters and persistence rules; authorized domain/use-case behavior |
| Optimistic concurrency | Opt-in opaque token contract | Phase 03 aggregate selection; Phase 04 SQL Server/EF mapping and conflict detection |
| Validation | DomainValidationException | Domain invariants in Phase 03; DTO/input validation in Phase 06 and use-case enforcement thereafter |
| Authorization | No HMS authorization implementation | Phase 05 Identity, roles, permissions/policies; backend checks for subsequent use cases |
| Transactions | No persistence or transaction implementation | Phase 04 persistence support, Phases 07–08 operation boundaries; state change and audit entry must commit together |
| Exceptions | Domain exception hierarchy; template Web error pipeline | Consistent application/transport handling as use cases are implemented; no internal details exposed |
| Logging | ASP.NET Core host infrastructure | Use-case diagnostics and structured integrations later; exclude secrets and sensitive payloads |
| Configuration | ASP.NET Core builder configuration passed to Infrastructure | Environment-appropriate settings and secret management; no credentials in source |
| Health checks | Basic `/health` mapping | Dependency checks when real dependencies exist; current endpoint does not prove database readiness |
| History preservation | Soft-delete metadata only | Preserve clinical/financial history and audit records; no casual destructive deletion |
| Async/cancellation | No database operations yet | Async I/O with cancellation support where available across future application/persistence operations |

The SpecKit constitution requires transactional audit records and backend authorization. Those are requirements, not already-implemented guarantees. Its audit rule takes precedence over roadmap examples that place audit writing after saving without a demonstrated shared transaction.

## 9. Data and security architecture

**Persistence status:** SQL Server with EF Core is configured in Infrastructure through the Phase 04A `ElsheiekhHmsDbContext` foundation. Phase 04B provides six entity configurations with scalar/table/key/audit/temporal/enum metadata and the approved Patient materialization accommodation. Phase 04C provides explicit relationships with Restrict delete behavior, approved lookup and uniqueness indexes, soft-delete query filters for Patient/Appointment/WalkInQueueEntry, and rowversion metadata for Patient/Appointment/WalkInQueueEntry. Phase 04D-B generated and inspected the initial migration and model snapshot; Phase 04D-C applied it only to the approved local `MSSQLLocalDB` database `ElsheiekhHMS_Dev` and verified the physical schema, migration history, startup, and health endpoint. Phase 04E verifies the migration and persistence behavior against a separate exact-target LocalDB database, removes that test database after each run, and leaves the development database untouched. Core entities remain persistence-independent apart from the approved private Patient materialization constructor; no repositories or Unit of Work are implemented.

**Planned security:** ASP.NET Core Identity integration starts in Phase 05. Authentication establishes identity; backend roles, permissions, and policies authorize operations. UI indicators improve usability but cannot replace those checks. User identities referenced by Core audit metadata remain opaque strings rather than Identity implementation types. Authentication, permission design, and account lifecycle are not established by the current template.

## 10. Future HMS domain map

These are logical capabilities, not current classes, bounded contexts, or approved aggregate definitions.

| Capability group | Documented scope |
|---|---|
| Patient and access workflows | Registration, demographics, vitals, walk-in queue, appointments |
| Staff and organization | Staff, departments, specialties |
| Clinical | EMR, consultations, diagnoses, treatment plans |
| Admissions | Wards, beds, admission, transfer, discharge |
| Laboratory | Orders, samples, results |
| Pharmacy | Prescriptions, dispensing, stock |
| Finance | Billing, payments, receipts |
| Administration | Users/roles, settings, in-app notifications, audit, dashboard |
| Deferred extensions | Reporting and insurance appear as future roadmap capabilities; not current v1 implementation |

PRD scope and prioritization govern delivery. This map creates no Phase 03 entities and makes no claim that deferred modules have approved detailed specifications.

## Architecture Invariants

1. Core has no outward project dependency and contains no EF/Identity/UI implementation.
2. Preserve the verified five-project reference graph unless an architectural change is explicitly approved.
3. Application orchestrates use cases; Core owns domain invariants; Infrastructure implements technical concerns.
4. Blazor is presentation, not the business or persistence layer.
5. Enforce security and important business rules on the backend.
6. Keep concurrency opt-in; Core does not implement conflict detection.
7. Preserve important clinical/financial history and exclude secrets from logs/audits.
8. Do not introduce future-phase technologies or speculative abstractions prematurely.
9. Verify architectural code changes with builds, tests, and dependency inspection.
10. Generated graphs never override exact source/project evidence.

## 11. Verification and documentation discrepancies

From the repository root, using the required environment wrapper where applicable:

```powershell
dotnet restore ElsheiekhHMS.slnx
dotnet build ElsheiekhHMS.slnx --no-restore
dotnet test ElsheiekhHMS.slnx --no-build
.\tools\graphify.ps1 -Check
```

Inspect solution membership and actual ProjectReference/PackageReference items. Refresh after meaningful structural change using `.\tools\graphify.ps1`; generic `graphify update` can replace this custom schema. Check source/generator fingerprints as well as timestamp, commit, and dirty state. A historical commit stamp alone is not proof of structural staleness.

Known conflicts are preserved for explicit reconciliation:

- Roadmap sections 3–4, 15–16 describe .NET 9 MVC projects, later completed modules, and a future Blazor migration. This checkout already has five .NET 10 projects and a Blazor template, with no such HMS modules or persistence. Do not recreate that claimed migration.
- Roadmap Phase 02 examples claim enums and ServiceResult; actual Core contains the foundation documented above. Its testing/package timeline also conflicts with already-present xUnit.
- PRD is marked Draft, names framework version 9 and a different PRD path, and uses a four-stage product schedule rather than the engineering twelve-phase sequence.
- README still records Phase 02 final review pending. This document does not promote the recorded phase.
- AGENTS omits DEVELOPMENT_ROADMAP from its earlier context hierarchy and retains an unresolved concurrency note. The current documentation request explicitly confirms the retained interface; ADR-008 records that final decision.
- SpecKit has nested duplicate scaffolding and no discovered feature specification outside templates. Its dependency arrow and statement assigning all business rules to Application conflict with the verified references and Core domain-rule responsibility. Do not use those statements to reverse dependencies.
- Roadmap historical bug-fix stories and service names are not verified history of this checkout. They are not imported as Accepted ADRs.

## Related Documentation

- [Repository instructions](../AGENTS.md)
- [Progress and resume checkpoint](../README.md)
- [Product requirements](../PRD.md)
- [Development roadmap](../DEVELOPMENT_ROADMAP.md)
- [Architecture decisions](DECISIONS.md)
- [Generated architecture report](../graphify-out/GRAPH_REPORT.md)
- [Interactive architecture explorer](../graphify-out/graph.html)
- [Detailed specification workspace](../speckit/my-project/)
