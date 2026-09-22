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
- Prefer measured, predictable performance and reliable failure handling; correctness, security, and data integrity take precedence over latency.

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

Core has no PackageReference, FrameworkReference, or ProjectReference. Application uses DI abstractions; Infrastructure uses EF Core, the SQL Server provider, DI, configuration abstractions, and the approved `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12 package. Tests use xUnit, its Visual Studio runner, Microsoft.NET.Test.Sdk, and coverlet.collector. Identity remains confined to Infrastructure.

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

The host, layer registration entry points, Core foundations, approved Phase 04A–04C EF Core persistence model, inspected Phase 04D-B migration/snapshot, verified Phase 04D-C local SQL Server schema, Phase 04E isolated persistence integration tests, Phase 05A Identity foundation, Phase 05B role/policy foundation, Phase 05D Identity/AuditLog migration and SQL verification, the Phase 07S allocator infrastructure prerequisite, the approved 07A Patient Application Service, the implemented 07B Department Application Service, the implemented 07C-P AppointmentCode allocator prerequisite, the implemented 07C Appointment Application Service, the implemented 07D Queue Application Service, the implemented 08B-P Appointment↔Queue durable-link prerequisite, and the implemented 08B Appointment Arrival & Queue Handoff workflow and the implemented Phase 09A/09B observability/readiness infrastructure now exist. Phase 07 is complete for the approved service scope, Phase 08 is complete with 08A, 08B-P, and 08B, and Phase 09 is complete with 09A and 09B; no 08C or 09C workflow is required. The conditional 07E Doctor/Provider Application Service is explicitly deferred because Phase06 Doctor/Provider contracts, a service contract, Doctor↔ApplicationUser ownership mapping and persistence, and ownership-aware authorization are not approved. The frontend interacts with the application and must not become the business layer.

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

**Application:** 07A adds `IPatientService`, `PatientService`, immutable `ServiceResult<T>`/`ServiceError` contracts, and the narrow EF-free `IPatientPersistence` port. 07B adds `IDepartmentService`, `DepartmentService`, the bounded `DepartmentSearchRequest` contract/validator, and the narrow EF-free `IDepartmentPersistence` port. 07C adds `IAppointmentService` and its narrow persistence port; 07D adds `IQueueService` and its narrow persistence port. 08B-P extends the queue contract minimally with nullable Appointment links, controlled appointment-linked creation, and indexed lookup. 08B adds the thin EF-free `IAppointmentArrivalQueueService` orchestration over existing Appointment and Queue services; it has no cross-service transaction or workflow audit event. Phase 05B adds plain role/policy vocabulary under `Common/Security`, and 05C-A adds the plain `IAuditEventWriter` contract, request model, and stable audit vocabulary under `Common/Auditing`. Phase 06 adds persistence-neutral immutable DTO contracts, request validation, bounded pagination/search/sort contracts, and projection-ready summary/detail shapes; it does not include an approved Doctor/Provider DTO family. Business orchestration belongs here; domain invariants belong in Core. No generic repository, Unit of Work, or CQRS abstraction is established. Provider-scoped operations remain deferred until Doctor↔ApplicationUser ownership and authorization are approved.

**Infrastructure:** Phase 04A registers `ElsheiekhHmsDbContext` with SQL Server using the `ElsheiekhHmsDatabase` connection key. Phase 04B owns scalar Fluent mappings, Phase 04C owns explicit historical-safe relationships, approved indexes/uniqueness, soft-delete filters, and rowversion metadata for the opted-in entities, Phase 04D-B/04D-C contain the inspected migration and verified local schema, and Phase 04E provides isolated LocalDB persistence integration tests. Phase 05A adds `ApplicationUser` under `Infrastructure/Identity`, changes the existing context to `IdentityDbContext<ApplicationUser>`, and registers Identity EF stores/roles/options. Phase 05C-A adds the append-only `AuditLog` persistence model, bounded EF configuration, and server-controlled writer. Phase 05D adds the single approved additive Identity/AuditLog migration, verifies the development and isolated integration schemas, and removes the temporary pending-model suppression. Phase 05E adds the central account-login eligibility service and explicit secret-backed administrator bootstrap primitive; account lifecycle workflows remain deferred. 08B-P adds the nullable restrictive Queue→Appointment FK and filtered unique Appointment link index in `AddAppointmentQueueLink`; existing queue rows remain unlinked. Phase 09A adds built-in W3C request correlation and safe structured completion logging; Phase 09B adds tagged anonymous `/health/live` process liveness, `/health/ready` SQL `CanConnectAsync` readiness, and preserves `/health` as liveness. No third-party telemetry, retries, pooling, caches, brokers, or background jobs are introduced. Repositories and Unit of Work remain deferred to later gates; roles/policies, current-user auditing, and durable event-producing workflows remain later Phase 05 subphases. Patient retains its validated public creation path plus a private EF-only materialization constructor, with getter-only `PatientCode` mapped through its compiler-generated backing field. Storage, in-app notifications, integrations, background processing, and logging integrations are later technical concerns only where product requirements justify them. An example comment mentioning email/SMS is not authorization to add them; PRD excludes external SMS/email delivery from current scope.

**Web:** [Program.cs](../ElsheiekhHMS.Web/Program.cs) calls both layer registration methods and the Phase 05B `AddHmsAuthorization` composition extension, configures the Identity application cookie, request authentication/authorization middleware, Interactive Server authentication-state revalidation, scoped stable-user propagation, security headers, Razor components, and the anonymous `/health/live`, `/health/ready`, and legacy `/health` endpoints plus Phase 09A request observability middleware. It configures HTTPS redirection, antiforgery, non-development exception handling/HSTS, and status-code re-execution. It is the composition root. Account lifecycle workflows and event-producing workflows remain later boundaries.

Future Blazor pages own presentation, navigation, input, and loading/error states. They call Application contracts. Important rules must not exist only in Razor components; hidden controls are not authorization. Current template middleware does not establish HMS authentication or authorization.

### Phase 07 closeout

Phase 07 Application Services is complete for the approved scope: 07S, 07A, 07B, 07C-P, 07C, and 07D are complete. 07E Doctor/Provider Application Service is deferred. Before 07E implementation, the project must approve Phase06-style Doctor/Provider contracts, service operations, a Doctor↔ApplicationUser relationship, ownership persistence, ownership-aware authorization, any required schema migration, and its tests. The existing Doctor domain entity, Department relationship, Appointment relationship, and EF persistence model are present; Doctor is not missing. Phase 08 Business Workflows is complete: 08A, 08B-P, and 08B are complete, and no 08C workflow is required. Phase 09 is complete: 09A structured observability and 09B SQL Server readiness are implemented; 09C is not required.

### Phase 08 closeout

The approved operational flow is Patient registration or existing Patient selection,
Appointment scheduling, explicit staff arrival/check-in, Appointment-linked Queue
handoff, and Queue lifecycle. Check-in alone does not create a Queue entry; 08B
coordinates the handoff. Queue is operational history and is not an Encounter.
Appointment cancellation does not automatically cancel Queue history, and the
existing cancellation path cannot cancel a CheckedIn Appointment. Provider-owned
workflows remain blocked by 07E, Patient self-service remains blocked by missing
Patient ownership, and clinical, laboratory, billing, inpatient, pharmacy, and
notification workflows require separate approved domain designs.

### Phase 09 status

Phase 09 Enterprise Infrastructure is complete for the approved scope. 09A uses
built-in `ILogger` scopes and W3C `Activity` correlation with monotonic request
duration and safe status completion logs; it performs no database lookup solely
for logging and excludes sensitive request data. 09B exposes anonymous process
liveness at `/health` and `/health/live` without SQL, and anonymous application
readiness at `/health/ready` using `DbContext.Database.CanConnectAsync`. Readiness
does not migrate, seed, write, or expose raw database errors. No third-party
observability, retry, pooling, cache, broker, or background infrastructure was
introduced. Phase 10 is in progress; 10A authorization/host-security, 10B SQL concurrency/atomicity, 10C lifecycle/workflow, and 10D controlled performance/production-readiness verification are complete for the implementation scope, while final Phase 10 closeout remains.

## 7. Testing architecture

[ElsheiekhHMS.Tests](../ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj) currently contains:

- `Unit/Domain/Common`: identity assignment, audit metadata defaults/supplied values, and soft-delete defaults.
- `Unit/Domain/Exceptions`: message and inner-exception preservation for the three exception types.
- `Unit/Domain/Organization`, `Unit/Domain/Patients`, and `Unit/Domain/Staff`: completed 03A–03C domain behavior.
- `Unit/Domain/Scheduling`: Phase 03D appointment and walk-in queue invariants and lifecycle transitions.
- `Unit/Infrastructure`: Phase 04A context contract plus 04B/04C EF model metadata, relationship, index, filter, uniqueness, concurrency, and shadow-FK assertions.
- `Unit/Application/Security`: Phase 05B canonical role and policy vocabulary assertions.
- `Unit/Application/Auditing`: Phase 05C-A category/action vocabulary assertions.
- `Unit/Infrastructure/Identity`: Phase 05A `ApplicationUser`, Identity model metadata, approved password/lockout registration assertions, Phase 05B role-seeder assertions, and 05E account-login eligibility/bootstrap registration assertions.
- `Unit/Infrastructure/Auditing`: Phase 05C-A AuditLog model and writer assertions.
- `Integration/Persistence`: Phase 04E isolated SQL Server migration, materialization, relationship, uniqueness, concurrency, audit, and soft-delete verification, plus Phase 05D Identity/AuditLog migration and SQL verification.
- `Unit/Infrastructure/Allocation` and `Integration/Persistence`: Phase 07S allocator contracts, atomic allocation behavior, migration/schema verification, and queue uniqueness by operational date.
- `Unit/Application/Patients` and `Integration/Persistence`: 07A Patient service contracts, validation, authorization, transactional auditing, concurrency, bounded queries, and isolated SQL persistence.
- `Unit/Application/Departments` and `Integration/Persistence`: 07B Department contracts, validation, SystemAdministrator-only authorization, transactional auditing, bounded projected queries, lifecycle behavior, and isolated SQL persistence.
- `Unit/Application/Appointments` and `Unit/Application/Queue`: 07C Appointment and 07D Queue contracts, validation, authorization, bounded projections, lifecycle behavior, allocator/collision handling, and transactional auditing.
- `Integration/Persistence`: 08B-P nullable Queue→Appointment materialization, restrictive delete behavior, filtered unique Appointment link, and isolated migration verification.

The current source contains the foundation, completed 03A–03C, Phase 03D domain test groups, the Phase 04E persistence integration suite, focused Phase 05A Identity/security-model tests, focused Phase 05B vocabulary/seeder tests, Phase 05C-A AuditLog vocabulary/model/writer tests, Phase 05D Identity/AuditLog SQL tests, 05E account eligibility/bootstrap registration tests, 07S allocator tests, 07A Patient service tests, 07B Department service tests, 07C-P AppointmentCode allocator tests, 07C Appointment service tests, and 07D Queue service tests. Test counts are reported from actual test runs rather than treated as architecture guarantees.

Testing evolves with implementation: foundation tests -> domain invariants -> Application services -> persistence/integration -> authorization/security and workflows -> UI/E2E. Earlier tests continue throughout the roadmap; Phase 10 expands and hardens testing rather than introducing xUnit for the first time. No future integration, security, or E2E suite is claimed as present.

## 8. Cross-cutting concerns

| Concern | Current foundation | Planned enforcement |
|---|---|---|
| Auditing | Phase 05C stable UserId entity lifecycle stamping plus Phase 05C-A bounded append-only AuditLog model/writer foundation and Phase 05D SQL migration | Event-producing security/business workflows, clinical auditing, read-access auditing, and retention policy remain separately gated |
| Soft deletion | Flag and deletion metadata | Phase 04 query filters and persistence rules; authorized domain/use-case behavior |
| Optimistic concurrency | Opt-in opaque token contract | Phase 03 aggregate selection; Phase 04 SQL Server/EF mapping and conflict detection |
| Validation | DomainValidationException | Domain invariants in Phase 03; DTO/input validation in Phase 06 and use-case enforcement thereafter |
| Authorization | Phase 05B canonical roles, eight named policies, authenticated fallback policy, anonymous health metadata, Identity cookie enforcement, and Interactive Server revalidation; resource checks remain deferred | Later Application workflows |
| Transactions | No persistence or transaction implementation | Phase 04 persistence support, Phases 07–08 operation boundaries; state change and audit entry must commit together |
| Exceptions | Domain exception hierarchy; template Web error pipeline | Consistent application/transport handling as use cases are implemented; no internal details exposed |
| Logging | ASP.NET Core host infrastructure | Use-case diagnostics and structured integrations later; exclude secrets and sensitive payloads |
| Configuration | ASP.NET Core builder configuration passed to Infrastructure | Environment-appropriate settings and secret management; no credentials in source |
| Health checks | Basic `/health` mapping | Dependency checks when real dependencies exist; current endpoint does not prove database readiness |
| History preservation | Soft-delete metadata only | Preserve clinical/financial history and audit records; no casual destructive deletion |
| Async/cancellation | No database operations yet | Async I/O with cancellation support where available across future application/persistence operations |
| Performance | No application workflows or performance claim yet | Measure important workflows; use bounded queries, server-side filtering/sorting/pagination, projections, and query-count review where justified |
| Reliability | Template exception pipeline and basic health endpoint | Centralized safe errors, structured logging, dependency failure handling, timeouts, cancellation, transaction boundaries, and resource-lifetime review |

The SpecKit constitution requires transactional audit records and backend authorization. Those are requirements, not already-implemented guarantees. Its audit rule takes precedence over roadmap examples that place audit writing after saving without a demonstrated shared transaction.

## 9. Data and security architecture

**Persistence status:** SQL Server with EF Core is configured in Infrastructure through the Phase 04A `ElsheiekhHmsDbContext` foundation. Phase 04B provides six entity configurations with scalar/table/key/audit/temporal/enum metadata and the approved Patient materialization accommodation. Phase 04C provides explicit relationships with Restrict delete behavior, approved lookup and uniqueness indexes, soft-delete query filters for Patient/Appointment/WalkInQueueEntry, and rowversion metadata for Patient/Appointment/WalkInQueueEntry. Phase 04D-B generated and inspected the initial migration and model snapshot; Phase 04D-C applied it only to the approved local `MSSQLLocalDB` database `ElsheiekhHMS_Dev` and verified the physical schema, migration history, startup, and health endpoint. Phase 04E verifies the migration and persistence behavior against a separate exact-target LocalDB database, removes that test database after each run, and leaves the development database untouched. Phase 05D applies and verifies the approved additive Identity/AuditLog migration in the development database and isolated integration database, confirms migration history and physical schema, and removes the temporary pending-model warning suppression. Phase 07S adds Infrastructure-only, database-backed PatientCode and queue-ticket allocators using serialized atomic database operations; 07C-P adds the UTC year-scoped AppointmentCode allocator, its dedicated allocation table, and the physical unique AppointmentCode index in `AddAppointmentCodeAllocator`, which has been applied and physically verified in `ElsheiekhHMS_Dev`. 07A consumes PatientCode allocation through a narrow Application port and persists Patient plus staged AuditLog in one save. These allocators are restart- and multi-instance-safe and do not use MAX+1 or process-local counters. Queue uniqueness is `(QueueDate, SequenceNumber)` with no Department dimension, and Patient phone remains non-unique. Core entities remain persistence-independent apart from the approved private Patient materialization constructor; no repositories, Unit of Work, MediatR, or CQRS are implemented.

**Security status:** Phase 05A establishes the Infrastructure `ApplicationUser`, same-context Identity model, EF stores, approved password/lockout options, and the approved security-model amendment. Phase 05B adds the five canonical roles, eight named policies, authenticated fallback authorization, anonymous health metadata, and a deterministic password-free role seeder without changing `ApplicationUser`. Phase 05C adds the Application `ICurrentUser` contract, Web claims adapter, UTC `TimeProvider`, and centralized Infrastructure entity lifecycle auditing using stable UserId attribution. Phase 05C-A adds the Application audit vocabulary/writer contract and Infrastructure bounded append-only `AuditLog` model/writer; no event-producing account or business workflows are implied. Phase 05D applies the single approved additive Identity/AuditLog migration, verifies both SQL Server targets, and preserves the unchanged `InitialCreate`. Phase 05E composes the Identity application cookie with eight-hour sliding expiry, HttpOnly/Secure/Lax settings, request-level security-stamp and administrative-state validation, five-minute Interactive Server circuit revalidation, scoped stable-user propagation, antiforgery retention, report-only CSP and baseline security headers, and an explicit secret-backed administrator bootstrap primitive. No forwarded-header trust is configured because deployment proxy networks are not yet approved; arbitrary public forwarding headers are not trusted. `ApplicationUser` continues to use the persistence-sensitive `AccountSecurityState : byte` values `Active = 0`, `Suspended = 1`, and `Banned = 2`, plus independent `LoginAllowed`. The finalized Phase 05 design preserves separate domain-person, login-identity, session, account-state, lockout, role, and policy concepts. Password-reset administration, username account-management workflows, production retention duration, IP/UserAgent capture, clinical/read auditing, durable event-producing workflows, and UI remain separately gated. Authentication establishes identity; backend roles, permissions, and policies authorize operations. UI indicators cannot replace those checks. User identities referenced by Core audit metadata remain opaque strings rather than Identity implementation types.

**Performance and reliability status:** The approved cross-cutting requirement applies to all remaining phases. No Redis, distributed cache, broker, microservice, CQRS framework, event bus, additional database, generic repository, custom pool, or speculative background-processing infrastructure is authorized without measured need. Future database work must justify indexes by query/business need, keep result sets bounded, prefer server-side filtering/sorting/pagination and projections, avoid N+1 and unnecessary materialization, and keep transactions and DbContexts appropriately scoped. Important workflows require measured latency/query-count/error/memory/CPU baselines before optimization claims. Identity authorization must balance security-stamp revocation latency against request/database overhead without weakening security. Phase 10/11 owns the dedicated performance and reliability review.

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

- The roadmap's former .NET 9/MVC current-state statements have been reconciled to this checkout's five .NET 10 projects and current Blazor host; historical roadmap examples remain clearly deferred and are not source evidence.
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
