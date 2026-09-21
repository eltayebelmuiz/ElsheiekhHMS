# Phase 03D — Appointment and Walk-in Queue Domain Design

**Status:** Approved blocker decisions incorporated and implemented as specified. This remains the single Phase 03D specification; persistence and later workflows are outside its scope.

## Scope and sources

Phase 03D proposes two independent scheduling/attendance roots in Core: `Appointment` and `WalkInQueueEntry`. Neither is an Encounter, consultation, admission, clinical record, or cross-aggregate query service. Patient remains durable identity; Department and Doctor remain unchanged. Sources: PRD, development roadmap, approved 03A–03C designs, owner's Phase 03D blocker decisions and `speckit/my-project/.specify/memory/data-integrity-standards.md`. The owner's required Patient decision supersedes the PRD's unregistered-name-only walk-in path. The PRD's six queue states and daily A-NNN ticket policy are explicit requirements.

## Shared boundaries and relationships

Both roots have a positive required `PatientId` and an immutable positive `DepartmentId` captured at creation. Appointment also requires a positive `DoctorId`; queue assignment has optional `DoctorId`, initially absent. Application verifies referenced records, active Department/Doctor and doctor-to-department fit when creating or assigning; Core cannot query them. Department deactivation/reassignment does not rewrite or invalidate historical operational records. No arbitrary department transfer or inverse collection on existing roots. Find or register a Patient **before** creating a walk-in entry. No copied name, phone or date of birth is stored on the entry.

```text
Department (03A) <-historical DepartmentId- Appointment (03D) -PatientId-> Patient (03C)
Doctor (03B)     <-DoctorId-----------/
Department (03A) <-historical DepartmentId- WalkInQueueEntry (03D) -PatientId-> Patient (03C)
Doctor (03B)     <-optional DoctorId--/

Future Encounter/clinical workflow owns treatment and observations.
```

Booking and walk-in queue are distinct. Appointment check-in does **not** automatically create a queue entry. A future unified waiting-room process would coordinate these in Application under a separate design.

## Time and identifiers

`ScheduledDate: DateOnly` and `ScheduledTime: TimeOnly` are the hospital-local **civil booking** in Appointment. They are not event timestamps or duplicate UTC representations. A configured hospital timezone outside Core interprets the booking, including invalid or ambiguous local times; Application enforces whether a new booking may be in the past. A future integration/persistence design may resolve an actual instant from these values and the configured zone without adding a competing Core scheduling property. Core rejects `DateOnly.MinValue`; `TimeOnly` is structurally valid by construction. There is no invented duration/end time, timezone service or `DateTime.Now` comparison in Core. Actual audit/action timestamps supplied to the entity are UTC `DateTimeOffset`, consistent with existing Core conventions and the data-integrity standards. Queue `QueueDate: DateOnly` is the hospital-local calendar date assigned by Application; `RegisteredAt`, `CalledAt?`, `CompletedAt?` and audit timestamps are UTC instants. Application establishes date/instant consistency. The hospital timezone is configured outside Core and never hard-coded.

`AppointmentCode` is a required trimmed identifier supplied by trusted Application, with no speculative format or Core generator. `WalkInQueueEntry` has `QueueNumber` in exact `A-NNN` form and corresponding `SequenceNumber` 1–999, supplied together by trusted Application; Core checks agreement and nondefault QueueDate. Ticket number is a display/reference value, not entity identity (`BaseEntity.Id`). One hospital-wide sequence per hospital-local calendar day resets at midnight; neither PRD nor standards defines a per-department partition. Application/persistence allocates the next number atomically, ensures uniqueness of `(QueueDate, SequenceNumber)` despite concurrent creation, and returns a controlled capacity result on exhaustion rather than wrapping. No Core `max + 1`, global/static counter, or caller-chosen authoritative ticket. No mutable queue position is stored.

## Appointment model

**Base and concurrency:** `SoftDeletableEntity, IHasConcurrencyToken`. Although `AuditableEntity` suffices for normal status history, the data-integrity standards explicitly require a Phase 04 global soft-delete filter for `Appointment`; this is the concrete exception to the owner's preferred base type. `RowVersion` follows the existing opaque interface and is mapped by Infrastructure in Phase 04. Same-row simultaneous cancellation/check-in or two status updates need optimistic concurrency; it does not prevent different new rows taking the same slot.

**Properties:** `AppointmentCode`, required `PatientId`, `DoctorId`, immutable `DepartmentId`, `ScheduledDate`, `ScheduledTime`, `Type: AppointmentType` (`General`, `Specialist`, `Emergency`, `FollowUp`, `LabTest`, `Radiology`), `Status: AppointmentStatus`, optional administrative `Notes`, `CancellationReason?`, `CancelledAt?`, inherited audit/deletion metadata, `RowVersion`. The roadmap's unqualified appointment Priority has no approved values or behavior and is deferred; do not invent an enum. No duration, end, UTC slot duplicate, navigation graph or consultation data.

**Construction/invariants:** Core validates nonblank normalized code, positive IDs, nondefault ScheduledDate, defined Type and UTC supplied creation timestamp before mutation; initial status is `Scheduled`. `TimeOnly` requires no extra range check. Status methods validate preconditions and UTC action/audit metadata before mutating. `CancelledAt`/reason describe cancellation only. No public status setter, reschedule, department reassignment or `MarkDeleted` in 03D. Authorized administrative correction/voiding is deferred.

**Lifecycle:**

```text
new -> Scheduled -> Confirmed
       Scheduled/Confirmed -> CheckedIn -> Completed
       Scheduled/Confirmed -> Cancelled
       Scheduled/Confirmed -> NoShow
```

Either Scheduled or Confirmed may become CheckedIn, Cancelled or NoShow; confirmation is optional before arrival. Explicit methods `Confirm`, `CheckIn`, `Complete`, `Cancel`, `MarkNoShow` enforce exactly these transitions. `Completed`, `Cancelled`, `NoShow` are terminal. CheckedIn means **only** that the patient arrived against the booking, never consultation, encounter start, treatment or admission. Completed means scheduling/attendance workflow finished, not clinical outcome. Application decides whether/when a booking is eligible for NoShow, who may act and any notifications. Roadmap `InProgress` is omitted because clinical progress would confuse this boundary; `CheckedIn` expresses approved administrative arrival. No automatic queue conversion.

Provider and Patient double booking, overlapping slots, working hours/leave, blocked times and department capacity require cross-record information and belong to Application/persistence. Exact overlap/slot-length policy is a later scheduling use-case decision, not an entity invariant. New bookings for inactive Departments are rejected by Application; existing bookings retain DepartmentId. Historical corrections and rescheduling require a separate design preserving prior information.

## WalkInQueueEntry model

**Base and concurrency:** `SoftDeletableEntity, IHasConcurrencyToken`. The data-integrity standards require a global soft-delete filter for `WalkInQueue`; this modeled entry is that persistence subject in Phase 04. Ordinary cancellation/completion do not set `IsDeleted`. Same-row call versus cancellation/hold and parallel staff updates justify an opt-in token. No EF mapping or token interpretation in Core.

**Properties:** required positive `PatientId`, immutable positive `DepartmentId`, optional positive `DoctorId` assigned for doctor routing, `QueueDate`, `QueueNumber`, `SequenceNumber`, `Priority: QueuePriority` (`Normal`, `Urgent`, `Emergency`), `Status: QueueStatus` (`Waiting`, `AtNurse`, `AtDoctor`, `Completed`, `Cancelled`, `OnHold`), `RegisteredAt`, `CalledAt?`, `CompletedAt?`, optional routing `Notes`, inherited audit/deletion metadata and `RowVersion`. Priority classifies operational urgency; it is not diagnosis/clinical triage. Do not duplicate demographics, store Position or embed vitals/Encounter state.

**Construction/invariants:** Core validates required positive IDs, ticket/sequence agreement, nondefault QueueDate, defined priority, UTC creation/registration timestamps; starts in Waiting. Trusted Application finds/registers Patient first and assigns ticket/date/priority. Application checks record existence, Department availability, duplicate active entries for the same Patient in the applicable queue, and ticket uniqueness. It determines ordering using status, priority, registered arrival and applicable department/queue; the entry itself cannot know other rows. Priority is recorded at creation; reprioritization is deferred until an authorized workflow is designed. Optional DoctorId is set only with routing to doctor; Application validates doctor and department. Methods validate before mutating status, timestamp, doctor assignment or audit metadata.

**Lifecycle:**

```text
Waiting -----> AtNurse -----> AtDoctor -----> Completed
    |                              ^
    +------------------------------+   (skip nurse)
    |
    +-----> OnHold -----> Waiting   (resume)

Waiting / AtNurse / AtDoctor / OnHold -----> Cancelled
```

Methods `CallToNurse` (Waiting), `SendToDoctor` (Waiting or AtNurse), `Hold` (Waiting), `Resume` (OnHold), `CompleteQueue` (AtDoctor), `Cancel` (any active state) enforce only these arrows. Terminal Completed and Cancelled reject further changes. `CalledAt` is the first nurse/doctor call and is not reset on hold/resume; `CompletedAt` is set when the queue episode ends through CompleteQueue. AtNurse and AtDoctor describe queue handoff/routing, **not** clinical treatment; Completed ends waiting/queue responsibility, **not** treatment, diagnosis, discharge or Encounter. Do not add Called, InService or NoShow states. Queue cancellation represents abandonment/removal from waiting, not deletion. Public displays must not reveal Patient identity or private routing notes without authorization.

## Core versus Application ownership

| Rule | Owner | Reason |
|---|---|---|
| Positive required PatientId and DepartmentId; positive required appointment DoctorId/optional queue DoctorId | Core | Structural identity shape. |
| Patient/Doctor existence, doctor-department fit | Application | Other roots must be loaded. |
| Intrinsic booking date/time and UTC action timestamp shape | Core | Entity-local validation; no wall-clock read. |
| Hospital timezone interpretation, DST ambiguity and new-booking-in-past policy | Application | Configured context and use-case policy. |
| Provider/patient collisions, overlap, hours, leave, blocked periods and capacity | Application | Queries and transaction-safe orchestration. |
| Current Department/Doctor availability for new records | Application | Current organizational status across aggregates. |
| Historical DepartmentId remains valid after deactivation | Core | Immutable captured identifier; no cascade transition. |
| Appointment lifecycle including nonclinical check-in | Core | One booking's allowed state changes. |
| Queue lifecycle and valid priority classification | Core | One entry's state and enum validity. |
| Duplicate active Patient entry in same applicable queue | Application | Requires other entries and persistence enforcement. |
| Queue ranking/position and priority ordering | Application | Collection-level policy/query. |
| Ticket shape and agreement with SequenceNumber | Core | Intrinsic local representation. |
| Next ticket, daily reset, uniqueness, overflow handling | Application | Hospital-local clock, concurrent rows and atomic allocation. |
| Same-row optimistic conflict mapping and soft-delete query filters | Phase 04 Infrastructure | Persistence configuration; Core opts in only. |
| Authorization, administrative corrections/voiding, exceptional soft deletion | Application (future) | Privilege and historical audit across records. |

## Deletion, history, deferrals and test boundary

Normal Cancelled/NoShow/Completed statuses preserve records and never trigger soft deletion. Both roots inherit deletion metadata because of the concrete SpecKit filter requirement, but Phase 03D exposes no routine deletion method or hard-delete behavior. Exceptional mistaken-record correction/voiding, authorization, audit history, post-terminal edits, rescheduling, department transfers, priority changes, slot duration/overlap, and a unified waiting room are separately designed later. Phase 04 configures filters and tokens; do not implement EF Core/SQL Server now. A filter hides **only explicitly soft-deleted** records, never legitimate cancelled/completed/no-show history.

After separate implementation approval, focused xUnit tests cover constructor invariants, every allowed/forbidden transition, terminal behavior, metadata consistency, historical DepartmentId retention, ticket shape and token opt-in. No unit test pretends to prove database uniqueness, collision prevention, timezone conversion, queue ordering, query filtering or concurrent persistence. No change to Department, Doctor, DoctorSchedule, Patient or other completed 03A–03C entities is required. The accompanying plan is `docs/superpowers/plans/2026-09-21-phase03d-appointment-walk-in-queue.md`.
