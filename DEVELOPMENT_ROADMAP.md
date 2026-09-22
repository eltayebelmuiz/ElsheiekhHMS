# ElsheiekhHMS — Master Development Roadmap

**Version:** 1.0.0
**Date:** September 20, 2026
**Owner:** Eltayeb Elmuiz
**Status:** Phase 08A Staff Patient Intake & Appointment Scheduling complete; Phase 08 in progress; 07E Doctor/Provider Application Service and 08B arrival/queue handoff remain deferred

---

## 1. Document Purpose

This is the authoritative 12-phase implementation roadmap for the Elshiekh Hospital Management System (ElsheiekhHMS). It consolidates every architectural decision, implementation rule, correction, and plan established across the full development history of this project.

### How to use this document

**Developers:** Read the current phase section before writing any code. Follow the Definition of Done before moving to the next phase. Do not implement features belonging to future phases.

**Codex and AI agents:** Read Section 14 first. Determine the current phase from Section 16. Read only the applicable phase section. Do not implement features from future phases. Build and test after every meaningful change.

**New team members:** Read Sections 2–6 first for context, then jump to the current phase.

**This document supersedes all earlier partial specifications.** Where any other file in the repository contradicts this roadmap, this roadmap reflects the final decision.

---

## 2. Project Vision

ElsheiekhHMS is a web-based, enterprise-grade Hospital Management System designed to digitize the complete patient journey at EL-Shiekh Medical Complex — and by architectural intent, at any hospital running a Windows Server LAN environment.

### Planned business capabilities

| Module | Description | Phase |
|--------|-------------|-------|
| Patient Management | Registration, search, demographics, vitals, soft-delete | 01/12 |
| Walk-in Queue | Daily ticket management, real-time staff board, public display | 01/12 |
| Staff Management | Doctors, nurses, receptionists, lab techs, pharmacists | 03/12 |
| Departments & Specialties | Organizational units, head doctors, activation | 03/12 |
| Appointment Management | Booking, confirmation, check-in, cancellation, conflict prevention | 04/12 |
| Clinical / EMR | Diagnosis, clinical notes, treatment plans, follow-up, finalization | 07/12 |
| Laboratory | Test ordering, sample workflow, result entry, abnormal flagging | 07/12 |
| Pharmacy | Prescription dispensing, stock management, batch/expiry tracking | 08/12 |
| Admissions & Wards | Ward/room/bed management, admission, transfer, discharge | 08/12 |
| Billing & Payments | Invoices, line items, partial payments, receipts | 08/12 |
| Notifications | In-app bell, unread count, event-driven alerts | 09/12 |
| Audit Trail | Full append-only activity log, before/after snapshots | 01/12 |
| Users & Roles | Five canonical roles, ASP.NET Identity, policy enforcement | 05/12 |
| System Settings | Hospital name, code prefixes, session config | 09/12 |
| Dashboard | Real-time KPIs per role, SignalR updates | 12/12 |
| Reporting | Revenue, workload, lab activity, bed occupancy | Future |
| Insurance | Claims, provider management | Future |
| Patient Portal | Self-service web portal | Future (v2) |
| Arabic UI / RTL | Right-to-left layout, Arabic language | Future (v2) |
| Mobile App | iOS / Android | Future (v2) |
| National Health Integration | Sudan national health system | Future (v2) |

---

## 3. Technology Stack

### Currently installed and operational

| Technology | Purpose | Project |
|-----------|---------|---------|
| .NET 10 / C# | Runtime and language | All |
| ASP.NET Core / Blazor Interactive Server | Web framework and current UI layer | Web |
| Entity Framework Core 10 | ORM, Code-First migrations | Infrastructure |
| SQL Server / LocalDB | Database | Infrastructure |
| ASP.NET Core Identity | Authentication, roles, password hashing | Infrastructure |
| Tabler Icons CDN | Icon system | Web |
| Custom CSS system | Layout, components, tables, dark mode | Web (wwwroot) |
| xUnit | Unit and integration testing | Tests |
| FluentAssertions | Readable test assertions | Tests |
| Moq | Interface mocking | Tests |

### Approved stack — planned for later phases

| Technology | Purpose | Earliest Phase |
|-----------|---------|---------------|
| Blazor Interactive Server | Future HMS UI expansion after backend readiness | Phase 12 |
| EF Core InMemory | Integration test database | Phase 10 |
| ILogger<T> structured logging | Observability | Phase 09 |
| Health checks | Infrastructure monitoring | Phase 09 |
| Background services | Notifications, cleanup jobs | Phase 09 |

### Explicitly rejected technologies

The following were considered and rejected. Do not introduce them:

- CQRS / MediatR — unnecessary abstraction for this scale
- Redis — no caching requirement at v1 scale
- Docker / Kubernetes — hospital LAN deployment, not cloud
- Event sourcing — overkill for HMS domain
- Message brokers (RabbitMQ, Azure Service Bus) — no async messaging required
- gRPC — standard HTTP/Blazor patterns are sufficient
- GraphQL — not needed
- Microservices — monolith is correct for this domain and team size

---

## 4. Solution Architecture

### Projects

```
ElsheiekhHMS.slnx
├── ElsheiekhHMS.Core/           ← Domain layer. ZERO project dependencies.
├── ElsheiekhHMS.Application/    ← Use-case contracts and security vocabulary. References Core.
├── ElsheiekhHMS.Infrastructure/ ← EF Core, SQL Server, Identity. References Application + Core.
├── ElsheiekhHMS.Web/            ← Blazor host and composition root. References Application + Infrastructure.
└── ElsheiekhHMS.Tests/          ← Tests. References Core + Application + Infrastructure.

The current checkout uses the `ElsheiekhHMS` namespace and project prefix.
```

### Project responsibilities

| Project | Responsibility | May Reference |
|---------|---------------|---------------|
| **Core** | Domain entities, enums, interfaces, exceptions, ServiceResult | Nothing |
| **Infrastructure** | DbContext, EF configs, Identity, role seeder, repositories, services | Application + Core |
| **Web** | Blazor components, authorization composition, middleware, wwwroot | Application + Infrastructure |
| **Tests** | Domain, application, infrastructure, and persistence tests | Core + Application + Infrastructure |

### Dependency direction (enforced, never violated)

```
Web ──► Application + Infrastructure
Infrastructure ──► Application + Core
Application ──► Core
Tests ──► Core + Application + Infrastructure

Core ──► (nothing — zero external references)
```

### Current folder structure (Core)

```
Core/
├── Enums/
│   └── Enums.cs          ← All domain enums (Gender, BloodGroup, PatientStatus, QueueStatus, etc.)
├── Exceptions/            ← Domain exceptions (planned Phase 02)
├── Interfaces/
│   ├── Repositories/     ← IRepository<T>, IUnitOfWork, IPatientRepository, IDoctorRepository, etc.
│   └── Services/         ← IPatientService, IDoctorService, ServiceResult<T>, PagedResult<T>
└── Models/
    └── DomainModels.cs   ← BaseEntity, ApplicationUser, Patient, Doctor, Appointment, etc.
```

### Blazor migration — planned solution (phase01-setup.ps1)

When the Blazor migration is initiated, the solution will be restructured as five projects:

```
ElsheiekhHMS.sln
├── ElsheiekhHMS.Core/
├── ElsheiekhHMS.Application/     ← New: DTOs, service interfaces, validators, use-case services
├── ElsheiekhHMS.Infrastructure/
├── ElsheiekhHMS.Web/              ← Blazor Interactive Server
└── ElsheiekhHMS.Tests/
```

The Application layer separates DTOs and service contracts from the Core domain, preventing domain entities from leaking into the UI layer.

---

## 5. Global Architecture Rules

These rules apply across all phases. Violations are architectural defects, not style preferences.

### Core independence rules
- Core has **zero** NuGet package dependencies — no EF Core, no Identity, no HTTP, no logging implementations
- No `using Microsoft.EntityFrameworkCore` inside any Core file
- No `DbContext`, `DbSet`, or `[Key]` EF attributes in Core entities (configuration belongs in Infrastructure)
- No DTOs in Core — DTOs belong in Application and presentation models belong in Web

### Service and business logic rules
- All business rules live in Application services — never in Blazor components or transport endpoints
- Controllers and Blazor components call services and handle results — they do not orchestrate business logic
- Authorization is verified **server-side** on every action — hiding a button in the UI is never sufficient
- Every write operation that changes domain state must produce an AuditLog entry within the same database transaction

### Data integrity rules
- **Soft-delete only** on all clinical, financial, and patient records — no hard-delete endpoints exist
- `IsDeleted = true` flag, never `DELETE FROM` for Patient, MedicalRecord, LabTest, Invoice, AuditLog
- All monetary values declared as `decimal` — `float` and `double` are forbidden in financial entities
- All timestamps stored in UTC — converted to local time at the display layer only
- Optimistic concurrency via `RowVersion` on Patient, Invoice, and Appointment entities
- Duplicate detection required: PatientCode (unique), NationalId (unique filtered), PassportNumber (unique filtered), InvoiceNumber (unique)

### Data access rules
- All database operations are async — no synchronous EF Core calls (`Find()`, `SaveChanges()`)
- `CancellationToken` passed through all async service and repository method signatures
- `SaveChangesAsync()` is called only via `IUnitOfWork` — never inside a repository method
- Never return `IEnumerable<T>` from a database query — materialize with `ToListAsync()` or `FirstOrDefaultAsync()`
- N+1 queries prohibited — use `Include()` and projection at the repository layer

### Security rules
- Passwords stored as PBKDF2 via ASP.NET Identity — never logged, never serialized, never in audit values
- Account lockout: 5 failed attempts → 15-minute lockout
- Session cookie: `HttpOnly=true`, `SameSite=Lax`, `Secure=true` in production
- CSRF anti-forgery tokens on all state-changing operations
- HTTPS enforced in production

### Code quality rules
- No magic strings — use constants, enums, and strongly-typed keys
- No `null!` suppression without a documented justification
- Nullable reference types enabled across all projects
- XML summary comments on all public service method signatures

---

## 6. Development Philosophy

### Backend-first strategy

The backend is built and verified before substantial UI work begins. This ensures business rules are testable, auditable, and UI-independent.

```
PHASE 01–11: Backend
──────────────────────────────────────────────────
Blazor Component
        ↓
Application Service (called via DI)
        ↓
Validation → Authorization → Business Rules
        ↓
Repository / EF Core
        ↓
SQL Server
        ↓
ServiceResult<T> returned to component

PHASE 12: Blazor UI
──────────────────────────────────────────────────
Components remain presentation-only.
They call services and handle ServiceResult.
They do NOT contain business logic.
```

### Why UI development is intentionally late

If business rules exist only in Razor components or transport endpoints, they cannot be tested independently, they cannot be reused by future clients (mobile app, API, background jobs), and they are invisible to the audit trail. By building services first, the Blazor UI becomes a thin presentation layer over a fully tested backend.

---

## 7. Master Phase Table

| Phase | Name | Purpose | Status | Main Deliverable |
|-------|------|---------|--------|-----------------|
| 01 | Solution & Architecture | Project structure, DI, health checks | ✅ Complete | Compiling five-project solution |
| 02 | Core Foundation | BaseEntity, enums, exceptions, ServiceResult | ✅ Complete | Core compiles with zero dependencies |
| 03 | Domain Entities | All HMS domain models | ✅ Complete | DomainModels.cs, IRepositories.cs |
| 04 | EF Core & Database | AppDbContext, Fluent config, migrations | ✅ Complete | Database created and migrated |
| 05 | Identity & Security | ApplicationUser, roles, policies, entity auditing, AuditLog foundation, migration, authentication hardening | ✅ Complete (05A–05E) | Identity/security foundations |
| 06 | DTOs & Validation | DTOs, input validation, shared pagination | ✅ Complete (06A–06D) | Approved validation contracts |
| 07 | Application Services | Application use cases and orchestration | ⏳ Not started | Tested application contracts |
| 08 | Business Workflows | Queue, appointments, EMR, billing | ⏳ Not started | Approved workflow services |
| 09 | Enterprise Infrastructure | Remaining audit events, notifications, settings, integrations | ⏳ Not started | Approved cross-cutting services |
| 10 | Testing & Hardening | Unit, integration, security, performance | ⏳ Not started | Hardening evidence |
| 11 | Backend Review | Full backend gate before UI | ⏳ Not started | Architecture audit passing |
| 12 | Blazor UI | HMS presentation and workflows | ⏳ Not started | Approved Blazor UI |

---

## PHASE 01 — Solution & Architecture

### Objective
Establish the multi-project solution, enforce dependency direction, configure DI boundaries, create the initial folder structure, and verify the solution builds.

### Why this phase exists
Architecture decisions made here are the most expensive to reverse. Getting dependency direction, project responsibilities, and DI registration strategy right before writing domain code prevents cascading structural problems in later phases.

### Prerequisites
None — this is the starting point.

### Scope
- Create solution file
- Create Core, Infrastructure, Web, Tests projects
- Wire project references in the correct direction
- Create `AddApplication()` / `AddInfrastructure()` DI extension methods
- Stub `Program.cs` with correct middleware order
- Create initial folder structure for all modules
- Configure `appsettings.json` and `appsettings.Development.json`
- Add `.gitignore`
- Health check endpoint at `GET /health`
- Verify build passes

### Out of scope
- No domain entities yet
- No EF Core DbContext
- No Identity configuration
- No actual service implementations
- No database connection

### Architecture decisions
- **Five projects** are established for the current .NET 10 Blazor-hosted architecture: Core, Application, Infrastructure, Web, Tests
- `Program.cs` calls `AddApplication()` and `AddInfrastructure()` — not hundreds of inline registrations
- Health check endpoint present from day one

### Projects affected
All

### Legacy folder structure (historical MVC reference)

```
ELShiekhMedicalComplex/
├── ELShiekhMedicalComplex.Core/
│   ├── Enums/
│   ├── Exceptions/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   └── Services/
│   └── Models/
├── ELShiekhMedicalComplex.Infrastructure/
│   ├── Data/
│   ├── Repositories/
│   ├── Services/
│   └── Migrations/
├── ELShiekhMedicalComplex.Web/
│   ├── Controllers/
│   ├── ViewModels/
│   ├── Views/
│   ├── Middleware/
│   └── wwwroot/
│       ├── css/
│       └── js/
└── ELShiekhMedicalComplex.Tests/
    ├── Unit/
    └── Integration/
```

### Verification commands

```bash
dotnet restore
dotnet build --no-restore
# Verify: zero errors, zero warnings about project references
dotnet run --project ELShiekhMedicalComplex.Web
# Verify: application starts, /health returns 200
```

### Definition of done

- [ ] Solution builds with zero errors
- [ ] All project references follow the approved dependency direction
- [ ] `AddApplication()` / `AddInfrastructure()` stubs exist
- [ ] `GET /health` returns HTTP 200
- [ ] `.gitignore` excludes bin/, obj/, secrets
- [ ] `appsettings.json` contains `HospitalSettings` section with name, code prefixes
- [ ] No EF Core, Identity, or HTTP packages in Core

### Deliverables
- `ELShiekhMedicalComplex.sln`
- Three project files with correct references
- Folder structure
- `Program.cs` stub
- `appsettings.json`, `appsettings.Development.json`

### Git checkpoint
```
git commit -m "Phase01: solution architecture, project references, DI stubs, health check"
```

### Next phase gate
Solution must build cleanly before proceeding to Phase 02.

---

## PHASE 02 — Core Foundation

### Objective
Establish the domain foundation in Core: base entity classes, all domain enums, domain exceptions, and the `ServiceResult<T>` wrapper. No EF Core. No external dependencies.

### Why this phase exists
Every subsequent entity and service depends on these building blocks. Establishing them correctly before entity design prevents retroactive changes cascading through the codebase.

### Prerequisites
Phase 01 complete and building.

### Scope
- `BaseEntity` — `Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`
- All domain enums in `Core/Enums/Enums.cs`
- Domain exceptions: `DomainException`, `BusinessRuleException`, `DomainValidationException`
- `ServiceResult<T>` and `ServiceResult` with `Success()`, `Failure()`, `ValidationFailure()`
- `PagedResult<T>` for paginated list results

### Actual enums established

```csharp
Gender           // Male, Female, Other
BloodGroup       // APositive → ONegative (8 values)
UserRole         // Admin, Doctor, Nurse, Receptionist, LabTechnician, Patient
PatientStatus    // AtReception, AtNurse, AtDoctor, Completed, Cancelled
AppointmentStatus// Scheduled, Confirmed, InProgress, Completed, Cancelled, NoShow
AppointmentType  // General, Specialist, Emergency, FollowUp, LabTest, Radiology
DoctorStatus     // Active, OnLeave, Inactive
LabTestStatus    // Ordered, SampleCollected, Processing, Completed, Cancelled
LabTestCategory  // Hematology, Biochemistry, Microbiology, Immunology, Radiology, Pathology, Endocrinology
InvoiceStatus    // Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled
PaymentMethod    // Cash, CreditCard, DebitCard, Insurance, BankTransfer, MobileMoney
QueueStatus      // Waiting, AtNurse, AtDoctor, Completed, Cancelled, OnHold
Priority         // Low, Medium, High, Critical
Shift            // Morning, Afternoon, Night
```

### Actual ServiceResult shape

```csharp
// In Core/Interfaces/Services/IServices.cs
public class ServiceResult<T>
{
    public bool    IsSuccess     { get; }
    public T?      Data          { get; }
    public string? ErrorMessage  { get; }
    public string? ErrorCode     { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    public static ServiceResult<T> Success(T data)
    public static ServiceResult<T> Failure(string message, string? code = null)
    public static ServiceResult<T> ValidationFailure(IDictionary<string, string[]> errors)
}

public class ServiceResult : ServiceResult<bool>
{
    public static ServiceResult Success()
    public static new ServiceResult Failure(string message, string? code = null)
}
```

**Critical naming rule:** Method names are `Success()` and `Failure()` — NOT `Ok()` and `Fail()`. This caused compiler errors in WalkInQueueService and was corrected. Always use `IsSuccess`, `ErrorMessage` — not `Succeeded`, `Error`.

### Out of scope
- No EF Core annotations on entities (those go in Infrastructure configurations)
- No navigation property configuration
- No database seeding
- No Identity setup

### Projects affected
Core only

### Definition of done

- [ ] `BaseEntity` with all audit fields in `Core/Models/`
- [ ] All enums in `Core/Enums/Enums.cs`
- [ ] `ServiceResult<T>` and `ServiceResult` with correct method names
- [ ] `PagedResult<T>` with `TotalPages`, `HasPreviousPage`, `HasNextPage`
- [ ] Domain exceptions defined
- [ ] Core builds with zero external NuGet packages
- [ ] Core references no Infrastructure or Web project

### Git checkpoint
```
git commit -m "Phase02: BaseEntity, enums, exceptions, ServiceResult, PagedResult"
```

---

## PHASE 03 — Domain Entities

### Objective
Design and implement all HMS domain entities in Core. Entities are plain C# classes with navigation properties and computed properties — no EF Core configuration.

### Why this phase exists
The domain model is the heart of the system. Getting entity relationships and lifecycle correct before writing persistence configuration prevents migration hell later.

### Prerequisites
Phase 02 complete.

### Actual entities implemented

**`Core/Models/DomainModels.cs`**

| Entity | Key Fields | Inherits | Soft-Delete |
|--------|-----------|----------|-------------|
| `ApplicationUser` | FirstName, LastName, FullName, Gender, Role, IsActive | IdentityUser | No |
| `Patient` | PatientCode, 4-part name, NationalId, Phone, Gender, BloodGroup, Status, Vitals, Insurance | BaseEntity | Yes |
| `Doctor` | DoctorCode, Specialization, IsGP, ConsultationFee, Status, DepartmentId, ApplicationUserId | BaseEntity | Yes |
| `DoctorSchedule` | DoctorId, DayOfWeek, StartTime, EndTime, SlotDurationMinutes | BaseEntity | No |
| `Department` | Name, Description, HeadDoctorId, PhoneExtension | BaseEntity | Yes |
| `Appointment` | AppointmentCode, PatientId, DoctorId, Date, Time, Type, Status, Priority | BaseEntity | Yes |
| `MedicalRecord` | PatientId, DoctorId, AppointmentId, Diagnosis, ICD10Code, ClinicalNotes, TreatmentPlan | BaseEntity | Yes |
| `LabTest` | TestCode, TestName, Category, PatientId, DoctorId, Status, Results, IsAbnormal, Fee | BaseEntity | Yes |
| `WalkInQueue` | QueueNumber, SequenceNumber, Prefix, PatientId, DoctorId, WalkInName, Status, QueueDate | BaseEntity | Yes |
| `Invoice` | InvoiceNumber, PatientId, Status, TotalAmount, PaidAmount, Discount, OutstandingAmount | BaseEntity | Yes |
| `InvoiceItem` | InvoiceId, Description, Quantity, UnitPrice, Total | — | No |
| `AuditLog` | UserId, UserName, Action, EntityName, EntityId, OldValues, NewValues, Timestamp, IPAddress | — | Never |
| `Notification` | UserId, Title, Message, Url, Icon, Type, IsRead | — | No |

### Patient — special design notes

```csharp
// 4-part name (Sudanese naming convention)
public string FirstName   { get; set; }
public string MiddleName  { get; set; }
public string ThirdName   { get; set; }
public string LastName    { get; set; }

// Computed — not mapped to DB column
[NotMapped]
public string FullName  => $"{FirstName} {MiddleName} {ThirdName} {LastName}".Trim();
[NotMapped]
public string ShortName => $"{FirstName} {LastName}".Trim();

// Computed financial
[NotMapped]
public decimal OutstandingAmount => TotalAmount - Discount - PaidAmount; // on Invoice
```

### Doctor — Identity link pattern

```csharp
public class Doctor : BaseEntity
{
    public string ApplicationUserId { get; set; } = null!;  // FK to IdentityUser
    public ApplicationUser? ApplicationUser { get; set; }   // navigation

    // Doctor's name is accessed via:
    // doctor.ApplicationUser.FirstName
    // doctor.ApplicationUser.LastName
    // NOT doctor.FirstName (does not exist on Doctor entity)
}
```

**This caused compiler errors in WalkInQueueService.** Always access doctor names through `doctor.ApplicationUser.FirstName`, never directly.

### WalkInQueue — queue number format

```
Format:  A-001
Prefix:  A (General), B (Specialist), etc.
Sequence: 3-digit zero-padded, resets daily via QueueDate field
Generated server-side in WalkInQueueService only — never in UI
```

### AuditLog — immutability rule

`AuditLog` is the one entity that must never be soft-deleted or hard-deleted. It is append-only. No update or delete endpoint exists for it. `IsDeleted` field does not apply.

### Out of scope
- No EF Core `[Key]`, `[Required]`, `[MaxLength]` attributes for configuration (those go in Infrastructure Fluent API)
- No DbContext
- No migration

**Note:** The current codebase does use some Data Annotations on entities (`[Required]`, `[MaxLength]`) for model validation purposes — these remain compatible with presentation binding, but authoritative database constraints must come from Fluent API in Infrastructure.

### Definition of done

- [ ] All entities in `Core/Models/DomainModels.cs`
- [ ] All navigation properties initialized: `ICollection<T> = new List<T>()`
- [ ] Computed properties marked `[NotMapped]`
- [ ] Doctor name accessed via `ApplicationUser` navigation — not directly
- [ ] `AuditLog` has no soft-delete fields
- [ ] Core builds with zero errors

### Git checkpoint
```
git commit -m "Phase03: all domain entities, navigation properties, computed properties"
```

---

## PHASE 04 — EF Core & Database

### Objective
Configure EF Core persistence for all domain entities. Create `AppDbContext`, entity configurations, indexes, constraints, global soft-delete filters, and generate the initial migration.

### Prerequisites
Phase 03 complete.

### Scope

#### AppDbContext

```csharp
// Infrastructure/Data/AppDbContext.cs
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Patient>        Patients        { get; set; }
    public DbSet<Department>     Departments     { get; set; }
    public DbSet<Doctor>         Doctors         { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<Appointment>    Appointments    { get; set; }
    public DbSet<MedicalRecord>  MedicalRecords  { get; set; }
    public DbSet<LabTest>        LabTests        { get; set; }
    public DbSet<WalkInQueue>    WalkInQueues    { get; set; }
    public DbSet<Invoice>        Invoices        { get; set; }
    public DbSet<InvoiceItem>    InvoiceItems    { get; set; }
    public DbSet<AuditLog>       AuditLogs       { get; set; }
    public DbSet<Notification>   Notifications   { get; set; }
}
```

#### Global soft-delete query filters (applied in OnModelCreating)

```csharp
builder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
builder.Entity<Doctor>().HasQueryFilter(e => !e.IsDeleted);
builder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
builder.Entity<Appointment>().HasQueryFilter(e => !e.IsDeleted);
builder.Entity<LabTest>().HasQueryFilter(e => !e.IsDeleted);
builder.Entity<WalkInQueue>().HasQueryFilter(e => !e.IsDeleted);
builder.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted);
// AuditLog — NO filter (append-only, always visible to Admin)
```

#### Critical entity configurations

```csharp
// Patient
e.HasIndex(p => p.PatientCode).IsUnique();
e.HasIndex(p => p.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL");
e.HasIndex(p => p.PassportNumber).IsUnique().HasFilter("[PassportNumber] IS NOT NULL");
e.Property(p => p.Weight).HasPrecision(5,2);
e.Property(p => p.Height).HasPrecision(5,2);
e.Property(p => p.Temperature).HasPrecision(4,1);

// Doctor → ApplicationUser (one-to-one)
e.HasOne(d => d.ApplicationUser)
 .WithOne(u => u.DoctorProfile)
 .HasForeignKey<Doctor>(d => d.ApplicationUserId)
 .OnDelete(DeleteBehavior.Restrict);

// WalkInQueue — AppDbContext.cs correction applied
// REMOVE: e.HasOne(q => q.Department) — WalkInQueue has NO Department navigation
// REMOVE: e.Ignore(q => q.WaitTime) — WaitTime is DTO-only, not on entity
e.HasIndex(q => new { q.DoctorId, q.QueueDate, q.QueueNumber });
e.HasOne(q => q.Patient).WithMany(p => p.QueueEntries).HasForeignKey(q => q.PatientId).OnDelete(DeleteBehavior.Restrict);
e.HasOne(q => q.Doctor).WithMany(d => d.QueuePatients).HasForeignKey(q => q.DoctorId).OnDelete(DeleteBehavior.Restrict);

// Invoice
e.HasIndex(i => i.InvoiceNumber).IsUnique();
e.Property(i => i.TotalAmount).HasPrecision(12,3);
e.Property(i => i.PaidAmount).HasPrecision(12,3);
e.Property(i => i.Discount).HasPrecision(12,3);
e.Ignore(i => i.OutstandingAmount); // computed property

// AuditLog
e.HasIndex(a => a.Timestamp);
e.HasIndex(a => new { a.EntityName, a.EntityId });
```

#### PassportNumber unique index — seed data correction

Empty string `''` violates the unique index when multiple patients have no passport number. **Always insert `NULL` for missing PassportNumber**, never empty string.

```sql
-- WRONG: causes IX_Patients_PassportNumber violation
INSERT ... VALUES (..., '', ...)

-- CORRECT
INSERT ... VALUES (..., NULL, ...)
```

#### Migration commands

```bash
# Create migration
dotnet ef migrations add InitialCreate \
  --project ELShiekhMedicalComplex.Infrastructure \
  --startup-project ELShiekhMedicalComplex.Web \
  --output-dir Migrations

# Apply migration
dotnet ef database update \
  --project ELShiekhMedicalComplex.Infrastructure \
  --startup-project ELShiekhMedicalComplex.Web

# Remove last unapplied migration
dotnet ef migrations remove \
  --project ELShiekhMedicalComplex.Infrastructure \
  --startup-project ELShiekhMedicalComplex.Web

# Generate SQL script for production review
dotnet ef migrations script \
  --project ELShiekhMedicalComplex.Infrastructure \
  --startup-project ELShiekhMedicalComplex.Web \
  --output migration.sql
```

#### Seed data rules

- Seed data inserted via SQL scripts (`seed_patients.sql`), not via `HasData()` or DbSeeder
- Enum values stored as **integers** (EF Core default) — NOT as strings
- Gender: Male=0, Female=1, Other=2
- BloodGroup: APositive=0, ANegative=1, BPositive=2 … ONegative=7
- PatientStatus: AtReception=0, AtNurse=1, AtDoctor=2, Completed=3, Cancelled=4
- Do NOT use `IDENTITY_INSERT` with explicit IDs if patients already exist — remove IDs and let DB auto-assign

#### Auto-migrate on startup

```csharp
// Program.cs — runs on every startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}
```

### Definition of done

- [ ] AppDbContext configured with all DbSets
- [ ] Global soft-delete filters applied to all soft-deletable entities
- [ ] WalkInQueue config has NO Department or WaitTime references
- [ ] PassportNumber and NationalId indexes use `HasFilter` for nullable uniqueness
- [ ] All monetary fields use `HasPrecision(12,3)`
- [ ] All computed properties have `e.Ignore()` in configuration
- [ ] Initial migration created and applied
- [ ] Database created and accessible via SQL Server
- [ ] `seed_patients.sql` inserts with NULL passport numbers and integer enum values

### Git checkpoint
```
git commit -m "Phase04: AppDbContext, entity configurations, initial migration, seed data fixes"
```

---

## PHASE 05 — Identity & Security

### Objective
Configure ASP.NET Core Identity, set up ApplicationUser, define and seed roles, implement authentication middleware, configure security policies, and add rate limiting and idempotency middleware.

### Prerequisites
Phase 04 complete, database migrated.

### Scope

#### Identity configuration (Program.cs)

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase       = true;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers      = true;
    options.User.RequireUniqueEmail         = true;
    options.SignIn.RequireConfirmedEmail     = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
```

#### Roles seeded at startup

```
Admin · Doctor · Nurse · Receptionist · LabTechnician · Pharmacist · Patient
```

Seeded via `RoleManager<IdentityRole>` in Program.cs startup block.

#### Auth cookie configuration

```csharp
options.LoginPath         = "/Account/Login";
options.LogoutPath        = "/Account/Logout";
options.AccessDeniedPath  = "/Account/AccessDenied";
options.ExpireTimeSpan    = TimeSpan.FromHours(8);
options.SlidingExpiration = true;
options.Cookie.HttpOnly   = true;
options.Cookie.SameSite   = SameSiteMode.Lax;
options.Cookie.IsEssential = true;
```

#### Middleware implemented

**`IdempotencyMiddleware.cs`**
- 30-second deduplication window using `IMemoryCache`
- Returns HTTP 409 on duplicate POST within window
- Applied to: Create, Edit endpoints

**`RateLimitMiddleware.cs`**
- Create routes: 10-second cooldown
- Edit routes: 5-second cooldown
- Delete routes: 30-second cooldown
- Returns HTTP 429 Too Many Requests

#### Security rules enforced

- `[ValidateAntiForgeryToken]` on all POST actions
- `[Authorize(Roles = "...")]` on every controller and action
- Anonymous access is limited to authentication/access-denied surfaces, `/health`, static/framework infrastructure, and separately approved public pages
- Server-side role check on every write — never rely on UI hiding

#### AccountController actions

- `GET/POST /Account/Login` — authenticate with email + password
- `GET /Account/Logout` — terminate session
- `GET/POST /Account/Setup` — one-time admin account creation (disabled after first admin exists)
- `GET/POST /Account/ResetPassword` — Admin-only password reset

### Definition of done

- [ ] Identity configured with password policy and lockout
- [x] Five canonical roles defined; password-free role seeding is implemented but invoked only through explicit post-migration bootstrap
- [ ] Login, logout, and setup pages functional
- [ ] `[Authorize]` present on all controllers
- [ ] `IdempotencyMiddleware` and `RateLimitMiddleware` registered in pipeline
- [ ] Auth cookie configured with HttpOnly and SameSite
- [ ] `[ValidateAntiForgeryToken]` on all POST actions

### Git checkpoint
```
git commit -m "Phase05: Identity config, roles seeded, auth cookie, security middleware"
```

---

## PHASE 06 — DTOs & Validation

### Objective
Create DTOs and presentation models for all module operations. Establish the pattern for separating presentation data from domain entities.

### Prerequisites
Phase 05 complete.

### Scope

#### Presentation model pattern (historical MVC reference)

```
Web/ViewModels/
├── Patient/
│   ├── PatientListViewModel.cs    ← Index page: TotalCount, Patients list, pagination
│   ├── PatientRowDto.cs           ← Single row in table: Id, Code, Name, Phone, Status
│   ├── CreatePatientViewModel.cs  ← Registration form
│   └── EditPatientViewModel.cs    ← Edit form (role-aware: Nurse=vitals only)
├── Doctor/
│   ├── DoctorListViewModel.cs
│   └── DoctorViewModels.cs
├── WalkInQueue/
│   ├── QueueBoardViewModel.cs     ← Index board: Date, Total, Waiting, AtNurse, etc.
│   ├── QueueRowViewModel.cs       ← Single row with computed display helpers
│   └── AddToQueueViewModel.cs     ← Add form: PatientId, WalkInName, Priority, Notes
└── Shared/
    └── PatientSearchModel.cs      ← _PatientSearch partial model
```

#### ServiceResult usage pattern

```csharp
// Controller pattern
var result = await _patientService.CreateAsync(vm);
if (!result.IsSuccess)                       // ← IsSuccess, NOT Succeeded
{
    ModelState.AddModelError("", result.ErrorMessage!);  // ← ErrorMessage, NOT Error
    return View(vm);
}
TempData["Success"] = "Patient registered.";
return RedirectToAction(nameof(Index));
```

#### QueueRowViewModel — display helper pattern

```csharp
// Computed properties eliminate logic from views
public bool IsWaiting   => Status == "Waiting";
public bool IsAtNurse   => Status == "AtNurse";
public bool IsActive    => Status is "Waiting" or "AtNurse" or "AtDoctor" or "OnHold";

public string StatusCssClass => Status switch {
    "Waiting"   => "status-atreception",
    "AtNurse"   => "status-atnurse",
    "AtDoctor"  => "status-atdoctor",
    "Completed" => "status-completed",
    "Cancelled" => "status-cancelled",
    _           => ""
};

public string RowStyle => Status switch {
    "AtNurse"   => "background:#fef9ec",
    "AtDoctor"  => "background:#f5f0ff",
    "Completed" => "opacity:.6",
    _           => ""
};
```

#### PatientSearchModel — reusable partial

```csharp
public class PatientSearchModel
{
    public string  FieldName          { get; set; } = "PatientId";
    public string  Label              { get; set; } = "Search Patient";
    public string  Placeholder        { get; set; } = "Search by name, code or phone...";
    public bool    Required           { get; set; } = false;
    public string  InstanceId         { get; set; } = "main";
    public string? CurrentPatientName { get; set; }  // for Edit pages
    public string? CurrentPatientCode { get; set; }
    public string? CurrentPatientPhone{ get; set; }
    public int?    CurrentPatientId   { get; set; }
}
```

### Definition of done

- [ ] ViewModels exist for all completed modules
- [ ] No EF Core entity exposed directly to any view
- [ ] Controller code uses `result.IsSuccess` and `result.ErrorMessage` — not `Succeeded` / `Error`
- [ ] `QueueRowViewModel` has all computed display helpers
- [ ] `PatientSearchModel` supports multi-instance via `InstanceId`

### Git checkpoint
```
git commit -m "Phase06: ViewModels, DTOs, ServiceResult usage pattern, PatientSearchModel"
```

---

## PHASE 07 — Application Services

**Status: IN PROGRESS (07S, 07A, 07B, 07C-P, 07C, and 07D implementation complete).** The remaining material in this section is planned
design guidance; later Phase 07 services, repositories, Unit of Work, and workflows are not
implemented in the current checkpoint.

### Phase 07S — Allocator / Schema Prerequisite

**Status: COMPLETE.** Infrastructure now contains database-backed PatientCode and queue-ticket
allocation primitives. The approved `AddPhase07AllocatorInfrastructure` migration was applied
exactly once to `ElsheiekhHMS_Dev`, and the physical allocator tables, checks, primary keys, and
queue uniqueness `(QueueDate, SequenceNumber)` were verified. Patient phone remains non-unique;
07A is complete under its approved implementation gate. The next Phase 07 sub-phase requires its
own design and implementation approval.

### Phase 07A — Patient Application Service

**Status: COMPLETE.** Application owns the `IPatientService` contract, immutable result/error
contracts, and the narrow EF-free `IPatientPersistence` port. Infrastructure implements bounded
Patient projections, PatientCode allocation through the approved 07S allocator, rowversion and
unique-identifier translation, and one-save Patient/AuditLog transactions. Patient phone remains
non-unique and duplicate candidates never block registration. The two approved implementation
decisions are recorded in ADR-018. No migration, snapshot, Core, Identity, or database change was
required.

### Phase 07B — Department Application Service

**Status: COMPLETE.** Application now owns the narrow
`IDepartmentService` contract, the minimal bounded `DepartmentSearchRequest` contract and
validator, and the EF-free `IDepartmentPersistence` port. Infrastructure provides direct
projected Department reads, server-side filtering/sorting/pagination, and tracked writes.
Create, update, and deactivate stage `DEPARTMENT_CREATED`, `DEPARTMENT_UPDATED`, and
`DEPARTMENT_DEACTIVATED` AuditLog events and commit each Department/AuditLog pair with one
`SaveChangesAsync`. The existing `CanConfigureSystem` role mapping is preserved: only
`SystemAdministrator` can manage Department configuration; Administrator, Receptionist,
Provider, and Patient are not broadened. Department names remain non-unique, reactivation and
hard delete are out of scope, and no schema or migration change is required.

### Phase 07C-P — AppointmentCode Allocator Prerequisite

**Status: COMPLETE.** Infrastructure provides a
year-scoped, UTC-based `AP-YYYY-NNNNN` allocator backed by the dedicated
`AppointmentCodeAllocations` table and serializable atomic allocation. The additive
`AddAppointmentCodeAllocator` migration adds that table and the physical unique
`UX_Appointments_AppointmentCode` index; it was applied exactly once to the development
database and physically verified. AppointmentService 07C and Queue Application Service 07D are complete and verified. Phase 07 is complete for the approved scope; 07E Doctor/Provider Application Service is deferred pending approved contracts, Doctor↔ApplicationUser ownership, ownership persistence, and ownership-aware authorization.

### Objective
Implement all application-layer services. Services orchestrate validation, authorization checks, business rules, repository calls, and audit logging.

### Prerequisites
Phase 06 complete.

### Planned services

| Service | Interface | File | Status |
|---------|-----------|------|--------|
| PatientService | IPatientService | Application/Patients/ | ✅ Complete (07A) |
| DoctorService | IDoctorService | Infrastructure/Services/ | ⏸ Deferred (07E prerequisites not approved) |
| DepartmentService | IDepartmentService | Application/Departments/ | ✅ Complete (07B) |
| AppointmentService | IAppointmentService | Application/Appointments/ | ✅ Complete (07C) |
| LabService | ILabService | Infrastructure/Services/ | Planned |
| WalkInQueueService | IQueueService | Application/Queue/ | ✅ Complete (07D) |
| AuditLogService | IAuditLogService | Infrastructure/Services/ | Planned |
| NotificationService | INotificationService | Infrastructure/Services/ | Planned |

### WalkInQueueService — corrected method signatures

**Critical fix:** `ServiceResult<T>` uses `Success()` and `Failure()` — NOT `Ok()` and `Fail()`.

```csharp
// WRONG — caused CS0117 errors
return ServiceResult<WalkInQueueDto>.Fail("message");
return ServiceResult<WalkInQueueDto>.Ok(dto);
result.Succeeded   // CS1061
result.Error       // CS1061

// CORRECT
return ServiceResult<WalkInQueueDto>.Failure("message");
return ServiceResult<WalkInQueueDto>.Success(dto);
result.IsSuccess
result.ErrorMessage
```

### DoctorService — HasActiveQueueAsync fix

```csharp
// WRONG — CS0411 (AnyAsync not on repository)
var hasActive = await _uow.WalkInQueues.AnyAsync(q => q.DoctorId == id && ...);

// WRONG — typo
q.DoctorId == date  // should be q.DoctorId == doctorId

// CORRECT — use HasActiveEntriesForDoctorAsync from repository
var hasActive = await _uow.WalkInQueues.HasActiveEntriesForDoctorAsync(id, DateTime.Today);
```

### UnitOfWork registration (Program.cs)

```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IWalkInQueueService, WalkInQueueService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
```

### Service method pattern

```csharp
public async Task<ServiceResult<WalkInQueueDto>> AddToQueueAsync(AddToQueueDto dto, string addedBy)
{
    try
    {
        // 1. Validate input
        if (dto.PatientId == null && string.IsNullOrWhiteSpace(dto.WalkInName))
            return ServiceResult<WalkInQueueDto>.Failure("...");

        // 2. Business rules
        // 3. Repository calls via _uow
        await _uow.WalkInQueues.AddAsync(entry);
        await _uow.SaveChangesAsync();

        // 4. Return result
        return ServiceResult<WalkInQueueDto>.Success(ToDto(entry, ...));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error adding to queue");
        return ServiceResult<WalkInQueueDto>.Failure("Failed to add to queue.");
    }
}
```

### Doctor name access pattern

```csharp
// Doctor entity has no FirstName/LastName — access via ApplicationUser
var doctorName = entry.Doctor?.ApplicationUser != null
    ? $"Dr. {entry.Doctor.ApplicationUser.FirstName} {entry.Doctor.ApplicationUser.LastName}"
    : null;
```

### Definition of done

- [ ] All services registered in DI
- [ ] All services use `IsSuccess`, `ErrorMessage` — not `Succeeded`, `Error`
- [ ] WalkInQueueService uses `HasActiveEntriesForDoctorAsync` not inline `AnyAsync`
- [ ] Doctor names accessed via `ApplicationUser` navigation
- [ ] All write operations call audit logging
- [ ] No `SaveChangesAsync()` inside repository methods

### Git checkpoint
```
git commit -m "Phase07: application services checkpoint"
```

---

### Phase 07 closeout

**Status: COMPLETE for the approved scope.** 07S, 07A, 07B, 07C-P, 07C, and 07D are complete. 07E Doctor/Provider Application Service is explicitly deferred. The Doctor domain entity, Department relationship, Appointment relationship, and EF persistence model already exist; the missing prerequisites are approved Phase06-style Doctor/Provider contracts, a service contract, Doctor↔ApplicationUser ownership mapping, ownership persistence, and ownership-aware authorization. Do not implement 07E until those prerequisites are designed and approved.


## PHASE 08 — Business Workflows

**Status: IN PROGRESS.** 08A Staff Patient Intake & Appointment Scheduling is complete; 08B Staff Appointment Arrival & Queue Handoff is not started.

### Objective
Implement complete business workflows — multi-step processes that involve validation, status transitions, transactions, audit trails, and cross-entity coordination.

### Prerequisites
Phase 07 must be complete before Phase 08 begins.

### Walk-in Queue workflow ✅ Complete

```
Walk-in registers → Receptionist adds to queue → Ticket A-001 issued
        ↓
Nurse calls next → status: Waiting → AtNurse
        ↓
Nurse records vitals → doctor calls → status: AtNurse → AtDoctor
        ↓
Doctor completes → status: AtDoctor → Completed
```

**Valid transitions only:**
- Waiting → AtNurse (Nurse/Admin)
- Waiting → AtDoctor (Doctor/Admin — skip nurse)
- Waiting → OnHold (Nurse/Receptionist/Admin)
- AtNurse → AtDoctor (Doctor/Admin)
- AtDoctor → Completed (Any authorized)
- Any active → Cancelled (Admin/Receptionist)
- OnHold → Waiting (resume)

**Invalid transitions** are rejected with `ServiceResult.Failure()` — never silently ignored.

### AppDbContext WalkInQueue — verified fix

```csharp
// CORRECT — no Department or WaitTime
builder.Entity<WalkInQueue>(e => {
    e.HasIndex(q => new { q.DoctorId, q.QueueDate, q.QueueNumber });
    e.HasOne(q => q.Patient).WithMany(p => p.QueueEntries).HasForeignKey(q => q.PatientId).OnDelete(DeleteBehavior.Restrict);
    e.HasOne(q => q.Doctor).WithMany(d => d.QueuePatients).HasForeignKey(q => q.DoctorId).OnDelete(DeleteBehavior.Restrict);
});
// Department and WaitTime lines caused CS1061 — removed
```

### Appointment booking workflow ⏳ Planned

```
Validate patient exists
→ Validate doctor exists and is Active
→ Validate date/time is in the future
→ Check for scheduling conflicts (same doctor, same time slot)
→ Create appointment (status: Scheduled)
→ Notify doctor
→ Audit log
```

### Lab test workflow ⏳ Planned

```
Doctor orders test from catalog
→ LabOrder created (status: Ordered)
→ Lab Tech collects sample (status: SampleCollected)
→ Processing begins (status: Processing)
→ Result entered — IsAbnormal flagged if needed (status: Completed)
→ Doctor notified
→ Fee added to patient invoice
```

### Billing workflow ⏳ Planned

```
Invoice created for patient
→ Line items auto-populated (consultation, lab, pharmacy fees)
→ Patient pays → payment recorded
→ If full payment: status → Paid
→ If partial: status → PartiallyPaid, outstanding balance updated
→ Receipt generated
→ Audit log
```

### Business invariants enforced

- Cannot schedule an inactive doctor
- Cannot assign an occupied bed
- Cannot discharge without active admission
- Cannot enter lab result on cancelled order
- Cannot pay a cancelled invoice
- Cannot edit a paid invoice
- Cannot hard-delete patient, EMR, lab test, or invoice records

### Definition of done

- [ ] Walk-in queue workflow fully operational with all 6 status values
- [ ] Queue entry invalid transitions rejected with clear error message
- [ ] AppDbContext WalkInQueue config has no Department or WaitTime references
- [ ] Appointment conflict detection working
- [ ] Lab test workflow: all 5 status transitions functional
- [ ] Billing: partial payments correctly tracked
- [ ] All workflow steps logged in AuditLog within same transaction

### Git checkpoint
```
git commit -m "Phase08: queue workflow, appointment booking, lab workflow, billing complete"
```

---

## PHASE 09 — Enterprise Infrastructure

### Objective
Implement cross-cutting infrastructure: audit logging service, notification system, security middleware, health checks, and structured logging.

### Prerequisites
Phase 08 complete.

### Audit trail ✅ Complete

**`AuditLog` entity fields:** `UserId`, `UserName`, `UserRole`, `Action`, `EntityName`, `EntityId`, `OldValues` (JSON), `NewValues` (JSON), `Timestamp` (UTC), `IPAddress`

**Actions logged:** CREATE, UPDATE, SOFT_DELETE, LOGIN, FAILED_LOGIN, PERMISSION_CHANGE, STATUS_CHANGE, LAB_RESULT_ENTERED, PAYMENT_RECEIVED

**Rules:**
- Written within the same database transaction as the triggering operation
- Append-only — no update or delete endpoint
- Passwords, tokens, PasswordHash, SecurityStamp excluded from OldValues/NewValues
- Indexed on Timestamp and composite (EntityName, EntityId)

### Notification system ✅ Complete

**Bell icon in topbar:** unread count badge, dropdown of last 10 unread, mark read on click, mark all read button

**Polling:** every 30 seconds via JavaScript `fetch('/Notification/Unread')`

**Controller endpoints:**
- `GET /Notification/Unread` → JSON `{ count, notifications[] }`
- `POST /Notification/MarkRead/{id}`
- `POST /Notification/MarkAllRead`

**Notification triggers:**
- New patient registered → Reception
- Lab result available → Ordering doctor
- Emergency priority queue entry → Nurse + Doctor
- Low medication stock → Pharmacist + Admin

### Security middleware ✅ Complete

**`IdempotencyMiddleware`** — 30s dedup window, HTTP 409 on duplicate
**`RateLimitMiddleware`** — route-specific cooldowns, HTTP 429

### Dashboard ✅ Complete

**Role-aware KPI sections:**
- Admin: all stats
- Doctor: own appointments, own queue patients
- Nurse: vitals pending, queue at nurse
- Receptionist: today's patients, queue waiting
- LabTech: pending lab orders

### CSS architecture ✅ Complete

```
wwwroot/css/
├── site.variables.css     ← Design tokens, color palette, spacing
├── site.layout.css        ← Sidebar, topbar (LTR only)
├── site.components.css    ← Cards, buttons, badges, patient search CSS
├── site.forms-tables.css  ← Forms, inputs, sticky actions
├── site.tables.css        ← Enterprise table system (548 lines)
├── site.mobile.css        ← Responsive rules
├── site.auth.css          ← Login page
├── site.logo.css          ← Logo styles
└── site.dark.css          ← Dark mode overrides
```

**Arabic/RTL removed:** All `[dir="rtl"]` blocks were removed. System is English-only LTR. Arabic UI is deferred to v2.

### JavaScript architecture ✅ Complete

**`wwwroot/js/site.js`** (1389 lines):
- Sidebar toggle, mobile menu
- Dark mode toggle
- Clock display
- Double-submit prevention
- `confirmAction()` — modal for destructive actions
- Notification polling and dropdown
- `EntTable.init()` — enterprise table engine
- `PS_init()` — patient search engine (multi-instance)
- Walk-in queue auto-refresh

### Enterprise table system ✅ Complete

**`site.tables.css`** provides:
- Sticky header + sticky first column
- Zebra stripes, row hover
- Kebab menu per row
- Bulk selection with bulk action bar
- Column chooser (localStorage)
- Density switcher: Comfortable / Compact / Dense
- Skeleton loading
- Professional pagination
- Empty and error states
- Mobile card view (≤640px)
- Dark mode

**Usage:**
```javascript
EntTable.init('patientTable', {
    onSearch: q => { /* server-side search */ },
    onSort: (col, dir) => { /* server-side sort */ }
});
```

### Patient search component ✅ Complete

**`Views/Shared/_PatientSearch.cshtml`** — reusable partial

```cshtml
@await Html.PartialAsync("_PatientSearch", new PatientSearchModel
{
    FieldName   = "PatientId",
    Label       = "Search Patient",
    Required    = true,
    InstanceId  = "queue"   // unique per page if multiple instances
})
```

**Features:** debounced search (300ms), spinner, clear button, keyboard nav (↑↓ Enter Esc), match highlighting, status badges, avatar initials, empty state with register link, recent patients on focus, multi-instance support.

**Backend required:** `GET /Patient/Search?q=&limit=5` → `{ id, fullName, patientCode, phone, status }`

### Definition of done

- [ ] Audit trail writing on all write operations
- [ ] Notification bell functional with polling
- [ ] Security middleware registered and working
- [ ] Dashboard showing role-appropriate KPIs
- [ ] `_PatientSearch` partial working in Add queue form
- [ ] Enterprise table working in Patient/Index
- [ ] Arabic/RTL code fully removed
- [ ] Dark mode toggle functional

### Git checkpoint
```
git commit -m "Phase09: audit trail, notifications, enterprise tables, patient search partial"
```

---

## PHASE 10 — Testing & Hardening

### Objective
Establish a meaningful test suite covering domain rules, service behavior, status transitions, authorization, financial calculations, and critical workflows.

### Prerequisites
Phase 09 complete.

### Test categories

#### Unit tests (no database)

```
Tests/Unit/Domain/
├── PatientTests.cs          ← PatientCode format, FullName computation
├── QueueStatusTests.cs      ← Valid/invalid status transitions
├── AppointmentTests.cs      ← Conflict detection, invalid time ranges
├── InvoiceTests.cs          ← OutstandingAmount calculation, decimal precision
└── ServiceResultTests.cs    ← Success/Failure/ValidationFailure behavior

Tests/Unit/Application/
├── WalkInQueueServiceTests.cs
├── PatientServiceTests.cs
└── BillingServiceTests.cs
```

#### Critical test cases

```csharp
// Queue status transitions
[Fact] WalkInQueue_AdvanceStatus_WaitingToAtNurse_Succeeds()
[Fact] WalkInQueue_AdvanceStatus_CompletedToWaiting_Fails()
[Fact] WalkInQueue_AdvanceStatus_CancelledToAtDoctor_Fails()

// Duplicate detection
[Fact] PatientService_Create_DuplicateNationalId_ReturnsFailure()
[Fact] PatientService_Create_DuplicatePhone_ReturnsWarning()

// Authorization
[Fact] WalkInQueueController_Cancel_NurseRole_ReturnsForbidden()
[Fact] PatientController_Delete_ReceptionistRole_ReturnsForbidden()

// Financial
[Fact] Invoice_PartialPayment_OutstandingBalanceCorrect()
[Fact] Invoice_Cancelled_PaymentAttempt_ReturnsFailure()
[Fact] InvoiceItem_Total_UsesDecimalNotFloat()

// Audit trail
[Fact] PatientService_Create_ProducesAuditLogEntry()
[Fact] AuditLog_NeverContainsPasswordHash()
```

#### Integration tests (EF InMemory)

```
Tests/Integration/
├── PatientRepositoryTests.cs    ← Search, pagination, soft-delete filter
├── QueueRepositoryTests.cs      ← GetTodayQueue, GetNextSequence
└── WorkflowTests.cs             ← Full patient visit end-to-end
```

#### Architecture tests

```csharp
// Core must have zero external package references
[Fact] Core_HasNoEfCoreReference()
[Fact] Core_HasNoIdentityReference()
[Fact] Core_HasNoHttpReference()

// Web must not reference DbContext directly in components
[Fact] BlazorComponents_DoNotInjectAppDbContext()
```

### Security hardening checklist

- [ ] All `[Authorize]` decorators present on every controller
- [ ] `[AllowAnonymous]` only on Login, Logout, Setup, Queue Display
- [ ] No sensitive data in any `_logger.Log*` call
- [ ] No `float` or `double` in any financial property
- [ ] No hard-delete endpoint for Patient, MedicalRecord, LabTest, Invoice, AuditLog
- [ ] All POST actions have `[ValidateAntiForgeryToken]`
- [ ] SQL injection impossible — no raw SQL, all EF Core parameterized

### Definition of done

- [ ] `dotnet test` passes with zero failures
- [ ] Status transition tests cover all valid and invalid paths
- [ ] Authorization tests confirm role enforcement
- [ ] Architecture tests confirm Core independence
- [ ] No compiler warnings in any project
- [ ] Security checklist above fully verified

### Git checkpoint
```
git commit -m "Phase10: test suite, security hardening, architecture validation"
```

---

## PHASE 11 — Backend Review

### Objective
Full architecture audit before beginning substantial Blazor UI work. This is the backend release gate.

### Prerequisites
Phase 10 complete, all tests passing.

### Audit checklist

#### Architecture
- [ ] Dependency direction: Core → nothing, Infrastructure → Core, Web → Core + Infrastructure
- [ ] No circular dependencies
- [ ] `AddApplication()` / `AddInfrastructure()` used in Program.cs
- [ ] No business logic in controllers or components

#### Domain model
- [ ] All entities inherit appropriate base class
- [ ] Navigation properties initialized to `new List<T>()`
- [ ] Computed properties marked `[NotMapped]`
- [ ] Doctor names accessed via `ApplicationUser`, not directly
- [ ] `AuditLog` has no soft-delete fields

#### Database
- [ ] All migrations apply cleanly on fresh database
- [ ] All unique indexes use `HasFilter` for nullable columns
- [ ] No `float`/`double` in monetary fields
- [ ] Global soft-delete filters applied to all soft-deletable entities
- [ ] WalkInQueue config has no Department or WaitTime references
- [ ] PassportNumber inserts use NULL not empty string

#### Security
- [ ] `[Authorize]` on every controller
- [ ] Password policy enforced (8 chars, uppercase, digit, special)
- [ ] Lockout after 5 failed attempts, 15 minutes
- [ ] Auth cookie: HttpOnly, SameSite=Lax
- [ ] No secrets in appsettings.json or in code

#### Services
- [ ] All services use `IsSuccess` / `ErrorMessage` — not `Succeeded` / `Error`
- [ ] `Success()` / `Failure()` method names — not `Ok()` / `Fail()`
- [ ] `SaveChangesAsync()` only in UnitOfWork
- [ ] Doctor name always via `ApplicationUser` navigation

#### Audit trail
- [ ] All write operations produce AuditLog entry
- [ ] AuditLog never contains password fields
- [ ] AuditLog written in same transaction as triggering operation
- [ ] No update or delete endpoint for AuditLog

#### Tests
- [ ] All tests pass
- [ ] Critical transition tests present
- [ ] Authorization tests present
- [ ] Architecture tests present

### Phase gate — do NOT proceed to Phase 12 until

All items above are checked. Zero compiler warnings. Zero test failures.

### Git checkpoint
```
git commit -m "Phase11: backend audit complete, all checks passing, ready for Blazor UI"
```

---

## PHASE 12 — Blazor UI

### Objective
Build the Blazor Interactive Server UI using the approved backend contracts. Blazor remains a presentation layer over tested Application workflows.

### Prerequisites
Phase 11 complete and signed off.

### Migration approach

The `setup-elshiekh-blazor.ps1` script creates the full Blazor solution structure:

```bash
# Run from empty folder
./setup-elshiekh-blazor.ps1
```

Creates:
- `ElsheiekhHMS.Core` — with all domain models and enums ported
- `ElsheiekhHMS.Application` — DTOs, service interfaces, validators
- `ElsheiekhHMS.Infrastructure` — AppDbContext, repositories, services
- `ElsheiekhHMS.Web` — Blazor Interactive Server with all module page folders
- `ElsheiekhHMS.Tests` — xUnit project

### Component architecture rules

```csharp
// CORRECT Blazor pattern
@inject IPatientService PatientService
@inject IWalkInQueueService QueueService

@code {
    private QueueBoardViewModel? Board;

    protected override async Task OnInitializedAsync()
    {
        var result = await QueueService.GetTodayQueueAsync();
        if (result.IsSuccess)
            Board = MapToViewModel(result.Data!);
        else
            ErrorMessage = result.ErrorMessage;
    }
}

// WRONG — never do this in a Blazor component
@inject AppDbContext Db  // ← forbidden
var patients = await Db.Patients.ToListAsync();  // ← forbidden
```

### Module pages planned

```
Components/Pages/
├── Dashboard/Index.razor          ← Role-aware KPI dashboard
├── Patients/
│   ├── Index.razor                ← Enterprise table with search
│   ├── Create.razor               ← 6-section registration form
│   ├── Edit.razor                 ← Role-aware (Nurse=vitals, Admin=full)
│   ├── Details.razor              ← Flow indicator, tabs, audit trail
│   └── EditVitals.razor           ← Nurse vitals form
├── WalkInQueue/
│   ├── Index.razor                ← Staff queue board, real-time
│   ├── Add.razor                  ← With _PatientSearch partial
│   └── Display.razor              ← Public bank-style display board
├── Appointments/
│   ├── Index.razor
│   ├── Create.razor
│   └── Details.razor
├── Laboratory/
│   ├── Index.razor
│   └── UpdateResult.razor
├── Billing/
│   ├── Index.razor
│   └── Create.razor
└── ...
```

### Queue display board — bank-style design

`/WalkInQueue/Display` (Layout=null, `[AllowAnonymous]`)

Design elements:
- Full-screen dark navy background (#0d1b2a)
- **Now Serving banner** — teal gradient, large ticket number, calling animation
- Ticket grid — color-coded by status (blue=waiting, amber=nurse, purple=doctor, green=done)
- Stats sidebar — Waiting / AtNurse / AtDoctor / Completed / Avg wait
- Marquee ticker bar at bottom
- Audio beep on new ticket called (Web Audio API)
- Auto-refresh every 12 seconds via `fetch('/WalkInQueue/Live')`
- `[AllowAnonymous]` — no login required for waiting room screen

### UX standards for Blazor components

- All forms validate inline on blur — errors shown below fields before submission
- Every save button shows spinner + disabled state during processing
- Every successful action shows top-right toast — auto-dismiss 4 seconds
- Every destructive action requires confirmation modal showing entity name and code
- Empty states: icon + message + primary CTA button
- Error states: icon + message + retry button — never blank screen
- Queue board and dashboard use SignalR for real-time updates where appropriate
- All tables follow enterprise table system (site.tables.css + EntTable.init())
- Mobile responsive: tables convert to card layout ≤640px with `data-label` attributes

### SpecKit and Codex commands

See the project's SpecKit and Codex command files for enforced standards per module.

### Definition of done

- [ ] All P0 module pages built and functional in Blazor
- [ ] No business logic in any Razor component
- [ ] Patient search partial reused across queue, appointment, billing forms
- [ ] Queue display board renders correctly on TV/monitor with auto-refresh
- [ ] Role-based UI: unauthorized sections hidden AND server-side authorization enforced
- [ ] All forms show loading, error, and success states
- [ ] Enterprise table system active on all Index pages
- [ ] Mobile layout functional on tablet
- [ ] All tests still passing after UI integration

### Git checkpoint
```
git commit -m "Phase12: Blazor UI complete, all P0 modules functional, display board live"
```

---

## 8. Cross-Phase Concerns

| Concern | Introduced | Matures | Final State |
|---------|-----------|---------|-------------|
| Soft-delete | Phase 02 (BaseEntity) | Phase 04 (query filters) | Global EF filters, never hard-delete |
| Audit trail | Phase 05 (middleware) | Phase 09 (service) | All writes logged with JSON diff |
| Optimistic concurrency | Phase 04 (RowVersion) | Phase 10 (tests) | Patient, Invoice, Appointment |
| Validation | Phase 06 (ViewModels) | Phase 07 (services) | Server-side authoritative |
| Authorization | Phase 05 (Identity) | Phase 09 (enforcement) | Server-side on every action |
| Transactions | Phase 07 (UoW) | Phase 08 (workflows) | All multi-step ops in transaction |
| Logging | Phase 07 (ILogger<T>) | Phase 09 (structured) | Never log passwords/tokens |
| Error handling | Phase 06 (ServiceResult) | Phase 10 (tests) | Consistent IsSuccess/ErrorMessage |
| Pagination | Phase 06 (PagedResult) | Phase 07 (services) | Default 25, max 250, server-side |
| Arabic/RTL | — | — | Deferred to v2. All RTL code removed. |

---

## 9. Dependency / Package Timeline

| Technology | Earliest Phase | Project | Purpose |
|-----------|---------------|---------|---------|
| No packages | Phase 01–02 | Core | Core must remain package-free |
| EF Core | Phase 04 | Infrastructure | ORM and migrations |
| EF Core.SqlServer | Phase 04 | Infrastructure | SQL Server provider |
| EF Core.Tools | Phase 04 | Infrastructure | `dotnet ef` CLI |
| Identity.EntityFrameworkCore | Phase 05 | Infrastructure | Identity + EF integration |
| EF Core.Design | Phase 04 | Web | Migration design-time support |
| Logging.Abstractions | Phase 07 | Application | ILogger<T> interface |
| xUnit | Phase 10 | Tests | Test runner |
| FluentAssertions | Phase 10 | Tests | Readable assertions |
| Moq | Phase 10 | Tests | Interface mocking |
| EF Core.InMemory | Phase 10 | Tests | Integration test DB |

**Do not add packages outside this timeline without explicit architectural justification.**

---

## 10. Testing Evolution

```
Phase 02 — Core unit tests
  └── ServiceResult behavior, enum values, BaseEntity fields

Phase 03 — Domain unit tests
  └── Entity invariants, computed properties, navigation initialization

Phase 07 — Service unit tests (Moq)
  └── PatientService, WalkInQueueService, BillingService

Phase 08 — Workflow tests
  └── Status transition validity, business rule enforcement

Phase 09 — Integration tests (EF InMemory)
  └── Repository queries, soft-delete filters, pagination

Phase 10 — Full test suite
  └── Architecture tests (Core independence)
  └── Authorization tests (role enforcement)
  └── Concurrency tests (RowVersion conflict handling)
  └── Security tests (no passwords in logs/audit)

Phase 12 — UI tests (future)
  └── Blazor component rendering tests
  └── End-to-end workflow tests
```

---

## 11. Architecture Evolution

```
Phase 01                    Phase 04                    Phase 12
───────                     ────────                    ────────
Core (empty)                Core (entities)             Core (stable)
Infrastructure (empty)  →   Infrastructure (EF+DB)  →   Infrastructure (all services)
Web (stub)                  Web (controllers)           Web (Blazor components)
                            Tests (unit)                Tests (full suite)
```

Dependency direction never changes across all phases:

```
Web  ──►  Infrastructure  ──►  Core
Web  ──────────────────────►  Core
Tests ──►  all
Core ──►  (nothing)
```

---

## 12. Phase Gate Matrix

| Phase | Build | Unit Tests | Integration Tests | Architecture Audit | Security Review | DB Verified | Manual Verify |
|-------|-------|-----------|------------------|--------------------|----------------|------------|---------------|
| 01 | ✅ | N/A | N/A | ✅ | N/A | N/A | ✅ /health |
| 02 | ✅ | ✅ | N/A | ✅ | N/A | N/A | N/A |
| 03 | ✅ | ✅ | N/A | ✅ | N/A | N/A | N/A |
| 04 | ✅ | N/A | ✅ | ✅ | N/A | ✅ | ✅ migrations |
| 05 | ✅ | N/A | N/A | ✅ | ✅ | N/A | ✅ login |
| 06 | ✅ | ✅ | N/A | ✅ | N/A | N/A | N/A |
| 07 | ✅ | ✅ | ✅ | ✅ | N/A | N/A | ✅ services |
| 08 | ✅ | ✅ | ✅ | ✅ | N/A | ✅ | ✅ workflows |
| 09 | ✅ | ✅ | ✅ | ✅ | ✅ | N/A | ✅ audit |
| 10 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ all |
| 11 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ full review |
| 12 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ UI complete |

---

## 13. Project Resume Protocol

When returning to this project after a break:

```bash
# 1. Check repository state
git status
git log --oneline -10

# 2. Verify build
dotnet restore
dotnet build --no-restore

# 3. Run tests
dotnet test --no-build

# 4. Verify database
dotnet ef migrations list \
  --project ELShiekhMedicalComplex.Infrastructure \
  --startup-project ELShiekhMedicalComplex.Web

# 5. Read current phase section in this document
# 6. Check Section 16 (Current Project Checkpoint)
# 7. Resume from "Next action" in Section 16
```

### Key project files

| File | Purpose |
|------|---------|
| `DEVELOPMENT_ROADMAP.md` | This file — master roadmap |
| `PRD.md` | Product Requirements Document |
| `phase01-setup.ps1` | Phase 01 legacy solution setup script |
| `setup-elshiekh-blazor.ps1` | Blazor migration setup script |
| `seed_patients.sql` | 100 patient seed records (integer enums, NULL passport) |
| `WalkInQueue-Backend.md` | Walk-in queue full backend spec |
| `PatientSearch-Component-Prompt.md` | Patient search component implementation guide |
| `EnterpriseTable-Prompt.md` | Enterprise table system for Doctor/Appointment/Lab/Dept views |
| `PatientIndex-Implementation-Prompt.md` | Patient/Index enterprise table full prompt |

---

## 14. AI Agent Instructions

Read this section before writing any code.

### Before starting any task

1. Read `DEVELOPMENT_ROADMAP.md` (this file) — specifically Section 7 (phase table) and Section 16 (checkpoint)
2. Identify the current phase
3. Read only the applicable phase section
4. Do not implement anything from a future phase

### Critical rules for code generation

```
DO:
✅ Use ServiceResult.Success() and ServiceResult.Failure() — exact names
✅ Use result.IsSuccess and result.ErrorMessage — exact names
✅ Access doctor names via doctor.ApplicationUser.FirstName
✅ Use HasActiveEntriesForDoctorAsync() — not inline AnyAsync()
✅ Insert NULL for empty PassportNumber — not empty string ''
✅ Use integer enum values in SQL seed data — not string names
✅ Keep Core project free of all external NuGet packages
✅ Build after every meaningful change: dotnet build --no-restore
✅ Test after every meaningful change: dotnet test --no-build

DO NOT:
❌ Use result.Succeeded or result.Error — these do not exist
❌ Use ServiceResult.Ok() or ServiceResult.Fail() — these do not exist
❌ Access doctor.FirstName directly — Doctor entity has no FirstName
❌ Use float or double for monetary values
❌ Hard-delete Patient, MedicalRecord, LabTest, Invoice, or AuditLog records
❌ Add HasOne(q => q.Department) to WalkInQueue config — no such navigation
❌ Add e.Ignore(q => q.WaitTime) to WalkInQueue config — WaitTime is not on entity
❌ Call SaveChangesAsync() inside a repository method
❌ Inject AppDbContext into a Blazor component or transport endpoint
❌ Add packages to Core project
❌ Add CQRS, MediatR, Redis, Docker, or microservices patterns
❌ Implement features belonging to future phases
❌ Claim success without running build and tests
❌ Commit or push unless explicitly instructed
```

### When generating a service method

```csharp
// Pattern to follow exactly
public async Task<ServiceResult<T>> DoSomethingAsync(Dto dto, string actorEmail)
{
    try
    {
        // 1. Validate
        if (invalid) return ServiceResult<T>.Failure("Clear message");

        // 2. Business rules
        // 3. Repository via _uow
        // 4. SaveChangesAsync via _uow
        await _uow.SaveChangesAsync();
        // 5. Audit log
        // 6. Return success
        return ServiceResult<T>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Context message");
        return ServiceResult<T>.Failure("User-friendly error message");
    }
}
```

---

## 15. Decision Log

### Decision: Backend-first development
**Reason:** Business rules must be testable independently of the UI. Blazor components must remain thin presentation layers.
**Consequence:** UI development (Phase 12) begins only after backend is audited (Phase 11).
**Phase:** Established at project start.

### Decision: Five-project .NET 10 Blazor-hosted solution
**Reason:** Core, Application, Infrastructure, Web, and Tests keep domain rules, orchestration, technical persistence, host composition, and verification separate.
**Consequence:** The verified project graph is preserved: Core has no project dependency; Application references Core; Infrastructure references Application/Core; Web references Application/Infrastructure; Tests references Core/Application/Infrastructure.
**Phase:** 01

### Decision: English-only, LTR layout
**Reason:** Arabic UI complexity was significant. All Arabic/RTL code was removed to unblock development.
**Consequence:** All `[dir="rtl"]` CSS blocks removed. Arabic deferred to v2.
**Phase:** During Phase 09 CSS architecture work.

### Decision: ServiceResult<T> with Success()/Failure() naming
**Reason:** Early code used `Ok()`/`Fail()` and `Succeeded`/`Error`. These caused CS0117 and CS1061 compiler errors in WalkInQueueService and WalkInQueueController. Corrected to `Success()`/`Failure()` and `IsSuccess`/`ErrorMessage`.
**Consequence:** All services must use exact method and property names.
**Phase:** 07

### Decision: Doctor name via ApplicationUser navigation
**Reason:** `Doctor` entity has no `FirstName`/`LastName` — it stores `ApplicationUserId` and navigates to `ApplicationUser`. Direct access caused CS1061 errors.
**Consequence:** Always access as `doctor.ApplicationUser.FirstName`.
**Phase:** 07

### Decision: WalkInQueue has no Department navigation
**Reason:** `WalkInQueue` entity was never given a `DepartmentId` or `Department` navigation property. AppDbContext config that referenced these caused CS1061 errors.
**Consequence:** Department and WaitTime lines removed from WalkInQueue EF configuration.
**Phase:** 04/08

### Decision: NULL for empty PassportNumber, not empty string
**Reason:** Unique index `IX_Patients_PassportNumber` was violated when multiple patients had `''` as passport number.
**Consequence:** All seed data and registration logic must insert `NULL` for absent passport number.
**Phase:** 04

### Decision: Integer enum values in SQL seed data
**Reason:** EF Core stores enums as integers by default. Seed SQL inserting string values (`'Male'`, `'APositive'`) caused Msg 245 conversion errors.
**Consequence:** All SQL seed data uses integer enum values (Male=0, Female=1, etc.).
**Phase:** 04

### Decision: IDENTITY_INSERT removed from seed script
**Reason:** Explicit IDs in seed script caused Msg 2627 PK violations when patients already existed in the database.
**Consequence:** Seed script removes IDENTITY_INSERT, lets database auto-assign IDs, uses PT-2026-XXXXX codes.
**Phase:** 04

### Decision: Soft-delete only on clinical and financial records
**Reason:** Hospital regulations require historical preservation of patient, clinical, and financial records. Hard delete is prohibited.
**Consequence:** No hard-delete endpoints exist for Patient, MedicalRecord, LabTest, Invoice, or AuditLog. IsDeleted=true only.
**Phase:** 02 (foundation), 04 (enforcement)

### Decision: Reject CQRS, MediatR, Redis, Docker, microservices
**Reason:** These patterns add complexity without proportional benefit for a hospital LAN monolith with 50 concurrent users.
**Consequence:** None of these patterns appear in the codebase.
**Phase:** Architecture review.

### Decision: Bank-style queue display board
**Reason:** Patient-facing display required a professional, immediately readable presentation comparable to bank queue systems — dark navy theme, large ticket numbers, animated calling banner.
**Consequence:** `/WalkInQueue/Display` uses `Layout=null`, `[AllowAnonymous]`, full-screen dark theme, 12-second auto-refresh.
**Phase:** 08/12

---

## 16. Current Project Checkpoint

```
Last completed phase:  Phase 07 — Application Services (approved scope)
Current phase:         Phase 08 — Business Workflows (not started)
Next phase:            Phase 08 — Business Workflows
Next action:           Review Phase 08 readiness; 07E remains explicitly deferred
Known blockers:        None
Important notes:
  - The current working codebase is the five-project .NET 10 ElsheiekhHMS solution
  - The Web project uses the Blazor Interactive Server host
  - PRD.md is complete and approved
  - SpecKit and Codex command files generated
  - seed_patients.sql corrected: integer enums, NULL passports, no IDENTITY_INSERT
  - Enterprise table system active on Patient/Index
  - _PatientSearch partial complete and in use in WalkInQueue/Add
  - Display board and future UI modules remain later workflow/UI scope
  - 07A Patient Application Service, 07B Department service, 07C Appointment service, and 07D Queue service are complete; Phase 07 is complete for the approved scope
  - Arabic/RTL removed — English-only confirmed
  - 05C-A stable audit vocabulary, bounded append-only model, server-controlled writer, and focused tests are complete
  - 05D additive Identity/AuditLog migration, development/integration SQL verification, and pending-model suppression reassessment are complete
  - AuditLog event-producing workflows, retention duration, IP/UserAgent capture, clinical/read auditing, and UI remain deferred after 05E security integration
  - All CSS in wwwroot/css/ split into 8 files
  - site.js is 1389 lines including EntTable and PS_init engines
```
