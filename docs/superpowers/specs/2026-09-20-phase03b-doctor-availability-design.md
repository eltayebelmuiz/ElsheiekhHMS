# Phase 03B Doctor and Availability Design

**Status:** Implemented and verified after approval on 2026-09-20.

## Confirmed sequence and scope

The approved Phase 03 working design defines this order:

- Phase 03A: Department foundation and focused tests — complete.
- Phase 03B: Doctor, recurring schedules, availability rules, and Identity-independent professional identity.
- Phase 03C: Patient demographics, identifiers, and the approved vitals representation.

Phase 03B follows Department because each Doctor has a current Department assignment. Doctor must exist before the later queue, appointment, clinical, laboratory, and billing batches can reference a healthcare professional. Phase 03B does not create those later records and does not implement Identity.

## Requirement traceability

| Requirement | Source | Classification |
|---|---|---|
| Model doctors as staff/healthcare professionals | `DEVELOPMENT_ROADMAP.md` module table and Phase 03 entity table; `PRD.md` Doctor persona and clinical workflows | REQUIRED |
| Doctor code | Roadmap Doctor field list | REQUIRED |
| Professional name stored independently of Identity | Doctor workflows require a stable professional identity; accepted architecture prohibits Identity implementation in Core | PROPOSED resolution of stale roadmap coupling |
| Specialization and general-practitioner indicator | Roadmap Doctor field list | REQUIRED |
| Consultation fee as `decimal` | Roadmap Doctor field list; PRD invoice includes consultation fees; constitution monetary rule | REQUIRED |
| `Active`, `OnLeave`, and `Inactive` statuses | Roadmap `DoctorStatus` enum | REQUIRED |
| Current Department assignment | Roadmap `DepartmentId`; completed Department foundation | REQUIRED |
| Weekly recurring schedules with day, start, end, and slot duration | Roadmap `DoctorSchedule` field list | REQUIRED |
| Inactive doctors cannot receive appointments | PRD edge case | REQUIRED; cross-aggregate enforcement belongs to Application |
| Doctor soft deletion | SpecKit data-integrity standards global-filter list; roadmap Doctor table | REQUIRED |
| Creation/update/deletion audit metadata | Constitution and Phase 02 bases | REQUIRED |
| Doctor-level optimistic concurrency | Earlier Phase 03 working design recommendation only | PROPOSED as unnecessary for this batch |
| Head-doctor assignment | Roadmap module/table only; no lifecycle or selection requirements | PROPOSED and deferred |
| Separate Specialty entity | Roadmap module label only; no catalog behavior or lifecycle requirements | PROPOSED and deferred |
| `ApplicationUser` navigation in Doctor | Stale roadmap implementation example | REJECTED for Phase 03B |

The PRD's seven roles describe authorization accounts. They do not require seven Core staff subclasses. Nurses, receptionists, lab technicians, pharmacists, and cashiers are outside this batch.

## Proposed entities

### Doctor

**Purpose:** An independently managed healthcare-professional record that remains meaningful without an authentication account.

**Base type:** `SoftDeletableEntity`. This is required because the mandatory data-integrity standards explicitly include Doctor in the global soft-delete set.

**Properties:**

- `DoctorCode`: required, trimmed professional identifier.
- `FullName`: required, trimmed professional display name. A single field avoids inventing an unsupported name-part scheme.
- `Specialization`: optional for a general practitioner; required for a non-GP doctor.
- `IsGeneralPractitioner`: identifies the roadmap's GP distinction.
- `ConsultationFee`: nonnegative `decimal`.
- `Status`: `DoctorStatus`, initially `Active`.
- `DepartmentId`: required positive identifier for the current Department.
- `Schedules`: read-only collection owned by Doctor.

**Construction:** A public constructor accepts all initial professional values, `DepartmentId`, `CreatedAt`, and `CreatedBy`. It normalizes strings, establishes `Active`, and validates the complete initial state. It does not accept an Identity user identifier.

**Setter strategy:** Scalar setters are private. The schedule collection is private and exposed read-only. Schedule construction and retirement occur through Doctor behavior.

**Behavior:**

- `UpdateProfessionalDetails` changes name, specialization/GP classification, and fee.
- `ChangeDepartment` changes the current required Department identity.
- `PlaceOnLeave` moves `Active -> OnLeave`.
- `ReturnToActive` moves `OnLeave -> Active`.
- `Deactivate` moves `Active|OnLeave -> Inactive`.
- `AddSchedule` creates a valid recurring schedule and rejects overlap with another active schedule for the same day.
- `RetireSchedule` closes an owned schedule without erasing it.
- `MarkDeleted` is allowed only after the Doctor is inactive and sets deletion metadata consistently.

All successful mutation records `UpdatedAt` and `UpdatedBy`. Failed operations leave domain and audit state unchanged. Inactive or deleted Doctors reject profile, Department, lifecycle, and schedule changes. Reactivating an inactive Doctor is not included because no requirement defines reinstatement.

### DoctorSchedule

**Purpose:** An owned weekly recurring availability interval. It describes when a Doctor normally accepts scheduling; it is not an appointment calendar, leave record, or guarantee that a particular slot is free.

**Base type:** `AuditableEntity`.

**Properties:**

- `DayOfWeek`: the BCL `System.DayOfWeek` value.
- `StartTime`: `TimeOnly`.
- `EndTime`: `TimeOnly`.
- `SlotDurationMinutes`: positive integer no longer than the interval.
- `IsActive`: initially true; false after retirement.

**Construction and setters:** Construction is controlled by Doctor. Setters are private. Phase 03B does not expose independent reassignment or deletion.

**Behavior:** The schedule validates its interval and can be retired once through Doctor. Changes use retire-and-replace so schedules already used to explain historical appointment availability are not silently rewritten.

## Relationships

| Relationship | Cardinality | Ownership | Required? | Lifecycle and history |
|---|---|---|---|---|
| Department to Doctor | One Department to many Doctors; one current Department per Doctor | Independent roots; Doctor stores `DepartmentId` | Required | Department deactivation does not delete or rewrite Doctor/history. Application verifies the target Department exists and is active when assigning. |
| Doctor to DoctorSchedule | One Doctor to many schedules | Doctor owns schedule creation and retirement | Optional collection | Retired schedules remain available for historical interpretation; they no longer participate in overlap checks. |
| Doctor to future account | No Phase 03B relationship | Phase 05 Identity design | Deferred | A future account may reference Doctor by identity from outside Core; Doctor does not depend on `ApplicationUser`. |
| Doctor to future appointments/queue/clinical records | One Doctor to many records conceptually | Independent later aggregate roots | Deferred but expected | Doctor deactivation/deletion must not erase historical records. |

No collection is added to Department. Head-doctor assignment is deferred, so Department requires no Phase 03B modification.

## Invariants and application rules

### Core invariants

- DoctorCode and FullName are nonempty after trimming.
- A non-GP Doctor has a nonempty Specialization; a GP may omit it.
- ConsultationFee is never negative.
- DepartmentId is positive.
- Doctor begins Active and follows only the approved status transitions.
- Deleted or inactive Doctors cannot be mutated.
- Deletion requires Inactive status and consistent deletion metadata.
- Schedule start is earlier than end.
- SlotDurationMinutes is positive and does not exceed the interval.
- Active schedule intervals for the same Doctor and day do not overlap; adjacent intervals are allowed.
- A schedule can be retired only once and only by its owning Doctor.
- Failed behavior preserves the last successful audit metadata.

### Application rules

- DoctorCode uniqueness across persisted Doctors.
- The assigned Department exists and is active.
- The caller is authorized to create, edit, deactivate, or delete Doctor records.
- An appointment may be booked only for an Active, nondeleted Doctor whose recurring schedule covers the proposed time.
- Appointment conflicts across persisted bookings.
- Transactional audit-log creation.
- Soft-delete filtering and authorized recovery.
- Any future account-to-Doctor uniqueness and account lifecycle coordination.

## Lifecycle and historical strategy

Doctor operational lifecycle:

```text
Active <-> OnLeave
Active ----> Inactive
OnLeave ---> Inactive
Inactive --> SoftDeleted
```

`OnLeave` is temporary and preserves recurring schedules for return. `Inactive` is retained, visible to authorized administration, and unavailable for new work. `IsDeleted` is a separate administrative suppression mechanism required by the data-integrity standards; it is not a substitute for leave or employment status.

DoctorSchedule lifecycle:

```text
Active -> Retired
```

Schedules are retired rather than overwritten or deleted. Later appointments remain independent historical records and must not cascade-delete when Doctor, Department, or a schedule becomes inactive.

## Auditing, deletion, and concurrency

Doctor uses inherited creation/update/deletion metadata. DoctorSchedule uses creation/update metadata. Callers supply UTC timestamps and opaque actor identifiers; Core does not depend on clocks, HTTP context, or Identity.

Doctor uses soft deletion because the data-integrity standards explicitly require it. DoctorSchedule does not use soft deletion because retirement is its complete business lifecycle.

Concurrency token decisions:

| Entity | Token | Reason |
|---|---|---|
| Doctor | NO | No documented same-record collision requires a token in this batch. Appointment booking conflicts and Department eligibility are cross-record Application/persistence concerns. Revisit if real concurrent profile/status editing is demonstrated. |
| DoctorSchedule | NO | It is owned and changed through Doctor; overlap and concurrent booking guarantees require Phase 04 constraints/transactions rather than a speculative child token. |

No EF `RowVersion` configuration is part of Phase 03B.

## Enums

| Enum | Values | Used by | Source | Classification |
|---|---|---|---|---|
| `DoctorStatus` | `Active`, `OnLeave`, `Inactive` | Doctor | Development roadmap | REQUIRED |

`System.DayOfWeek` is reused for schedules. No `Shift`, `Specialty`, `UserRole`, or availability enum is justified in this batch.

## Proposed folder structure

```text
ElsheiekhHMS.Core/
└── Domain/
    ├── Organization/
    │   └── Entities/
    │       └── Department.cs          existing, unchanged
    └── Staff/
        ├── Entities/
        │   ├── Doctor.cs
        │   └── DoctorSchedule.cs
        └── Enums/
            └── DoctorStatus.cs

ElsheiekhHMS.Tests/
└── Unit/
    └── Domain/
        └── Staff/
            ├── DoctorTests.cs
            └── DoctorScheduleTests.cs
```

Only these folders are created with their first files. No empty Specialty, Identity, repository, configuration, or persistence folders are introduced.

## Unit-test design

### Doctor tests

- valid construction normalizes fields, starts Active, records creation audit metadata, and exposes no schedules;
- null/empty/whitespace DoctorCode and FullName are rejected;
- specialist without Specialization is rejected; GP may omit it;
- negative fee and nonpositive DepartmentId are rejected without mutation;
- professional details and Department changes update audit metadata;
- invalid updates preserve all fields and prior audit metadata;
- Active can enter OnLeave; OnLeave can return to Active;
- Active or OnLeave can become Inactive;
- repeated/invalid status transitions are rejected without mutation;
- inactive and deleted Doctors reject profile, Department, and schedule changes;
- deletion is rejected while Active/OnLeave, succeeds while Inactive, and cannot repeat;
- deletion metadata remains internally consistent.

### DoctorSchedule tests

- Doctor adds a valid schedule with creation audit metadata;
- start equal to/after end is rejected;
- zero, negative, or interval-exceeding slot duration is rejected;
- same-day overlapping active interval is rejected;
- adjacent and different-day schedules are accepted;
- retiring an owned schedule records update metadata and preserves the schedule;
- repeated retirement and retirement by another Doctor are rejected;
- a replacement interval can be added after the conflicting schedule is retired.

No tests simulate EF concurrency, database uniqueness, Identity, authorization, or appointment queries.

## Impact on Department

None. Department remains `AuditableEntity` with its approved properties and Active-to-Inactive lifecycle. Doctor stores the current `DepartmentId`; Department receives no Doctor collection or head-doctor field in Phase 03B.

## Deliberate deferrals

- Head-doctor assignment awaits concrete selection and lifecycle requirements.
- A managed Specialty catalog awaits evidence that free-text specialization is insufficient.
- Account linkage belongs to Phase 05 Identity design.
- Date-specific availability exceptions, holidays, timezone boundaries, and booked-slot conflict detection belong with the Appointment/Application design.
- EF relationships, foreign keys, query filters, indexes, and transactions belong to Phase 04.

These deferrals do not block the proposed Phase 03B implementation.

## Risks

- Copying the roadmap's `ApplicationUser` navigation would couple Core to Phase 05 Identity.
- Treating `Inactive` and `IsDeleted` as synonyms would make lifecycle behavior ambiguous.
- Treating recurring schedules as booked calendars would put cross-record queries inside Core.
- Updating schedule rows in place could obscure which availability rules existed for older bookings.
- Doctor tokens alone would not prevent overlapping new appointments.
- A single FullName may later need structured naming for search/reporting; no current requirement defines Doctor name parts.

## Proposed implementation order

1. Write failing Doctor tests for construction, normalization, fee/Department validation, and required specialization behavior.
2. Add `DoctorStatus` and the minimal Doctor model to pass those tests.
3. Write failing lifecycle and soft-deletion tests, then add the approved Doctor behavior.
4. Write failing schedule interval, ownership, overlap, and retirement tests.
5. Add DoctorSchedule and Doctor-owned schedule behavior.
6. Run focused tests, then restore, build, and the full test suite.
7. Audit Core dependencies, confirm Department is unchanged, regenerate the custom Graphify snapshot, and verify freshness.

Implementation must use test-first development and follow the approved detailed implementation plan.

## Files proposed for implementation

- `ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs`
- `ElsheiekhHMS.Core/Domain/Staff/Entities/DoctorSchedule.cs`
- `ElsheiekhHMS.Core/Domain/Staff/Enums/DoctorStatus.cs`
- `ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorTests.cs`
- `ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorScheduleTests.cs`

The implementation plan, after design approval, would be written to `docs/superpowers/plans/2026-09-20-phase03b-doctor-availability.md`.
