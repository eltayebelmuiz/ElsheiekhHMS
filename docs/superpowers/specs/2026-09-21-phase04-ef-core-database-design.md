# PHASE 04 — EF CORE & DATABASE DESIGN REVIEW

**Status:** Design review only; implementation is not approved by this document.

**Date:** 2026-09-21

**Scope:** Persist the six approved Phase 03 domain types with EF Core and SQL Server while keeping Core free of persistence concerns.

## 1. Current state verification

The actual solution is five .NET 10 projects. Phase 01, Phase 02, and Phase 03 are recorded complete in the current README and the Phase 03 final review. Phase 04 is the next design gate.

Production source was searched for `Microsoft.EntityFrameworkCore`, SQL Server provider references, `DbContext`, `DbSet`, `IEntityTypeConfiguration`, `UseSqlServer`, migrations, connection strings, and EF attributes. No implementation was found. The only matching source text is the intentional Phase 04 comment in `InfrastructureServiceExtensions.cs`.

There are no package references for EF Core, SQL Server, Identity, or EF test providers. There is no `DbContext`, migration, database configuration, repository, or EF attribute in Core. Empty scaffold directories such as `Data`, `Configurations/Entities`, `Migrations`, and `Persistence` exist under Infrastructure, but they contain only `.gitkeep` files and are not implementations.

The repository has `sqlcmd` and `sqllocaldb`; `MSSQLLocalDB` is installed but currently stopped. `dotnet ef` is not installed as a global tool. No connection string exists in either Web appsettings file.

## 2. Confirmed Phase 04 scope

Phase 04 owns EF Core persistence, SQL Server mapping, migrations, constraints, indexes, soft-delete filters, and opt-in rowversion mapping. It does not own Identity, DTOs, application services, workflow orchestration, clinical encounters, or UI.

The current source and approved Phase 03 specifications supersede stale legacy roadmap sections that describe a .NET 9 MVC application, many entities that do not exist, and Phase 04 as already complete. Those sections are recorded as documentation conflicts; they are not implementation requirements.

## 3. Requirements traceability

| Requirement | Source | Status |
|---|---|---|
| EF Core and SQL Server belong in Infrastructure | `docs/ARCHITECTURE.md`, ADR-010 | REQUIRED |
| Core remains EF-independent | AGENTS.md, README, architecture | REQUIRED |
| Map the approved Phase 03 entities only | Phase 03A–03D specifications, actual Core | REQUIRED |
| Fluent configuration in separate Infrastructure configuration units | SpecKit data-integrity standards | REQUIRED, subject to the owned-schedule note below |
| PatientCode unique; nullable NationalId and PassportNumber filtered unique | SpecKit data-integrity standards | REQUIRED |
| Patient, Appointment, and WalkInQueueEntry rowversion | Phase 03C/03D specifications and standards | REQUIRED |
| Soft-delete filters for Patient, Doctor, Appointment, and WalkInQueueEntry | Phase 03D and data-integrity standards | REQUIRED |
| No unique Patient phone | approved Phase 03C decision | REQUIRED |
| Queue ticket property mapping; uniqueness scope remains deferred | approved Phase 04 blocker decision | REQUIRED mapping; no uniqueness constraint |
| Use .NET 10-compatible EF Core packages | actual project target framework | REQUIRED; reconciles stale PRD EF9 wording |
| SQL Server production; local environment inspected before use | approved Phase 04 decision and PRD | REQUIRED provider; no infrastructure is assumed |
| Initial migration and database verification | PRD and current Phase 04 objective | REQUIRED |
| Seed fake patients or identity users | no approved current requirement | NOT REQUIRED |
| Identity tables and authentication | roadmap Phase 05 | NOT YET REQUIRED |
| Generic repository or Unit of Work abstraction | no approved current API | NOT REQUIRED |

## 4. Persistent domain inventory

### Department

Path: `ElsheiekhHMS.Core/Domain/Organization/Entities/Department.cs`  
Namespace: `ElsheiekhHMS.Core.Domain.Organization.Entities`  
Base: `AuditableEntity`  
Interfaces: none

Properties: `Id`, `Name`, nullable `Description`, nullable `PhoneExtension`, `IsActive`, and inherited audit fields. `Name` is required and trimmed. The lifecycle is active then inactive; an inactive department cannot be changed. There is no soft delete and no concurrency token.

### Doctor

Path: `ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs`  
Namespace: `ElsheiekhHMS.Core.Domain.Staff.Entities`  
Base: `SoftDeletableEntity`  
Interfaces: none

Properties: `DoctorCode`, `FullName`, nullable `Specialization`, `IsGeneralPractitioner`, `ConsultationFee`, `DoctorStatus Status`, positive `DepartmentId`, read-only `Schedules`, and inherited audit/deletion fields. A doctor owns schedules through a private list. No concurrency token is approved for Doctor in Phase 03B.

### DoctorSchedule

Path: `ElsheiekhHMS.Core/Domain/Staff/Entities/DoctorSchedule.cs`  
Namespace: `ElsheiekhHMS.Core.Domain.Staff.Entities`  
Base: `AuditableEntity`  
Interfaces: none

Properties: `DayOfWeek`, `StartTime`, `EndTime`, `SlotDurationMinutes`, `IsActive`, and inherited audit fields. The constructor and retirement method are internal and the type is aggregate-owned by Doctor. The proposed relational mapping uses a required shadow `DoctorId` FK and the private backing collection; it is not an independent application root.

### Patient

Path: `ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs`  
Namespace: `ElsheiekhHMS.Core.Domain.Patients.Entities`  
Base: `SoftDeletableEntity`  
Interfaces: `IHasConcurrencyToken`

Properties: immutable `PatientCode`; required first and last names; optional middle/third names; computed `FullName` and `ShortName`; `DateOfBirth`; `Gender`; nullable `BloodGroup`; nullable `NationalId` and `PassportNumber`; required `Phone` and `Address`; optional city, emergency-contact fields, and insurance provider; `RowVersion`; inherited audit/deletion fields. Shared phones are valid. The code shape is `PT-YYYY-NNNNN`; generation and cross-record uniqueness belong outside Core.

### Appointment

Path: `ElsheiekhHMS.Core/Domain/Scheduling/Entities/Appointment.cs`  
Namespace: `ElsheiekhHMS.Core.Domain.Scheduling.Entities`  
Base: `SoftDeletableEntity`  
Interfaces: `IHasConcurrencyToken`

Properties: immutable `AppointmentCode`, required `PatientId`, `DoctorId`, historical `DepartmentId`, `ScheduledDate`, `ScheduledTime`, `AppointmentType Type`, `AppointmentStatus Status`, optional `Notes`, nullable cancellation reason/time, `RowVersion`, and inherited audit/deletion fields. Status transitions are Core-owned; collision detection and current-record validation are Application/persistence concerns.

### WalkInQueueEntry

Path: `ElsheiekhHMS.Core/Domain/Scheduling/Entities/WalkInQueueEntry.cs`  
Namespace: `ElsheiekhHMS.Core.Domain.Scheduling.Entities`  
Base: `SoftDeletableEntity`  
Interfaces: `IHasConcurrencyToken`

Properties: required `PatientId` and historical `DepartmentId`, optional `DoctorId`, `QueueDate`, `SequenceNumber`, immutable `QueueNumber`, `QueuePriority Priority`, `QueueStatus Status`, `RegisteredAt`, nullable `CalledAt` and `CompletedAt`, optional `Notes`, `RowVersion`, and inherited audit/deletion fields. Queue allocation, duplicate active detection, ordering, and daily reset remain outside Core.

No tables are proposed for Encounter, Admission, Prescription, ClinicalObservation, Invoice, or any other future concept.

## 5. Persistence architecture

EF Core belongs in `ElsheiekhHMS.Infrastructure`. Core remains unaware of EF Core, SQL Server, migrations, query filters, and database exceptions. Application will later orchestrate use cases and translate Infrastructure outcomes into application results. Web remains the composition root and calls one Infrastructure registration method.

The smallest future structure reuses the existing Infrastructure scaffold without populating competing folders:

```text
ElsheiekhHMS.Infrastructure/
├── Persistence/
│   └── ElsheiekhHmsDbContext.cs
├── Configurations/Entities/
│   ├── DepartmentConfiguration.cs
│   ├── DoctorConfiguration.cs
│   ├── DoctorScheduleConfiguration.cs
│   ├── PatientConfiguration.cs
│   ├── AppointmentConfiguration.cs
│   └── WalkInQueueEntryConfiguration.cs
└── Migrations/
```

The existing empty `Data` scaffold should not become a second persistence root. No repositories or seed folders are needed for the first persistence batch.

## 6. DbContext design

Use `ElsheiekhHMS.Infrastructure.Persistence.ElsheiekhHmsDbContext : DbContext`. `ElsheiekhHmsDbContext` is specific to this HMS, avoids generic `ApplicationDbContext` naming, and leaves Phase 05 free to choose Identity integration deliberately.

The context will have `DbSet<Department> Departments`, `DbSet<Doctor> Doctors`, `DbSet<Patient> Patients`, `DbSet<Appointment> Appointments`, and `DbSet<WalkInQueueEntry> WalkInQueueEntries`. `DoctorSchedule` is mapped through the Doctor-owned relationship and does not need an application-facing root `DbSet`.

The constructor accepts `DbContextOptions<ElsheiekhHmsDbContext>`. `OnModelCreating` calls the base implementation and applies configurations from the Infrastructure assembly. The migration assembly is Infrastructure because the context and migrations live together. A design-time factory is justified only if the EF CLI cannot construct the context from the Web startup project.

## 7. NuGet package plan

### Required for the first implementation batch

| Package | Project | Purpose | Version strategy |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore` | Infrastructure | DbContext and Fluent API | Recommend 10.0.12 to align with existing Microsoft.Extensions references; verify availability before implementation |
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure | SQL Server provider and SQL Server-specific mappings | Exact same 10.0.12 version |
| `Microsoft.EntityFrameworkCore.Design` | Infrastructure, `PrivateAssets=all` | Design-time model and migration tooling | Exact same 10.0.12 version |

The test project does not need a provider in 04A. SQL Server integration tests will add only the provider/test packages approved for the chosen environment in 04E.

### Not yet required

`Microsoft.EntityFrameworkCore.Tools` is not required for `dotnet ef`; install it only if a Package Manager Console workflow is adopted. `Microsoft.EntityFrameworkCore.InMemory` is not a substitute for SQL Server verification and is deferred unless a later Application test specifically needs it. No Identity packages belong to Phase 04.

No package is installed during this review.

## 8. Entity configuration strategy

Use one Fluent configuration file per persistent type. The aggregate-owned DoctorSchedule is the exception in structure: `DoctorConfiguration` must configure the backing collection relationship, while `DoctorScheduleConfiguration` configures its table, key, scalar properties, and shadow FK. This satisfies the intent of dedicated configuration without adding a public navigation or independent aggregate API.

### DepartmentConfiguration

- Table `Departments`, schema `dbo`, integer identity `Id` primary key.
- `Name` required Unicode, max 200; `Description` optional Unicode, max 2000; `PhoneExtension` optional Unicode, max 32.
- `IsActive` required; audit fields mapped with UTC application values.
- Non-unique index on `Name` only if department search is introduced; no uniqueness is approved for names.

### DoctorConfiguration

- Table `Doctors`; integer identity key.
- `DoctorCode` required Unicode, max 32; do not add a unique index unless a later approved requirement defines DoctorCode as a stable unique business identifier.
- `FullName` required Unicode, max 200; `Specialization` optional Unicode, max 200.
- `IsGeneralPractitioner` required; `ConsultationFee` required `decimal(12,3)`; `Status` integer; `DepartmentId` required FK.
- Map inherited audit and soft-delete fields; add `!IsDeleted` query filter.
- Configure required Department relationship with Restrict delete and the private schedules backing field.

### DoctorScheduleConfiguration

- Table `DoctorSchedules`; integer identity `Id` primary key.
- Shadow `DoctorId` required FK; `DayOfWeek` and `IsActive` required; `StartTime`/`EndTime` as `time(0)` or `time(7)` consistently; `SlotDurationMinutes` required small integer/int.
- Map inherited audit fields; no soft-delete filter and no rowversion.
- Index `(DoctorId, DayOfWeek, StartTime)` for schedule lookup. Overlap remains an Application/domain query concern.

### PatientConfiguration

- Table `Patients`; integer identity key.
- `PatientCode` required Unicode max 13 with unique index.
- Names required/optional Unicode max 100; computed `FullName` and `ShortName` are ignored.
- `DateOfBirth` maps to SQL `date`; `Gender` and nullable `BloodGroup` map to `int`.
- National ID/passport optional Unicode max 100 with filtered unique indexes excluding nulls.
- Phone max 32 and Address max 500 required; City max 100; emergency contact name max 200, phone max 32, relationship max 100; insurance provider max 200.
- Map `RowVersion` as SQL Server `rowversion`; add `!IsDeleted` query filter.

### AppointmentConfiguration

- Table `Appointments`; integer identity key.
- `AppointmentCode` required Unicode max 64; do not add a unique index without an approved stable uniqueness rule and scope.
- Required PatientId, DoctorId, DepartmentId FKs; `ScheduledDate` SQL `date`; `ScheduledTime` SQL `time(0)` or `time(7)`; `Type` and `Status` integer.
- Notes max 2000; cancellation reason max 1000; nullable `CancelledAt` `datetimeoffset(7)`.
- Map audit/deletion fields and `RowVersion`; add `!IsDeleted` query filter.
- No duration, UTC slot, navigation collection, or automatic queue relationship.

### WalkInQueueEntryConfiguration

- Table `WalkInQueueEntries`; integer identity key.
- Required PatientId and DepartmentId; optional DoctorId.
- `QueueDate` SQL `date`; `SequenceNumber` small integer/int; `QueueNumber` required Unicode max 5; `Priority` and `Status` integer.
- `RegisteredAt`, `CalledAt`, `CompletedAt`, and audit timestamps map to `datetimeoffset(7)`.
- Notes max 2000; map `RowVersion`; add `!IsDeleted` query filter.
- Map QueueDate, SequenceNumber, and QueueNumber, but do not add a queue-ticket uniqueness constraint; Phase 03 deferred its scope and reset policy.

## 9. Table / column naming

Use plural PascalCase table names in `dbo`: `Departments`, `Doctors`, `DoctorSchedules`, `Patients`, `Appointments`, and `WalkInQueueEntries`. Use EF property names as PascalCase column names and conventional `{Principal}Id` FK names. Explicit table names stabilize the schema; unnecessary column renaming is avoided.

## 10. String length and Unicode strategy

All person names, descriptions, addresses, notes, identifiers, and free-text fields remain Unicode (`nvarchar`) to support Arabic and other hospital data. Every string receives a bounded meaning-based length as listed in the configuration section. Phone fields are bounded strings and indexed non-uniquely only where search requirements justify it. No field is silently mapped to `nvarchar(max)` and no arbitrary tiny limit is introduced.

## 11. Date and time mapping

`DateOnly` maps to SQL Server `date`; `TimeOnly` maps consistently to `time(0)` or `time(7)` after one precision decision. Appointment `ScheduledDate`/`ScheduledTime` and queue `QueueDate` remain local civil values. `DateTimeOffset` audit and queue event timestamps map to `datetimeoffset(7)` and are written as UTC by the caller. Core does not receive a clock or timezone service. SQL Server `rowversion` generates concurrency bytes; it is not a domain timestamp.

## 12. Enum storage strategy

Store all seven enums as integers. This matches the established roadmap and the current explicit numeric enum values, saves space, and avoids persisted display names. Existing numeric values must never be renumbered; adding values requires migrations and compatibility review. String storage is more readable but makes renames and localization costly and is not required by the current domain.

## 13. Relationship mapping

All relationships use shadow or conventional FK metadata without adding domain navigation collections:

| Principal | Dependent | FK | Required | Cardinality | Navigation strategy |
|---|---|---|---|---|---|
| Department | Doctor | `DepartmentId` | yes | 1 to many | no public navigation; Infrastructure relationship |
| Doctor | DoctorSchedule | shadow `DoctorId` | yes | 1 to many | private Doctor backing collection |
| Patient | Appointment | `PatientId` | yes | 1 to many | no domain navigation |
| Doctor | Appointment | `DoctorId` | yes | 1 to many | no domain navigation |
| Department | Appointment | `DepartmentId` | yes | 1 to many historical reference | no domain navigation |
| Patient | WalkInQueueEntry | `PatientId` | yes | 1 to many | no domain navigation |
| Doctor | WalkInQueueEntry | nullable `DoctorId` | no | 1 to many optional routing | no domain navigation |
| Department | WalkInQueueEntry | `DepartmentId` | yes | 1 to many historical reference | no domain navigation |

No navigation collection is added merely for EF convenience.

## 14. Delete behaviors

| Relationship | Delete behavior | Reason |
|---|---|---|
| Department → Doctor | Restrict | Preserve staff history and prevent master deletion from removing doctors |
| Doctor → DoctorSchedule | Restrict | Doctor is soft-deleted; physical deletion must not silently remove schedule history |
| Patient → Appointment | Restrict | Preserve appointment history |
| Doctor → Appointment | Restrict | Preserve provider history |
| Department → Appointment | Restrict | Preserve historical department ownership |
| Patient → WalkInQueueEntry | Restrict | Preserve queue history |
| Doctor → WalkInQueueEntry | Restrict | Preserve routing history |
| Department → WalkInQueueEntry | Restrict | Preserve historical department ownership |

No cascade or SetNull behavior is proposed. Soft deletion is an explicit state change, not a database cascade.

## 15. Soft-delete strategy

Apply `!IsDeleted` global query filters to Patient, Doctor, Appointment, and WalkInQueueEntry. A filter hides only records explicitly marked deleted; Cancelled, Completed, and NoShow statuses remain visible. Department and DoctorSchedule do not receive filters because they use active/retired lifecycle fields.

Administrative audit, recovery, and approved reporting paths must explicitly bypass filters. Filtered unique indexes must be designed independently of query filters so historical identifiers cannot be accidentally reused. No automatic soft-delete interceptor is proposed in the first persistence batch.

## 16. Auditing persistence

Phase 04 maps `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`, `DeletedAt`, and `DeletedBy`. It does not create the constitutional append-only AuditLog and does not invent a current-user abstraction before Phase 05.

Domain constructors and methods already receive audit timestamps and actors. Phase 04 must preserve those values rather than overwrite them with database defaults or an interceptor. Automatic user population is deferred until Identity and the Application write boundary exist. Automatic timestamp population is also deferred unless a later approved use case demonstrates a safe Infrastructure-owned write path.

## 17. Concurrency / RowVersion

| Entity | Concurrency | Collision scenario | EF mapping |
|---|---|---|---|
| Patient | required | simultaneous demographic/contact/identifier edits | `RowVersion` as generated SQL `rowversion`, concurrency token |
| Appointment | required | simultaneous status changes such as check-in/cancel | same rowversion mapping |
| WalkInQueueEntry | required | simultaneous call, hold, route, or cancel operations | same rowversion mapping |
| Department | none approved | lifecycle/details editing is not opted in | no token |
| Doctor | none approved | revisit only if concurrent profile/status edits become evidenced | no token |
| DoctorSchedule | none approved | schedule overlap and booking are cross-record concerns | no token |

Only types implementing `IHasConcurrencyToken` receive the mapping. Infrastructure catches provider concurrency exceptions; Core never references `DbUpdateConcurrencyException`.

## 18. Index strategy

Required or strongly justified non-unique indexes:

- FK indexes on all relationship columns.
- Patient `Phone` for search, non-unique only.
- Appointment `(DoctorId, ScheduledDate, ScheduledTime, Status)` and `(PatientId, ScheduledDate)` for later collision/history queries.
- Queue `(DepartmentId, QueueDate, Status, Priority, RegisteredAt)` for active-board ordering and `(QueueDate, RegisteredAt)` for daily history.
- DoctorSchedule `(DoctorId, DayOfWeek, StartTime)`.

Unique indexes:

- Patient `PatientCode`.
- Filtered nullable Patient `NationalId` and `PassportNumber` indexes, excluding nulls.
- No queue-ticket uniqueness index; ticket scope remains an Application/persistence workflow decision.
- No DoctorCode or AppointmentCode uniqueness index without a later explicit stable-business-rule approval.

No unique index is created on Phone, Department Name, or QueueNumber alone. No index is created for imagined encounter, billing, or reporting queries.

## 19. Uniqueness constraints

| Rule | Classification | Database action |
|---|---|---|
| PatientCode unique | intrinsic identifier / SpecKit required | unique index |
| NationalId unique when present | application and database integrity | filtered unique index |
| PassportNumber unique when present | application and database integrity | filtered unique index |
| Queue ticket uniqueness | scope deferred by Phase 03 | no database uniqueness in Phase 04 |
| AppointmentCode unique | no approved stable scope | no unique index |
| DoctorCode unique | no approved stable scope | no unique index |
| Phone may be shared | explicit domain decision | no unique index |
| Department name duplicate detection | application policy not approved | no unique constraint in 04A |

Application validation remains necessary; database conflicts must be translated safely rather than exposed as raw SQL errors.

## 20. Database constraints

Use required columns, bounded string lengths, required/optional FKs, unique indexes, rowversion generation, and a `SequenceNumber BETWEEN 1 AND 999` check if SQL Server migration review confirms it is portable and useful. Avoid brittle SQL checks for enum names, queue-ticket text, schedule overlap, appointment collision, current status transitions, or UTC offsets; those belong to Core/Application. Do not duplicate every domain rule in SQL.

## 21. Schema strategy

Use the default `dbo` schema for Phase 04. The current project has no established multi-schema convention, and organization/patient/scheduling schemas would add migration and operational complexity without a current requirement. A later bounded-context split can introduce schemas with an explicit migration plan.

## 22. Migration strategy

Keep migrations in `ElsheiekhHMS.Infrastructure/Migrations`. Use `ElsheiekhHmsDbContext` and a descriptive migration such as `InitialHospitalSchema`. The expected future command is:

```text
dotnet ef migrations add InitialHospitalSchema --context ElsheiekhHmsDbContext --project ElsheiekhHMS.Infrastructure --startup-project ElsheiekhHMS.Web
```

Use a design-time factory only if startup configuration cannot construct the context. Review the generated migration for table names, Unicode, precision, filtered indexes, rowversion, filters, and delete behavior before applying it. Apply migrations explicitly through deployment or a controlled development command; do not auto-migrate production on every Web startup. The legacy roadmap's auto-migrate snippet is therefore a proposed behavior requiring a separate decision, not an instruction for 04A.

## 23. Connection string and secrets strategy

Use the key `ConnectionStrings:ElsheiekhHmsDatabase`. Configuration belongs to Web/environment configuration and is passed into Infrastructure registration. Development values belong in User Secrets or environment variables; production values belong in a managed secret store or protected environment configuration. No credentials, passwords, or server details are committed to appsettings or Core. EF command logging must redact connection strings and patient data.

## 24. Local SQL Server strategy

SQL Server is the production provider. This machine has LocalDB tooling and an `MSSQLLocalDB` instance, currently stopped; no usable configured SQL Server instance has been established by this review. Do not install or assume LocalDB, Express, Docker, or another instance. Inspect the environment first; if no safe SQL Server test database is available when database execution is reached, stop and report options. Earlier package/context/mapping work may proceed without physical database execution.

## 25. Seeding strategy

No reference data is currently required by the approved Phase 03 model. Do not seed fake patients, doctors, appointments, queue entries, or identity users through migrations. Development fixtures belong in isolated tests or an explicitly approved development bootstrap later. Identity roles/users belong to Phase 05.

## 26. Persistence testing strategy

Keep existing domain unit tests provider-free. Add Phase 04 model tests for model creation, table/column metadata, required fields, enum conversion, filters, indexes, relationships, delete behavior, and rowversion metadata. Add SQL Server integration tests against a real isolated SQL Server database already available to the project/developer for migrations, filtered indexes, rowversion collisions, DateOnly/TimeOnly, query filters, and restrict-delete behavior. Do not silently downgrade to LocalDB, SQLite, or InMemory.

EF InMemory may be useful for later Application orchestration tests, but it cannot validate SQL Server-specific behavior and must not be the Phase 04 persistence gate. Tests must use isolated databases or transactions and must never rely on production data.

## 27. Transaction strategy

`ElsheiekhHmsDbContext` and `SaveChangesAsync` provide the initial unit-of-work boundary. Do not add `IUnitOfWork`, a generic transaction manager, or distributed transaction infrastructure in Phase 04. Explicit transactions become justified in later Application workflows for ticket allocation, booking collision prevention, or multi-record audit writes.

## 28. Repository decision

Do not create `IRepository<T>` or `GenericRepository<T>`. The context is an Infrastructure detail. Application-specific query and command abstractions should be introduced only when a real use case exists in Phase 06/07. This keeps Phase 04 focused on persistence mapping.

## 29. SaveChanges pipeline

No interceptor is required for 04A–04C. Preserve caller-supplied audit metadata and domain state. Do not add domain events, automatic soft-delete rewriting, or synthetic actor values. A later approved write boundary may add a narrowly scoped audit/timestamp component after Identity and Application contracts exist.

## 30. EF exception boundary

Infrastructure owns `DbUpdateConcurrencyException`, unique/FK/constraint `DbUpdateException`, and provider-specific SQL exceptions. A later Application boundary translates them into safe conflict/validation results. Raw EF exception types must not cross into Core or Web. Phase 04 design documents the boundary but does not implement translation.

## 31. Phase 05 Identity compatibility

Keep `ElsheiekhHmsDbContext` as a plain `DbContext` in Phase 04. Phase 05 must use the same physical SQL Server database unless a later security or operational requirement justifies separation. Phase 04 must not lock the choice between extending this context to `IdentityDbContext<ApplicationUser>` and using a separate Identity context. Keep this context and its migrations free of Identity types so either Phase 05 option remains viable.

## 32. Security and sensitive data considerations

Patient names, identifiers, contact fields, and clinical scheduling references are sensitive. Avoid duplicating patient demographics into appointments or queue entries. Keep patient values out of SQL/EF logs, exception messages, test output, and Graphify content where possible. Use least-privilege database credentials, protected connection strings, encrypted backups, controlled restore access, and later role-based authorization. These practices do not by themselves claim regulatory compliance.

## 33. Performance considerations

Bound strings, FK indexes, patient search indexes, appointment lookup indexes, and queue-board indexes are the only Phase 04 performance work justified now. Query filters must not cause accidental required-navigation elimination because the domain has no public navigations. Application queries should project DTOs, paginate lists, avoid unbounded Includes, and use `AsNoTracking` for read-only work. No cache, CQRS, replicas, sharding, partitioning, or event sourcing is proposed.

## 34. Proposed database diagram

```text
dbo.Departments
  Id PK | Name | IsActive | audit
       │
       ├──< dbo.Doctors
       │      Id PK | DepartmentId FK | DoctorCode
       │           │
       │           └──< dbo.DoctorSchedules
       │                 Id PK | DoctorId FK(shadow) | DayOfWeek | times
       │
       ├──< dbo.Appointments
       │      Id PK | PatientId FK | DoctorId FK | DepartmentId FK
       │           AppointmentCode | date | time | RowVersion
       │
       └──< dbo.WalkInQueueEntries
              Id PK | PatientId FK | DoctorId FK? | DepartmentId FK
              QueueDate | SequenceNumber | QueueNumber | RowVersion

dbo.Patients
  Id PK | PatientCode [UQ] | NationalId [UQ filtered]
  PassportNumber [UQ filtered] | Phone [non-unique] | RowVersion
       │
       ├──< Appointments
       └──< WalkInQueueEntries
```

All clinical/future tables are intentionally absent.

## 35. Proposed Infrastructure structure

```text
ElsheiekhHMS.Infrastructure/
├── Persistence/ElsheiekhHmsDbContext.cs
├── Configurations/Entities/
│   ├── DepartmentConfiguration.cs
│   ├── DoctorConfiguration.cs
│   ├── DoctorScheduleConfiguration.cs
│   ├── PatientConfiguration.cs
│   ├── AppointmentConfiguration.cs
│   └── WalkInQueueEntryConfiguration.cs
└── Migrations/
```

Future modifications: `InfrastructureServiceExtensions.cs`, the Infrastructure `.csproj`, and Web environment configuration. No files are created by this review except the design documents named at the end.

## 36. Proposed test structure

```text
ElsheiekhHMS.Tests/
└── Integration/Persistence/
    ├── ElsheiekhHmsDbContextModelTests.cs
    ├── ElsheiekhHmsDbContextSqlServerTests.cs
    └── SqlServerTestDatabaseFixture.cs
```

Unit tests remain in the existing Domain directories. The SQL Server fixture must use an isolated database on an already available SQL Server instance, apply migrations, clean up deterministically, and never depend on a shared production database. If no safe instance exists, this verification sub-batch stops and reports options.

## 37. Core impact

Expected Core impact: **NONE**. The current private setters, internal schedule constructor, read-only collection, `DateOnly`, `TimeOnly`, UTC `DateTimeOffset`, and opt-in `RowVersion` contract are all mappable through Infrastructure Fluent API. No public setters, EF attributes, public parameterless constructors, navigation collections, or database-specific types are required. If an implementation experiment appears to require a Core change, stop and request approval with the exact type, reason, alternative, and architectural impact.

## 38. Recommended Phase 04 sub-batches

### 04A — EF Core foundation

- Objective: add approved EF packages, `ElsheiekhHmsDbContext`, registration, and the `ElsheiekhHmsDatabase` configuration key.
- Files: Infrastructure `.csproj`, `Persistence/ElsheiekhHmsDbContext.cs`, `InfrastructureServiceExtensions.cs`, Web appsettings files only after configuration approval.
- Dependencies: completed Phase 03 and package/version approval.
- Verification: restore/build; context contract can be constructed without a database; full model finalization follows 04B mappings; Core dependency scan remains clean.
- Completion gate: context and registration review approved; no migration yet.

### 04B — Scalar mappings and aggregate-owned schedule

- Objective: add six dedicated configuration units, scalar lengths, Unicode, precision, temporal mappings, enum conversion, and Doctor schedules.
- Files: `Configurations/Entities/*.cs`.
- Dependencies: 04A.
- Verification: EF model metadata tests and build.
- Completion gate: every actual property has an intentional mapping; no future entity appears.

### 04C — Relationships, filters, indexes, constraints, and concurrency

- Objective: add restrict-delete relationships, soft-delete filters, approved uniqueness, lookup indexes, and opt-in rowversion without speculative queue/code uniqueness.
- Files: configuration files and model tests.
- Dependencies: 04B and the approved rule that no speculative DoctorCode, AppointmentCode, or queue-ticket uniqueness is added.
- Verification: relational model assertions; SQL Server integration tests for rowversion/filter/index/delete behavior where available.
- Completion gate: migration review shows no cascade history loss and no phone uniqueness.

### 04D — Migration and SQL Server schema verification

- Objective: generate, review, and apply the initial migration only when an already available safe SQL Server test database is confirmed.
- Files: `Migrations/*`, design-time factory only if required.
- Dependencies: 04C and confirmed existing SQL Server/test database availability.
- Verification: migration script review, database creation, schema inspection, rollback/recreate procedure.
- Completion gate: migration is reproducible and no production database is touched.

### 04E — Persistence integration tests and handoff

- Objective: verify SQL Server behavior and document the Application exception boundary and operational workflow.
- Files: `Tests/Integration/Persistence/*`, test project package references, phase documentation.
- Dependencies: 04D.
- Verification: isolated SQL Server tests for mappings, filters, constraints, rowversion, DateOnly/TimeOnly, and restrict deletes; full restore/build/test; Graphify refresh.
- Completion gate: all tests pass, architecture remains clean, and Phase 04 implementation report is approved before Phase 05.

## 39. Section 39 resolution matrix

| Open question | Decision | Resolved |
|---|---|---|
| LocalDB versus dedicated SQL Server test environment | Inspect existing availability first; do not install or assume infrastructure. Stop at physical database verification if no safe SQL Server exists. | YES |
| Context name | `ElsheiekhHmsDbContext` in `ElsheiekhHMS.Infrastructure.Persistence`. | YES |
| Enum storage and numeric stability | Persist enums as integers; do not reorder or renumber existing members after migration. | YES |
| Schema strategy | Use `dbo`; no domain-mirroring schemas. | YES |
| DoctorCode and AppointmentCode uniqueness | No uniqueness without an explicitly approved stable business rule and scope. | YES |
| Queue ticket uniqueness/reset scope | Do not invent a database-wide unique constraint; defer scope to Application/persistence workflow design. | YES |
| Audit population | Map existing audit columns; do not fake authenticated identity before Phase 05. | YES |
| InMemory versus SQL Server integration tests | InMemory and SQLite are not authoritative; SQL Server-specific tests require a real isolated SQL Server database. | YES |
| Phase 05 Identity context | Same physical database is the compatibility direction; separate versus integrated context remains a Phase 05 design decision. | YES |
| Automatic versus controlled migration application | Keep migrations in Infrastructure and apply through a controlled approved workflow; do not auto-migrate production startup. | YES |

## 40. Risks

- The legacy roadmap contains contradictory .NET 9, MVC, entity, and Phase 04-complete claims. Implementers must use actual source and current approved specifications.
- No usable SQL Server test database is currently established; physical database verification must stop and report options if none is available at 04D/04E.
- Filtered indexes and rowversion require SQL Server tests; provider-free or InMemory tests can give false confidence.
- Required historical FKs with Restrict delete mean administrative correction workflows must use explicit soft-delete or archival policy.
- Changing enum numeric values after data exists would corrupt meaning; migrations must preserve explicit values.
- Future Identity integration can affect migrations if Phase 05 changes the context base; the Phase 04 context remains deliberately Identity-neutral.

## 41. Proposed implementation plan

The controlled implementation plan is saved at `docs/superpowers/plans/2026-09-21-phase04-ef-core-database.md`. Human approval has authorized 04A–04C; 04D–04E remain separately gated. The plan follows 04A–04E and uses testable gates.

## 42. Files created or planned by the staged implementation

- **04A complete:** `ElsheiekhHMS.Infrastructure/Persistence/ElsheiekhHmsDbContext.cs`
- **04A complete:** `ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextTests.cs`
- **04B complete:** `ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs` (private EF-only constructor)
- **04B complete:** `ElsheiekhHMS.Infrastructure/Configurations/Entities/*Configuration.cs`
- **04C complete:** relationship, filter, index, uniqueness, and rowversion metadata in the six Infrastructure configuration units plus focused model metadata tests.
- **04B complete:** `ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextModelTests.cs`
- **04B planned:**
- `ElsheiekhHMS.Infrastructure/Configurations/Entities/DepartmentConfiguration.cs`
- `ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorConfiguration.cs`
- `ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorScheduleConfiguration.cs`
- `ElsheiekhHMS.Infrastructure/Configurations/Entities/PatientConfiguration.cs`
- `ElsheiekhHMS.Infrastructure/Configurations/Entities/AppointmentConfiguration.cs`
- `ElsheiekhHMS.Infrastructure/Configurations/Entities/WalkInQueueEntryConfiguration.cs`
- `ElsheiekhHMS.Infrastructure/Migrations/*` after migration approval
- **04E planned:** `ElsheiekhHMS.Tests/Integration/Persistence/*`
- A design-time context factory only if the first migration command requires one.

## 43. Files modified or planned by the staged implementation

- **04A complete:** `ElsheiekhHMS.Infrastructure/ElsheiekhHMS.Infrastructure.csproj`; `ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs`
- **04B complete:** architecture and decision documentation updated for the approved Patient materialization accommodation
- `ElsheiekhHMS.Web/appsettings.json`
- `ElsheiekhHMS.Web/appsettings.Development.json` with a non-secret development configuration strategy
- `ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj` only when approved integration-test packages are selected

No Core entity or enum file should be modified beyond the separately approved private Patient materialization constructor.

## 44. Superpowers documents created

- `docs/superpowers/specs/2026-09-21-phase04-ef-core-database-design.md`
- `docs/superpowers/plans/2026-09-21-phase04-ef-core-database.md`

The specification and plan preceded implementation. Approved 04A adds the three Infrastructure EF packages, `ElsheiekhHmsDbContext`, SQL Server DI registration, and the context-contract test. Approved 04B adds six scalar configuration units, metadata tests, and the private EF-only Patient materialization constructor with backing-field mapping. Approved 04C adds explicit historical-safe relationships, approved indexes/uniqueness, soft-delete filters, rowversion metadata, and negative model assertions. No migration, connection string value, database, repository, or Identity implementation was created.

## 45. Git status and design review decision

The Phase 04 design remains the governing staged review artifact. 04A–04C are implemented and verified under their approved gates; 04D–04E remain unstarted and require their own review and environment checks. No project, source, package, configuration, migration, database, or generated graph file outside the approved 04A–04C scope is implied.
