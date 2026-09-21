# Phase 03C Patient Foundation Design (Vitals Deferred)

**Status:** Phase 03C architectural decisions approved by the owner on 2026-09-21; implementation remains unapproved. The filename retains the original review identity; vitals implementation is explicitly outside 03C.

## Goal, sequence, and scope

Establish a durable, persistence-independent Patient master record with demographic, identifier, and contact data. Phase 03A Department and Phase 03B Doctor/DoctorSchedule are complete and unchanged. The [approved Phase 03B design](2026-09-20-phase03b-doctor-availability-design.md) named patient demographics, identifiers, and an *approved* vitals representation next. The 2026-09-21 owner decision resolves that representation: **no vital data or observation entity in 03C**; clinical observation design belongs to a later clinical batch. The earlier Phase 03 working sequence placed WalkInQueue and Appointment in 03D. Patient is needed before operational records can refer to a durable person. Phase 03 remains in progress.

The roadmap's single `DomainModels.cs`, Patient `Status` and latest-vitals fields, Identity navigation, and EF examples are older implementation sketches. The actual Core domain-oriented structure and these approved decisions govern this batch. The PRD's registration postcondition `AtReception` is interpreted as visit workflow state for a later operational record, **not** an attribute set on registration of the master Patient. No existing 03A/03B entity needs modification.

## Requirements and resolved decisions

| Requirement/decision | Evidence | 03C disposition |
|---|---|---|
| Registration, required first/last name, DOB, gender, phone, address; optional national ID, blood group, insurance, emergency contact | `PRD.md` P0-F001, lines 253-296 | Patient model; REQUIRED |
| Middle/third names, computed full and short names | `DEVELOPMENT_ROADMAP.md` lines 494-507; owner's name decision | Retain documented fields as optional; required first/last; Unicode permitted |
| Optional city and passport number | `PRD.md` data model line 802; SpecKit integrity standards lines 20-24 | Patient fields; blank passport normalizes to null |
| Hospital patient code `PT-YYYY-NNNNN` | PRD P0-F001; SpecKit integrity standards lines 64-69 | Patient owns immutable value/shape; Application later generates; persistence enforces uniqueness |
| National ID and passport uniqueness | SpecKit integrity standards lines 20-27 | Application validation and Phase 04 filtered unique indexes; not in Core |
| Duplicate phone: PRD warns in alternate flow but blocks in acceptance | `PRD.md` lines 284-291; owner's 2026-09-21 decision | Shared phones valid; optional Application warning/search later; **no phone unique index** |
| Patient soft deletion and durable clinical history | PRD P0-F001; SpecKit constitution II; data-integrity standards lines 40-59 | `SoftDeletableEntity`; no hard delete |
| Patient optimistic concurrency | PRD technical design line 616; ADR-008; SpecKit integrity standards lines 29-37 | Existing opt-in `IHasConcurrencyToken`; Phase 04 mapping/conflicts |
| Gender Male/Female/Other and eight blood groups | Roadmap lines 395-396, 689-690 | 03C enums; blood-group intermediate numeric order is chosen in this design |
| Visit state on Patient in older PRD/roadmap | PRD registration/queue flow and roadmap Patient table; owner's 2026-09-21 decision | **Excluded**; future Appointment/Encounter/Admission/BedAssignment owns its own workflow state |
| Vitals with timestamps | `PRD.md` P1-F005; owner's 2026-09-21 decision | **Deferred** to clinical/encounter observation design; no Patient latest-vitals fields |
| Clinical correction and deletion | Constitution II; owner's 2026-09-21 decision | Future observation design must preserve history and define entered-in-error/corrected/amended/superseded behavior |
| Emergency contact | PRD optional registration field; owner's 2026-09-21 decision | Three optional Patient-owned scalar fields; no separate aggregate |
| DOB not in future; age derived | PRD required DOB; owner's 2026-09-21 decision | `DateOnly`, required; caller supplies reference date for Core validation; no stored Age or approximate-date framework |

The owner decisions resolve the six prior design blockers. They supersede the PRD's duplicate-phone block and earlier Patient visit/vitals sketches for this 03C boundary. They do not silently change later application or clinical requirements.

## Patient model and invariants

`ElsheiekhHMS.Core.Domain.Patients.Entities.Patient` is master/demographic data and an independent aggregate root. It derives from `SoftDeletableEntity` because the PRD requires soft deletion. It opts into the existing `IHasConcurrencyToken` contract (`byte[] RowVersion { get; set; }`); simultaneous edits to the same demographics/identifiers must not silently overwrite one another. Core never produces or interprets token bytes. The existing interface's public setter and mutable byte array are retained unchanged; conflict detection, original-token handling, and SQL rowversion generation belong to Phase 04.

| Property group | Exact proposed properties | Rules |
|---|---|---|
| Identity | inherited `Id`; `PatientCode` | Code required, trimmed, `PT-YYYY-NNNNN` shape, unchanged after construction. It arrives from a trusted caller, never generated or globally checked by Core. |
| Names | `FirstName`, `MiddleName?`, `ThirdName?`, `LastName`; computed `FullName`, `ShortName` | First/last required, all trimmed; optional blanks become null. No ASCII/English-only regex, arbitrary minimum, or name uniqueness. Computed strings join nonempty parts with one space; age is not stored. |
| Demographics | `DateOfBirth: DateOnly`, `Gender: Gender`, `BloodGroup: BloodGroup?` | DOB required, not `DateOnly.MinValue`, and no later than a caller-supplied `asOfDate` on construction/update. Valid enum values only; optional blood group may be null. |
| Identifiers | `NationalId?`, `PassportNumber?` | Trim; empty/whitespace become null. Core does not query globally or claim uniqueness. |
| Contact | `Phone`, `Address`, `City?`, `EmergencyContactName?`, `EmergencyContactPhone?`, `EmergencyContactRelationship?` | Phone/address required and trimmed. Three emergency-contact strings are independently optional and trimmed/nullable. Shared phone numbers are valid. No unapproved phone-format restriction or contact aggregate. |
| Insurance | `InsuranceProvider?` | Trim; empty becomes null. No policy, payer, or billing aggregate. |
| Audit/deletion/concurrency | inherited `CreatedAt/By`, `UpdatedAt/By`, `IsDeleted`, `DeletedAt/By`; `RowVersion` | Supplied UTC event metadata. Later Application/Infrastructure create transactional AuditLog and handle the opaque token. |

`Gender` has `Male=0`, `Female=1`, `Other=2`. `BloodGroup` has `APositive=0`, `ANegative=1`, `BPositive=2`, `BNegative=3`, `ABPositive=4`, `ABNegative=5`, `OPositive=6`, `ONegative=7`. Values are domain vocabulary; persistence mappings are deferred. No `PatientStatus` enum is introduced for this batch.

### Construction, behavior and lifecycle

A public constructor receives code, demographic/identifier/contact data, `asOfDate`, and supplied creation actor/time. It validates all inputs before assigning fields. Read access is public; domain state has private setters. `RowVersion` follows the existing public contract. Mutation is grouped into `UpdateDemographics` (names, DOB, gender, blood group, reference date, audit actor/time), `UpdateContactDetails` (phone, address, city, emergency-contact fields, insurance, audit actor/time), and `UpdateIdentifiers` (national ID, passport, audit actor/time). Each validates candidate values before changing any state/audit field. The code is fixed. A corrected code needs a separate authorized identity-correction process, outside 03C.

`MarkDeleted` sets deletion and update metadata atomically and rejects repeated deletion. Deleted Patients reject all further demographic/contact/identifier edits. No reactivation, hard-delete method, or separate `IsActive` status is proposed. The reference date is supplied by a trusted caller, not read from a clock in Core. Reject a future DOB; do not guess an age or invent approximate/unknown-DOB rules when PRD requires a DOB. Input may contain Arabic or other Unicode names; only surrounding whitespace is trimmed. Do not normalize distinct people together using name, phone, DOB or emergency contact.

Registration -> authorized updates -> soft-deleted. These are master-record changes, not visit-state transitions. A Patient can have zero, one or many appointments, encounters and admissions over time; none is created or controlled by this aggregate in 03C.

## Relationships and historical integrity

| Source to target | Cardinality and requiredness | Ownership and history |
|---|---|---|
| Patient to Department/Doctor | No direct 03C relationship | Existing independent roots remain unchanged. Future operational records may refer to both Patient and Doctor. |
| Future Appointment/WalkInQueue/Encounter/Admission to Patient | Many historical/operational records may refer to one required Patient; a Patient may have none | Later aggregates own their status and history; Patient deletion must not erase related history. This design adds no collection/navigation. |
| Future clinical observations to future Encounter/Patient | Many timestamped records per episode/person, exact ownership to be designed | Vitals never overwrite earlier measurements or become mutable Patient master fields. Clinical correction/amendment and no destructive deletion require a later clinical design. |

```text
03A Department  1 ---- 0..* 03B Doctor ---- 0..* DoctorSchedule

03C Patient (durable identity, demographics, contacts)
      |
      +---- 0..* future Appointment / WalkInQueue (03D)
      +---- 0..* future Encounter / Admission
                    +---- 0..* future timestamped clinical observations
```

Inactive, deleted, cancelled, completed and historical have different meanings. For Patient this batch defines soft deletion only. Visit cancellation/completion and clinical correction are not Patient operations. Soft-deleted master data and later clinical/financial history remain subject to authorized audit/reporting; no cascade deletion is permitted for clinical relationships.

## Core versus Application and future work

| Rule | Owner |
|---|---|
| Required values, Unicode-friendly name handling, code shape, valid enums/DOB against supplied reference date, normalized optional strings, safe updates and deletion | Core intrinsic invariants |
| Code generation, duplicate national ID/passport check, optional phone-match warning, patient search, permissions, coordination with visits and corrections, transactionally recording audit actions | Application use cases in later phases |
| Unique indexes, nullable filtered identifiers, RowVersion mapping/conflict response, soft-delete filters, clinical relationships/no cascade, precision | Phase 04 persistence plus Application error handling |

Domain audit metadata does not implement the constitutional AuditLog. No EF Core, SQL Server configuration, Identity, repository, DTO, service, mapper, controller, Blazor or migration is introduced in Phase 03C. In particular, no vitals, Pulse, Weight, Height, Temperature, BloodPressure, `VitalObservation`, `ClinicalObservation`, generic unit framework or clinical correction API appears in Patient/Core during this batch.

## Controlled implementation after a separate approval

Only proposed production files:

- `ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs`
- `ElsheiekhHMS.Core/Domain/Patients/Enums/Gender.cs`
- `ElsheiekhHMS.Core/Domain/Patients/Enums/BloodGroup.cs`

Only proposed test file: `ElsheiekhHMS.Tests/Unit/Domain/Patients/PatientTests.cs`. Tests cover valid construction, code shape and immutability, required fields, optional normalization, Unicode names and computed names, enum validation, DOB future/default rejection with supplied date, shared phone acceptance, safe updates, deletion/audit metadata, post-delete rejection, and concurrency-contract presence. No database tests in 03C. Nothing in completed 03A/03B changes.

The accompanying plan breaks Patient construction/enums, mutation, and soft-delete/concurrency verification into test-first increments. After future approval, verify with `dotnet restore`, `dotnet build`, `dotnet test`, Git diff/status and `graphify update .` after code changes. No production/test file is created by this design decision task. Phase 03D and the later clinical-observation design require their own gates.
