# ElsheiekhHMS

ElsheiekhHMS is an existing commercial clinic and small-hospital management system built with .NET 10, ASP.NET Core/Blazor Interactive Server, EF Core 10, SQL Server, and ASP.NET Core Identity. Development is incremental and backend-first; future product direction is not treated as implemented functionality.

## Current checkpoint

- Branch: `main`
- Accepted checkpoint: `2fc6b2bbc6ff891556759fe6807e338477b22c35`
- Full automated suite: **462 passing tests**
- Build: passing
- Current milestone: **Phase 13D — Encounter Persistence complete**
- Next approved milestone: **Phase 13E — Encounter Application Contracts & Validation**
- `07E` Doctor/Provider Application Service remains explicitly deferred.

Implemented through the accepted checkpoint:

- Solution/core foundation, Patient, Department, Doctor, Appointment, Queue
- Identity, roles/policies, audit infrastructure, observability, and SQL readiness
- Patient intake and Appointment arrival → Queue handoff workflows
- Blazor operational UI through the frozen Phase 12 baseline
- Provider ↔ Doctor ownership mapping and active-account resolution
- Encounter domain model and SQL Server persistence (`dbo.Encounters`, `AddEncounterPersistence`)

Not yet implemented:

- Encounter Application contracts, validators, persistence port, service, workflow, and UI
- Clinical notes, diagnosis, vitals, laboratory, radiology, pharmacy, billing, and payments
- Visit, HospitalService, and ServiceRequest concepts
- Patient self-service and broader Provider/Doctor CRUD

## Architecture

```text
Web -> Application -> Core
Web -> Infrastructure -> Application + Core
Tests -> production projects required by accepted scenarios
Core -> no project dependency
```

Application remains EF-free. Infrastructure owns EF Core, SQL Server, Identity, migrations, and technical persistence. Blazor is presentation; backend authorization is authoritative. Patient, Appointment, Queue, and Encounter remain separate concepts. Encounter provenance is PatientId, DepartmentId, DoctorId, and QueueEntryId; no direct AppointmentId, VisitId, or ServiceRequestId is currently persisted.

The operational timezone is Africa/Kigali. Persisted and comparison timestamps use UTC where required by the model. Stable Identity UserId is the audit actor; DoctorId identifies clinical responsibility. Existing migrations are immutable and automated tests never use `ElsheiekhHMS_Dev`.

## Development workflow

Before a substantial task, read [AGENTS.md](AGENTS.md), this checkpoint, the relevant [PRD](PRD.md), [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md), specifications/decisions, exact source/tests, migrations, and Graphify output. Search for existing equivalents first. Use the smallest justified change, run focused tests then the full suite/build, review the diff, and commit/push only when explicitly authorized. Never start the next phase automatically.

## Project resources

- [Engineering operating contract](AGENTS.md)
- [Product requirements and commercial direction](PRD.md)
- [Development roadmap](DEVELOPMENT_ROADMAP.md)
- [Architecture reference](docs/ARCHITECTURE.md)
- [Clinical Encounter architecture](docs/CLINICAL_ENCOUNTER_ARCHITECTURE.md)
- [Architecture decisions](docs/DECISIONS.md)
- [Development data guidance](docs/DEVELOPMENT_DATA.md)
- [Graphify report](graphify-out/GRAPH_REPORT.md)
- [UI QA handoffs](docs/UI_PRE_MANUAL_QA.md)
- [Speckit workspace](speckit/my-project/)

## Verification commands

```powershell
dotnet restore ElsheiekhHMS.slnx
dotnet build ElsheiekhHMS.slnx --no-restore
dotnet test ElsheiekhHMS.slnx --no-build
.\tools\graphify.ps1 -Check
```

## Current next action

Prepare and approve the Phase 13E contract/validation design, then implement only that bounded EF-free Application milestone. Do not begin Encounter workflow, Visit, ServiceRequest, billing, triage, or UI work until separately authorized.
