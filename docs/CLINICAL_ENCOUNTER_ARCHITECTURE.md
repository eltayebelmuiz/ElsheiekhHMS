# Clinical Encounter Architecture

Status: Phase 13C provider ownership foundation complete; no clinical production functionality is implemented.

## 1. Purpose

This document defines the minimum safe clinical boundary that can follow the frozen operational
baseline. It records evidence, proposed Encounter semantics, authorization prerequisites, and a
sequenced Phase 13 roadmap. Phase13C has implemented the approved provider ownership mapping and
resolution foundation; it does not authorize or implement an Encounter, clinical child records,
Encounter persistence mapping, migration, clinical service, validator, or UI.

Source code and project files are authoritative. Accepted source tests and approved specifications
follow them; historical planning examples in DEVELOPMENT_ROADMAP.md are not source evidence when
they describe models that are absent from the current checkout.

## 2. Current HMS baseline

The repository has completed Phase 12H and Phase13C. The backend and frontend baselines were
accepted and frozen before the clinical work; the current verified automated suite is 447 passing
tests.
The current operational path is:

Patient -> Appointment or walk-in Queue -> appointment arrival/check-in handoff -> Queue operations

The current system stops at operational queue history. It has no clinical Encounter or clinical
authoring workflow. Patient self-service and provider-scoped clinical authoring remain deferred.

## 3. Existing relevant domain inventory

- Patient (Core/Domain/Patients/Entities/Patient.cs) is a soft-deletable durable identity
  with database-generated Id, immutable PatientCode, demographics, contact and insurance
  values, audit metadata, and an opt-in RowVersion. The current source has no Height or Weight
  properties.
- Department is an auditable organizational record with Name, optional description and phone
  extension, and one-way active/deactivated state. It has no domain collection of clinical records.
- Doctor (Core/Domain/Staff/Entities/Doctor.cs) is a soft-deletable domain record with
  database-generated Id, DoctorCode, FullName, optional specialization, GP flag,
  consultation fee, DoctorStatus, required DepartmentId, schedules, and lifecycle methods.
  Doctor has no concurrency token.
- Appointment is a scheduling/attendance record with required PatientId, DoctorId,
  DepartmentId, civil ScheduledDate/ScheduledTime, type, notes, AppointmentCode, rowversion,
  and Scheduled, Confirmed, CheckedIn, Completed, Cancelled, and NoShow states.
- WalkInQueueEntry is operational waiting/routing history with required PatientId and
  DepartmentId, optional AppointmentId and DoctorId, queue date/ticket, priority, operational
  statuses, timestamps, rowversion, and a filtered unique AppointmentId link.
- Existing base types provide identity, audit metadata, soft deletion, and opt-in opaque
  concurrency. AuditLog is an Infrastructure append-only persistence record and is not a Core
  entity.

No Encounter, VitalSigns, Diagnosis, ClinicalNote, Prescription, Medication, LabOrder,
Radiology, or Billing domain type exists.

## 4. Existing Doctor/Provider model

Doctor is a genuine domain entity and has a stable domain identifier: Doctor.Id inherited from
BaseEntity. The current EF model maps Doctors, a required Restrict DepartmentId foreign key,
Doctor schedules, and Restrict relationships from Appointment.DoctorId and
WalkInQueueEntry.DoctorId.

Appointment.DoctorId is required and is validated by AppointmentService through
DoctorIsActiveInDepartmentAsync. Queue routing may assign an optional DoctorId at the AtDoctor
stage. These are operational scheduling/routing relationships.

The source still contains no Doctor.ApplicationUserId or ApplicationUser.DoctorId. Phase13C adds
an Infrastructure-owned DoctorApplicationUserLink mapping with unique DoctorId/UserId keys, a
bounded Application assignment/resolution contract, and ownership-aware provider resolution;
Identity remains unchanged and no Doctor/Provider CRUD service was introduced.
The canonical Provider role exists, and CanAccessClinicalRecords currently names Provider and
Patient, but that policy does not establish which Doctor a Provider owns. The current appointment
and queue services authorize Administrator/Receptionist operational access; they do not infer
provider ownership.

Historical roadmap examples that mention Doctor.ApplicationUser or a MedicalRecord are
superseded planning text and do not describe this checkout.

## 5. Current Appointment/Queue workflow

Appointment scheduling accepts Patient, Department, and Doctor identifiers, validates Patient
existence, active Department, active Doctor-in-Department, and future Africa/Kigali civil time,
then persists one Appointment and its audit event atomically. Appointment lifecycle completion
is independent of clinical completion.

Queue creation is explicit. Queue date is the current Africa/Kigali date; ticket allocation is
database-backed. Queue statuses are Waiting, AtNurse, AtDoctor, OnHold, Completed, and Cancelled.
Queue completion ends operational waiting responsibility and is not clinical care.

CheckInAndQueueAsync reuses AppointmentService and QueueService. It checks in an eligible
Appointment, creates or returns its durable Queue link idempotently, and never creates an
Encounter. Appointment-linked Queue rows copy PatientId and DepartmentId from the Appointment;
the filtered unique index permits at most one linked Queue row per Appointment. There is no
cross-service transaction and no clinical transition.

## 6. Definition of Encounter

An Encounter is a clinical care episode for one Patient, opened after the operational workflow
has handed the patient to a doctor. It is distinct from:

- Appointment, which records scheduling and attendance;
- Queue, which records waiting, routing, and operational completion; and
- AuditLog, which records who performed a mutation.

An Encounter should exist only through an explicit clinical start action. Appointment Check-In,
Appointment completion, Queue creation, and Queue completion do not create one.

## 7. Proposed Encounter lifecycle

The minimum lifecycle is intentionally small:

InProgress -> Completed

There is no initial clinical Cancelled state. An abandoned or administratively cancelled
Appointment/Queue remains its own operational history; a future amendment/abandonment decision
must be explicit.

Each transition must have an authenticated actor, server-owned UTC timestamp from TimeProvider,
an optimistic concurrency check, and an append-only audit event. Start requires a queue-eligible
operational context and no existing Encounter for that context. Complete requires an
InProgress Encounter and a current concurrency token. Completion does not silently perform
billing, orders, Appointment completion, or Queue completion.

## 8. Encounter provenance

Recommended authoritative provenance is one required QueueEntryId. It is the boundary that
unifies appointment arrivals and walk-ins:

- PatientId, DepartmentId, and effective DoctorId are copied from the queue/appointment context
  into immutable Encounter history at start.
- Appointment provenance is derived through the QueueEntry optional AppointmentId rather than
  duplicated as a second mutable relationship.
- QueueEntryId is unique on Encounter, enforcing one clinical episode per operational queue entry.
- A direct AppointmentId is not required initially; adding both keys would create redundant
  consistency rules. If a later reporting requirement proves a direct key necessary, it must be
  immutable and constrained to agree with the Queue link.

## 9. Patient relationship

Encounter should store a required PatientId even though Queue and Appointment already contain it.
The direct required foreign key makes clinical ownership, historical queries, and authorization
explicit and avoids joining through mutable operational records. It is populated from the
authoritative Queue context once and never changed. Restrict delete behavior and preserved
Patient history prevent orphaning.

## 10. Department relationship

Encounter should store a required DepartmentId as historical context, populated from the Queue
context at start. It must not dynamically follow a later Department change or Doctor transfer.
Restrict deletion preserves reports and historical clinical attribution.

## 11. Doctor relationship

Encounter should store a required DoctorId for the clinician responsible for the episode. For an
appointment-linked queue this is the Appointment DoctorId; for a walk-in it is the DoctorId
assigned before the queue reaches AtDoctor. A future start workflow must reject an unassigned
doctor and must verify the current provider ownership/authorization relationship before clinical
authoring. No multi-provider or reassignment model is proposed in the initial Encounter.

## 12. Appointment relationship

Encounter does not directly store AppointmentId in the initial model. Appointment provenance is
derived through the required QueueEntryId and its optional AppointmentId. This avoids duplicate
foreign keys and keeps a walk-in Encounter naturally appointment-free while preserving the
durable appointment-to-queue link.

## 13. Queue relationship

QueueEntryId is required, Restrict-delete, immutable provenance. A Queue entry may create at most
one Encounter. Encounter start is allowed only when the Queue entry is at the doctor stage
(AtDoctor) and has an effective DoctorId. Starting an Encounter does not mutate Queue status;
Queue remains operational state. A later explicitly approved workflow may complete Queue separately
or coordinate both writes transactionally, but the clinical record must not hide that transition.

## 14. Queue->Encounter workflow

The recommended flow is:

Appointment or walk-in
  -> Queue Waiting/AtNurse
  -> Queue AtDoctor with an effective DoctorId
  -> explicit Start Encounter by an authorized provider
  -> Encounter InProgress
  -> clinical authoring
  -> explicit Complete Encounter

Start is a new application workflow, not a change to the existing check-in handoff. It loads a
bounded Queue projection, verifies Patient/Department/Doctor context, verifies no existing
Encounter for QueueEntryId, verifies provider ownership, and creates exactly one Encounter plus
its audit event in one persistence transaction. It does not complete Queue or Appointment.
Retry after a duplicate/unique race returns the existing Encounter safely.

## 15. Encounter completion semantics

Complete means the clinical episode is closed for authoring. It sets CompletedAtUtc, retains
all approved clinical history, emits an audit event, and rejects later ordinary edits unless a
future amendment policy is approved. It does not complete the Appointment, complete the Queue,
bill the Patient, close orders, or create a discharge record.

## 16. Clinical data scope

Phase 13 should establish the Encounter lifecycle and provenance first. Child clinical records
must not be bundled speculatively.

| Capability | Phase 13 classification | Boundary |
|---|---|---|
| Encounter foundation | PHASE13 REQUIRED | Core lifecycle, provenance, concurrency, persistence, service, workflow |
| Vitals | PHASE13 OPTIONAL | Separate time-stamped observations only after fields and authoring are approved |
| Clinical notes | PHASE13 OPTIONAL | Minimal bounded note model may follow ownership decision; no SOAP framework |
| Diagnosis | DEFERRED | No coding system, multi-diagnosis, or authoring contract is approved |
| Prescription | DEFERRED | Pharmacy/medication catalog is separate scope |
| Lab | DEFERRED | No order/result workflow is approved |
| Radiology | DEFERRED | No imaging workflow is approved |
| Billing | DEFERRED | Encounter completion must not depend on financial workflow |

## 17. Vitals decision

No vital fields are present in current source. If Phase 13 later includes vitals, they should be
time-specific child observations with explicit units, a server/clinician observation timestamp,
bounded values, and their own author/concurrency rules. Patient profile demographics must not be
reinterpreted as clinical observations.

## 18. Clinical notes decision

The current source has only operational Appointment/Queue notes. A future minimal clinical note
may be a bounded, authored Encounter child or field, but its fields, maximum size, amendment
semantics, and author permissions require a separate approval. Do not introduce a SOAP,
templating, or document-storage framework.

## 19. Diagnosis decision

Diagnosis is outside the initial Encounter foundation. No free-text/coded decision, ICD/SNOMED
dependency, primary/secondary model, or post-completion edit policy is approved. A later design
must identify the clinical author and correction/amendment rules first.

## 20. Prescription decision

Prescription and medication catalog/dispensing are deferred. Encounter completion has no pharmacy
side effect.

## 21. Lab/Radiology decision

Lab and radiology orders/results are deferred. The Encounter boundary must remain usable without
either subsystem.

## 22. Billing decision

Billing is deferred. No financial fields or billing precondition belong on Encounter.

## 23. Provider identity/ownership analysis

A Doctor record is not an authenticated account. Phase13C now provides the approved
Infrastructure-owned `DoctorApplicationUserLink` mapping with one-to-one DoctorId/UserId keys,
administrator-controlled assignment/unassignment, Provider-role and active-account checks, and a
bounded Application resolution contract. This keeps Identity out of Core and leaves
`ApplicationUser` unchanged. Provider role membership alone still does not authorize clinical
authoring: future clinical operations must resolve the mapped active Doctor and perform resource
checks.

## 24. Authorization proposal

Use backend authorization and resource checks, not UI visibility:

- Provider accounts may view/start/edit/complete only Encounters owned by their approved Doctor
  mapping.
- Patient access remains deferred; no portal or self-service clinical access is added.
- Receptionist may see operational existence/status only if a separate bounded read contract is
  approved; Receptionist may not author notes, vitals, diagnosis, or complete clinical records.
- Administrator and SystemAdministrator retain administrative privileges but are not clinical
  superusers and may not author, sign, or act as a Doctor.
- Stable Identity UserId remains the audit actor. The clinical author is the mapped DoctorId;
  these identities must not be conflated.
- Provider role membership alone is insufficient; ownership resolution and resource checks are
  required for every future clinical operation.

## 25. Audit model

Encounter start, clinical updates, and completion should emit approved business audit events in
the same write transaction as the clinical mutation. AuditLog remains append-only and server
controlled. ActorUserId is supplied by the authenticated current-user abstraction; timestamps are
server supplied UTC. Clinical author attribution is a separate DoctorId on the Encounter/child
record. Do not place note text, diagnosis text, tokens, passwords, or other sensitive payloads in
routine logs or metadata. Read-access auditing remains a separately approved privacy requirement.

## 26. Concurrency proposal

Encounter requires an opaque RowVersion/optimistic concurrency token because completion and
clinical authoring are high-value concurrent writes. Every clinical child record that can be
independently edited should have its own token. Application mutation methods must require and
compare the expected token; persistence translates conflicts to bounded ServiceResult errors.
The unique QueueEntryId constraint is the cross-request duplicate-start guard; a database
constraint is required in addition to application checks.

## 27. Timezone/time semantics

All persisted Encounter timestamps are UTC DateTimeOffset values: CreatedAtUtc, StartedAtUtc,
CompletedAtUtc, and any observation times. Application services obtain current time only from
TimeProvider; domain methods receive timestamps and do not call DateTime.Now. Africa/Kigali is
the operational presentation timezone. Civil input, if any future clinical form accepts it, is
interpreted in Africa/Kigali, rejects invalid/ambiguous local times, and converts to UTC before
comparison/persistence. UI formats historical timestamps as Kigali with the UTC source retained.

## 28. Data integrity constraints

The later persistence design should include:

- required Restrict FKs to Patient, Department, Doctor, and QueueEntry;
- unique QueueEntryId (one Queue entry to at most one Encounter);
- immutable provenance values;
- RowVersion concurrency metadata;
- bounded lengths and required status/timestamps;
- indexes for PatientId, DoctorId, DepartmentId, QueueEntryId, Status, and StartedAtUtc;
- query filters only if a future approved soft-delete policy exists.

Do not add a direct Appointment FK, soft-delete filter, or child table until the corresponding
clinical semantics are approved.

## 29. Query requirements

All clinical list operations must be bounded and server-side with projection, filtering, sorting,
and pagination. Minimum filters are PatientId, DoctorId, DepartmentId, status, QueueEntryId, and
a UTC date/time range. If an operational code is later approved, it may be an indexed exact
lookup. Avoid loading complete notes or sensitive children in registry queries and avoid N+1
navigation materialization. Patient Details may later expose a read-only paged Encounter history.

## 30. UI information architecture

Do not add UI in Phase 13A. A later Web surface should be small:

1. Encounter registry: bounded filters, status, patient/doctor/department context.
2. Encounter details: immutable identity/provenance and authorized clinical summary.
3. Active Encounter workspace: start/complete actions plus only approved clinical sections.

The workspace must prominently show PatientCode, patient name, DOB or another approved
disambiguator, Department, Doctor, queue/appointment provenance, and current status. Never make
a public queue display a clinical surface and never rely on hidden UI controls for authorization.

## 31. Privacy/logging considerations

Clinical notes, diagnoses, vitals, prescriptions, and results are sensitive. Do not expose their
contents in exceptions, request logs, metrics, tracing tags, or AuditLog metadata. Log bounded
event identifiers, actor UserId, target EncounterId, reason where appropriate, and correlation
context. Clinical/read access auditing, retention duration, and production compliance validation
remain future decisions. No HIPAA, GDPR, Rwanda-regulation, FHIR, or HL7 compliance claim is made.

## 32. Deferred scope

Ownership-aware clinical resource policies, Patient self-service, rich vitals,
clinical notes, diagnosis/coding, prescriptions, lab/radiology, billing, encounter amendments,
read auditing, retention policy, Encounter code allocation, dashboards, notifications,
interoperability, admissions, and all other clinical modules remain deferred until separately
designed and approved.

## 33. Risks

- Without an ownership-aware resource check on each clinical operation, a Provider account could
  be incorrectly granted access to another clinician's records.
- Duplicating AppointmentId alongside QueueEntryId could create contradictory provenance.
- Treating Queue or Appointment completion as clinical completion would lose workflow boundaries.
- Ordinary soft deletion or unrestricted edits would damage clinical history.
- A database-only uniqueness check without a constraint would permit duplicate clinical starts.
- Vitals and notes without explicit author/time/unit semantics would create ambiguous clinical data.
- Historical roadmap examples contain obsolete ApplicationUser navigation and MedicalRecord/height/
  weight assumptions; implementation must follow current source.

## 34. Open decisions

These are non-blocking for Phase 13B domain-model review but must be closed before clinical
authoring:

- approve the exact Infrastructure Doctor<->ApplicationUser mapping and reassignment lifecycle;
- approve whether the first clinical extension is vitals, a minimal note, or neither;
- define amendment/correction policy for completed records;
- confirm whether an Encounter operational code is needed;
- confirm whether a future read-access audit is required;
- confirm Queue completion coordination after Encounter completion.

## 35. Recommended Phase13 implementation plan

- 13A — Discovery & Architecture: this document; no production change.
- 13B — Encounter Domain Model: Core Encounter invariants and tests only; no Identity/schema.
- 13C — Provider Ownership & Clinical Authorization: Infrastructure mapping, administrative
  assignment/unassignment, active-account resolution, ownership audit events, and migration are
  complete; clinical resource checks remain part of later clinical services.
- 13D — Encounter Persistence & Migration: DbSet/configuration/FKs/indexes/RowVersion and an
  additive migration after 13B/13C approval; verify isolated and development targets safely.
- 13E — Encounter Application Contracts & Validation: bounded DTOs, requests, validators,
  persistence port, and ServiceResult errors; no EF in Application.
- 13F — Encounter Service & Queue Start Workflow: explicit Start/Complete with one-save
  audit atomicity and idempotent duplicate handling.
- 13G — Clinical Extensions: only approved Vitals/Notes, each with clear author/time/
  concurrency semantics.
- 13H — Encounter UI: registry/details/workspace over approved Application contracts.
- 13I — Clinical Testing & Hardening: unit, integration, authorization, privacy, concurrency,
  performance, and reliability evidence.
- 13J — Final Clinical Acceptance: documentation, Graphify if structural changes require it,
  freeze, and checkpoint.

## 36. GO / NO-GO recommendation for Phase13B

GO for Phase13B domain-model design/implementation only. Encounter definition, Queue
provenance, lifecycle, boundaries, and initial clinical scope are sufficiently bounded. NO-GO
for clinical authoring or Start Encounter service implementation until Provider ownership and
resource authorization are explicitly approved and persisted. Phase13B must not add an
ApplicationUser relationship, schema, migration, UI, or clinical child records.

## Decision table

| Capability | Decision | Phase | Rationale |
|---|---|---|---|
| Encounter | Required minimal clinical episode | 13B-13F | Current operational flow needs a distinct clinical boundary |
| Vitals | Optional | 13G | No approved fields in current requirements/source |
| Clinical Notes | Optional | 13G | Needs bounded authoring and amendment decision |
| Diagnosis | Deferred | Later | Coding/authoring semantics absent |
| Prescription | Deferred | Later | Pharmacy is separate scope |
| Lab | Deferred | Later | No order/result contract |
| Radiology | Deferred | Later | No imaging contract |
| Billing | Deferred | Later | No financial coupling |
| Provider ownership | Required before clinical authoring | 13C | Mapping and ownership resolution foundation are present; clinical resource checks remain deferred |
| Patient self-service | Deferred | Later | No Patient<->ApplicationUser ownership |
| Encounter code | Not required initially | Later decision | Database Id is sufficient until operational need is proven |
| Queue->Encounter | Explicit Start from AtDoctor | 13F | Preserves Queue as operational history |
| Appointment linkage | Derived through QueueEntryId | 13B | Avoids redundant provenance and supports walk-ins |

## Proposed model summary

The proposed initial Encounter is a non-soft-deletable clinical record with:

- database-generated Id;
- required immutable PatientId, DepartmentId, DoctorId, QueueEntryId;
- InProgress/Completed status;
- CreatedAtUtc, StartedAtUtc, optional CompletedAtUtc;
- stable audit metadata and separate clinical author DoctorId semantics;
- RowVersion concurrency token.

The Encounter model remains a proposal only. Phase13C provider ownership code and EF mapping exist,
but no Encounter persistence mapping exists.

## Final discovery decision

There are no Critical architecture ambiguities blocking the completed Phase13B model or Phase13C
ownership foundation. Provider ownership is now available as a prerequisite, but authenticated
clinical authoring and Start Encounter remain blocked until Encounter persistence, resource checks,
and workflow contracts are separately approved. Phase13C does not add clinical workflow behavior.
