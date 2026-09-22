# ElsheiekhHMS

Enterprise Hospital Management System built with **.NET 10, ASP.NET Core, Blazor, Entity Framework Core, SQL Server, and ASP.NET Core Identity**.

> **Current development stage:** Phase 12H — Final UI acceptance complete; frontend baseline frozen and backend remains accepted and frozen
>
> **Phase 01:** ✅ Complete
>
> **Phase 02 Setup:** ✅ Complete and verified
>
> **Phase 03A / 03B / 03C:** ✅ Complete
>
> **Phase 03D:** ✅ Implemented and verified
>
> **Phase 04A:** ✅ EF Core foundation implemented and verified
>
> **Phase 04B:** ✅ Entity Fluent mappings implemented and verified
>
> **Phase 04C:** ✅ Relationships, filters, indexes, constraints, and concurrency metadata implemented and verified
>
> **Phase 04D-A:** ✅ SQL Server environment readiness verified
>
> **Phase 04D-B0:** ✅ EF design-time tooling verified
>
> **Phase 04D-B:** ✅ Initial migration generated and inspected; database not created or updated
>
> **Phase 04D-C:** ✅ Initial migration applied and local SQL Server schema verified
>
> **Phase 04E:** ✅ Persistence integration tests implemented and verified
>
> **Phase 05A:** ✅ Identity foundation and security-model amendment implemented and verified
>
> **Phase 05B:** ✅ Roles, authorization policies, fallback policy, and password-free role seeder implemented and verified
>
> **Phase 05C:** ✅ Current-user foundation and entity lifecycle auditing implemented and verified
>
> **Phase 05C-A:** ✅ Durable security/business AuditLog model and writer foundation implemented and verified; event-producing workflows, retention policy, IP/UserAgent capture, and clinical/read auditing remain deferred
>
> **Phase 05D:** ✅ Identity and AuditLog migration applied and verified against development and isolated integration SQL Server databases; pending-model suppression removed
>
> **Phase 05E:** ✅ Security integration and hardening implemented and verified; account workflows, durable event producers, retention, IP/UserAgent capture, clinical/read auditing, and UI remain deferred
>
> **Phase 07S:** ✅ Database-backed allocator prerequisite implemented, migration applied, physical schema verified, and regression suite passed
>
> **Phase 07A:** ✅ Patient Application Service implemented, tested, and reviewed; registration and PatientCode allocation remain one-save transactional
>
> **Phase 07B:** ✅ Department Application Service complete, implemented, tested, and reviewed
>
> **Phase 07C-P:** ✅ AppointmentCode allocator prerequisite complete; migration applied and physical schema verified
>
> **Phase 07C:** ✅ Appointment Application Service complete
>
> **Phase 07D:** ✅ Queue Application Service complete, implemented, verified, and closed out
>
> **Phase 07E:** ⏸ Doctor/Provider Application Service deferred; Doctor/Provider contracts and Doctor↔ApplicationUser ownership-aware authorization are not yet approved
>
> **Phase 07:** ✅ Application Services complete for the approved scope; 07E is explicitly deferred
>
> **Phase 08A:** ✅ Staff Patient Intake & Appointment Scheduling implemented, tested, and verified; partial-success retry semantics preserve newly registered Patients
>
> **Phase 08B-P:** ✅ Appointment↔Queue durable-link prerequisite implemented; `AddAppointmentQueueLink` applied and physically verified
>
> **Phase 08B:** ✅ Appointment Arrival & Queue Handoff implemented, verified, and closed out
>
> **Phase 08:** ✅ Business Workflows complete; 08A, 08B-P, and 08B are complete; 08C is not required
>
> **Phase 09A:** ✅ Structured Application Observability complete
>
> **Phase 09B:** ✅ SQL Server readiness health checks complete
>
> **Phase 09:** ✅ Enterprise Infrastructure complete; 09A and 09B complete; 09C is not required
>
> **Phase 10:** ✅ Testing & Hardening complete; 10A, 10B, 10C, and 10D complete; 418 tests passing
>
> **Phase 11:** ✅ Backend Review complete; 11A passed, 11B was not required, and 11C accepted and froze the backend baseline
>
> **Phase 12A:** ✅ Blazor UI architecture, application shell, navigation foundation, design tokens, reusable feedback/form/list patterns, and accessibility baseline established
>
>
> **Phase 12B:** ✅ Authentication presentation, anonymous/authenticated shell separation, current-user/logout controls, unauthorized UX, and role-aware navigation complete; external browser QA remains required

> **Phase 12C:** ✅ Patient registry, server-side search/pagination, registration, details, and concurrency-safe edit UI complete; external browser QA remains required

> **Phase 12D:** ✅ Department registry, search/filter/sort, create, details, edit, and one-way deactivation UI complete; external browser QA remains required
>
> **Phase 12E:** ✅ Appointment registry, bounded Patient lookup, scheduling, lifecycle details/actions, Kigali presentation, and concurrency-safe mutation UI complete; external browser QA remains required
>
> **Phase 12F:** ✅ Queue registry, explicit walk-in workflow, queue lifecycle UI, and Appointment→Queue handoff UI complete; external browser QA remains required
>
> **Phase 12G:** ✅ Operational dashboard, cross-module UI hardening, accessibility source review, and consolidated QA handoff complete; external browser QA remains required
>
> **Phase 12H:** ✅ Final UI acceptance, static-asset authentication-boundary fix, acceptance documentation, and frontend freeze complete; Antigravity and authenticated browser evidence remain environment-dependent
>
> **Next gate:** Roadmap decision after the frozen Phase 12 frontend; 07E remains deferred and provider/patient ownership workflows remain blocked

---

# 1. Project Purpose

ElsheiekhHMS is being developed as a professional, modular Hospital Management System rather than a collection of Blazor CRUD pages.

The backend architecture is being established before substantial UI development.

The intended application flow is:

```text
Blazor
   ↓
Application Layer
   ↓
Validation / Authorization
   ↓
Business Rules
   ↓
Core Domain
   ↓
Infrastructure
   ↓
Entity Framework Core
   ↓
SQL Server
```

Business logic must not be placed directly inside Blazor components.

---

# 2. Technology Stack

Planned technology stack:

```text
Language                 C#
Framework                .NET 10
Frontend                 Blazor
Backend                  ASP.NET Core
ORM                      Entity Framework Core
Database                 SQL Server
Authentication           ASP.NET Core Identity
Dependency Injection     Microsoft.Extensions.DependencyInjection
Logging                  ILogger<T>
Testing                  xUnit
Version Control          Git / GitHub
```

Some technologies listed above intentionally remain deferred because development is being completed phase by phase. EF Core mappings, the inspected Phase 04 migration, the local development schema, isolated persistence integration tests, the 05A Identity foundation, 05B role/policy foundation, 05C current-user/entity-lifecycle auditing, 05C-A AuditLog model/writer foundation, the 05D Identity/AuditLog migration, and 05E security integration are complete. Event-producing workflows, production retention policy, IP/UserAgent capture, clinical/read auditing, and UI remain later gates. The approved performance, reliability, and quality requirement applies to every remaining phase; optimization and infrastructure additions require measured need.

---

# 3. Solution Architecture

The solution currently contains exactly five projects:

```text
ElsheiekhHMS/
│
├── ElsheiekhHMS.Core/
│
├── ElsheiekhHMS.Application/
│
├── ElsheiekhHMS.Infrastructure/
│
├── ElsheiekhHMS.Web/
│
└── ElsheiekhHMS.Tests/
```

All projects target:

```text
net10.0
```

---

# 4. Dependency Direction

The required dependency architecture is:

```text
ElsheiekhHMS.Web
        │
        ├──────────────► ElsheiekhHMS.Infrastructure
        │                         │
        ▼                         │
ElsheiekhHMS.Application ◄────────┘
        │
        ▼
ElsheiekhHMS.Core
```

Actual project rules:

```text
Core
└── No project dependencies

Application
└── Core

Infrastructure
├── Application
└── Core

Web
├── Application
└── Infrastructure

Tests
├── Core
├── Application
└── Infrastructure
```

## Critical Architecture Rule

`ElsheiekhHMS.Core` must never depend on:

```text
Application
Infrastructure
Web
Blazor
ASP.NET Core
Entity Framework Core
SQL Server
ASP.NET Core Identity
```

Core represents the domain foundation and must remain independent of persistence and presentation technologies.

---

# 5. Project Responsibilities

## ElsheiekhHMS.Core

Contains the fundamental domain model and domain rules.

Eventually includes:

```text
Entities
Enums
Domain exceptions
Domain abstractions
Domain constants
Common base types
Business rules
```

Core does **not** contain:

```text
DbContext
EF Core configuration
SQL queries
Blazor components
HTTP logic
Identity implementation
Infrastructure services
```

---

## ElsheiekhHMS.Application

Coordinates application use cases.

Application use-case implementation remains deferred; it will eventually contain:

```text
DTOs
Application services
Service interfaces
Validation
Mappings
Query models
Pagination models
Application workflows
```

Application depends on Core but does not depend on Web.

---

## ElsheiekhHMS.Infrastructure

Implements technical concerns.

Currently contains the EF Core/SQL Server foundation, entity configurations, and the inspected initial migration; it will eventually contain:

```text
EF Core
SQL Server
ElsheiekhHmsDbContext
Entity configurations
Repositories where justified
Identity
File storage
Notifications
Background processing
Persistence
Database migrations
Seed/bootstrap logic
```

---

## ElsheiekhHMS.Web

The Blazor presentation layer and application composition root.

Eventually contains:

```text
Blazor components
Pages
Layouts
Navigation
Forms
Tables
Dashboards
Presentation services
Static assets
```

Web should call the Application layer rather than contain HMS business rules.

---

## ElsheiekhHMS.Tests

Contains automated tests.

Planned areas:

```text
Unit/
Integration/
Helpers/
```

Testing will expand as each development phase introduces real behavior.

---

# 6. Development Roadmap

The project follows a strict phased development process.

```text
PHASE 01
Solution & Architecture
        │
        ▼
PHASE 02
Core Foundation
        │
        ▼
PHASE 03
Domain Entities
        │
        ▼
PHASE 04
EF Core & Database
        │
        ▼
PHASE 05
Identity & Security
        │
        ▼
PHASE 06
DTOs & Validation
        │
        ▼
PHASE 07
Application Services
        │
        ▼
PHASE 08
Business Workflows
        │
        ▼
PHASE 09
Enterprise Infrastructure
        │
        ▼
PHASE 10
Testing & Hardening
        │
        ▼
PHASE 11
Backend Review
        │
        ▼
PHASE 12
Blazor UI
```

Do not skip phases without reviewing the architectural impact.

---

# 7. Phase Status

| Phase | Description | Status |
|---|---|---|
| 01 | Solution & Architecture | ✅ Complete |
| 02 | Core Foundation | ✅ Complete |
| 03 | Domain Entities | ✅ Complete |
| 04 | EF Core & Database | ✅ Complete |
| 05 | Identity & Security | ✅ Complete (05A, 05B, 05C, 05C-A, 05D, and 05E) |
| 06 | DTOs & Validation | ✅ Complete (06A–06D) |
| 07 | Application Services | ✅ Complete for approved scope (07S, 07A, 07B, 07C-P, 07C, 07D; 07E deferred) |
| 08 | Business Workflows | ✅ Complete (08A, 08B-P, 08B) |
| 09 | Enterprise Infrastructure | ✅ Complete (09A, 09B) |
| 10 | Testing & Hardening | ✅ Complete (10A–10D) |
| 11 | Backend Review | ✅ Complete (11A–11C) |
| 12 | Blazor UI | ⏳ Not started |

---

# 8. Phase 01 — Solution & Architecture

**Status: ✅ COMPLETE**

Phase 01 established the architectural foundation.

Completed:

```text
[✓] .NET 10 solution
[✓] Five-project architecture
[✓] Core project
[✓] Application project
[✓] Infrastructure project
[✓] Web project
[✓] Tests project

[✓] Correct project references
[✓] Core independence
[✓] No circular dependencies

[✓] Application DI boundary
[✓] Infrastructure DI boundary
[✓] Web composition root

[✓] Initial folder architecture
[✓] Health-check foundation
[✓] .gitignore
[✓] Git repository hygiene

[✓] dotnet restore
[✓] dotnet build
[✓] Web startup
[✓] Blazor HTTP response
[✓] /health endpoint
```

Final independent Phase 01 audit passed all architectural checks.

---

# 9. Phase 02 — Core Foundation

**Status: 🟡 SETUP COMPLETE AND PASSING**

Phase 02 establishes reusable Core foundations before HMS entities are introduced.

The setup was performed using:

```text
phase02-setup.ps1
```

The setup completed successfully.

Build and verification checks passed.

---

## Current Core Foundation

```text
ElsheiekhHMS.Core/
│
├── Common/
│   ├── BaseEntity.cs
│   ├── AuditableEntity.cs
│   └── SoftDeletableEntity.cs
│
├── Exceptions/
│   ├── DomainException.cs
│   ├── BusinessRuleException.cs
│   └── DomainValidationException.cs
│
├── Interfaces/
│   └── IHasConcurrencyToken.cs
│
├── Constants/
├── Entities/
└── Enums/
```

---

# 10. Base Entity Hierarchy

Current inheritance:

```text
BaseEntity
     │
     ▼
AuditableEntity
     │
     ▼
SoftDeletableEntity
```

## BaseEntity

Provides the fundamental entity identifier.

Conceptually:

```csharp
public abstract class BaseEntity
{
    public int Id { get; protected set; }
}
```

It intentionally contains no EF Core attributes.

---

## AuditableEntity

Adds audit information such as:

```text
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
```

Timestamps use `DateTimeOffset`.

The Core layer does not determine the current user and does not depend on ASP.NET Core Identity.

Audit values will later be populated by the appropriate application/infrastructure mechanism.

---

## SoftDeletableEntity

Adds:

```text
IsDeleted
DeletedAt
DeletedBy
```

This provides a foundation for entities that should be archived rather than physically deleted.

Not every entity must automatically inherit this class.

Phase 04C now applies EF Core global query filters to Patient, Appointment, and
WalkInQueueEntry. Doctor, Department, and DoctorSchedule lifecycle fields remain
unfiltered; filters are persistence behavior, not domain deletion.

---

# 11. Concurrency Foundation

Phase 02 currently contains:

```text
IHasConcurrencyToken
```

with a concurrency token concept based on:

```text
RowVersion
```

Concurrency is intentionally **opt-in**.

Do not make every entity implement this interface.

During Phase 03, determine which aggregates genuinely require optimistic concurrency.

Likely candidates may eventually include records where simultaneous edits could cause significant conflicts.

The actual EF Core/SQL Server concurrency configuration belongs to Phase 04.

Core must not use EF-specific attributes such as:

```text
[Timestamp]
```

---

# 12. Domain Exception Foundation

Current hierarchy:

```text
System.Exception
       │
       ▼
DomainException
       │
       ├── BusinessRuleException
       │
       └── DomainValidationException
```

These exceptions remain domain-oriented.

Core must not introduce HTTP concepts such as:

```text
HTTP status codes
ProblemDetails
BadRequest
NotFoundResult
IActionResult
```

HTTP/application exception handling belongs outside Core.

---

# 13. Phase 02 Tests

Phase 02 introduced actual Core foundation tests.

Current test areas include:

```text
ElsheiekhHMS.Tests/
└── Unit/
    └── Domain/
        ├── Common/
        │   ├── BaseEntityTests.cs
        │   ├── AuditableEntityTests.cs
        │   └── SoftDeletableEntityTests.cs
        │
        └── Exceptions/
            └── DomainExceptionTests.cs
```

The Phase 02 setup verified that tests are actually discovered and executed.

---

# 14. Things Intentionally NOT Implemented Yet

The following are intentionally postponed.

## Phase 03+

The approved backend now contains the Department, Doctor, DoctorSchedule, Patient,
Appointment, and WalkInQueueEntry domain roots. These product areas remain future
scope until separately designed and approved:

```text
Employee
Nurse
Specialty
Encounter
Diagnosis
Admission
Ward
Room
Bed
Medication
Prescription
LabOrder
Invoice
Payment
Insurance
```

## Phase 04+

Persistence foundation, scalar mappings, approved relational metadata, the inspected migration, the local development schema, and isolated SQL Server integration tests now exist. These remain deferred:

```text
Database seed
```

## Phase 05+

The 05A Identity foundation, security-model amendment, 05B Roles & Authorization foundation, 05C current-user/entity-lifecycle auditing, 05C-A durable AuditLog model/writer foundation, 05D Identity/AuditLog migration, and 05E security integration are implemented; these remain deferred:

```text
Security/business workflows that emit AuditLog events
Production retention durations pending Rwanda-first and future Sudan compliance validation
IP/UserAgent host capture, clinical auditing, and read-access auditing
```

This staged boundary is intentional.

Do not treat these as missing Phase 02 work.

---

# 15. Architecture Rules Going Forward

Every future phase must preserve these rules.

### Rule 1 — Core remains independent

Never introduce Infrastructure, Web, EF Core, Blazor, or ASP.NET dependencies into Core.

### Rule 2 — UI is not the business layer

Avoid:

```text
Blazor Component
      ↓
Business Logic
      ↓
DbContext
```

Target:

```text
Blazor Component
      ↓
Application Service
      ↓
Domain / Business Rules
      ↓
Infrastructure
```

### Rule 3 — Do not expose EF entities directly to UI

Application DTOs will be introduced later.

### Rule 4 — Backend rules are authoritative

UI validation can improve user experience, but important business rules must be enforced by the backend.

### Rule 5 — Avoid speculative abstractions

Do not create interfaces, repositories, services, base classes, or patterns merely because they sound "enterprise."

Create them when they solve an actual architectural problem.

### Rule 6 — Avoid giant entities and services

Keep modules cohesive and responsibilities clear.

### Rule 7 — Important hospital history should not disappear

Clinical, financial, admission, audit, and other important historical records will require carefully designed archival/deletion rules.

### Rule 8 — Use async database operations

Once persistence is introduced, database operations should use asynchronous APIs and cancellation tokens where appropriate.

### Rule 9 — Security is backend-enforced

Hiding a button in Blazor is not authorization.

The backend must independently enforce permissions.

### Rule 10 — Every phase must pass verification

Do not continue simply because code compiles.

Review architecture, tests, dependencies, and phase boundaries before proceeding.

---

# 16. HMS Modules Planned

The backend is eventually expected to support:

```text
Patient Management

Staff Management

Departments & Specialties

Appointment Management

Clinical / Encounter Management

Diagnosis Management

Admission Management

Ward / Room / Bed Management

Laboratory Management

Medication Management

Pharmacy Management

Prescription Management

Billing

Payments

Insurance

Documents

Notifications

Users

Roles

Permissions

Audit Logs

System Settings

Reporting

Dashboard Data
```

These should evolve as modules rather than becoming one giant CRUD system.

---

# 17. Current Development Position

When opening this repository after a break, read this section first.

```text
LAST COMPLETED PHASE:
Phase 12H — Final UI acceptance and frontend freeze

CURRENT PHASE:
Phase 12 — Blazor UI (12H complete; frontend baseline frozen)

CURRENT STATUS:
Phase 04A foundation, 04B scalar mappings, 04C relational metadata, 04D-A environment readiness,
04D-B0 design-time tooling, 04D-B migration generation/inspection, 04D-C local schema verification,
04E isolated SQL Server persistence integration tests, 05A Identity foundation, 05B Roles & Authorization, 05C current-user/entity auditing, 05C-A AuditLog foundation, 05D Identity/AuditLog migration and SQL verification, 05E security integration/hardening, 06A–06D DTOs & Validation, 07S allocator infrastructure, 07A Patient Application Service, 07B Department Application Service, 07C-P AppointmentCode allocator infrastructure, 07C Appointment Application Service, 07D Queue Application Service, the Phase 07 closeout, and Phase 08 Business Workflows are complete for the approved scope. Phase 08 includes 08A staff intake/scheduling, 08B-P the durable Appointment↔Queue link, and 08B arrival/queue handoff; no 08C is required. Phase 09A adds built-in request correlation/duration logging scopes and safe completion diagnostics. Phase 09B adds `/health/live` process liveness, `/health/ready` SQL dependency readiness, and preserves `/health` as liveness. Phase 10A authorization and host-security verification, 10B SQL concurrency/atomicity verification, 10C lifecycle/workflow verification, and 10D controlled performance/production-readiness smoke verification are complete; Phase 10 is formally closed with 418 tests passing. Phase 11A backend-wide review passed, 11B was not required, and 11C accepted and froze the backend baseline for Phase 12. Its accepted non-blocking limitations are full production-error/antiforgery host assertions, deterministic application-lock and Phase08B partial-success SQL failure injection, hard local latency assertions, and production certification of PRD performance targets. 07E Doctor/Provider Application Service is explicitly deferred because the Phase06 Doctor/Provider contract family, service contract, Doctor↔ApplicationUser ownership mapping, ownership persistence design, and ownership-aware authorization scope are not approved.
The six approved entity configurations, explicit historical-safe relationships, approved indexes/uniqueness,
soft-delete filters, three opted-in rowversion mappings, and the Infrastructure migration/snapshot are present.
Full restore, build, and tests passed (418 tests). The integration fixture uses only the exact
`ElsheiekhHMS_IntegrationTests` LocalDB target and removes it after each run; `ElsheiekhHMS_Dev`
was updated only through the approved additive migration. `ApplicationUser`, the
same-context Identity foundation, approved account-security state model, options, role stores, and focused model tests are present.
The five canonical roles, eight approved policies, authenticated fallback policy, anonymous health endpoint,
and deterministic password-free role seeder are implemented. The 05C current-user contract, Web claims adapter,
TimeProvider registration, centralized EF entity auditing, and RowVersion regression coverage are implemented.
The 05C-A stable AuditLog vocabulary, bounded append-only persistence model, server-controlled writer, transaction
boundary, sensitive-data exclusions, model tests, and focused writer tests are implemented. The 05D migration,
physical Identity/AuditLog schema verification, migration-history verification, isolated SQL tests, and removal of the
temporary pending-model suppression are complete. Phase 05E adds Identity cookie composition, request-level security-stamp/account-state validation,
Interactive Server circuit revalidation, scoped stable-user propagation, antiforgery/security-header hardening, and an explicit secret-backed administrator bootstrap primitive.
Event-producing security and business workflows, retention duration, IP/UserAgent capture, clinical/read auditing, and future module UI remain deferred.
Phase 12C adds the authorized-staff Patient registry with bounded server-side search, sorting, paging, registration, details, and three concurrency-safe edit sections over the frozen Patient Application contract. Duplicate-candidate review is not exposed by the accepted contract, so no blocking or warning behavior was invented; Patient self-service and clinical history remain deferred. External browser/Antigravity QA remains required. Phase 12D adds the SystemAdministrator-only Department registry over the frozen Department Application contract, including server-side search/status filtering/sorting/paging, create, details, edit, and one-way deactivation with historical records preserved; reactivation, delete, provider assignment, and workflow relationship editing remain out of scope.
The 07S migration `AddPhase07AllocatorInfrastructure` is applied exactly once to `ElsheiekhHMS_Dev`; allocator tables and queue uniqueness were physically verified. Patient phone remains non-unique. 07A provides the first approved application service, a narrow EF-free persistence port, bounded projections, stable UserId authorization, and transactional Patient/AuditLog writes. Its two reviewed decisions are that allocator access remains on the narrow persistence port and registration audits target the durable PatientCode before the single save. 07B adds the bounded Department service, the minimal Department search contract, SystemAdministrator-only configuration authorization through the existing `CanConfigureSystem` capability, and transactional Department/AuditLog writes; Department names remain non-unique and reactivation remains out of scope. 07C-P adds the Infrastructure-only UTC year-scoped AppointmentCode allocator; `AddAppointmentCodeAllocator` is applied exactly once to `ElsheiekhHMS_Dev` and its physical schema was verified. 07C adds the EF-free Appointment service, Africa/Kigali civil-time validation, active Patient/Department/Doctor checks, bounded appointment projections, lifecycle orchestration, stable UserId authorization, and transactional Appointment/AuditLog writes. Same Department and scheduled-instant collisions are serialized with a transaction-scoped SQL Server application lock; Cancelled, NoShow, and Completed history does not block a slot. 07D adds the EF-free Queue service, Kigali operational-date calculation, registered-Patient and active-Department checks, atomic ticket allocation through the approved QueueTicket allocator, bounded queue projections, deterministic priority/arrival/sequence ordering, lifecycle orchestration, stable UserId authorization, and transactional Queue/AuditLog writes. Same Patient and Kigali queue-date duplicate-active requests are serialized with a transaction-scoped SQL Server application lock; Completed and Cancelled history does not block a future queue entry. 08A adds the EF-free staff intake orchestration over the existing Patient and Appointment services. It supports existing-Patient scheduling and new-Patient registration followed by scheduling, preserves the registered Patient when appointment scheduling fails, returns the Patient identity for retry, and adds no workflow-level audit event, queue interaction, schema, or ownership behavior. 08B-P adds the nullable restrictive Queue→Appointment relationship, filtered unique appointment link, controlled linked queue creation, and indexed lookup. 08B adds `CheckInAndQueueAsync`, reuses AppointmentService and QueueService, preserves partial-success retry semantics, recovers existing active or historical handoffs idempotently, and adds no cross-service transaction or workflow audit event. Provider-scoped and Patient self-service operations remain deferred. 07E is not implemented: before it can begin, the project must approve Phase06-style Doctor/Provider contracts, service operations, a Doctor↔ApplicationUser relationship, ownership persistence, ownership-aware authorization, any required schema migration, and its tests.

Phase 12G replaces the foundation showcase with a bounded operational dashboard. Authorized Administrator and Receptionist users see real current Africa/Kigali appointment and waiting-queue totals plus bounded attention lists sourced from the existing Application services. Quick actions link only to approved Patient, Appointment, and Queue workflows; SystemAdministrator users retain a Department configuration entry point without invented operational metrics. Shared CSS now uses the defined muted token, removes unused showcase rules, improves logical positioning and dashboard responsive layouts, and the shell exposes an accessible navigation relationship. The consolidated external browser handoff is `docs/UI_PHASE12G_QA.md`; local source QA does not claim browser or production SLA certification.

NEXT ACTION:
Make the next roadmap decision against the frozen Phase 12 frontend. The local browser run verified anonymous login, protected-route redirects, styled static assets, invalid-login feedback, keyboard focus order, and representative responsive widths. Antigravity, authenticated browser workflows, and screenshot archiving remain external environment-dependent limitations. Keep 07E provider ownership and Patient self-service deferred until their prerequisites are approved; future clinical, laboratory, billing, inpatient, pharmacy, and notification workflows require separate domain designs.

DO NOT:
Do not implement 07E before its deferred prerequisites are approved and established. Do not change backend behavior merely to simplify Phase 12 UI work; backend authorization and business rules remain authoritative.
```

---

# 18. How to Resume Development

When returning to the project, first open a terminal at the repository root.

Expected location:

```powershell
C:\Projects\ElsheiekhHMS
```

Check Git:

```powershell
git status
git log --oneline -5
```

Check the solution:

```powershell
dotnet sln list
```

Verify build:

```powershell
dotnet restore
dotnet build --no-restore
```

Run tests:

```powershell
dotnet test --no-build
```

If everything passes, read:

```text
Section 17 — Current Development Position
```

and continue from the listed **NEXT ACTION**.

---

# 19. Standard Resume Checklist

Before continuing development after a long break:

```text
[ ] Read README.md

[ ] Check Current Development Position

[ ] Run git status

[ ] Review latest Git commits

[ ] Confirm correct branch

[ ] Run dotnet sln list

[ ] Run dotnet restore

[ ] Run dotnet build --no-restore

[ ] Run dotnet test --no-build

[ ] Verify current phase

[ ] Review architecture rules

[ ] Continue only from NEXT ACTION
```

If the build or tests fail, resolve the existing problem before starting the next phase.

---

# 20. Git Checkpoint Strategy

Each completed phase should have a clear Git checkpoint.

Suggested commit style:

```text
chore: complete Phase 01 solution architecture

feat: complete Phase 02 core foundation

feat: complete Phase 03 domain model

feat: complete Phase 04 persistence foundation

feat: complete Phase 05 identity and authorization
```

Before committing:

```powershell
git status
git diff
git diff --cached
```

Then:

```powershell
git add .
git diff --cached
git commit -m "..."
```

Never commit secrets.

Never blindly commit:

```text
bin/
obj/
.vs/
TestResults/
```

---

# 21. Phase Completion Process

Every phase follows approximately:

```text
PLAN
  ↓
SETUP
  ↓
IMPLEMENT
  ↓
BUILD
  ↓
TEST
  ↓
ARCHITECTURE AUDIT
  ↓
FIX FINDINGS
  ↓
FINAL VERIFICATION
  ↓
GIT CHECKPOINT
  ↓
UPDATE README
  ↓
NEXT PHASE
```

A setup script succeeding does not automatically mean the phase is complete.

---

# 22. Phase 03 Preview

Phase 03 will introduce the actual HMS domain model.

Expected major areas include:

```text
Patient
Staff
Departments
Appointments
Clinical Encounters
Admissions
Laboratory
Pharmacy
Billing
Payments
Insurance
Documents
```

However, Phase 03 should **not generate every entity blindly in one operation**.

Domain modeling should proceed in logical groups with relationships and ownership carefully reviewed.

Before implementation, determine:

```text
Aggregate boundaries
Entity responsibilities
Inheritance
Relationships
Required vs optional data
Lifecycle
Status enums
Business invariants
Deletion/archive behavior
Concurrency requirements
Navigation relationships
```

EF Core configuration still belongs primarily to Phase 04.

---

# 23. Definition of Backend Ready

Substantial Blazor UI work should not begin until the backend foundation includes:

```text
Architecture
Core domain
Domain entities
Database
Identity
Authorization
DTOs
Validation
Application services
Business workflows
Transactions
Auditing
Concurrency
Pagination
Search/filtering
Exception handling
Logging
Configuration
File architecture
Notifications
Reporting
Security
Tests
```

The goal is for future Blazor components to remain relatively simple:

```csharp
var result = await PatientService.CreateAsync(model);
```

rather than implementing the HMS inside Razor components.

---

# 24. Development Principle

> **The frontend should interact with the application.  
> The frontend should not become the application.**

ElsheiekhHMS is being developed backend-first so that Blazor becomes a clean presentation layer over a tested, secure, maintainable HMS architecture.

---

# 25. README Maintenance Rule

This README is part of the development workflow.

At the end of every phase:

1. Update the phase status table.
2. Update **Current Development Position**.
3. Record important architectural decisions.
4. Record intentionally postponed work.
5. Update the **NEXT ACTION**.
6. Verify commands still match the repository.
7. Commit the README with the phase checkpoint.

This ensures development can resume even after a long interruption.
