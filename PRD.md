# ElsheiekhHMS — Product Requirements Document

---

## Section 1 — Project Identity

| Field | Value |
|-------|-------|
| **Project Name** | Elshiekh Hospital Management System |
| **Project ID** | PRD-001 |
| **Version** | v1.0 |
| **Status** | Draft |
| **Priority** | High |
| **Created Date** | September 20, 2026 |
| **Last Updated** | September 20, 2026 |
| **Owner / PM** | Eltayeb Elmuiz |
| **Team Members** | Backend Dev / Frontend Dev / QA / Designer |
| **Tech Stack** | .NET 10 · ASP.NET Core 10 · Blazor Interactive Server · EF Core 10 · SQL Server · ASP.NET Identity |
| **Repository** | [TBD] |
| **Git Branch Prefix** | NNN-feature-name |
| **PRD File Path** | PRD.md |

> **Implementation baseline note:** This Draft PRD describes the broader product
> vision and future modules. The approved repository baseline currently targets
> .NET 10/ASP.NET Core 10/EF Core 10 and implements the five canonical roles
> `SystemAdministrator`, `Administrator`, `Receptionist`, `Provider`, and
> `Patient`. Additional roles and clinical, billing, laboratory, pharmacy,
> inpatient, notification, and portal requirements remain future product scope.

---

## Section 2 — Problem & Purpose

### Problem Statement
Hospital staff at EL-Shiekh Medical Complex — and hospitals broadly — currently manage patients, queues, lab results, and billing manually or across disconnected, incompatible systems. This causes lost patient records, excessive wait times, billing errors, and zero real-time visibility for management into daily operations.

### Project Purpose
ElsheiekhHMS is a unified, web-based Hospital Management System designed to digitize the complete patient journey — from walk-in registration through clinical consultation, laboratory, pharmacy, billing, and discharge — while enforcing role-based access and maintaining a full, tamper-proof activity trail of every action performed in the system.

### Business Value
- Eliminates paper-based patient registration and manual queue management
- Reduces average patient wait time by surfacing real-time queue status to all staff
- Prevents lost lab results through end-to-end order tracking
- Replaces spreadsheet-based billing with structured, auditable invoice generation
- Gives hospital management a real-time operational dashboard
- Provides complete accountability: every action is logged with who did it, when, and what changed

### Opportunity
The system is architected to be hospital-agnostic — any hospital running Windows Server on a LAN can deploy and operate it with minimal IT overhead. This positions ElsheiekhHMS as a reusable foundation for multiple hospital deployments beyond EL-Shiekh Medical Complex.

---

## Section 3 — Goals & Objectives

### Primary Goal
Deliver a working HMS that allows hospital staff to register patients, manage the walk-in queue, record clinical notes, process lab orders, and generate invoices — all from one unified system — with every action fully tracked by user and timestamp.

### Objectives

1. Reduce patient registration time to under 3 minutes per patient
2. Eliminate all paper-based queue management — 100% of daily queues managed digitally
3. Ensure zero lost lab results — all orders tracked from creation to result delivery
4. Generate 100% of invoices inside the system — no spreadsheet billing
5. Log 100% of system actions with user identity, timestamp, and before/after data snapshot
6. Provide management with a real-time dashboard with less than 30 seconds data lag

### Success Definition
The system is considered successfully delivered when all P0 features are live, all 7 roles can perform their core workflows without workarounds, and the audit trail captures every create, update, and delete action across all modules.

### Non-Goals
- Building a mobile application
- Implementing Arabic UI or RTL layout
- Integrating with any national health information system
- Providing telemedicine or remote consultation features
- Building a patient-facing self-service portal

---

## Section 4 — Scope

### In Scope
- Patient registration, search, demographic management, and vitals tracking
- Walk-in queue management with real-time staff board and patient-facing display screen
- Appointment booking, confirmation, check-in, and cancellation
- Role-based access control across 7 staff roles
- Electronic Medical Records (EMR) — diagnosis, clinical notes, treatment plans
- Laboratory module — test ordering, sample collection, result entry, abnormal flagging
- Billing module — invoice creation, line items, partial payments, receipts
- Pharmacy module — prescription dispensing and medication stock management
- In-app notification system with bell indicator and unread count
- Real-time management dashboard with daily KPIs
- Ward and bed management for inpatient admissions
- Patient vitals tracking (weight, height, temperature, blood pressure)
- Full system activity audit trail — every action logged with user, timestamp, and data diff

### Out of Scope
- Native mobile application (iOS or Android)
- Arabic UI or right-to-left layout
- Patient self-service portal
- Telemedicine or video consultation
- Integration with national or regional health information systems
- AI-assisted diagnosis or clinical decision support
- External SMS or email notification delivery

### Assumptions
- The system runs on a hospital LAN — internet connectivity is not required for core operation
- All users access the system via a modern desktop browser (Chrome, Edge, Firefox)
- The hospital server runs Windows Server with IIS and SQL Server Express
- Each staff member has a unique login account assigned by the Admin
- Patient records are never physically deleted — soft-delete only

### Constraints
- SQL Server Express edition — 10 GB database limit is sufficient for v1
- No cloud dependency — system must be fully operational offline on the hospital LAN
- Budget favors free or included Microsoft stack components
- Must be trainable by non-technical hospital staff within 2 hours per role

### Dependencies
- Windows Server 2019/2022 available for production deployment
- SQL Server Express 2019/2022 installed on the server
- ASP.NET Core 10 Runtime installed on the server
- IIS 10 configured for the application
- Hospital LAN with stable Ethernet connectivity between workstations and server

---

## Section 5 — Users & Personas

### Primary Users
All hospital staff who interact with the system daily across 7 defined roles.

### Secondary Users
Hospital management and administrators who monitor system activity, generate reports, and manage user accounts.

---

### Persona 1 — Receptionist

| Field | Detail |
|-------|--------|
| **Name** | Hawa |
| **Role** | Receptionist |
| **Goal** | Register patients quickly, issue queue tickets, and book appointments with zero paperwork |
| **Pain Point** | Writes patient information by hand, loses records, has no visibility into queue status or how many patients are waiting |
| **Tech Level** | Low to Medium |
| **Frequency** | Daily — primary user of the system throughout the working day |
| **Success** | Patient registered and added to queue in under 3 minutes with no paper involved |

---

### Persona 2 — Doctor

| Field | Detail |
|-------|--------|
| **Name** | Dr. Omar |
| **Role** | Doctor |
| **Goal** | View own patient queue, record clinical notes, order lab tests, and write prescriptions from one screen |
| **Pain Point** | Currently receives patient information verbally or on paper — no access to history, previous diagnoses, or lab results during consultation |
| **Tech Level** | Medium |
| **Frequency** | Daily — uses EMR and queue modules during every consultation |
| **Success** | Full patient history accessible before the patient enters the room; clinical record completed without leaving the application |

---

### Persona 3 — Nurse

| Field | Detail |
|-------|--------|
| **Name** | Fatima |
| **Role** | Nurse |
| **Goal** | Record patient vitals and advance queue status efficiently |
| **Pain Point** | Vitals recorded on paper and not linked to the patient record; no digital queue to know who is next |
| **Tech Level** | Low |
| **Frequency** | Daily — manages vitals and queue throughout the shift |
| **Success** | Vitals entered digitally and queue updated in under 60 seconds per patient |

---

### Persona 4 — Lab Technician

| Field | Detail |
|-------|--------|
| **Name** | Khalid |
| **Role** | Lab Technician |
| **Goal** | See ordered tests, update status through workflow stages, and enter results |
| **Pain Point** | Lab orders arrive on paper — results are handwritten and sometimes lost before reaching the doctor |
| **Tech Level** | Medium |
| **Frequency** | Daily — primary user of the laboratory module |
| **Success** | All lab orders visible in the system; results entered digitally and immediately visible to the ordering doctor |

---

### Persona 5 — Admin

| Field | Detail |
|-------|--------|
| **Name** | Eltayeb |
| **Role** | System Administrator |
| **Goal** | Manage all user accounts, monitor system activity, and access the full audit trail |
| **Pain Point** | No way to know who changed a patient record, who cancelled an appointment, or who accessed sensitive data |
| **Tech Level** | High |
| **Frequency** | Daily — monitors activity log and manages system configuration |
| **Success** | Every system action traceable by user, timestamp, and data change — zero unaccountable modifications |

---

## Section 6 — MoSCoW Feature Prioritization

### Must Have — P0

| ID | Feature | Status | Description | Assigned To | Sprint |
|----|---------|--------|-------------|-------------|--------|
| P0-F001 | Patient Registration & Search | TODO | Register new patients, generate unique PatientCode, search by name/code/phone/national ID | Backend Dev | 1 |
| P0-F002 | Walk-in Queue Management | TODO | Add patients to daily queue, advance through Waiting → Nurse → Doctor → Completed, hold/resume/cancel | Backend Dev | 1 |
| P0-F003 | Patient-Facing Queue Display Board | TODO | Full-screen public display showing Now Serving, ticket grid, stats, and scrolling ticker — no login required | Frontend Dev | 1 |
| P0-F004 | Appointment Booking & Management | TODO | Book, confirm, check-in, complete, and cancel appointments with conflict prevention | Backend Dev | 2 |
| P0-F005 | Role-Based Access Control | TODO | 7 roles: Admin, Doctor, Nurse, Receptionist, LabTechnician, Pharmacist, Cashier — enforced server-side | Backend Dev | 1 |
| P0-F006 | Electronic Medical Records (EMR) | TODO | Doctor creates clinical record with diagnosis, notes, treatment plan, and follow-up date | Backend Dev | 2 |
| P0-F007 | Laboratory Orders & Results | TODO | Order tests, track through workflow stages, enter results with abnormal flagging | Backend Dev | 2 |
| P0-F008 | Billing & Payment Receipts | TODO | Create invoices with line items, receive payments, issue receipts, track outstanding balance | Backend Dev | 3 |
| P0-F009 | Full System Activity Audit Trail | TODO | Log every create, update, delete, login, and permission change with user identity, timestamp, and before/after data snapshot | Backend Dev | 1 |

### Should Have — P1

| ID | Feature | Status | Description | Assigned To | Sprint |
|----|---------|--------|-------------|-------------|--------|
| P1-F001 | Pharmacy & Stock Management | TODO | Dispense prescriptions, deduct stock, track batches and expiry, reorder alerts | Backend Dev | 3 |
| P1-F002 | In-App Notification System | TODO | Bell icon with unread count, dropdown of last 10 notifications, mark read, poll every 30 seconds | Frontend Dev | 3 |
| P1-F003 | Real-Time Management Dashboard | TODO | KPIs: patients today, queue status, revenue today, pending labs, low stock — refreshes every 30 seconds | Frontend Dev | 3 |
| P1-F004 | Ward & Bed Management | TODO | Admit patients to ward/room/bed, track bed status, discharge, prevent double-assignment | Backend Dev | 4 |
| P1-F005 | Patient Vitals Tracking | TODO | Nurse records weight, height, temperature, blood pressure — stored in patient record with timestamp | Backend Dev | 1 |

### Could Have — P2

| ID | Feature | Status | Description | Assigned To | Sprint |
|----|---------|--------|-------------|-------------|--------|
| P2-F001 | Advanced Reporting | TODO | Daily revenue, doctor workload, lab activity, bed occupancy reports | Backend Dev | 5 |
| P2-F002 | Audit Log Admin UI | TODO | Filterable audit log page for Admin — by user, entity, date range | Frontend Dev | 5 |
| P2-F003 | User Management Panel | TODO | Admin UI to create, edit, deactivate staff accounts and reset passwords | Frontend Dev | 4 |
| P2-F004 | Print / Export | TODO | Print patient card, receipt, lab result PDF | Frontend Dev | 5 |
| P2-F005 | Dark Mode | TODO | System-wide dark theme toggle | Frontend Dev | 5 |

### Won't Have — P3

| ID | Feature | Status | Description | Assigned To | Sprint |
|----|---------|--------|-------------|-------------|--------|
| P3-F001 | Native Mobile App | SKIPPED | iOS / Android application — deferred to v2 | — | — |
| P3-F002 | Arabic UI / RTL | SKIPPED | Right-to-left Arabic interface — deferred to v2 | — | — |
| P3-F003 | Patient Self-Service Portal | SKIPPED | Patient-facing web portal — deferred to v2 | — | — |
| P3-F004 | Telemedicine | SKIPPED | Video consultation — deferred to v2 | — | — |
| P3-F005 | National Health Integration | SKIPPED | Integration with Sudan national health system — deferred to v2 | — | — |
| P3-F006 | AI-Assisted Diagnosis | SKIPPED | Clinical decision support — deferred to v2 | — | — |

---

## Section 7 — Functional Requirements

---

### P0-F001 — Patient Registration & Search

**Description:** Allows receptionists and admins to register new patients with full demographic information. The system auto-generates a unique PatientCode. All patients are searchable by name, code, phone, and national ID.

**User Story:**
As a Receptionist, I want to register a new patient quickly and receive a unique patient code, so that all subsequent visits and records are linked to one identity.

**Trigger:** Receptionist clicks "Register Patient" from the Patients page.

**Pre-conditions:**
- User is authenticated as Receptionist or Admin
- Duplicate-candidate review may use phone or national ID; phone is not globally unique

**Post-conditions:**
- Patient record created with unique PatientCode (format: PT-YYYY-NNNNN)
- Patient status set to AtReception
- Audit log entry created: Action=CREATE, Entity=Patient, User=receptionist, Timestamp=UTC

**Main Flow:**
1. Receptionist opens Patients → clicks Register Patient
2. Form loads with required fields: First Name, Last Name, Date of Birth, Gender, Phone, Address
3. Receptionist fills all required fields and optional fields (National ID, Blood Group, Insurance, Emergency Contact)
4. Receptionist clicks Save
5. System validates all inputs server-side
6. System checks duplicate-candidate signals; phone does not enforce uniqueness
7. System generates PatientCode using format PT-{YEAR}-{5-digit-sequence}
8. Patient record saved to database
9. System redirects to Patient Details page showing the new PatientCode
10. Success toast notification displayed: "Patient registered successfully — PT-2026-00001"
11. Audit trail entry written

**Alternate Flows:**
- If phone number already exists: system may display a non-blocking duplicate-candidate warning — "A patient with this phone number already exists. View existing record?"
- If national ID already exists: system blocks save and shows error — "National ID already registered to another patient."
- If required field is missing: inline validation error shown before form submission

**Acceptance Criteria:**
- [ ] PatientCode is unique, auto-generated, and follows PT-YYYY-NNNNN format
- [ ] Duplicate national ID is detected and blocked; duplicate phone numbers remain allowed and may be used as duplicate-candidate search signals
- [ ] Patient record is soft-deletable only — no hard delete endpoint exists
- [ ] Audit trail entry created on every successful registration
- [ ] Search returns results within 500ms for up to 100,000 patient records
- [ ] Patient list supports pagination: 25 / 50 / 100 / 250 rows per page
- [ ] Search works across: full name, PatientCode, phone number, national ID

---

### P0-F002 — Walk-in Queue Management

**Description:** Manages the daily patient queue digitally. Replaces paper numbering. Receptionists add patients, nurses and doctors advance status, and any authorized staff can cancel or hold entries.

**User Story:**
As a Nurse, I want to call the next waiting patient digitally so I know exactly who to see next without asking at the reception desk.

**Trigger:** Receptionist clicks "Add to Queue" or Nurse clicks "Call Next."

**Pre-conditions:**
- User is authenticated with queue-relevant role
- For registered patients: patient exists in the system
- For walk-ins: at minimum a name is provided

**Post-conditions:**
- Queue entry created with unique ticket number (format: A-001)
- Queue date set to today — ticket sequence resets at midnight
- Status transitions logged in audit trail

**Main Flow:**
1. Receptionist opens Walk-in Queue → clicks Add to Queue
2. Receptionist selects registered patient via enhanced search or enters walk-in name + phone
3. Receptionist sets priority (Normal / Urgent / Emergency) and optional chief complaint note
4. System assigns next sequential ticket number for today (A-001, A-002…)
5. Entry appears on queue board with status: Waiting
6. Nurse opens queue board → clicks "Call to Nurse" on the next Waiting entry
7. Status changes to AtNurse — nurse records vitals
8. Doctor opens queue board → clicks "Send to Doctor"
9. Status changes to AtDoctor — doctor begins consultation
10. Doctor clicks "Done" → status changes to Completed
11. All status changes written to audit trail

**Alternate Flows:**
- Hold: nurse places patient on hold (stepped out) → status OnHold → resumed manually
- Cancel: receptionist or admin cancels entry with confirmation modal → status Cancelled
- Emergency priority: ticket card displayed with red priority dot and appears at top of active queue

**Acceptance Criteria:**
- [ ] Ticket number is sequential per day and resets at midnight
- [ ] All 6 status values function: Waiting, AtNurse, AtDoctor, Completed, Cancelled, OnHold
- [ ] Queue board auto-refreshes every 30 seconds without full page reload
- [ ] Patient-facing display board accessible at /WalkInQueue/Display with no login required
- [ ] Display board shows Now Serving banner, ticket grid grouped by status, stats sidebar
- [ ] Audio beep plays on display board when a new ticket is called
- [ ] Average estimated wait time calculated and shown
- [ ] All status transitions logged in audit trail with user and timestamp

---

### P0-F005 — Role-Based Access Control

**Description:** Every action in the system is gated by role and permission, enforced server-side. The UI may hide controls, but the backend independently verifies every request.

**User Story:**
As an Admin, I want to assign roles to staff accounts so that each person sees and can do only what their job requires.

**Trigger:** Admin creates a new staff account and assigns a role.

**Pre-conditions:**
- Admin is authenticated
- Target user account does not already exist

**Post-conditions:**
- User account created with hashed password
- Role assigned via ASP.NET Identity
- Account active and usable immediately

**Main Flow:**
1. Admin opens Users → clicks Create User
2. Admin enters: name, email, password, role
3. System creates ApplicationUser via ASP.NET Identity
4. System assigns selected role
5. Account active — staff member can log in immediately
6. Audit trail entry written: Action=CREATE_USER, Role=assigned role

**Alternate Flows:**
- Deactivate account: Admin sets IsActive=false — user cannot log in but record preserved
- Password reset: Admin triggers reset — new temporary password generated and displayed once
- Role change: logged in audit trail with old role and new role captured in data diff

**Acceptance Criteria:**
- [ ] 7 roles defined: Admin, Doctor, Nurse, Receptionist, LabTechnician, Pharmacist, Cashier
- [ ] Every API endpoint and Blazor action independently verifies role server-side
- [ ] Deactivated accounts cannot authenticate
- [ ] Role assignments and changes logged in audit trail
- [ ] Passwords stored as PBKDF2 hash — never plaintext
- [ ] Account lockout after 5 failed login attempts for 15 minutes
- [ ] Password minimum: 8 characters, 1 uppercase, 1 digit, 1 special character

---

### P0-F009 — Full System Activity Audit Trail

**Description:** Every action performed in the system — create, update, delete, login, permission change, status transition — is recorded with the acting user's identity, timestamp, and a before/after data snapshot. The audit trail is read-only and cannot be modified or deleted.

**User Story:**
As an Admin, I want to see exactly who changed a patient record, what they changed, and when, so that I can investigate any discrepancy or unauthorized action.

**Trigger:** Any write operation (create, update, soft-delete) occurs anywhere in the system.

**Pre-conditions:**
- User is authenticated
- A write operation is about to complete

**Post-conditions:**
- AuditLog record written with: UserId, UserName, UserRole, Action, EntityName, EntityId, OldValues (JSON), NewValues (JSON), Timestamp (UTC), IPAddress
- AuditLog record is immutable — no update or delete endpoint exists

**Main Flow:**
1. Any authenticated user performs a write action (e.g. edits a patient record)
2. Application service captures the before-state as a JSON snapshot
3. Operation executes and succeeds
4. Application service captures the after-state as a JSON snapshot
5. AuditLog entry written atomically within the same database transaction
6. If the main operation fails, the audit entry is also rolled back (transaction integrity)

**Alternate Flows:**
- Login event: successful login writes Action=LOGIN with timestamp and IP
- Failed login: writes Action=FAILED_LOGIN — after 5 failures, account lockout is triggered
- Status transitions (queue, appointment, lab): written as Action=STATUS_CHANGE with old and new status in NewValues

**Acceptance Criteria:**
- [ ] Every create, update, and soft-delete action produces an AuditLog entry
- [ ] Login and failed login events are logged
- [ ] OldValues and NewValues stored as JSON — passwords and tokens never included
- [ ] AuditLog table has no update or delete endpoint — append-only
- [ ] Admin can filter audit log by: user, entity type, entity ID, date range, action type
- [ ] Audit log entries indexed by Timestamp and by EntityName+EntityId for fast filtering
- [ ] Audit entries written within the same database transaction as the triggering operation

---

### P0-F006 — Electronic Medical Records (EMR)

**Description:** Doctors create and manage clinical records for patient visits. Each EMR links to a patient and optionally to an appointment. Once finalized, EMRs are immutable — amendments create a new linked record.

**User Story:**
As a Doctor, I want to record my clinical notes, diagnosis, and treatment plan digitally so that future visits can reference the patient's full history.

**Trigger:** Doctor opens a patient's record and clicks "New Clinical Record."

**Pre-conditions:**
- User is authenticated as Doctor or Admin
- Patient exists and is currently in the queue or has an appointment

**Post-conditions:**
- MedicalRecord created and linked to Patient (and Appointment if applicable)
- Lab orders and prescriptions linked to this EMR record
- Audit trail entry written

**Main Flow:**
1. Doctor opens Patient Details → clicks New Clinical Record
2. Form loads: Chief Complaint, Clinical Notes, Diagnosis (free text + optional ICD-10 code), Treatment Plan, Follow-up Date
3. Doctor completes fields and optionally orders lab tests or writes prescription inline
4. Doctor clicks Save — record saved with status Draft
5. Doctor clicks Finalize — record becomes read-only
6. Any future amendment creates a new linked MedicalRecord with reference to the original

**Acceptance Criteria:**
- [ ] Finalized EMRs cannot be edited — amendment workflow enforced
- [ ] EMRs are never hard-deleted
- [ ] Lab orders created from within EMR are linked to the same record
- [ ] Patient's full EMR history visible to authorized doctors in chronological order
- [ ] Nurses have read-only access — cannot create or edit EMR
- [ ] Audit trail entry on every save and finalize action

---

### P0-F007 — Laboratory Orders & Results

**Description:** Doctors order lab tests from a catalog. Lab technicians process orders through defined workflow stages and enter results. Abnormal results are flagged. Results are immediately visible to the ordering doctor.

**User Story:**
As a Lab Technician, I want to see all pending lab orders and update their status so that doctors receive results without paper delays.

**Trigger:** Doctor orders a test from EMR or directly from the Lab module.

**Pre-conditions:**
- Patient exists
- Doctor is authenticated and authorized

**Main Flow:**
1. Doctor selects tests from catalog → Lab Order created (status: Ordered)
2. Lab Tech opens Lab module → sees Ordered tests
3. Lab Tech collects sample → updates status to SampleCollected
4. Processing begins → status updated to Processing
5. Lab Tech enters results → marks as Completed
6. If any value is abnormal → IsAbnormal flag set to true → warning badge shown
7. Result immediately visible in patient EMR and patient details page
8. Ordering doctor receives in-app notification: "Lab result available for [Patient Name]"

**Acceptance Criteria:**
- [ ] Lab order workflow: Ordered → SampleCollected → Processing → Completed
- [ ] Cancelled orders cannot receive results
- [ ] Abnormal results display ⚠ Abnormal badge in red
- [ ] Doctor receives notification when result is available
- [ ] Lab fees automatically added to patient invoice
- [ ] All status changes logged in audit trail

---

### P0-F008 — Billing & Payment Receipts

**Description:** Cashiers generate invoices for all patient services. Invoices auto-collect fees from lab and pharmacy. Partial and full payments are recorded. Receipts are generated per payment.

**User Story:**
As a Cashier, I want to generate an invoice for a patient's visit with all service fees pre-populated so I do not have to manually calculate totals.

**Main Flow:**
1. Cashier opens Billing → clicks New Invoice for patient
2. System auto-populates line items: consultation fee, lab test fees, pharmacy fees
3. Cashier adds any additional charges and applies discount if applicable
4. Invoice saved with status Issued and unique InvoiceNumber (INV-YYYY-NNNNN)
5. Patient pays → Cashier records payment with method (Cash / Bank Transfer / Insurance / Mobile Money)
6. If full payment: invoice status → Paid
7. If partial payment: invoice status → PartiallyPaid — outstanding balance displayed
8. Receipt generated and printable

**Acceptance Criteria:**
- [ ] All monetary values use decimal — never float or double
- [ ] Cancelled invoices cannot receive payments
- [ ] Paid invoices cannot be edited
- [ ] Partial payments supported — balance correctly calculated
- [ ] InvoiceNumber unique and auto-generated: INV-YYYY-NNNNN
- [ ] Payment method captured: Cash, Bank Transfer, Insurance, Mobile Money
- [ ] All invoice and payment actions logged in audit trail

---

## Section 8 — Non-Functional Requirements

### Performance
| Requirement | Target |
|-------------|--------|
| Page load time on hospital LAN | < 2 seconds |
| Patient search response time | < 500ms |
| Queue board refresh cycle | 12–30 seconds |
| Dashboard data lag | < 30 seconds |
| Concurrent users supported | Up to 50 simultaneous sessions |
| Database capacity | Handles 100,000+ patient records without degradation |

### Security
| Requirement | Detail |
|-------------|--------|
| Password storage | PBKDF2 via ASP.NET Identity — never plaintext |
| Account lockout | 5 failed attempts → 15-minute lockout |
| Password policy | Min 8 chars, 1 uppercase, 1 digit, 1 special character |
| Session cookies | HttpOnly, SameSite=Lax, Secure (HTTPS only in production) |
| CSRF protection | Anti-forgery tokens on all state-changing operations |
| Authorization | Server-side on every endpoint — UI visibility alone is not sufficient |
| Sensitive data logging | Passwords, tokens, and security stamps never written to logs or audit trail |
| HTTPS | Enforced in production via IIS |
| Transport security | TLS 1.2 minimum, TLS 1.3 preferred |

### Availability
| Requirement | Target |
|-------------|--------|
| Uptime during hospital hours | 99% |
| Scheduled maintenance | Off-hours only (after 22:00) |
| Queue display degradation | Client-side caches last known state if server briefly unavailable |

### Scalability
- SQL Server Express (10 GB limit) sufficient for v1 — upgrade path to full SQL Server documented
- Application stateless — horizontal scaling possible via additional IIS instances on LAN
- Blazor Server — each session maintains a SignalR connection; 50 concurrent users = 50 active circuits

### Accessibility
- WCAG 2.1 AA compliance for all P0 workflows
- Keyboard navigation on all forms and tables
- ARIA labels on interactive elements
- Screen reader compatible for critical paths

### Compatibility
| Component | Supported Versions |
|-----------|-------------------|
| Chrome | Latest 2 versions |
| Edge | Latest 2 versions |
| Firefox | Latest 2 versions |
| Screen resolution | 1280×720 minimum (desktop primary) |
| Tablet | Responsive layout supported |

### Data & Compliance
- All clinical and financial records soft-deleted only — no hard delete
- Patient data stored exclusively on hospital LAN server — no cloud transmission
- Audit trail retained indefinitely — append-only, no purge mechanism in v1
- Backup: daily full, 6-hourly differential, hourly transaction log

### Observability
- Structured logging via ILogger<T> throughout all layers
- All log entries include: UserId, Operation, EntityId, CorrelationId, Duration (ms)
- Passwords, tokens, and sensitive clinical values excluded from all log output
- Health check endpoint: GET /health — returns database and application status

---

## Section 9 — Technical Architecture

### Frontend
- **Blazor Interactive Server** (ASP.NET Core 10)
- SignalR-based real-time UI updates — no page reloads for queue and dashboard
- Custom CSS system (site.variables.css, site.components.css, site.tables.css) — no Bootstrap dependency
- Tabler Icons CDN for medical-appropriate iconography
- Component structure: one folder per module under Components/Pages/

### Backend
- **ASP.NET Core 10** application layer
- Clean layered architecture: Core → Application → Infrastructure → Web
- Core layer: zero external dependencies — pure domain entities, enums, interfaces, exceptions
- Application layer: DTOs, service interfaces, validators, use-case implementations
- Infrastructure layer: EF Core, Identity, narrow persistence ports, audit service; file storage remains future scope
- Web layer: Blazor components only — no business logic, no direct DbContext access

### Database Architecture
- **SQL Server Express 2019/2022** (production) / LocalDB (development)
- EF Core 10 Code-First with Fluent API configuration per entity
- Global soft-delete query filters on all clinical and operational entities
- Optimistic concurrency (RowVersion) on Patient, Invoice, and Appointment
- Decimal precision configured: monetary fields at (12,3), vitals at appropriate precision
- Unique indexes: PatientCode, NationalId (filtered), PassportNumber (filtered), InvoiceNumber
- Composite indexes on high-frequency query patterns: (DoctorId, AppointmentDate), (QueueDate, Status)

### Auth & Authorization
- **ASP.NET Core Identity** with ApplicationUser extending IdentityUser
- Current approved baseline: five canonical roles enforced through backend policy checks; broader product roles remain future scope
- Permission-based checks for fine-grained actions (e.g. Patients.Edit.Vitals vs Patients.Edit.Full)
- Cascading auth state in Blazor via CascadingAuthenticationState
- Session cookie: 8-hour expiry, sliding renewal, HttpOnly, SameSite=Lax

### Infrastructure & Deployment
- **IIS 10** on Windows Server 2019/2022
- ASP.NET Core 10 Runtime installed as Windows hosting bundle
- SQL Server Express on same server or dedicated LAN server
- HTTPS via self-signed or domain certificate (Let's Encrypt if domain available)
- Automated SQL Server backup via SQL Server Agent or Windows Task Scheduler

### External Integrations
- None in v1 — system is fully self-contained on the hospital LAN

---

## Section 10 — Implementation Phases

### Phase 1 — Foundation (Weeks 1–3)

**Goal:** Solution architecture, database, authentication, patient registration, queue management, and audit trail operational.

- [ ] Create 5-project solution: Core, Application, Infrastructure, Web, Tests
- [ ] Configure EF Core with AppDbContext and all entity configurations
- [ ] Run initial migration and verify database schema
- [ ] Seed roles: Admin, Doctor, Nurse, Receptionist, LabTechnician, Pharmacist, Cashier
- [ ] Implement ASP.NET Identity with ApplicationUser
- [ ] Build login, logout, and account setup pages
- [ ] Implement patient registration, search, and details (P0-F001)
- [ ] Implement patient vitals entry for nurses (P1-F005)
- [ ] Implement walk-in queue — add, advance, hold, cancel (P0-F002)
- [ ] Implement patient-facing queue display board (P0-F003)
- [ ] Implement full audit trail service — all write operations logged (P0-F009)
- [ ] Implement role-based access control across all above features (P0-F005)
- [ ] Write unit tests for patient registration and queue status transitions

**Validation:** Admin can log in, register a patient, add them to the queue, advance through all statuses, and view the audit trail with full user + timestamp detail.

---

### Phase 2 — Clinical (Weeks 4–6)

**Goal:** Appointment management, EMR, and laboratory module operational.

- [ ] Implement appointment booking, confirmation, check-in, and cancellation (P0-F004)
- [ ] Implement EMR — create, finalize, amendment workflow (P0-F006)
- [ ] Implement lab order creation from EMR (P0-F007)
- [ ] Implement lab workflow: Ordered → SampleCollected → Processing → Completed
- [ ] Implement abnormal result flagging and doctor notification
- [ ] Write unit tests for appointment conflict detection and lab status transitions
- [ ] Verify audit trail covers all EMR and lab actions

**Validation:** Doctor can book appointment, see patient in queue, create EMR, order lab test, and receive notification when result is available.

---

### Phase 3 — Financial & Pharmacy (Weeks 7–9)

**Goal:** Billing, payments, pharmacy dispensing, and notifications operational.

- [ ] Implement invoice creation with auto-populated line items (P0-F008)
- [ ] Implement partial and full payment recording with receipt generation
- [ ] Implement pharmacy prescription dispensing and stock deduction (P1-F001)
- [ ] Implement stock transaction history and reorder alerts
- [ ] Implement in-app notification system with bell indicator (P1-F002)
- [ ] Implement real-time management dashboard (P1-F003)
- [ ] Write unit tests for billing calculations and payment allocation
- [ ] Verify audit trail covers all billing and payment actions

**Validation:** Cashier can generate invoice from a patient visit, receive payment, issue receipt, and see outstanding balance correctly calculated.

---

### Phase 4 — Advanced & Hardening (Weeks 10–12)

**Goal:** Admissions, user management UI, audit log UI, reporting, and production hardening.

- [ ] Implement ward, room, and bed management with occupancy tracking (P1-F004)
- [ ] Implement admission and discharge workflow with bed status synchronization
- [ ] Build user management panel for Admin (P2-F003)
- [ ] Build audit log filter UI for Admin (P2-F002)
- [ ] Implement basic reports: daily revenue, doctor workload, lab activity (P2-F001)
- [ ] Security review: verify all endpoints enforce server-side authorization
- [ ] Performance review: add missing database indexes, optimize N+1 queries
- [ ] Configure IIS production deployment with HTTPS
- [ ] Configure automated SQL Server backup schedule
- [ ] End-to-end testing of all P0 and P1 workflows
- [ ] Staff training documentation per role

**Validation:** All P0 and P1 features fully operational. Admin can view complete audit trail. Backup verified with test restore.

---

## Section 11 — User Flows & Edge Cases

### Core User Flow — Patient Visit

```
Staff opens system → logs in with role credentials
        ↓
Receptionist → Patients → Register Patient
        ↓
System generates PatientCode: PT-2026-00001
        ↓
Receptionist → Walk-in Queue → Add to Queue → issues ticket A-001
        ↓
Queue Display Board shows: A-001 — Waiting
        ↓
Nurse → Queue Board → Call to Nurse → A-001 moves to AtNurse
        ↓
Nurse → Patient Details → Edit Vitals → saves weight, height, temp, BP
        ↓
Doctor → Queue Board → Send to Doctor → A-001 moves to AtDoctor
        ↓
Doctor → Patient Details → New Clinical Record → fills EMR
        ↓
Doctor → Orders lab test from within EMR
        ↓
Lab Tech → Lab Module → updates status → enters result
        ↓
Doctor receives notification: "Lab result available"
        ↓
Doctor → finalizes EMR
        ↓
Queue Board → Doctor clicks Done → A-001 Completed
        ↓
Cashier → Billing → New Invoice → line items auto-populated
        ↓
Patient pays → Receipt generated
        ↓
Every action above written to Audit Trail with user + timestamp
```

### Edge Cases

| Condition | System Behavior | User Feedback |
|-----------|----------------|---------------|
| Duplicate national ID on registration | Block save | "National ID already registered to another patient." |
| Doctor tries to book inactive doctor | Validation fails | "This doctor is currently inactive and cannot receive appointments." |
| Two nurses try to advance same queue entry simultaneously | Optimistic concurrency conflict | "This queue entry was updated by another user. Please refresh." |
| Lab result entered on a cancelled order | Operation blocked | "Cannot enter results for a cancelled lab order." |
| Payment attempted on cancelled invoice | Operation blocked | "This invoice has been cancelled and cannot receive payments." |
| Network interruption during form save | Form re-enables, error shown | "Unable to save. Please check your connection and try again." |
| Unauthorized role accessing admin page | 403 returned server-side | Redirect to Access Denied page |
| Empty queue when nurse clicks Call Next | Service returns failure | "No patients are currently waiting in the queue." |
| Cashier attempts to edit a paid invoice | Operation blocked | "Paid invoices cannot be modified." |
| Admin deletes a patient | Soft-delete only — record preserved | Patient marked IsDeleted=true, hidden from lists, accessible via audit |

### API Design (High Level)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/auth/login | Authenticate user |
| POST | /api/auth/logout | End session |
| GET | /api/patients | List patients (paginated, filtered) |
| POST | /api/patients | Register new patient |
| GET | /api/patients/{id} | Get patient details |
| PUT | /api/patients/{id} | Update patient |
| PUT | /api/patients/{id}/vitals | Update vitals (Nurse only) |
| GET | /api/queue/today | Get today's full queue |
| POST | /api/queue | Add patient to queue |
| PUT | /api/queue/{id}/advance | Advance queue status |
| GET | /api/queue/live | JSON for display board (anonymous) |
| GET | /api/appointments | List appointments |
| POST | /api/appointments | Book appointment |
| PUT | /api/appointments/{id}/cancel | Cancel appointment |
| POST | /api/emr | Create medical record |
| GET | /api/emr/patient/{id} | Get patient EMR history |
| POST | /api/lab/orders | Create lab order |
| PUT | /api/lab/orders/{id}/result | Enter lab result |
| GET | /api/billing/invoices | List invoices |
| POST | /api/billing/invoices | Create invoice |
| POST | /api/billing/payments | Record payment |
| GET | /api/audit | Query audit log (Admin only) |
| GET | /health | Health check (anonymous) |

### Data Model (Core Entities)

**Patient:** id, patientCode, firstName, middleName, thirdName, lastName, dateOfBirth, gender, bloodGroup, nationalId, phone, address, city, weight, height, temperature, bloodPressure, insuranceProvider, status, isDeleted, createdAt, createdBy

**WalkInQueue:** id, queueNumber, sequenceNumber, prefix, patientId, walkInName, walkInPhone, doctorId, status, queueDate, registeredAt, calledAt, completedAt, priority, notes, isDeleted

**Appointment:** id, appointmentCode, patientId, doctorId, appointmentDate, appointmentTime, type, status, priority, notes, cancellationReason, cancelledAt, isDeleted

**MedicalRecord:** id, patientId, doctorId, appointmentId, chiefComplaint, diagnosis, icd10Code, clinicalNotes, treatmentPlan, followUpDate, isFinalized, isDeleted

**LabTest:** id, testCode, testName, category, patientId, doctorId, medicalRecordId, status, results, isAbnormal, fee, isDeleted

**Invoice:** id, invoiceNumber, patientId, status, totalAmount, paidAmount, discount, paymentMethod, isDeleted

**AuditLog:** id, userId, userName, userRole, action, entityName, entityId, oldValues, newValues, timestamp, ipAddress

### UX Rules

- All forms validate inline on blur — errors shown below each field before submission
- Every save button shows loading state with spinner and disabled state while processing
- Every successful save shows a toast notification (top-right, auto-dismiss 4 seconds)
- Every destructive action (cancel, delete, discharge) requires a confirmation modal
- Empty states always show an icon, a message, and a primary call-to-action button
- Error states (network failure, server error) show a retry button — never a blank screen
- Queue board and dashboard auto-refresh without full page reload
- All monetary values display with 3 decimal places (SDG currency convention)

---

## Section 12 — Success Metrics and KPIs

### Business Metrics
| Metric | Target | Measurement Method | Owner |
|--------|--------|-------------------|-------|
| Patient registration time | < 3 minutes | Average time from form open to PatientCode generated (server timestamp diff) | PM |
| Paper queue elimination | 100% of queues managed digitally | Zero paper queue records after go-live | Admin |
| Invoice digitization | 100% of invoices in system | Zero spreadsheet invoices after go-live (manual audit) | Admin |
| Lost lab results | 0 per month | Count of lab orders without a result after 72 hours | Lab Manager |

### Product Metrics
| Metric | Target | Measurement Method | Owner |
|--------|--------|-------------------|-------|
| Core workflow completion rate | > 90% | Sessions that complete full patient journey without abandonment | PM |
| Patient search success rate | > 95% | Searches that return the correct patient in top 3 results | QA |
| Queue status accuracy | 100% | Queue entries with correct final status at end of day | Nurse Lead |
| System error rate | < 0.5% | Application exceptions per total requests (structured log analysis) | Dev Lead |

### Technical Metrics
| Metric | Target | Measurement Method | Owner |
|--------|--------|-------------------|-------|
| Page load time (LAN) | < 2 seconds | Browser performance timing — P95 | Dev Lead |
| Patient search response | < 500ms | Server-side query duration logged per request | Dev Lead |
| Database query time (P95) | < 200ms | EF Core command logging | Dev Lead |
| Audit trail write latency | < 50ms overhead | Measured as delta vs same operation without audit | Dev Lead |
| Uptime during hospital hours | > 99% | IIS uptime monitoring | IT Admin |

### User Satisfaction Metrics
| Metric | Target | Measurement Method | Review |
|--------|--------|-------------------|--------|
| Staff adoption rate | > 80% within 2 weeks of go-live | Percentage of staff using system daily | 2-week post-launch |
| Training completion | 100% of staff trained | Training attendance record per role | Pre-launch |
| Support tickets (critical) | 0 per week after week 2 | Helpdesk ticket count by severity | Weekly |

**Tracking Tool:** Structured log analysis via ILogger output + manual review
**Review Frequency:** Weekly for first month, monthly thereafter
**Owner:** Eltayeb Elmuiz (PM)

---

## Section 13 — Timeline and Milestones

**Start Date:** September 20, 2026
**Target Launch:** December 13, 2026
**Total Duration:** 12 weeks

| Milestone | Description | Due Date | Status | Owner |
|-----------|-------------|----------|--------|-------|
| Kickoff | Solution created, team onboarded, PRD approved | Sep 27, 2026 | TODO | Eltayeb Elmuiz |
| PRD Approved | All stakeholders sign off on PRD v1.0 | Sep 27, 2026 | TODO | Eltayeb Elmuiz |
| Phase 1 Complete | Auth, patient, queue, audit trail live | Oct 18, 2026 | TODO | Backend Dev |
| Phase 2 Complete | Appointments, EMR, laboratory live | Nov 8, 2026 | TODO | Backend Dev |
| Phase 3 Complete | Billing, pharmacy, notifications live | Nov 29, 2026 | TODO | Backend Dev |
| Phase 4 Complete | Admissions, admin UI, reports, hardening | Dec 13, 2026 | TODO | Dev Lead |
| Beta Launch | System deployed to staging for staff testing | Dec 6, 2026 | TODO | IT Admin |
| Staff Training | All 7 roles trained on their workflows | Dec 10, 2026 | TODO | Eltayeb Elmuiz |
| Public Launch | System live on hospital LAN for all staff | Dec 13, 2026 | TODO | Eltayeb Elmuiz |

---

## Section 14 — Risk Register

| ID | Description | Likelihood | Impact | Score | Mitigation | Owner |
|----|-------------|-----------|--------|-------|-----------|-------|
| R01 | Scope creep — new features requested mid-development | High | High | 9 | Enforce MoSCoW strictly; all new requests go to v2 backlog | Eltayeb Elmuiz |
| R02 | Staff resistance to digital system after years of paper | Medium | High | 6 | Appoint a champion user per department; phased rollout with parallel paper backup for 2 weeks | Eltayeb Elmuiz |
| R03 | Power outages disrupting operation | High | High | 9 | UPS required on server and key workstations; Blazor Server reconnects automatically after brief outage | IT Admin |
| R04 | Hardware failure causing data loss | Medium | Critical | 9 | Automated daily full backup + hourly transaction log backup + weekly off-site copy | IT Admin |
| R05 | Concurrent editing conflicts on patient or invoice records | Low | Medium | 2 | Optimistic concurrency implemented on critical entities; clear user error message on conflict | Dev Lead |
| R06 | SQL Server Express 10 GB limit reached | Low | High | 3 | Monitor database size monthly; upgrade path to full SQL Server documented and budgeted for v2 | IT Admin |
| R07 | Key developer unavailability | Medium | High | 6 | Document all architectural decisions; code reviewed and understood by at least 2 team members | Dev Lead |
| R08 | Network instability on hospital LAN | Medium | Medium | 4 | Blazor Server handles brief disconnects via SignalR reconnection; queue display caches last state | IT Admin |
| R09 | Training insufficient for low-tech staff | Medium | Medium | 4 | Role-specific 2-hour training sessions; simplified UI with clear labels and help text | Eltayeb Elmuiz |

*Score = Likelihood × Impact (High=3, Medium=2, Low=1)*

---

## Section 15 — Stakeholders and Approvals

### Stakeholders

| Name | Role | Involvement | Contact |
|------|------|-------------|---------|
| Eltayeb Elmuiz | Product Owner / PM | Full project oversight and approval | [TBD] |
| Hospital Director | Executive Sponsor | Final go/no-go for launch | [TBD] |
| Head Receptionist | Department Lead | Validates patient and queue workflows | [TBD] |
| Head Doctor | Clinical Lead | Validates EMR and lab workflows | [TBD] |
| Finance Manager | Financial Lead | Validates billing and payment workflows | [TBD] |
| IT Administrator | Deployment Owner | Server setup, backup, maintenance | [TBD] |
| Backend Developer | Engineering | Core system implementation | [TBD] |
| Frontend Developer | Engineering | Blazor UI components | [TBD] |
| QA Engineer | Quality | Testing and acceptance verification | [TBD] |

### Approval Gates

| Gate | Approver | Required By | Status |
|------|----------|-------------|--------|
| PRD v1.0 Approved | Eltayeb Elmuiz + Hospital Director | Sep 27, 2026 | Pending |
| Phase 1 Accepted | Dev Lead + Head Receptionist | Oct 18, 2026 | Pending |
| Phase 2 Accepted | Dev Lead + Head Doctor | Nov 8, 2026 | Pending |
| Phase 3 Accepted | Dev Lead + Finance Manager | Nov 29, 2026 | Pending |
| Beta Accepted | All department leads | Dec 8, 2026 | Pending |
| Launch Approved | Hospital Director + Eltayeb Elmuiz | Dec 13, 2026 | Pending |

---

## Section 16 — References and Links

| Resource | Link |
|----------|------|
| Repository | [TBD] |
| API Documentation | [TBD] |
| Architecture Diagram | [TBD] |
| Database Schema | [TBD] |
| Staging Environment | http://[server-ip]:8080 |
| Production Environment | http://[server-ip] |
| CI/CD Pipeline | [TBD] |
| Monitoring / Logs | IIS Logs + Application Event Viewer |
| Related PRD (Architecture) | HMS_Enterprise_BackendCore_Archit.md |
| Setup Script | setup-elshiekh-blazor.ps1 |
| Phase 01 Script | phase01-setup.ps1 |
| Slack / Communication Channel | [TBD] |
| Meeting Notes | [TBD] |

---

## Section 17 — Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | September 20, 2026 | Eltayeb Elmuiz | Initial PRD created |
