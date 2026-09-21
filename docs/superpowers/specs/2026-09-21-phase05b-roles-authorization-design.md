# Phase 05B — Roles and Authorization Business Reconciliation

> **Status:** Design reconciliation complete; implementation not started. This artifact requires separate human approval before 05B implementation.
>
> **Scope:** Reconcile the approved Identity/account-security foundation with the Rwanda-first first-release business model. Do not create role/policy constants, register policies, seed roles, add claims or handlers, modify `ApplicationUser`, generate migrations, update databases, or begin 05C/05D/05E/Phase 06.

## 1. Existing design summary

The earlier 05B design proposed seven roles: `Admin`, `Doctor`, `Nurse`, `Receptionist`, `LabTechnician`, `Pharmacist`, and `Cashier`. It proposed `CanManageUsers` and `CanViewAuditTrail`, with `CanManageUserSecurity` added for sensitive account operations. Role seeding was designed as deterministic, idempotent, password-free `RoleManager<IdentityRole>` work owned by Infrastructure. Application would own plain security vocabulary; Web would register policies and enforce endpoint/component boundaries; Core would remain free of Identity.

The earlier artifact did not define a fallback authorization policy, Patient as an authenticated role, System Administrator as a separate boundary, resource ownership, multi-facility context, or clinical confidentiality limits for administrators. Its `Admin` capability matrix also granted clinical access too broadly.

## 2. Stale decisions found

- The seven-role list is stale for the clarified first release. Nurse, Cashier, Pharmacist, LabTechnician, and Accountant are future roles and are deferred.
- `Admin` is too ambiguous. It is replaced by distinct `Administrator` and `SystemAdministrator` roles.
- `Doctor` is replaced by the general provider role `Provider`; Dentist is a specialty/domain classification, not an Identity role.
- Patient was missing even though authenticated first-release Patient access is required.
- Administrator must not receive clinical-note access by default.
- A role alone cannot authorize Patient own-resource access or Provider care-context access.
- The fallback policy and anonymous endpoint exceptions require an explicit design decision.

## 3. Final role set

| Role | Decision | Business Purpose | Reason |
|---|---|---|---|
| `SystemAdministrator` | IMPLEMENT IN 05B | Technical/security/facility administration and privileged account recovery | Separates technical security authority from operational administration without granting automatic clinical access |
| `Administrator` | IMPLEMENT IN 05B | Operational user, patient, appointment, configuration, billing, payment, and report administration within approved policies | Required first-release operational role; does not imply clinical confidentiality bypass |
| `Receptionist` | IMPLEMENT IN 05B | Registration, permitted demographics, appointments, arrival, operational queue, balances, and approved receipts/payments | Required first-release operational workflow |
| `Provider` | IMPLEMENT IN 05B | General clinical provider responsibility across specialties | One stable role covers General Practitioner, Cardiology, Neurology, Dentistry, and future specialties |
| `Patient` | IMPLEMENT IN 05B | Authenticated patient portal identity | Required first-release business role; always subject to own-resource checks |
| `Doctor` | REMOVE/RENAME | — | Domain terminology remains valid, but the authorization role is the broader `Provider` |
| `Dentist` | REMOVE/RENAME | — | Specialty/domain classification, never a fundamental authorization role |
| `Nurse` | DEFER | Future nursing workflows | No current first-release role requirement |
| `Cashier` | DEFER | Future cashier workflows | No current first-release role requirement |
| `Pharmacist` | DEFER | Future pharmacy workflows | No current first-release role requirement |
| `LabTechnician` | DEFER | Future laboratory workflows | No current first-release role requirement |
| `Accountant` | DEFER | Future accounting workflows | No current first-release role requirement |

No separate `Doctor` and `Provider` roles are created. The existing Doctor domain entity is a provider subtype/domain record; Identity role naming is intentionally broader.

## 4. Canonical role contract

| Constant | Persisted Name | Meaning |
|---|---|---|
| `RoleNames.SystemAdministrator` | `SystemAdministrator` | Technical security, facility/system configuration, integrations, and privileged recovery |
| `RoleNames.Administrator` | `Administrator` | Operational HMS administration within explicit policies |
| `RoleNames.Receptionist` | `Receptionist` | Reception and operational patient/appointment workflows |
| `RoleNames.Provider` | `Provider` | General clinical provider across specialties |
| `RoleNames.Patient` | `Patient` | Authenticated patient portal access, constrained to owned resources |

Names are stable persisted identifiers, not localized display labels. They belong in the future Application security vocabulary without an Identity dependency. No role is named after a specialty.

## 5. Final policy set

These are capability boundaries, not button-level CRUD policies:

| Policy | Purpose | Allowed Roles | Resource Check Required |
|---|---|---|---|
| `CanManageUsers` | Ordinary account creation, profile administration, and approved role administration | `Administrator`, `SystemAdministrator` | YES for target-account restrictions and final-admin protection |
| `CanManageUserSecurity` | Login block/unblock, session revocation, credential-reset initiation, username changes, suspension, restoration, banning, and privileged role changes | `Administrator`, `SystemAdministrator` | YES; account state, self-targeting, restricted accounts, and last-admin rules apply |
| `CanViewAuditTrail` | Administrative audit and security-event review | `Administrator`, `SystemAdministrator` | YES for redaction/scope rules |
| `CanManagePatients` | Patient registration, permitted demographic administration, and patient search | `Administrator`, `Receptionist` | YES for permitted fields and workflow context |
| `CanManageAppointments` | Appointment creation, rescheduling, cancellation, and operational scheduling | `Administrator`, `Receptionist`, `Provider` | YES for provider schedule, assigned care, and workflow context |
| `CanAccessClinicalRecords` | Clinical record access and clinical operations | `Provider`, `Patient` | YES; Provider care context and Patient ownership are mandatory |
| `CanManageBilling` | Approved billing configuration, payments, balances, and receipts | `Administrator`, `Receptionist` | YES for payment/refund and financial workflow rules |
| `CanConfigureSystem` | Facility/system configuration and technical integrations | `SystemAdministrator` | YES for deployment/facility scope |

No policy grants clinical-note access to `Administrator` or `SystemAdministrator` by implication.

## 6. Role / policy / resource boundary

Roles provide broad responsibility grouping. Policies provide named capabilities. Resource and context checks decide which record and operation are allowed. Account security state decides whether the account may access the system at all.

Role/policy alone is sufficient only for coarse boundaries such as whether an account may enter an administrative area. The following require application business rules and resource authorization:

| Workflow | Required boundary |
|---|---|
| Patient own profile | `Patient` plus ownership of the authenticated Patient identity |
| Patient own appointments | `Patient` plus appointment ownership/eligibility |
| Patient own medical records | `Patient` plus permitted record ownership and disclosure rules |
| Patient own bills/receipts | `Patient` plus financial-record ownership |
| Provider patients under care | `Provider` plus assigned-care/context validation |
| Provider assigned appointments | `Provider` plus appointment assignment/context validation |
| Provider department patients | `Provider` plus department/care-context validation |
| Provider own schedule/availability | `Provider` plus schedule ownership and operational rules |
| Administrator records | Administrative policy plus target-record and least-privilege checks |
| Administrator clinical records | No default authorization; a separately approved clinical policy would be required |

No authorization handlers or resource engine are implemented in 05B; this document identifies the future boundary.

## 7. Clinical confidentiality

**CAN ADMINISTRATOR READ CLINICAL NOTES BY DEFAULT:** NO

**CAN SYSTEM ADMINISTRATOR READ CLINICAL NOTES BY DEFAULT:** NO

**CAN RECEPTIONIST READ CLINICAL NOTES:** NO

**CAN DOCTOR/PROVIDER READ ALL CLINICAL NOTES:** NO

**CAN PATIENT READ ALL PATIENT CLINICAL NOTES:** NO

Future clinical access requires the appropriate policy plus care-context, disclosure, and ownership rules. Provider status does not mean access to every patient. Patient status does not mean access to every record. Administrative privilege does not bypass clinical confidentiality.

## 8. Administrator vs System Administrator

Both roles are needed in the first-release authorization vocabulary, but they have different boundaries:

- `Administrator` owns operational HMS administration: ordinary users, approved role administration, patient and appointment administration, provider schedules, billing configuration, payments, and reports through explicit policies.
- `SystemAdministrator` owns technical/security/facility configuration, integration configuration, and privileged account recovery through explicit policies.

Neither role is a clinical superuser. No magical global bypass is created. `CanManageUserSecurity` may be held by both roles for the approved sensitive account operations, but target-account, final-admin, and audit rules still apply.

## 9. Role management security

- Only an authorized account-security administrator may assign or revoke roles.
- Ordinary role assignment uses the canonical role set only.
- Granting `Administrator` or `SystemAdministrator` requires `CanManageUserSecurity` and the later user-management workflow.
- Users cannot promote themselves.
- Self-demotion is subject to final-admin protection and must not leave the deployment without a usable administrator.
- Blocked, suspended, or banned accounts cannot receive either administrative role.
- Role removal is audited through 05C and must obey final-admin protection.
- Role changes are atomic with their security event in the later workflow.

05B defines the vocabulary and policy boundary. 05C owns persistent audit attribution. 05E owns hardening and security verification. Later Application/User Management owns the complete workflow.

## 10. Account-security precedence

```text
Account/login restriction
    ↓
Authentication validity and Identity lockout
    ↓
Administrative account state and session validation
    ↓
Authorization policy
    ↓
Resource/context authorization
    ↓
Business operation
```

Blocked, suspended, or banned accounts cannot regain access through an allowed role. Security-stamp rotation revokes existing sessions; framework-supported validation must balance revocation latency with request/database overhead. No custom session table, token system, cache, or Redis infrastructure is introduced.

## 11. Blazor authorization foundation

Future pages may use `[Authorize]`, `[Authorize(Policy = "...")]`, and `AuthorizeView` for navigation and presentation. These are usability aids only. Application use cases and server endpoints must enforce the same policies and resource checks independently of hidden buttons or rendered components. Resource authorization belongs in Application/approved authorization services when concrete workflows exist; Web owns authentication and policy composition.

## 12. Fallback policy / anonymous access

The future Web host should use a fallback policy requiring an authenticated user by default. Anonymous access is explicitly limited to:

- the login/authentication surface;
- access-denied handling;
- the health endpoint;
- static assets and framework infrastructure that do not expose HMS data; and
- separately approved public pages.

Patient-facing portal pages are authenticated. No anonymous HMS data or operational endpoint is implied. This is a design decision only; Web registration is not implemented in 05B reconciliation.

## 13. Role seeding design

Infrastructure will eventually seed exactly the five canonical roles. Seeding must be deterministic, idempotent, password-free, safe to repeat, and separate from user creation and privileged-user provisioning. It must not run before 05D creates the Identity tables and must not alter `InitialCreate`.

## 14. First privileged user bootstrap

Public registration remains disabled. Role seeding creates no user and no password. The first privileged account should be provisioned by an explicit deployment/development bootstrap command that reads a username, display name, and password from User Secrets or environment variables, fails safely when values are absent/invalid, is not exposed as a permanent setup endpoint, and does not log or migrate credentials. The bootstrap must be auditable and must preserve final-admin protection.

## 15. Patient account ownership

`Patient` role and Patient domain record remain separate. No `ApplicationUser.PatientId` is added now. A later Application/User Management and patient-portal design should define a controlled account-to-patient association using stable UserId and explicit ownership checks. Patient profile, appointments, records, bills, receipts, reports, and notifications must all validate ownership and permitted disclosure; the role alone is insufficient.

## 16. Provider account ownership

`Provider` role and Doctor/provider domain record remain separate. No `DoctorId`, `ProviderId`, `EmployeeId`, or `DepartmentId` is added to `ApplicationUser`. Later staff/application workflow design should establish contextual association, assigned-care checks, schedule ownership, and department/facility context without coupling Core to Identity.

## 17. Multi-facility authorization

The current deployment model remains one database/deployment per hospital or country. Facilities and branches exist inside that deployment. Role membership alone will not be sufficient once facility-scoped workflows exist, but 05B does not invent a tenant system or facility tables. Facility membership and context should be designed with the organization/facility domain and relevant Application workflows before those workflows are implemented.

## 18. Performance analysis

**EXPECTED AUTHORIZATION OVERHEAD:** Framework policy/role evaluation plus bounded resource checks at use-case or endpoint boundaries; no checks on every component render.

**EXPECTED DATABASE ACCESS:** No new database access from this design. Future Identity validation must use framework-supported claim/security-stamp behavior and avoid uncontrolled `UserManager` lookups.

**KNOWN HOT PATH:** Authentication/cookie validation, sensitive account-security operations, and resource ownership queries for patient/provider workflows.

**CACHE REQUIRED NOW:** NO

**REDIS REQUIRED NOW:** NO

**PERFORMANCE BLOCKER:** NONE. Measure query count, latency, and security-stamp validation cost when concrete workflows exist; do not weaken security to reduce overhead.

## 19. Package review

No new NuGet package is required. ASP.NET Core authorization, Identity role stores, and existing EF/Identity packages are sufficient.

## 20. Layer ownership

| Layer | Ownership |
|---|---|
| Core | Domain rules only; no Identity, ASP.NET authorization, claims, or HttpContext |
| Application | Plain canonical role/policy vocabulary and future use-case/resource authorization contracts where concrete workflows justify them |
| Infrastructure | Identity persistence, role seeding technical implementation, and account technical services |
| Web | Authentication middleware, fallback/anonymous policy composition, endpoint/component authorization, and challenge/forbidden behavior |

## 21. Test strategy

05B implementation tests should cover canonical role stability, deterministic/idempotent role seeding, policy registration, unauthenticated challenge behavior, unauthorized denial, administrator clinical denial, receptionist clinical denial, provider policy boundaries, Patient policy foundation, account-security precedence, no self-promotion, and no username-based authorization.

05E should cover cookie/session behavior, blocked/suspended/banned precedence, security-stamp revocation, role-management hardening, final-admin protection, and security integration. Patient and provider ownership/resource tests belong with their Application workflows. No tests are implemented by this reconciliation.

## 22. Migration / database boundary

**INITIALCREATE CHANGE REQUIRED:** NO

**IDENTITY MIGRATION REQUIRED DURING 05B:** NO

**DATABASE UPDATE REQUIRED DURING 05B:** NO

Role seeding is separate from migration generation. 05D owns the finalized Identity migration and SQL verification.

## 23. Documentation changes

The reconciled role, policy, confidentiality, resource-authorization, fallback, bootstrap, ownership, and multi-facility decisions are recorded in this artifact and the official Phase 05 design. README, architecture, and decision records remain aligned with 05B design reconciliation complete and implementation not started.

## 24. Deviations / open decisions

NONE. The remaining implementation approvals are the exact 05B file/test scope and the human implementation gate. No source change is authorized by this reconciliation.

## 25. Implementation gate

**DESIGN COMPLETE**

**IMPLEMENTATION NOT STARTED**

Before implementation, separately approve the exact Application role/policy constants, Web policy registration and fallback policy, deterministic role-seeder invocation, focused tests, and the boundary between policy checks and future resource authorization.
