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
