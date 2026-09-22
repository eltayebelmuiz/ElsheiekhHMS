# ElsheiekhHMS — Architecture Decision Log

This ADR-lite log records supported architectural choices, not a changelog or a claim that planned features are implemented. **Accepted** means the choice is established; the phase and consequences identify implementation timing. **Proposed** is unresolved; **Superseded** requires evidence of an adopted replacement. Historical adoption dates are omitted because they were not reliably established.

The source-backed architecture is described in [ARCHITECTURE.md](ARCHITECTURE.md). Conflicting roadmap claims about an MVC codebase and later completed phases are not imported as verified history. No speculative supersession chain is reconstructed. The earlier concurrency-removal proposal is not the final decision; ADR-008 records retention as confirmed by source, README, and the current documentation instruction.

## ADR-001 — Five-project solution on .NET 10

**Status:** Accepted
**Phase:** 01

### Context

The application needs explicit domain, use-case, technical, presentation, and test boundaries.

### Decision

Use the existing Core, Application, Infrastructure, Web, and Tests projects, all targeting net10.0.

### Rationale

Separate responsibilities support independent testing and prevent presentation concerns from shaping the domain.

### Consequences

Preserve the verified solution membership and reference graph. The roadmap's .NET 9 MVC layout is not this checkout's current architecture.

### Evidence / Notes

[Solution](../ElsheiekhHMS.slnx); [README sections 3–4](../README.md); each member's project file.

## ADR-002 — Independent, persistence-free Core

**Status:** Accepted
**Phase:** 01–02

### Context

Domain foundations must not require a web host, database provider, or identity framework.

### Decision

Keep Core free of outward project references and external packages; persistence mappings and implementation belong outside Core.

### Rationale

Domain behavior stays testable without infrastructure and avoids coupling to storage or UI technology.

### Consequences

No EF configuration, SQL Server implementation, Identity implementation, or Blazor dependency in Core. Future technical contracts need a concrete use case.

### Evidence / Notes

[Core project](../ElsheiekhHMS.Core/ElsheiekhHMS.Core.csproj); [README architecture rules](../README.md).

## ADR-003 — Backend-first, phase-gated delivery

**Status:** Accepted  
**Phase:** 01 onward; review 11, UI 12

### Context

Hospital workflows need verifiable rules before substantial presentation work.

### Decision

Build foundations, domain, persistence, security, and application workflows in their authorized phases; audit the backend before substantial Blazor UI.

### Rationale

Backend behavior must be independently testable and enforceable.

### Consequences

An existing Blazor template does not mean Phase 12 is complete. Phase completion requires relevant builds, tests, and architecture checks; do not add future infrastructure prematurely.

### Evidence / Notes

[AGENTS phase boundaries](../AGENTS.md); [README roadmap](../README.md); [roadmap development philosophy](../DEVELOPMENT_ROADMAP.md).

## ADR-004 — Blazor presentation with Application-owned orchestration

**Status:** Accepted  
**Phase:** 01 foundation; 07–08 services/workflows; 12 UI

### Context

UI-only business rules can be bypassed and are difficult to reuse or test.

### Decision

Use Blazor for presentation, Application for use cases/orchestration, Core for domain invariants, and Infrastructure for technical implementations.

### Rationale

This separates behavior from rendering and preserves backend enforcement.

### Consequences

Blazor calls Application contracts rather than DbContext. Do not interpret conceptual runtime flow as a Core dependency on Infrastructure. Actual use cases are not implemented yet.

### Evidence / Notes

[Web startup](../ElsheiekhHMS.Web/Program.cs); [README sections 5 and 15](../README.md); [AGENTS](../AGENTS.md).

## ADR-005 — Layer-owned dependency injection registration

**Status:** Accepted  
**Phase:** 01

### Context

The host must compose layers without accumulating their internal registration details.

### Decision

Keep Program.cs as composition root and expose AddApplication() and AddInfrastructure(configuration) registration entry points.

### Rationale

Registrations remain close to their owning layer while the host controls composition.

### Consequences

Both extension methods currently return the service collection without registering services. Populate them in the relevant phases; their presence does not imply implemented services.

### Evidence / Notes

[Application registration](../ElsheiekhHMS.Application/ApplicationServiceExtensions.cs); [Infrastructure registration](../ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs); [Program.cs](../ElsheiekhHMS.Web/Program.cs).

## ADR-006 — Entity identity and audit foundation

**Status:** Accepted  
**Phase:** 02

### Context

Entities need reusable identity and audit metadata without hidden user or clock services.

### Decision

Use abstract BaseEntity with integer Id and protected setter; derive AuditableEntity with protected creation/update metadata.

### Rationale

Derived domain behavior controls mutation; callers provide timestamps and opaque actor identifiers.

### Consequences

No automatic stamping or audit-log persistence exists. UTC is a documented caller responsibility, not a property-level guarantee.

### Evidence / Notes

[BaseEntity](../ElsheiekhHMS.Core/Common/BaseEntity.cs); [AuditableEntity](../ElsheiekhHMS.Core/Common/AuditableEntity.cs); [README sections 10 and 13](../README.md).

## ADR-007 — Soft-delete foundation and historical preservation

**Status:** Accepted  
**Phase:** 02 foundation; 04 persistence enforcement

### Context

Clinical and financial history must remain available for accountability.

### Decision

Provide SoftDeletableEntity over AuditableEntity with IsDeleted, DeletedAt, and DeletedBy; preserve important records instead of casually destroying them.

### Rationale

Deletion metadata supports history preservation while keeping persistence policy outside Core.

### Consequences

The base class does not perform deletion or apply query filters. Future domain behavior must keep metadata consistent; persistence filtering and authorized access require implementation.

### Evidence / Notes

[SoftDeletableEntity](../ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs); [PRD scope](../PRD.md); [SpecKit data integrity principle](../speckit/my-project/.specify/memory/constitution.md).

## ADR-008 — Retain opt-in concurrency contract in Core

**Status:** Accepted  
**Phase:** 02 contract; 03 aggregate selection; 04 persistence

### Context

Some future aggregates need protection against conflicting edits, but not every entity requires a token.

### Decision

Retain IHasConcurrencyToken with byte[] RowVersion { get; set; }. Only aggregates that need optimistic concurrency implement it; configure EF Core/SQL Server concurrency in Phase 04.

### Rationale

An opaque opt-in contract expresses the capability without embedding EF attributes or conflict-detection behavior in Core.

### Consequences

It is not part of the base hierarchy. The approved Phase 03D `Appointment` and `WalkInQueueEntry` aggregates implement the interface; database row-version generation, mapping, and conflict handling remain future persistence behavior.

### Evidence / Notes

[Interface](../ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs); [README section 11](../README.md); current documentation request explicitly confirms retention.

## ADR-009 — Domain-specific exception hierarchy

**Status:** Accepted  
**Phase:** 02

### Context

Domain failures need vocabulary distinct from transport and persistence failures.

### Decision

Use DomainException : Exception with sealed BusinessRuleException and DomainValidationException specializations.

### Rationale

This distinguishes business-rule violations from invalid domain values without framework coupling.

### Consequences

Message and inner-exception constructors preserve the cause. No HTTP status mapping or application result wrapper is implied by these types.

### Evidence / Notes

[Domain exceptions](../ElsheiekhHMS.Core/Exceptions/); [exception tests](../ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs).

## ADR-010 — EF Core and SQL Server behind Infrastructure

**Status:** Accepted  
**Phase:** 04 complete (04A–04E complete); 05D migration rollout complete

### Context

Persistence is required by the product but must not contaminate foundation/domain work.

### Decision

Use EF Core with SQL Server in Infrastructure. Phase 04A establishes the context/provider foundation, Phase 04B adds scalar Fluent mappings, and Phase 04C adds explicit historical-safe relationships, approved indexes/uniqueness, soft-delete filters, and opted-in rowversion metadata. Phase 04D-B generates and inspects the initial migration; Phase 04D-C applies it only to the approved local `MSSQLLocalDB` database `ElsheiekhHMS_Dev` and verifies the physical schema. Phase 04E verifies persistence behavior against a separate exact-target `MSSQLLocalDB` database named `ElsheiekhHMS_IntegrationTests`, with a safety guard that rejects every other database and fixture cleanup after each run. The development database is never used by integration tests.

### Rationale

This follows the selected Microsoft stack and hospital deployment requirements while preserving the domain boundary.

### Consequences

`ElsheiekhHmsDbContext`, SQL Server DI registration, six scalar configuration units, the Phase 04C relational metadata, the inspected Phase 04D-B migration/snapshot, the verified local Phase 04D-C schema, and the Phase 04E isolated persistence integration suite now exist in Infrastructure/Tests. Phase 05D adds the single approved additive `20260921182651_AddPhase05IdentityAndAuditLog` migration, applies it to the approved local development database, verifies the physical Identity/AuditLog schema and migration history, and applies it to the guarded isolated integration database. Patient has a private EF-only materialization constructor while retaining its validated public creation path; getter-only `PatientCode` is mapped through its existing backing field. Database provisioning outside the approved local development target, repositories, and Unit of Work remain deferred. Roadmap repository/Unit-of-Work examples are not proof of current APIs.

### Evidence / Notes

[README technology stack and postponed work](../README.md); [PRD database architecture](../PRD.md); [AGENTS](../AGENTS.md).

## ADR-011 — Identity and backend-enforced authorization

**Status:** Accepted  
**Phase:** 05 in progress (05A, 05B, 05C, 05C-A, and 05D complete)

### Context

Patient and financial operations require reliable access control beyond UI visibility.

### Decision

Introduce ASP.NET Core Identity in the security phase and enforce roles, permissions, and policies on the backend.

### Rationale

Clients and UI controls cannot be trusted as authorization boundaries.

### Consequences

Identity implementation stays outside Core. Phase 05A provides `ApplicationUser`, the same-context Identity EF foundation, role stores, approved password/lockout options, and the approved security-model amendment in Infrastructure. Phase 05B provides the five canonical roles, eight named policies, authenticated fallback authorization, anonymous health metadata, and a deterministic password-free role seeder; it does not modify `ApplicationUser` or invoke the seeder during normal startup. Phase 05C provides the Application `ICurrentUser` contract, Web claims adapter, UTC `TimeProvider`, and Infrastructure entity lifecycle auditing using stable UserId attribution. Phase 05C-A provides the stable audit vocabulary, Application writer contract, bounded append-only Infrastructure `AuditLog` model, and server-controlled writer; it does not implement the workflows that emit security/business events. Phase 05D applies the single approved additive `20260921182651_AddPhase05IdentityAndAuditLog` migration, verifies the development and guarded integration schemas, confirms the migration history, and removes the temporary pending-model warning suppression. Phase 05E composes the Identity cookie, request-level security-stamp/account-state validation, Interactive Server circuit revalidation, scoped current-user propagation, baseline security headers with report-only CSP, and an explicit secret-backed administrator bootstrap primitive without startup provisioning. `AccountSecurityState : byte` uses fixed values `Active = 0`, `Suspended = 1`, and `Banned = 2`; independent `LoginAllowed` replaces the ambiguous `IsActive` security meaning. The finalized Phase 05 design preserves the separation between domain persons, login identities, administrative account state, sessions, lockout, roles, and policies. Password-reset workflow, role-management workflows, durable event emission, production retention duration, IP/UserAgent capture, and clinical/read auditing remain later Phase 05 gates. Authorization-aware UI is supplementary.

### Evidence / Notes

[README Phase 05 postponements](../README.md); [PRD auth requirements](../PRD.md); [SpecKit security principle](../speckit/my-project/.specify/memory/constitution.md).

## ADR-012 — xUnit tests alongside implementation

**Status:** Accepted  
**Phase:** 01 test infrastructure; 02 foundation onward

### Context

Foundations and subsequent domain behavior need executable protection.

### Decision

Use the existing Tests project and xUnit, adding meaningful coverage as each layer develops.

### Rationale

Early tests verify behavior before persistence and UI increase complexity.

### Consequences

Current tests cover Core metadata and exceptions. Integration, security, workflows, and UI coverage grow later. Static graph counts must not be reported as executed test results.

### Evidence / Notes

[Tests project](../ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj); [foundation tests](../ElsheiekhHMS.Tests/Unit/Domain/); [README section 13](../README.md).

## ADR-013 — Phase 05E server security composition

**Status:** Accepted
**Phase:** 05E

### Decision

Use the ASP.NET Core Identity application cookie and framework middleware at the Web composition root. Validate the security stamp and administrative account state on every authenticated request, revalidate Interactive Server circuits every five minutes, and propagate only the stable UserId, informational username, and role snapshot through a scoped current-user accessor. Add baseline security headers with CSP report-only and retain the existing antiforgery, HTTPS, HSTS, and exception pipeline. Provide an explicit secret-backed administrator bootstrap service without invoking it during startup.

### Boundaries

No custom session registry, cache, distributed store, new package, migration, database update, account lifecycle workflow, durable event producer, IP/UserAgent capture, clinical/read auditing, or UI workflow is introduced. Existing roles and policies remain unchanged. Administrator bootstrap refuses to overwrite any existing privileged account and never logs or persists a plaintext password.

## ADR-013 — Graphify as generated navigation evidence

**Status:** Accepted  
**Phase:** Cross-phase tooling

### Context

Repeated repository-wide scanning is costly, while generated information can become stale.

### Decision

Use graphify-out as a structural index, verified against source; refresh the custom schema using tools/graphify.ps1.

### Rationale

Fast discovery helps navigation without displacing authoritative source files.

### Consequences

Check fingerprints before relying on the snapshot. Read exact files before editing. Refresh after meaningful structural changes, not every trivial documentation edit; generic Graphify updating can overwrite custom metadata.

### Evidence / Notes

[Generator](../tools/graphify.ps1); [graph report](../graphify-out/GRAPH_REPORT.md); [AGENTS Graphify policy](../AGENTS.md).

## ADR-014 — Distinct context documents and explicit checkpoints

**Status:** Accepted  
**Phase:** Cross-phase documentation

### Context

Future sessions need product intent, detailed specifications, working rules, and an honest resume point.

### Decision

Use PRD for requirements, roadmap for phases, SpecKit for applicable approved specifications, AGENTS for working rules, and README for progress; architecture and decision documents explain structure and rationale.

### Rationale

Distinct roles reduce duplication and prevent a generated index or stale status statement from overriding source evidence.

### Consequences

Update checkpoints only after verified completion. Report conflicts; do not rewrite history silently. Templates are not approved feature specifications. Never stage, commit, or push without authorization.

### Evidence / Notes

[AGENTS context protocol](../AGENTS.md); [README maintenance rule](../README.md); [SpecKit workspace](../speckit/my-project/).

## ADR-015 — Measured performance, reliability, and quality gates

**Status:** Accepted
**Phase:** Cross-phase requirement

### Context

The HMS must remain correct, secure, reliable, and responsive under realistic hospital workloads without speculative infrastructure or premature optimization.

### Decision

Apply a cross-cutting performance and reliability review to every remaining phase. Prefer bounded, server-side, measured database operations; appropriate async I/O and cancellation; safe exception handling; structured logging; health checks; deliberate retries and timeouts; correct resource lifetimes; and explicit concurrency behavior. Add caching, indexes, background infrastructure, or other performance technology only when a measured and approved use case justifies it.

### Rationale

Correctness, security, data integrity, and predictable behavior have priority over unmeasured latency improvements. Measurements and realistic workflows are required before claiming high performance or introducing operational complexity.

### Consequences

Phase gates must report performance, reliability, security, database-query, and regression-test impact. Phase 06/07 must define the shared pagination contract before large list services proliferate. Phase 10/11 must perform a dedicated performance and reliability review covering representative SQL, indexes, N+1 detection, bounded queries, pagination, latency, Identity overhead, concurrency, exception handling, logging, health checks, resource lifetimes, load testing, and regressions. No current source, package, index, cache, Identity, migration, or database change follows from this documentation decision.

### Evidence / Notes

[Approved performance, reliability, and quality requirement](superpowers/specs/2026-09-21-performance-reliability-quality-requirement.md); [architecture overview](ARCHITECTURE.md); [development roadmap](../DEVELOPMENT_ROADMAP.md).

## ADR-016 — Phase 06 DTO and validation boundaries

**Status:** Accepted
**Phase:** 06

### Context

Phase 06 establishes application contracts before use-case services are introduced.
The contracts must remain persistence-neutral and must not encode unapproved workflow
or resource decisions.

### Decision

Use immutable summary/detail/request contracts with bounded, explicitly validated
pagination, search, filtering, and sorting. Normalize textual input by trimming and
converting blank optional values to `null` while preserving Unicode and caller-visible
values. Patient phone numbers are non-unique: duplicates remain allowed and phone may
only provide a duplicate-candidate search signal. Patient email remains optional.
Persistence-assisted validation, duplicate lookups, resource authorization, and
application services remain outside Phase 06. Apply opaque concurrency tokens only to
the explicitly approved Patient, Appointment, and queue contracts.

### Consequences

DTO validation cannot replace domain invariants or persistence constraints. Phase 07
may orchestrate these contracts, but must not reinterpret duplicate-phone signals as
uniqueness or introduce speculative FacilityId, provider/doctor DTO, rescheduling,
reactivation, queue-position, or ticket-generation behavior.

### Evidence / Notes

[Architecture overview](ARCHITECTURE.md); [development roadmap](../DEVELOPMENT_ROADMAP.md);
Phase 06 contract and validator sources under `ElsheiekhHMS.Application`.

## ADR-017 — Phase 07S database-backed identifier allocation

**Status:** Accepted
**Phase:** 07S

### Context

Patient codes and walk-in queue tickets must remain unique under concurrent requests,
application restarts, and multiple application instances. Queue identity is operationally
scoped by date, while duplicate patient phone numbers remain valid.

### Decision

Keep allocation in Infrastructure and persist the next sequence in SQL Server allocation
tables. Allocation uses a serialized transaction and atomic update/output operation rather
than `MAX+1`, a process-local/static counter, or an identifier derived from a phone number
or name. Walk-in queue uniqueness is the tuple `(QueueDate, SequenceNumber)` and does not
include Department. Patient phone has no unique constraint. Development and integration
verification databases remain separate.

### Consequences

The approved additive `AddPhase07AllocatorInfrastructure` migration owns the two allocation
tables and queue uniqueness index. Application contracts remain persistence-neutral, and no
repository, Unit of Work, MediatR, CQRS, broker, or cache is introduced by this prerequisite.
The next Phase 07 service sub-phases must use these boundaries rather than reimplementing
allocation in application code.

### Evidence / Notes

[Phase 07S allocator implementation](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/);
[Architecture overview](ARCHITECTURE.md); [development roadmap](../DEVELOPMENT_ROADMAP.md).

## ADR-018 — Phase 07A Patient service transaction boundaries

**Status:** Accepted
**Phase:** 07A

### Context

Patient registration requires the database-backed 07S PatientCode allocator and a
durable business audit event. Patient integer identity is database-generated and is
not available before the write is saved.

### Decision

Keep `AllocatePatientCodeAsync` on the narrow `IPatientPersistence` port. Do not add
another allocator abstraction or perform an early save. Registration stages the
`PATIENT_REGISTERED` AuditLog with the durable PatientCode as its target identifier,
then saves Patient and AuditLog together exactly once.

### Rationale

Allocation is persistence-dependent, and the existing 07S allocator is the approved
concurrency-safe implementation. PatientCode is available before persistence and is
stable, unique, and sufficient to identify the registration event while preserving
the atomic Patient/AuditLog transaction.

### Consequences

Patient phone remains non-unique, duplicate candidates remain non-blocking, stable
UserId remains the actor identifier, and no Core, Identity, schema, migration, or
package change is required. Later Phase 07 services remain separately gated.

### Evidence / Notes

[07A Patient service](../ElsheiekhHMS.Application/Patients/);
[narrow persistence port](../ElsheiekhHMS.Application/Patients/Persistence/);
[07S allocator decision](#adr-017--phase-07s-database-backed-identifier-allocation).

## ADR-019 — Phase 07B Department service boundaries

**Status:** Accepted
**Phase:** 07B

### Context

Department is organization master data with active/inactive lifecycle behavior. The
application needs bounded management use cases without adding a generic repository, a
schema change, or a new authorization vocabulary.

### Decision

Implement `IDepartmentService` with only `GetByIdAsync`, `SearchAsync`, `CreateAsync`,
`UpdateAsync`, and `DeactivateAsync`, backed by the narrow EF-free
`IDepartmentPersistence` port. Search uses direct projections, server-side filtering,
sorting, and bounded pagination. Use the existing `CanConfigureSystem` capability mapping:
only `SystemAdministrator` may manage Department configuration. Create, update, and
deactivate stage `DEPARTMENT_CREATED`, `DEPARTMENT_UPDATED`, and `DEPARTMENT_DEACTIVATED`
AuditLog events with stable UserId attribution and commit the state change plus audit entry
with one `SaveChangesAsync`.

Department names remain non-unique because the existing EF model has no approved unique
constraint. Deactivation preserves historical references; reactivation and hard delete are
outside 07B. Department has no concurrency token, so none is introduced.

### Consequences

Application remains EF-free and no Core, ApplicationUser, schema, migration, snapshot,
package, generic repository, Unit of Work, MediatR, CQRS, cache, broker, or workflow change
is required. The minimal `DepartmentSearchRequest` contract fills the missing Phase06 list
query shape without reopening the broader Phase06 contract family.

### Evidence / Notes

[Department service](../ElsheiekhHMS.Application/Departments/);
[Department persistence](../ElsheiekhHMS.Infrastructure/Persistence/Departments/);
[Department EF configuration](../ElsheiekhHMS.Infrastructure/Configurations/Entities/DepartmentConfiguration.cs);
[authorization policy mapping](../ElsheiekhHMS.Web/Security/AuthorizationConfiguration.cs).

## ADR-020 — Phase 07C-P AppointmentCode allocation

**Status:** Accepted
**Phase:** 07C-P

### Context

Appointment creation needs a durable operational identifier that remains stable
when an appointment is rescheduled and remains unique across application
instances and restarts. The approved identifier is independent of the scheduled
date and must be allocated before the future AppointmentService write.

### Decision

Keep AppointmentCode allocation in Infrastructure. Allocate from a dedicated
SQL Server `AppointmentCodeAllocations` row keyed by the current UTC year,
using a serializable transaction and an atomic update/insert operation. Format
the result as `AP-YYYY-NNNNN`; the sequence resets per allocator year and the
complete formatted value is protected by the unique
`UX_Appointments_AppointmentCode` index. Formatting width is a minimum display
width, not a hard sequence cap. The `AddAppointmentCodeAllocator` migration is
the single additive migration for this prerequisite and has been applied exactly
once to the approved development database after read-only preflight.

### Consequences

The Appointment domain entity, Core, ApplicationUser, existing migrations, and
unrelated Appointment collision rules remain unchanged. The allocator is
restart-safe and multi-instance-safe without `MAX+1`, static counters, or
process-local state. Automated SQL verification uses only the exact isolated
integration database; AppointmentService and 07D remain separately gated.

### Evidence / Notes

[AppointmentCode allocator](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/AppointmentCodeAllocator.cs);
[AddAppointmentCodeAllocator migration](../ElsheiekhHMS.Infrastructure/Migrations/20260922013639_AddAppointmentCodeAllocator.cs);
[Development roadmap](../DEVELOPMENT_ROADMAP.md).

## ADR-021 — Phase 07C Appointment application service boundaries

**Status:** Implemented; closed
**Phase:** 07C

### Context

Appointment scheduling needs an EF-free application boundary over the existing
Phase 06 contracts and the approved 07C-P AppointmentCode allocator. Booking
times are hospital-local civil values while appointment state and audit writes
must remain concurrency-safe and historical.

### Decision

Implement `IAppointmentService` with bounded reads (`GetByIdAsync`,
`SearchAsync`) and the approved schedule/lifecycle writes (`CreateAsync`,
`ScheduleAsync`, `CancelAsync`, `MarkNoShowAsync`, `CheckInAsync`, and
`CompleteAsync`). Application interprets civil booking values in the configured
`Africa/Kigali` timezone, rejects invalid/ambiguous/past instants, and supplies
UTC action timestamps to the Core aggregate. New bookings require an existing
Patient, an active Department, and an active Doctor assigned to that Department.

Appointment collisions are defined as the same Department and scheduled
instant while an existing appointment is Scheduled, Confirmed, or CheckedIn;
Cancelled, NoShow, and Completed records do not block a slot. Infrastructure
performs the authoritative check inside a serializable transaction and acquires
a transaction-scoped SQL Server application lock for the Department/date/time
key, avoiding a new schema constraint while preventing concurrent duplicate
slots under the current model.

Each mutation stages its business AuditLog event with the stable UserId actor and
durable AppointmentCode target, then performs exactly one SaveChanges operation
for the Appointment/AuditLog pair. Provider-scoped and Patient self-service
operations remain deferred because no safe ownership mapping exists.

### Consequences

Application remains EF-free. Core, ApplicationUser, the database schema,
migrations, snapshot, and package set remain unchanged. No rescheduling, queue
creation, encounter creation, FacilityId, generic repository, Unit of Work,
MediatR, CQRS, broker, or cache is introduced. Search uses direct projections,
server-side filtering/sorting, bounded pagination, and cancellation propagation.

### Evidence / Notes

[Appointment service](../ElsheiekhHMS.Application/Appointments/);
[Appointment persistence](../ElsheiekhHMS.Infrastructure/Persistence/Appointments/);
[Phase 03D scheduling design](../docs/superpowers/specs/2026-09-21-phase03d-appointment-walk-in-queue-design.md).

## ADR-022 — Phase 07D Queue application service boundaries

**Status:** Implemented; closed
**Phase:** 07D

### Context

Walk-in queue operations require an EF-free application boundary over the
approved queue DTOs, the existing `WalkInQueueEntry` lifecycle, and the 07S
database-backed daily ticket allocator. Queue records represent operational
waiting workflow and retain only the registered Patient reference.

### Decision

Implement `IQueueService` with bounded reads (`GetByIdAsync`, `SearchAsync`),
queue creation (`AddAsync`), nurse call, doctor routing, hold/resume,
completion, and cancellation operations. QueueDate is derived from the
server-supplied UTC clock in the hospital timezone `Africa/Kigali`; tickets are
allocated by the existing `QueueTicketAllocator` and are never generated with
`MAX+1` or process-local state. New entries require an existing Patient and an
active Department. Active duplicate entries for the same Patient and local
queue date are rejected; Completed and Cancelled history does not block a
future entry.

Infrastructure performs the duplicate check inside a serializable transaction
with a transaction-scoped SQL Server application lock keyed by Patient/date,
then persists the Queue/AuditLog pair with one `SaveChangesAsync`. Reads use
direct projections, server-side filters/sorting/pagination, and cancellation.
Only authenticated users with stable UserId and the existing Administrator or
Receptionist roles may use this service; Patient self-service and
Provider-scoped access remain deferred because no safe ownership mapping exists.

### Consequences

Core, ApplicationUser, schema, migrations, snapshot, and package set remain
unchanged. No queue position, anonymous demographics, automatic Patient or
Encounter creation, appointment check-in integration, generic repository,
Unit of Work, MediatR, CQRS, cache, broker, or provider model is introduced.
Queue lifecycle and historical Department references remain governed by the
existing Core aggregate.

### Evidence / Notes

[Queue service](../ElsheiekhHMS.Application/Queue/);
[Queue persistence](../ElsheiekhHMS.Infrastructure/Persistence/Queue/);
[Queue domain design](../docs/superpowers/specs/2026-09-21-phase03d-appointment-walk-in-queue-design.md).
## ADR-023 — Phase 07 closeout and 07E deferment

**Status:** Accepted
**Phase:** 07

### Decision

Phase 07 Application Services is complete for the approved scope: 07S allocator
infrastructure, 07A Patient, 07B Department, 07C-P AppointmentCode allocation,
07C Appointment, and 07D Queue are complete. The conditional 07E Doctor/Provider
Application Service is explicitly deferred.

The Doctor domain entity, its Department relationship, the required Appointment
relationship, the optional Queue routing relationship, and the EF persistence model
already exist. 07E cannot begin until the project approves Phase06-style
Doctor/Provider contracts and validators, the service operations, a
Doctor↔ApplicationUser ownership relationship, ownership persistence, and an
ownership-aware authorization scope. Any required schema migration and its tests
must be designed and approved before implementation. Doctor is not a missing
entity, and Department/Appointment relationships are not missing.

At the time of this closeout, Phase 08 Business Workflows had not started. The
later approved 08A staff intake orchestration is recorded in ADR-024; no Core,
ApplicationUser, schema, migration, snapshot, or package change belongs to it.

### Consequences

07E remains deferred until its prerequisites are explicitly reviewed and approved.
Phase 07 may close for the currently approved service scope, while provider-scoped
and ownership-dependent workflows remain unavailable. The next checkpoint recorded
at that time was Phase 08 review; 07E must still be revisited before implementing
Doctor/Provider workflows.

## ADR-024 — Phase 08A staff intake orchestration

**Status:** Implemented; ready for closeout
**Phase:** 08A

### Decision

Implement one EF-free `IPatientIntakeAppointmentService` operation,
`IntakeAndScheduleAsync`, for authenticated Administrators and Receptionists.
The request selects exactly one mode: an existing Patient identifier or a new
`RegisterPatientRequest`; it then schedules through the existing
`IAppointmentService`. Existing Patient scheduling relies on the Appointment
service's authoritative existence validation and does not perform a duplicate
Patient lookup.

Patient registration and appointment scheduling retain their existing separate
atomic transaction boundaries. If a new Patient is registered but scheduling
fails, the Patient remains valid master data and the workflow returns its
identity plus the safe appointment errors so a retry can use the existing
Patient without registering a duplicate. No compensation delete is attempted.

The workflow adds no audit writer, queue interaction, Appointment-to-Queue
relationship, provider ownership, persistence port, schema change, migration,
package, generic repository, Unit of Work, MediatR, CQRS, or workflow engine.

### Consequences

PatientService and AppointmentService remain authoritative for validation,
allocation, persistence, audit, time semantics, and collision protection.
08B owns any later arrival or queue handoff design. Provider-owned and Patient
self-service workflows remain deferred.

### Evidence / Notes

[08A workflow contract and implementation](../ElsheiekhHMS.Application/Workflows/PatientIntake/);
[08A focused tests](../ElsheiekhHMS.Tests/Unit/Application/Workflows/PatientIntakeAppointmentServiceTests.cs);
[Phase 08 scope review](../README.md#17-current-development-position).

## ADR-025 — Phase 08B-P durable Appointment↔Queue prerequisite

**Status:** Implemented; ready for 08B handoff orchestration
**Phase:** 08B-P

### Decision

Add the smallest durable relationship required for a later appointment-arrival
workflow: `WalkInQueueEntry.AppointmentId` is nullable, a queue entry may link to
zero or one Appointment, and an Appointment may have at most one linked queue
entry across its history. SQL Server enforces the latter with the filtered unique
index `UX_WalkInQueueEntries_AppointmentId` where `AppointmentId IS NOT NULL`.
The foreign key uses restrictive/no-action delete behavior so appointment history
cannot cascade-delete queue history. Existing queue rows remain null; no links are
backfilled by inference.

QueueDate remains the Africa/Kigali local calendar date at actual queue creation,
using the existing TimeProvider and allocator semantics. Existing active
Patient/date duplicate protection and serializable application-lock behavior are
preserved. Queue contracts expose the nullable link and provide controlled
appointment-linked creation plus indexed lookup, while 08B orchestration,
cross-aggregate validation, and any Patient/Provider ownership remain deferred.

### Consequences

`AddAppointmentQueueLink` is the only schema migration for this prerequisite and
was applied to the approved development database after isolated integration
verification. No old migration, Core base type, ApplicationUser, package, or
Phases.md was changed. The next step is the separately gated 08B arrival/queue
handoff workflow; this prerequisite does not create an Encounter or queue
automatically during appointment lifecycle operations.

### Evidence / Notes

[WalkInQueueEntry](../ElsheiekhHMS.Core/Domain/Scheduling/Entities/WalkInQueueEntry.cs);
[queue persistence mapping](../ElsheiekhHMS.Infrastructure/Configurations/Entities/WalkInQueueEntryConfiguration.cs);
[AddAppointmentQueueLink migration](../ElsheiekhHMS.Infrastructure/Migrations/20260922115337_AddAppointmentQueueLink.cs).

## ADR-026 — Phase 08B appointment arrival and queue handoff

**Status:** Implemented; ready for closeout
**Phase:** 08B

### Decision

Implement `CheckInAndQueueAsync` as a thin, EF-free Application workflow for
Administrators and Receptionists with a stable UserId. The workflow loads the
authoritative Appointment through `IAppointmentService`, checks it in through
the existing `CheckInAsync` operation when it is Scheduled or Confirmed, and
then creates or recovers the Appointment-linked queue entry through
`IQueueService`. PatientId and DepartmentId always come from the Appointment;
QueueService remains authoritative for the Africa/Kigali operational QueueDate,
active Patient/day duplicate protection, allocation, persistence, and audit.

Already CheckedIn appointments skip a second check-in. Existing linked queue
entries, including Completed or Cancelled history, are returned as an
`ExistingHandoff`. A Queue failure after check-in returns `PartialSuccess` and
keeps the Appointment CheckedIn so the caller can retry. A duplicate-link race
re-queries the indexed AppointmentId link and returns the winner when available.
No unrelated walk-in queue entry is attached, no cross-service transaction is
introduced, and no extra workflow audit event is written.

### Consequences

The workflow adds only Application contracts/service registration and tests. Core,
ApplicationUser, EF schema, migrations, model snapshot, and packages remain
unchanged. Appointment completion, no-show, cancellation, Encounter creation,
Provider ownership, and Patient self-service remain separate concerns.

### Evidence / Notes

[AppointmentArrivalQueueService](../ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueService.cs);
[focused workflow tests](../ElsheiekhHMS.Tests/Unit/Application/Workflows/AppointmentArrivalQueueServiceTests.cs);
[isolated SQL workflow test](../ElsheiekhHMS.Tests/Integration/Persistence/AppointmentArrivalQueueSqlServerTests.cs).
