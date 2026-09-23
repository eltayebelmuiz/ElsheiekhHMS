# ElsheiekhHMS Agent Operating Contract

ElsheiekhHMS is an existing commercial clinic and small-hospital management system being extended incrementally. Preserve the current solution, data, architecture, and accepted decisions. Never restart, recreate, or restructure the solution without explicit architectural approval.

## Mandatory workflow

For every substantial task:

```text
DISCOVER -> UNDERSTAND -> PLAN -> IMPLEMENT -> VERIFY -> REVIEW -> DOCUMENT
-> COMMIT/PUSH only when authorized -> REPORT -> STOP
```

Before creating anything, search the repository for an existing equivalent. Read this file, the current README checkpoint, the relevant PRD/roadmap sections, applicable specifications and decision records, exact source and tests, and relevant migration history. Use Git history when intent is unclear. Use Graphify as a map after checking freshness; source and tests remain authoritative.

## Source of truth

Resolve conflicts in this order:

1. Actual source and project files
2. Accepted automated tests
3. Current approved specifications and acceptance records
4. Architecture and decision records
5. PRD and commercial product direction
6. Development roadmap
7. README summaries
8. Graphify output

Report meaningful conflicts. Do not silently rewrite history or treat future product direction as implemented behavior.

## Architecture boundaries

The verified project direction is:

```text
Core           -> no project dependency
Application    -> Core
Infrastructure -> Application, Core
Web            -> Application, Infrastructure
Tests          -> production projects required by accepted scenarios
```

`Core` remains independent of EF Core, SQL Server, ASP.NET Core, Identity, Blazor, and outer projects. `Application` remains EF-free and owns use-case contracts, validation, orchestration, authorization decisions, and bounded result models. `Infrastructure` owns EF Core, SQL Server, Identity, migrations, technical persistence, and audit implementation. `Web` is presentation and composition; UI visibility is never the security boundary.

Use narrow persistence ports justified by a real use case. Do not add generic repositories, Unit of Work, CQRS, MediatR, event buses, workflow engines, microservices, Redis, or speculative abstractions without explicit approval and measured need.

## Frozen domain and security invariants

Verify these rules against current source/tests before changing them:

- Patient != Appointment.
- Appointment != Queue.
- Queue != Encounter.
- Appointment != Encounter.
- Queue completion != Encounter completion.
- Appointment completion != Encounter completion.
- Patient phone is not globally unique.
- Appointment rescheduling is not implemented unless separately approved.
- Department reactivation is not implemented unless separately approved.
- Do not add `FacilityId` without an approved requirement.
- `ApplicationUser` is authentication identity; `Doctor` is clinical responsibility.
- Stable Identity `UserId` is the authoritative audit actor; `DoctorId` is clinical responsibility.
- Administrator/SystemAdministrator are not automatically clinical superusers.
- Provider role membership alone does not establish Doctor ownership.
- Backend authorization and resource checks are authoritative; UI controls are only presentation.
- Africa/Kigali is the operational timezone; persisted/comparison timestamps use UTC where the model requires it.
- Existing migrations are immutable historical records; schema changes are forward-only.
- `ElsheiekhHMS_Dev` is never used by automated tests; integration tests use their guarded isolated database.
- Clinical Encounter provenance is PatientId, DepartmentId, DoctorId, and QueueEntryId. Do not add AppointmentId, VisitId, or ServiceRequestId speculatively.
- Encounter start is explicit from an eligible doctor-stage Queue entry; it does not complete Queue or Appointment.
- Encounter completion does not complete Queue or Appointment and does not perform billing.

## Database discipline

Before any schema work, inspect the current `DbContext`, configurations, model snapshot, migration sequence, and target database. Preserve data. Never delete or recreate a database as a shortcut. Generate only additive, reviewed migrations. Verify fresh and upgrade paths, foreign-key delete behavior, indexes, concurrency metadata, pending model changes, and isolated SQL integration tests.

## Security and privacy

Use server-side authorization, stable UserId attribution, bounded inputs, safe errors, and append-only audit behavior. Never place passwords, tokens, hashes, connection strings, or other secrets in source, logs, documentation, tests, or commits. Clinical resource operations require active Provider ownership resolution and per-resource checks. Do not expose sensitive clinical payloads in routine diagnostics.

## Testing and verification

Run focused tests first, then the full solution regression and build. Use real SQL integration tests where persistence/concurrency behavior matters. Preserve cancellation, bounded server-side queries, projections, and transaction/audit atomicity. Do not claim success from historical results; report the commands actually run, failures, warnings, skipped tests, and remaining limitations. For documentation-only changes, verify content and diff plus the baseline build/test when the affected checkpoint requires it.

## Git and change boundaries

Inspect `git status --short`, branch, and recent commits before significant work. Preserve unrelated changes. Review `git diff --check`, the full diff, and the staged diff. Never stage `Phases.md`, secrets, development database artifacts, or generated junk. Commit and push only when the task explicitly authorizes it. Never force-push or rewrite history. Never automatically begin the next phase.

## Current accepted checkpoint

At HEAD `2fc6b2bbc6ff891556759fe6807e338477b22c35` on `main`:

- Phases 01–12 are complete for their approved scopes.
- 07E Doctor/Provider Application Service remains deferred.
- Phase 13A architecture, 13B Encounter domain, 13C Provider ownership, and 13D Encounter persistence are complete.
- `dbo.Encounters` and migration `AddEncounterPersistence` are present and verified.
- Encounter application contracts, service/workflow, clinical extensions, and UI do not yet exist.
- The verified full test baseline is 462 passing tests.
- The next approved milestone is Phase 13E — Encounter Application Contracts & Validation.
- Visit, HospitalService, ServiceRequest, billing, triage/vitals, laboratory, pharmacy, and broader staff/attendance architecture remain future design work unless separately approved.

README, the roadmap, architecture/decision records, and clinical documentation must distinguish `IMPLEMENTED`, `PARTIAL`, `PLANNED`, `DEFERRED`, and `NOT YET DESIGNED`.

## Project resources

Use the applicable root and scoped `AGENTS.md` files, installed skills, repository `.agents/`, Speckit resources under `speckit/my-project/`, source/tests, `docs/`, migrations, Graphify, and Git history. Speckit templates and a constitution do not themselves authorize a feature. Use Speckit for substantial new capabilities or architecture when requirements must be resolved before implementation; do not require a ceremony for a tiny defect.

Stop at the authorized boundary, report blockers and source/spec conflicts, and wait for explicit approval before implementing the next milestone.
