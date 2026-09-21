# Phase 04 EF Core & Database Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task after human approval. 04A and 04B are approved and complete; do not begin 04C without a separate review.

**Goal:** Add SQL Server persistence for the six approved Phase 03 domain types without changing Core or introducing Identity, repositories, or future domain tables.

**Architecture:** EF Core lives in Infrastructure behind `ElsheiekhHmsDbContext`; Fluent configurations remain outside Core. Web supplies configuration through `AddInfrastructure`, while Application owns later use-case orchestration and exception translation. SQL Server migrations remain with Infrastructure.

**Tech Stack:** .NET 10, EF Core 10.0.x, SQL Server, an already available isolated SQL Server test database, xUnit, Graphify.

**Spec:** `docs/superpowers/specs/2026-09-21-phase04-ef-core-database-design.md`

## Global Constraints

- Do not modify Core entities, enums, base classes, or `IHasConcurrencyToken`, except the separately approved private EF-only constructor added to `Patient` for materialization.
- Do not add Identity, `ApplicationUser`, `IdentityDbContext`, DTOs, Application services, repositories, generic Unit of Work, or UI behavior.
- Do not create tables for Encounter, Admission, Prescription, ClinicalObservation, Invoice, or any other entity absent from Phase 03 source.
- Core remains free of EF Core, SQL Server, ASP.NET Core, and Infrastructure references.
- Use .NET 10-compatible EF Core packages on one exact 10.0.x patch line.
- Use Fluent API configuration in Infrastructure; do not add EF attributes to Core.
- Preserve Unicode, UTC `DateTimeOffset` semantics, local civil `DateOnly`/`TimeOnly`, integer IDs, and explicit enum numeric values.
- Preserve appointment/queue historical `DepartmentId` values and use Restrict delete behavior for all clinical/operational FKs.
- Apply query filters only to Patient, Doctor, Appointment, and WalkInQueueEntry, and filter only `IsDeleted`.
- Map rowversion only for Patient, Appointment, and WalkInQueueEntry.
- Do not create a unique index on Patient.Phone.
- Do not auto-apply migrations from Web production startup; use a controlled approved workflow.
- Do not assume or install LocalDB, SQL Server Express, Docker, or another SQL Server instance during implementation without explicit approval.
- If no safe SQL Server test database is available at database verification, stop and report options; do not substitute InMemory or SQLite.
- Do not stage, commit, push, or execute this plan without explicit implementation approval.

## Review Focus

- Historical DepartmentId values must survive department changes and parent deletes; Task 3 verifies Restrict FKs and migration metadata.
- Local civil scheduling values must not be converted into invented UTC columns; Task 2 verifies `date`/`time` mappings.
- Shared patient phone numbers must remain valid; Task 3 verifies the non-unique phone index and absence of a unique phone constraint.
- Soft-delete filters must hide only explicit deletion and must not hide cancelled/completed/no-show history; Task 3 verifies filter expressions.
- SQL Server rowversion and filtered unique indexes cannot be proven by EF InMemory; Task 4 uses an isolated SQL Server database.

---

### Task 1: EF Core foundation and context

**Files:**
- Modify: `ElsheiekhHMS.Infrastructure/ElsheiekhHMS.Infrastructure.csproj`
- Create: `ElsheiekhHMS.Infrastructure/Persistence/ElsheiekhHmsDbContext.cs`
- Modify: `ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs`
- Modify: `ElsheiekhHMS.Web/appsettings.Development.json` only after the connection-key decision is approved
- Test: `ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextTests.cs`

**Interfaces:**
- Consumes: existing `ElsheiekhHMS.Infrastructure` project references and `IConfiguration` registration boundary.
- Produces: `ElsheiekhHmsDbContext : DbContext`, `DbSet<Department>`, `DbSet<Doctor>`, `DbSet<Patient>`, `DbSet<Appointment>`, and `DbSet<WalkInQueueEntry>`; `AddInfrastructure` registration using `ConnectionStrings:ElsheiekhHmsDatabase`.

- [x] **Step 1: Record approved package and connection decisions.** Confirm the EF Core 10.0.x patch, inspect available SQL Server connectivity without installing infrastructure, and use the `ElsheiekhHmsDatabase` key before editing project files.
- [x] **Step 2: Add the three approved Infrastructure packages.** Add `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.SqlServer`, and `Microsoft.EntityFrameworkCore.Design` with one exact 10.0.x version; keep Design private. Do not add Tools, InMemory, or Identity packages in this task.
- [x] **Step 3: Add the context without Core changes.** Create `ElsheiekhHmsDbContext` in `ElsheiekhHMS.Infrastructure.Persistence`, expose only the five aggregate-root DbSets, call the base model builder, and apply configurations from the Infrastructure assembly.
- [x] **Step 4: Register the context through Infrastructure.** Add `AddDbContext<ElsheiekhHmsDbContext>` inside `AddInfrastructure`; keep SQL Server configuration out of `Program.cs` and Application.
- [x] **Step 5: Add a context-contract test.** Verify the context type and five root-set properties without a test provider; full model construction remains a 04B concern until scalar mappings bind the existing domain constructors. Do not use InMemory or SQLite as SQL Server proof.
- [x] **Step 6: Verify the foundation.** Run `dotnet restore ElsheiekhHMS.slnx`, `dotnet build ElsheiekhHMS.slnx --no-restore`, and the focused context-contract test. Result: zero warnings/errors and no Core dependency change.

### Task 2: Scalar configuration and Doctor-owned schedules

**Files:**
- Create: `ElsheiekhHMS.Infrastructure/Configurations/Entities/DepartmentConfiguration.cs`
- Create: `ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorConfiguration.cs`
- Create: `ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorScheduleConfiguration.cs`
- Create: `ElsheiekhHMS.Infrastructure/Configurations/Entities/PatientConfiguration.cs`
- Create: `ElsheiekhHMS.Infrastructure/Configurations/Entities/AppointmentConfiguration.cs`
- Create: `ElsheiekhHMS.Infrastructure/Configurations/Entities/WalkInQueueEntryConfiguration.cs`
- Modify: `ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextModelTests.cs`

**Interfaces:**
- Consumes: `ElsheiekhHmsDbContext` and the six actual Core types.
- Produces: six dedicated Fluent configuration units applied through `ApplyConfigurationsFromAssembly`.

- [x] **Step 1: Configure Department.** Map `Departments`, identity `Id`, bounded Unicode Name/Description/PhoneExtension, IsActive, and inherited audit fields; do not invent a unique Department Name constraint.
- [x] **Step 2: Configure Doctor and its schedules.** Map `Doctors`, bounded code/name/specialization, `decimal(12,3)` ConsultationFee, integer status, required DepartmentId, soft-delete scalars, and the private schedules collection. Configure the required shadow DoctorId relationship.
- [x] **Step 3: Configure DoctorSchedule.** Map `DoctorSchedules`, identity Id, shadow DoctorId, DayOfWeek, `time` values, slot duration, IsActive, and audit fields. Defer lookup indexes to 04C.
- [x] **Step 4: Configure Patient.** Map required/optional Unicode fields, `date` DateOfBirth, integer Gender/BloodGroup, computed FullName/ShortName ignored, audit/deletion scalars, and the approved getter-only PatientCode backing field. Defer uniqueness, filters, and rowversion to 04C.
- [x] **Step 5: Configure Appointment.** Map required IDs, immutable civil date/time, integer Type/Status, bounded notes/cancellation fields, audit/deletion fields, and no queue navigation. Defer filters and rowversion to 04C.
- [x] **Step 6: Configure WalkInQueueEntry.** Map required/optional IDs, QueueDate, sequence/ticket, integer priority/status, UTC event timestamps, notes, and audit/deletion fields. Defer filters and rowversion to 04C.
- [x] **Step 7: Add scalar metadata tests.** Assert table names, Unicode/bounded string mappings, `decimal(12,3)`, `date`, `time`, `datetimeoffset`, ignored computed properties, enum conversion, and constructor/materialization behavior.
- [x] **Step 8: Verify the scalar batch.** Restore, build, full test, focused model tests, architecture scan, and Graphify validation passed.

### Task 3: Relationships, filters, indexes, constraints, and concurrency

**Files:**
- Modify: `ElsheiekhHMS.Infrastructure/Configurations/Entities/*.cs`
- Modify: `ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextModelTests.cs`

**Interfaces:**
- Consumes: scalar mappings from Task 2 and the approved Department/Doctor/Patient/Scheduling relationships.
- Produces: Restrict-delete FK metadata, query filters, lookup indexes, uniqueness constraints, and opt-in SQL Server rowversion metadata.

- [x] **Step 1: Configure every FK with Restrict.** Add Department→Doctor, Doctor→DoctorSchedule, Patient/Doctor/Department→Appointment, and Patient/Doctor/Department→WalkInQueueEntry relationships without adding public domain navigations.
- [x] **Step 2: Add filters.** Apply `!IsDeleted` only to Patient, Doctor, Appointment, and WalkInQueueEntry; leave Department and DoctorSchedule lifecycle fields unfiltered.
- [x] **Step 3: Add required indexes.** Add FK indexes, patient non-unique Phone, appointment lookup indexes, queue board/history indexes, and DoctorSchedule lookup index.
- [x] **Step 4: Add approved uniqueness.** Add only PatientCode unique and filtered nullable NationalId/PassportNumber unique. Do not add queue-ticket, DoctorCode, AppointmentCode, Department Name, or Phone uniqueness without a later explicit stable-business-rule approval.
- [x] **Step 5: Add rowversion only to opted-in types.** Configure Patient, Appointment, and WalkInQueueEntry `RowVersion` as generated SQL Server rowversion concurrency tokens; verify Department, Doctor, and DoctorSchedule receive none.
- [ ] **Step 6: Add restrained database checks.** No additional SQL check was added in 04C; the queue sequence range is already enforced by Core and any portable database check remains subject to 04D migration review. Do not encode status transitions, schedule overlap, appointment collisions, or ticket text parsing as SQL checks.
- [x] **Step 7: Test relational metadata.** Assert delete behaviors, filter application, unique/non-unique indexes, filtered predicates, and exactly three concurrency tokens using relational model/query metadata.
- [x] **Step 8: Verify the architecture boundary.** Search Core for EF/SQL/Infrastructure references and run the full build. Result: no Core change and zero warnings/errors.

### Task 4: Migration and SQL Server verification

**Files:**
- Create: `ElsheiekhHMS.Infrastructure/Migrations/*`
- Create: `ElsheiekhHMS.Infrastructure/Persistence/ElsheiekhHmsDbContextFactory.cs` only if `dotnet ef` cannot construct the context
- Create: `ElsheiekhHMS.Tests/Integration/Persistence/SqlServerTestDatabaseFixture.cs`
- Create: `ElsheiekhHMS.Tests/Integration/Persistence/ElsheiekhHmsDbContextSqlServerTests.cs`

**Interfaces:**
- Consumes: approved context/configurations from Tasks 1–3 and an isolated SQL Server connection already available to the project/developer.
- Produces: reproducible Infrastructure migrations and provider-specific persistence verification.

- [ ] **Step 1: Verify the test database.** Inspect the already available SQL Server configuration outside source control; use a unique test database name and a secret-free environment variable. If no safe instance exists, stop and report options.
- [ ] **Step 2: Generate the migration.** Run `dotnet ef migrations add InitialHospitalSchema --context ElsheiekhHmsDbContext --project ElsheiekhHMS.Infrastructure --startup-project ElsheiekhHMS.Web` only after approval.
- [ ] **Step 3: Review generated SQL/model.** Confirm six tables only, dbo schema, identity integer keys, bounded Unicode strings, decimal precision, date/time types, filtered indexes, Restrict FKs, filters, and rowversion columns. Reject any future table or cascade path.
- [ ] **Step 4: Apply the migration to the isolated test database.** Run `dotnet ef database update` against the isolated database only; never use a production or shared database.
- [ ] **Step 5: Verify SQL Server-specific behavior.** Test rowversion stale updates, filtered nullable uniqueness, DateOnly/TimeOnly round trips, soft-delete filter behavior, and prohibited cascade deletes. Do not claim queue-ticket uniqueness.
- [ ] **Step 6: Test history preservation.** Insert appointment and queue rows, attempt parent deletion, and assert Restrict prevents physical history loss. Marking a row soft-deleted must hide it through the normal query and preserve it through an authorized filter bypass.
- [ ] **Step 7: Verify migration repeatability.** Recreate a clean isolated database, apply the migration from scratch, and compare schema results; record the exact provider/version.

### Task 5: Infrastructure boundary and handoff

**Files:**
- Modify: `ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs` only for approved registration refinements
- Modify: `ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj` only for approved SQL Server test packages
- Create: `docs/superpowers/plans/2026-09-21-phase04-implementation-report.md` after implementation, not during design review

**Interfaces:**
- Consumes: verified context, mappings, migration, and integration fixture.
- Produces: a reviewed Phase 04 implementation checkpoint for later Application/Identity work.

- [ ] **Step 1: Verify exception ownership.** Ensure Infrastructure catches/handles provider concurrency and constraint exceptions at a boundary that can later return safe Application results; no EF exception reaches Core.
- [ ] **Step 2: Verify no repository overreach.** Confirm no generic repository, Unit of Work, domain event interceptor, auto-seeding, auto-migration, or Identity context was added.
- [ ] **Step 3: Run final checks.** Run `dotnet restore ElsheiekhHMS.slnx`, `dotnet build ElsheiekhHMS.slnx --no-restore`, `dotnet test ElsheiekhHMS.slnx --no-build`, the Core dependency scan, and `git diff --check`.
- [ ] **Step 4: Refresh Graphify.** Run `./tools/graphify.ps1` and `./tools/graphify.ps1 -Check`; verify Infrastructure packages/configurations are indexed and Core remains independent.
- [ ] **Step 5: Review the diff and gate.** Confirm only approved Phase 04 files changed, no secrets are present, and obtain the separate Phase 04 implementation approval before continuing to Phase 05.

## Plan self-review

- **Spec coverage:** Tasks 1–5 cover the context, packages, all six configurations, relationships, filters, indexes, constraints, rowversion, migrations, connection boundary, SQL Server testing, exception boundary, and Graphify handoff required by the design specification.
- **Placeholder scan:** No implementation step depends on an unspecified type or an unowned future entity. Open decisions are explicit approval gates in the specification, not hidden implementation placeholders.
- **Type consistency:** The plan uses `ElsheiekhHmsDbContext`, `ElsheiekhHMS.Infrastructure.Persistence`, the six actual Phase 03 types, and the exact configuration/test paths consistently.
- **Review focus:** Each high-risk condition is assigned to a task with a concrete metadata or SQL Server verification step.
