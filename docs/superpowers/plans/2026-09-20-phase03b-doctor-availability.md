# Phase 03B Doctor and Availability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved Doctor aggregate, recurring schedule child entity, Doctor lifecycle, and focused domain tests without persistence or Identity coupling.

**Architecture:** Doctor is a `SoftDeletableEntity` aggregate root with a required current Department identity and an owned read-only collection of `DoctorSchedule` children. Core enforces intrinsic value, lifecycle, ownership, and in-memory overlap rules; Application and later persistence phases retain cross-record checks, authorization, and transactions.

**Tech Stack:** .NET 10, C#, xUnit, PowerShell, custom Graphify generator.

**Spec:** `docs/superpowers/specs/2026-09-20-phase03b-doctor-availability-design.md`

## Global Constraints

- Implement only Doctor, DoctorSchedule, DoctorStatus, and their domain tests.
- Do not modify Department.
- Add no package/project references, EF Core, SQL Server, Identity, ApplicationUser, repository, DTO, service, controller, or UI code.
- Doctor implements no concurrency interface.
- DoctorSchedule implements no concurrency interface and no soft-delete base.
- Use test-first development and observe the expected RED before each production increment.
- Do not stage, commit, or push.
- Regenerate Graphify only after source, build, and tests pass.

## Review Focus

- Invalid professional updates must not partially mutate Doctor or its audit metadata.
- Deleted and inactive Doctors must reject every mutation permitted while active.
- Schedule overlap uses half-open intervals, so touching endpoints are allowed.
- Retired schedules remain in the collection but no longer block replacement intervals.
- A Doctor must not retire a schedule owned by another Doctor.
- Undefined `DayOfWeek` values must be rejected without changing schedules or audit metadata.

---

### Task 1: Doctor construction and professional details

**Files:**
- Create first: `ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorTests.cs`
- Create after RED: `ElsheiekhHMS.Core/Domain/Staff/Enums/DoctorStatus.cs`
- Create after RED: `ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs`

**Interfaces:**
- Consumes: `SoftDeletableEntity`, `DomainValidationException`.
- Produces: the Doctor constructor, scalar properties, `UpdateProfessionalDetails`, and `ChangeDepartment`.

Approved constructor:

```csharp
public Doctor(
    string doctorCode,
    string fullName,
    string? specialization,
    bool isGeneralPractitioner,
    decimal consultationFee,
    int departmentId,
    DateTimeOffset createdAt,
    string? createdBy)
```

Approved scalar API:

```csharp
public string DoctorCode { get; private set; }
public string FullName { get; private set; }
public string? Specialization { get; private set; }
public bool IsGeneralPractitioner { get; private set; }
public decimal ConsultationFee { get; private set; }
public DoctorStatus Status { get; private set; }
public int DepartmentId { get; private set; }

public void UpdateProfessionalDetails(
    string fullName,
    string? specialization,
    bool isGeneralPractitioner,
    decimal consultationFee,
    DateTimeOffset updatedAt,
    string? updatedBy);

public void ChangeDepartment(
    int departmentId,
    DateTimeOffset updatedAt,
    string? updatedBy);
```

- [ ] Write tests for normalized valid construction, initial Active/nondeleted state, and audit metadata. The empty owned schedule collection is introduced with DoctorSchedule in Task 3.
- [ ] Add theory cases for null/empty/whitespace DoctorCode and FullName.
- [ ] Test that a non-GP requires specialization while a GP accepts null; nonempty specialization is trimmed.
- [ ] Test rejection of negative consultation fee and nonpositive DepartmentId.
- [ ] Test successful professional and Department changes with audit metadata.
- [ ] Test invalid professional and Department changes preserve every previous value and audit field.
- [ ] Run `rtk proxy dotnet test ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj --no-restore --filter FullyQualifiedName~DoctorTests`; require compilation RED because Doctor/DoctorStatus do not exist.
- [ ] Add `DoctorStatus` with explicit values `Active = 0`, `OnLeave = 1`, `Inactive = 2`.
- [ ] Add the minimal Doctor construction and professional behavior. Validate into local values before assigning so failures are atomic.
- [ ] Run the focused tests; require GREEN.

### Task 2: Doctor lifecycle and soft deletion

**Files:**
- Modify first: `ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorTests.cs`
- Modify after RED: `ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs`

**Interfaces:**
- Consumes: Task 1 Doctor state.
- Produces: approved operational transitions and terminal soft deletion.

Approved lifecycle API:

```csharp
public void PlaceOnLeave(DateTimeOffset updatedAt, string? updatedBy);
public void ReturnToActive(DateTimeOffset updatedAt, string? updatedBy);
public void Deactivate(DateTimeOffset updatedAt, string? updatedBy);
public void MarkDeleted(DateTimeOffset deletedAt, string? deletedBy);
```

- [ ] Add tests for Active-to-OnLeave and OnLeave-to-Active with audit metadata.
- [ ] Add theory coverage showing Active and OnLeave can become Inactive.
- [ ] Add tests rejecting repeated or otherwise invalid transitions without replacing audit metadata.
- [ ] Add tests rejecting deletion from Active/OnLeave and accepting it from Inactive.
- [ ] Assert successful deletion sets `IsDeleted`, `DeletedAt`, `DeletedBy`, `UpdatedAt`, and `UpdatedBy` consistently.
- [ ] Test repeated deletion and all profile/Department/lifecycle mutations after deletion are rejected.
- [ ] Test profile and Department changes while Inactive are rejected.
- [ ] Run focused tests; require behavioral RED for missing lifecycle methods.
- [ ] Implement the minimal transition guards using `BusinessRuleException`; ensure mutation occurs only after all checks pass.
- [ ] Run focused tests; require GREEN.

### Task 3: Owned recurring schedules

**Files:**
- Create first: `ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorScheduleTests.cs`
- Modify first: `ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorTests.cs`
- Create after RED: `ElsheiekhHMS.Core/Domain/Staff/Entities/DoctorSchedule.cs`
- Modify after RED: `ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs`

**Interfaces:**
- Consumes: mutable Active/OnLeave Doctor from Tasks 1–2.
- Produces: owned read-only schedules and half-open overlap enforcement.

Approved schedule API:

```csharp
public IReadOnlyCollection<DoctorSchedule> Schedules { get; }

public DoctorSchedule AddSchedule(
    DayOfWeek dayOfWeek,
    TimeOnly startTime,
    TimeOnly endTime,
    int slotDurationMinutes,
    DateTimeOffset createdAt,
    string? createdBy);

public void RetireSchedule(
    DoctorSchedule schedule,
    DateTimeOffset updatedAt,
    string? updatedBy);
```

Approved child properties:

```csharp
public DayOfWeek DayOfWeek { get; private set; }
public TimeOnly StartTime { get; private set; }
public TimeOnly EndTime { get; private set; }
public int SlotDurationMinutes { get; private set; }
public bool IsActive { get; private set; }
```

- [ ] Test the initially empty collection plus valid schedule creation, creation audit metadata, ownership, and read-only exposure.
- [ ] Test equal/reversed time bounds and zero/negative/interval-exceeding slot duration.
- [ ] Test undefined weekday enum values, including unchanged schedules and audit metadata.
- [ ] Test same-day partial, containing, and exact overlap rejection.
- [ ] Test adjacent same-day and same-time different-day schedules are accepted.
- [ ] Test retirement preserves the child in the collection and records update metadata.
- [ ] Test repeated retirement and retirement by another Doctor are rejected without mutation.
- [ ] Test a retired interval no longer blocks a replacement.
- [ ] Test Inactive and deleted Doctors reject schedule additions/retirement; OnLeave remains administratively editable.
- [ ] Run focused Staff tests; require compilation/behavioral RED for missing schedule API.
- [ ] Implement sealed DoctorSchedule with an internal constructor and internal retirement behavior.
- [ ] Add a private Doctor list and a read-only wrapper. Use overlap rule `newStart < existing.EndTime && newEnd > existing.StartTime` for active schedules on the same day.
- [ ] Run focused Staff tests; require GREEN.
- [ ] Run the full suite with `--no-restore`; require all tests pass.

### Task 4: Verify architecture and refresh Graphify

**Files:**
- Regenerate: `graphify-out/graph.json`
- Regenerate: `graphify-out/graph.html`
- Regenerate: `graphify-out/GRAPH_REPORT.md`

**Interfaces:**
- Consumes: completed Phase 03B source/tests and `tools/graphify.ps1`.
- Produces: a fresh structural snapshot containing Doctor, DoctorSchedule, DoctorStatus, inheritance, ownership/test relationships.

- [ ] Run `rtk proxy dotnet restore ElsheiekhHMS.slnx`.
- [ ] Run `rtk proxy dotnet build ElsheiekhHMS.slnx --no-restore`; require zero warnings and errors.
- [ ] Run `rtk proxy dotnet test ElsheiekhHMS.slnx --no-build`; require zero failed and skipped tests.
- [ ] Verify Core has no project, framework, or package references and no EF Core, SQL Server, Identity, Blazor, Application, Infrastructure, or Web dependency.
- [ ] Confirm Department source and tests have no diff.
- [ ] Run `rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\graphify.ps1`.
- [ ] Run the same generator with `-Check`; require matching source and generator fingerprints.
- [ ] Verify graph nodes and edges for Doctor, DoctorSchedule, DoctorStatus, Doctor inheritance from SoftDeletableEntity, and Staff tests.
- [ ] Run `rtk proxy git diff --check`, `rtk proxy git diff --stat`, and `rtk proxy git status --short --untracked-files=all`.
- [ ] Confirm the diff contains only approved Phase 03B source/tests/documents and regenerated Graphify outputs.

No task stages, commits, or pushes files. Phase 03C remains unstarted.
