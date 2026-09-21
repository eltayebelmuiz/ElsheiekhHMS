# Phase 03D Appointment and Walk-in Queue Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved, independent Appointment and WalkInQueueEntry Core roots and focused domain tests without adding cross-record services or persistence.

**Architecture:** Each root keeps an immutable historical DepartmentId and required PatientId; both inherit SoftDeletableEntity because SpecKit requires soft-delete filters and opt into the existing opaque concurrency interface. Explicit state methods guard nonclinical attendance/queue transitions. Application later supplies local scheduling context, validates other roots, and allocates tickets; Phase 04 maps filters/tokens.

**Tech Stack:** .NET 10, C#, xUnit, PowerShell, Graphify.

**Spec:** `docs/superpowers/specs/2026-09-21-phase03d-appointment-walk-in-queue-design.md`

**Execution status:** Approved and executed. The checklist below records the implementation workflow and verification boundary; no future-phase work is authorized by this plan.

## Global constraints

- The original separate implementation-approval gate has been satisfied. Do not extend this plan into Phase 03E or Phase 04.
- Create only the four approved scheduling enums, two entities, and two focused test files listed below. Do not modify 03A–03C production or tests, project references, packages, or `IHasConcurrencyToken`.
- No Application services, repositories, EF Core/SQL Server, DbContext, migrations, Identity, UI, clinical Encounter, Phase 03E or Phase 04.
- Constructor and method timestamps are caller-supplied UTC `DateTimeOffset`; Core reads no clock or configured timezone. A local ScheduledDate/ScheduledTime is a civil booking, not an event timestamp. Core cannot establish uniqueness, actual referenced-row existence, scheduling conflict or queue position.
- Cancellation/completion/no-show are status changes; do not create `MarkDeleted`/hard-delete APIs for these records. Inherited soft-delete metadata is reserved for a separately designed correction workflow.
- Preserve working-tree changes. Do not stage/commit/push without request. After source changes and passing checks, run `graphify update .` and inspect generated diff.

## Files and shared API

Create `ElsheiekhHMS.Core/Domain/Scheduling/Enums/{AppointmentType,AppointmentStatus,QueuePriority,QueueStatus}.cs`, `ElsheiekhHMS.Core/Domain/Scheduling/Entities/{Appointment,WalkInQueueEntry}.cs`, and `ElsheiekhHMS.Tests/Unit/Domain/Scheduling/{AppointmentTests,WalkInQueueEntryTests}.cs`. Namespaces follow corresponding paths. Keep string normalization and exception conventions consistent with Patient/Doctor/Department (`DomainValidationException` for invalid values; `BusinessRuleException` for invalid state changes). All validation occurs before mutation. `RowVersion` is the existing `IHasConcurrencyToken` property (`byte[]`, initialized empty). Do not introduce a new concurrency abstraction.

Proposed public signatures to hold the design stable while writing tests:

```csharp
public Appointment(string appointmentCode, int patientId, int doctorId,
    int departmentId, DateOnly scheduledDate, TimeOnly scheduledTime,
    AppointmentType type, string? notes, DateTimeOffset createdAt, string? createdBy);
public void Confirm(DateTimeOffset updatedAt, string? updatedBy);
public void CheckIn(DateTimeOffset updatedAt, string? updatedBy);
public void Complete(DateTimeOffset updatedAt, string? updatedBy);
public void Cancel(string? reason, DateTimeOffset cancelledAt, string? cancelledBy);
public void MarkNoShow(DateTimeOffset updatedAt, string? updatedBy);

public WalkInQueueEntry(int patientId, int departmentId, DateOnly queueDate,
    int sequenceNumber, string queueNumber, QueuePriority priority,
    string? notes, DateTimeOffset registeredAt, string? createdBy);
public void CallToNurse(DateTimeOffset calledAt, string? updatedBy);
public void SendToDoctor(int doctorId, DateTimeOffset calledAt, string? updatedBy);
public void Hold(DateTimeOffset updatedAt, string? updatedBy);
public void Resume(DateTimeOffset updatedAt, string? updatedBy);
public void CompleteQueue(DateTimeOffset completedAt, string? updatedBy);
public void Cancel(DateTimeOffset updatedAt, string? updatedBy);
```

`Appointment` preserves `DepartmentId`, `PatientId`, `DoctorId`, date/time, code and type as read-only properties. `WalkInQueueEntry` preserves PatientId, DepartmentId, QueueDate, SequenceNumber, QueueNumber and Priority as read-only; DoctorId is private-set on routing only. All status setters are private. `CreatedAt` on queue equals `RegisteredAt`; its methods update inherited audit metadata. No automatic cross-aggregate creation. Ticket constructor values come only from a trusted later Application caller, never directly from a client.

## Review focus

- Every queue entry needs a positive PatientId, and neither root contains duplicated Patient demographic fields.
- ScheduledDate is a hospital-local civil date, not a Core UTC conversion; past policy, provider/patient collisions, working hours and DST remain outside Core.
- CheckedIn and AtNurse/AtDoctor have administrative routing meanings only. Completed status on either root has no clinical meaning.
- Both roots retain DepartmentId even after their state changes; no Department/Doctor/Patient edits are needed.
- Queue ticket matches `A-NNN`, sequence 1–999; no local next-number logic or global position. Unit tests cannot claim concurrent allocation correctness.
- Cancellation and completion never set IsDeleted. No routine deletion method is introduced. Each root implements IHasConcurrencyToken, but no EF mapping is written.

### Task 1: Queue construction, identity, ticket and enums

**Files:** Create `ElsheiekhHMS.Tests/Unit/Domain/Scheduling/WalkInQueueEntryTests.cs` first. After RED, create `QueueStatus.cs`, `QueuePriority.cs` and `WalkInQueueEntry.cs` under the approved Scheduling paths.

**Interfaces:** Constructor and immutable identity/ticket/priority properties above. `QueueStatus` contains exactly Waiting, AtNurse, AtDoctor, Completed, Cancelled, OnHold; `QueuePriority` exactly Normal, Urgent, Emergency. Use defined-enum checks. No Patient navigation or name/phone snapshot.

- [ ] **Step 1 — RED:** Add xUnit tests asserting construction starts Waiting, retains positive PatientId and DepartmentId, stores hospital-local QueueDate and supplied UTC RegisteredAt/CreatedAt, retains normalized optional routing Notes and Normal/Urgent/Emergency, starts without DoctorId or calls, and implements IHasConcurrencyToken. Theory cases reject zero/negative IDs, DateOnly.MinValue, sequence 0/1000, wrong prefix/padding or disagreement (e.g. sequence 7 with `A-008`), undefined priority, and non-UTC registeredAt. Assert `IsDeleted == false`.

```csharp
var entry = new WalkInQueueEntry(41, 3, new DateOnly(2026, 9, 21),
    7, "A-007", QueuePriority.Normal, null,
    new DateTimeOffset(2026, 9, 21, 7, 0, 0, TimeSpan.Zero), "desk-1");
Assert.Equal(QueueStatus.Waiting, entry.Status);
Assert.Equal(41, entry.PatientId);
Assert.Equal(3, entry.DepartmentId);
Assert.Equal("A-007", entry.QueueNumber);
Assert.False(entry.IsDeleted);
```

- [ ] **Step 2 — Verify RED:** Run `dotnet test ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj --filter FullyQualifiedName~WalkInQueueEntryTests` and confirm missing types/API cause the expected failure.
- [ ] **Step 3 — GREEN:** Define enums and minimal entity construction/validation. Require exact ordinal `A-` plus three ASCII digits and sequence equality; no ticket generation, other-record lookup, soft-delete method or mutation beyond required API. Preserve input state on validation failure.
- [ ] **Step 4 — Verify GREEN:** Run the same focused filter; review test output and fix only this slice.

### Task 2: Queue transition graph and metadata

**Files:** Extend `WalkInQueueEntryTests.cs` and `WalkInQueueEntry.cs` only.

**Interfaces:** Add CallToNurse, SendToDoctor, Hold, Resume, CompleteQueue, Cancel methods from shared API; all update audit metadata from supplied UTC timestamps. First call sets CalledAt once; CompleteQueue sets CompletedAt; no clinical event.

- [ ] **Step 1 — RED:** Test Waiting→AtNurse→AtDoctor→Completed; Waiting→AtDoctor; Waiting→OnHold→Waiting; cancellation from Waiting/AtNurse/AtDoctor/OnHold; terminal Completed/Cancelled rejection; forbidden hold from AtNurse and repeated call. Check DoctorId positive and only assigned when routing; CalledAt remains first call across nurse→doctor and hold/resume; CompletedAt only upon completion. Reject non-UTC action time and ensure status/audit/DoctorId/timestamps are unchanged on failure.

```csharp
entry.CallToNurse(callAt, "nurse-1");
entry.SendToDoctor(17, handoffAt, "desk-2");
entry.CompleteQueue(doneAt, "desk-2");
Assert.Equal(QueueStatus.Completed, entry.Status);
Assert.Equal(callAt, entry.CalledAt);
Assert.Equal(doneAt, entry.CompletedAt);
Assert.False(entry.IsDeleted);
```

- [ ] **Step 2 — Verify RED:** Run the focused queue test filter and observe the expected missing methods or failed transition assertions.
- [ ] **Step 3 — GREEN:** Implement exact arrows in the spec with guards before assignments. AtNurse/AtDoctor are routing stages; no new Called/InService/NoShow enum. Reject changes from terminal states.
- [ ] **Step 4 — Verify GREEN:** Run focused queue tests and inspect transitions for accidental treatment semantics or deleted-state changes.

### Task 3: Appointment construction, local civil slot and enums

**Files:** Create `ElsheiekhHMS.Tests/Unit/Domain/Scheduling/AppointmentTests.cs` first. After RED, create `AppointmentStatus.cs`, `AppointmentType.cs` and `Appointment.cs`.

**Interfaces:** Constructor and read-only properties above. Status exactly Scheduled, Confirmed, CheckedIn, Completed, Cancelled, NoShow. Type exactly General, Specialist, Emergency, FollowUp, LabTest, Radiology. No InProgress, appointment priority enum, UTC scheduled instant or duration.

- [ ] **Step 1 — RED:** Tests assert Scheduled construction, immutable historical DepartmentId, required PatientId/DoctorId, hospital-local date/time preserved without conversion, optional Notes, audit timestamp, `IsDeleted == false`, RowVersion interface. Theory cases reject blank code, nonpositive IDs, DateOnly.MinValue, undefined Type and non-UTC createdAt. Demonstrate an otherwise structurally valid past civil date is accepted in Core (Application owns policy).

```csharp
var booking = new Appointment("AP-41", 41, 17, 3,
    new DateOnly(2026, 9, 21), new TimeOnly(9, 30),
    AppointmentType.General, null,
    new DateTimeOffset(2026, 9, 21, 7, 0, 0, TimeSpan.Zero), "desk-1");
Assert.Equal(AppointmentStatus.Scheduled, booking.Status);
Assert.Equal(new TimeOnly(9, 30), booking.ScheduledTime);
Assert.Equal(3, booking.DepartmentId);
```

- [ ] **Step 2 — Verify RED:** Run `dotnet test ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj --filter FullyQualifiedName~AppointmentTests`; confirm missing types/API cause the expected failure.
- [ ] **Step 3 — GREEN:** Implement only constructor validation and properties. Do not read a clock, look up schedules or convert the local civil booking to UTC in Core.
- [ ] **Step 4 — Verify GREEN:** Run focused appointment tests and inspect constructor for partial initialization and unnecessary dependencies.

### Task 4: Appointment attendance lifecycle

**Files:** Extend `AppointmentTests.cs` and `Appointment.cs` only.

**Interfaces:** Add Confirm, CheckIn, Complete, Cancel and MarkNoShow from shared API. Cancel sets CancelledAt and normalized optional CancellationReason. No auto queue creation or clinical outcome.

- [ ] **Step 1 — RED:** Tests cover Scheduled→Confirmed; Scheduled/Confirmed→CheckedIn; CheckedIn→Completed; Scheduled/Confirmed→Cancelled or NoShow. Assert Completed/Cancelled/NoShow terminal rejection, CheckedIn cannot cancel or become NoShow under this design, CheckIn cannot mean Encounter state, invalid UTC timestamp does not partially change status/audit/reason, and `IsDeleted` stays false. Use a separate fresh instance for each allowed source and forbidden transition.

```csharp
booking.CheckIn(arrivedAt, "desk-1");
booking.Complete(finishedAt, "desk-2");
Assert.Equal(AppointmentStatus.Completed, booking.Status);
Assert.Equal(3, booking.DepartmentId);
Assert.False(booking.IsDeleted);
```

- [ ] **Step 2 — Verify RED:** Run focused appointment filter and confirm the expected method/behavior failures.
- [ ] **Step 3 — GREEN:** Enforce exactly the approved transition graph with one-entity guards and audit updates. No `InProgress`, rescheduling, MarkDeleted or generic state setter. Application later authorizes NoShow based on elapsed configured local slot.
- [ ] **Step 4 — Verify GREEN:** Rerun the appointment filter and review terminal behavior and historical Department preservation.

### Task 5: Verification and architecture review

**Files:** Review the eight new source/test files; do not create more files without revisiting the approved design.

- [ ] Run `dotnet restore ElsheiekhHMS.slnx`, `dotnet build ElsheiekhHMS.slnx --no-restore`, and `dotnet test ElsheiekhHMS.slnx --no-build` after the focused checks. Record actual counts/warnings; never assert a pass from static inspection.
- [ ] Check that Core still has no Infrastructure/Application/EF references; that Department, Doctor, DoctorSchedule and Patient are untouched; and that no appointment→queue auto-creation, ticket allocator, timezone service, cross-record collision lookup or clinical data slipped into Core.
- [ ] Run `graphify update .` after code changes; inspect `git diff`, `git status --short` and generated graph changes. Do not discard unrelated pre-existing changes.
- [ ] Report implementation result, test evidence, unresolved Application/Phase 04 work and any newly discovered conflicts. Stop on a conflict requiring a change to completed 03A–03C entities rather than modifying them in this plan.

## Execution record

The approved plan was executed on 2026-09-21 after the implementation gate was granted. The focused RED run failed because the Scheduling types did not yet exist; after the minimal implementation, the focused suite passed 38/38. `dotnet restore ElsheiekhHMS.slnx`, `dotnet build ElsheiekhHMS.slnx --no-restore`, and `dotnet test ElsheiekhHMS.slnx --no-build` then passed with 0 warnings, 0 errors, and 138/138 tests. Graphify was refreshed with `tools/graphify.ps1`. No completed 03A–03C source was modified and no Phase 03E/04 work was started.
