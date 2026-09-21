# Phase 05 — Identity & Security Design

> **Status:** Design finalized after approval of username administration and administrative account control. Phase 05A Identity foundation and its approved security-model amendment are implemented and verified; 05B design is finalized, while later implementation subphases remain gated.
>
> **Scope:** This artifact records the approved design baseline. Its original design-review non-actions remain historical; implementation status and remaining gates are recorded in the repository checkpoint documentation.

## 1. Evidence and scope

The repository is a five-project .NET 10 solution. Phase 04 is complete, including the existing `ElsheiekhHmsDbContext`, the `InitialCreate` migration, the `ElsheiekhHMS_Dev` LocalDB development database, and the isolated `ElsheiekhHMS_IntegrationTests` strategy. The current source contains the approved 05A Identity types, package, same-context foundation, and stores, but no login UI, Web authentication middleware, named authorization policies, current-user abstraction, or audit-log entity.

The PRD is marked ASP.NET Core 9 / EF Core 9, while the actual solution targets `net10.0` and resolves EF Core packages at `10.0.12`. The actual solution and current approved architecture therefore govern this design; Phase 05 implementation uses the existing .NET 10/EF Core 10 stack unless the requirements are formally changed.

The current source now contains the approved 05A `ApplicationUser`, same-context Identity foundation, and Infrastructure registration. It still contains no login UI, named authorization policies, current-user abstraction, audit-log entity, Identity migration, or database rollout; those remain later subphases.

Requirements used by this design:

- five first-release roles: SystemAdministrator, Administrator, Receptionist, Provider, Patient;
- future roles Nurse, Cashier, Pharmacist, LabTechnician, and Accountant remain deferred;
- Dentist is a specialty/domain classification under Provider, not an authorization role;
- every staff member receives an administrator-assigned login account;
- authorization is enforced server-side;
- passwords use ASP.NET Core Identity PBKDF2 storage and are never logged or stored in plaintext;
- lockout follows five failed attempts for fifteen minutes;
- the initial password policy is at least eight characters, one uppercase letter, one digit, and one special character;
- user and role changes are auditable;
- all system writes, logins, permission changes, and status transitions are attributable to user, role, timestamp, and before/after data;
- patient self-service is out of scope and external email/SMS delivery is out of scope.

## 2. Responsibility boundaries

**Authentication** answers who is signing in. ASP.NET Core Identity validates the login credential and issues the server application cookie.

**Authorization** answers what an authenticated account may do. Roles provide broad staff grouping; policies provide named server-side requirements at application and Web boundaries.

**Account management** creates, enables, disables, locks, unlocks, resets, and assigns roles to login accounts. It does not edit clinical or employee records.

**Application identity** is `ApplicationUser`, an Identity login account with only account-oriented metadata.

**Domain staff** are hospital people and role-specific records such as the existing `Doctor`. A staff record can exist without a login account; a login account is not itself a Doctor, Nurse, Receptionist, or Employee.

**Auditing** records the immutable account identifier as the actor reference, with username and role captured as display-time snapshots. Core continues to expose opaque string audit fields and remains unaware of Identity.

**Security configuration** covers password, lockout, cookie, antiforgery, and authorization composition. It does not include clinical rules, staff profile data, or UI workflows.

## 3. Identity architecture decision

### Option A — Extend the existing `ElsheiekhHmsDbContext`

**Benefits**

- one physical database and one EF transaction boundary for domain writes, Identity changes, and future audit writes;
- one Infrastructure-owned migration stream;
- one connection-string and LocalDB test strategy;
- straightforward cross-querying when a later approved use case needs account or role data;
- no cross-context transaction coordination or duplicated design-time configuration.

**Costs**

- `ElsheiekhHmsDbContext` becomes an `IdentityDbContext<ApplicationUser>`;
- the migration snapshot includes Identity tables;
- the context model is larger and Identity-specific configuration remains in Infrastructure.

### Option B — Add a separate Identity `DbContext`

**Benefits**

- domain persistence and Identity model remain separately versioned;
- Identity can be changed without rebuilding the domain context model;
- a future service split could move the Identity context independently.

**Costs**

- two migration streams and two design-time contexts;
- cross-context transactions for an account change plus an audit/domain write;
- duplicate connection and test setup;
- more operational steps while the application deliberately uses one SQL Server database;
- no current requirement for service or database separation.

### Recommendation

Use **Option A**. Change the existing context base to `IdentityDbContext<ApplicationUser>` only during the approved implementation subphase. Keep all Identity entities, configurations, and stores in Infrastructure. `InitialCreate` remains unchanged; Identity is introduced by a new migration.

## 4. Physical database decision

Identity should share the existing physical SQL Server database `ElsheiekhHMS_Dev` in development and the single production SQL Server database in deployment. The physical-database decision is independent of the EF-context decision: this design chooses both the existing database and the existing context.

The isolated integration database remains `ElsheiekhHMS_IntegrationTests`. Its safety guard remains exact-target-only. Fresh integration tests must apply `InitialCreate` followed by the new Identity migration without touching `ElsheiekhHMS_Dev`.

## 5. ApplicationUser design

`ApplicationUser` belongs in `ElsheiekhHMS.Infrastructure/Identity/Entities/ApplicationUser.cs` because it derives from the ASP.NET Identity framework. It must not be placed in Core or Application.

The original 05A foundation shape was:

```csharp
public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
    public string? DisabledBy { get; set; }
}
```

The approved 05A security-model amendment removes `IsActive` and adds `SecurityState` and `LoginAllowed` as described in Section 10A. `DisplayName`, `CreatedAt`, `DisabledAt`, and `DisabledBy` remain. `Email`, `PhoneNumber`, normalized fields, password hash, security stamp, lockout fields, and confirmation fields remain Identity-owned.

Do not add DepartmentId, DoctorId, NurseId, PatientId, salary, clinical information, or a detailed employee profile to `ApplicationUser`. Future staff/account association belongs to the staff model or a separately approved link, not to the login aggregate.

## 6. Username normalization and change policy

Usernames are case-insensitive for authentication lookup, uniqueness, and account identification. ASP.NET Core Identity's standard username normalization mechanism supplies this behavior; the application must not add scattered custom `ToLower()`/`ToUpper()` authentication logic or a parallel normalized-username system. The administrator-assigned display form may remain in `UserName`, while Identity uses its normalized representation for lookup and uniqueness.

Ordinary users cannot change their own username. An authorized administrator may change a username through supported Identity account-management mechanisms for legitimate corrections or approved naming conventions. The stable Identity `UserId` does not change when `UserName` changes. Usernames are therefore not durable audit identities or foreign keys.

## 7. Identity key type

Use the default **opaque string Identity key** (`IdentityUser` / `IdentityRole`) with framework-generated values. Do not change `BaseEntity.Id`; domain identities remain `int`.

- **String:** best fit for the existing `CreatedBy`, `UpdatedBy`, and `DeletedBy` string fields and the default ASP.NET Core Identity stores; no domain key coupling is introduced.
- **Guid:** strong opaque semantics, but requires generic Identity types and conversion to the existing string audit fields.
- **Int:** compact and familiar in SQL Server, but encourages accidental coupling with domain IDs and exposes sequence-oriented account identifiers.

Generated Identity keys are opaque and never user-entered. The audit fields are already `nvarchar(200)`; the implementation must keep generated key values within that existing limit. Username and email are not stored as the immutable actor reference.

## 8. Domain and identity boundary

`Patient != ApplicationUser`. The current Patient aggregate is hospital master data. Patient is an authenticated first-release role, while portal workflows and ownership checks remain later Application/Web work.

The existing `Doctor` is a domain record and has no login relationship today. Future Staff/Doctor/Nurse records may have an optional one-account-per-person association, owned by the staff/application model. Some hospital records may not require login access. Disabling a login account must never delete or cascade-delete the hospital person or historical records.

## 9. Authentication strategy

- **Login identifier:** administrator-assigned unique username. Email is optional contact metadata and is not required to be unique or used as the sole login identifier; the PRD does not establish that every staff member has a unique email address.
- **Cookie authentication:** ASP.NET Core Identity application cookie for the server-rendered/Blazor application.
- **JWT:** not used now. A token scheme requires a separately approved API or integration requirement.
- **Self-registration:** `NO`. Staff accounts are administrator-provisioned.
- **Patient login:** supported as the first-release authenticated Patient role; portal workflows remain later work.

Login failures should use a generic public message and avoid revealing whether a username exists. Administrative diagnostics remain in protected logs.

## 10. Account lifecycle baseline

Before the security-model amendment, the 05A implementation baseline contained `IsActive` plus `DisabledAt`/`DisabledBy`; it preserved accounts for attribution and history and did not delete domain records. The approved amendment removes `IsActive` and uses the separate `SecurityState` and `LoginAllowed` properties. Identity failed-password lockout remains separate from administrative account state, and Doctor status, Department activity, employment status, and clinical availability remain domain concerns.

## 10A. Administrative account-control refinement

The approved security requirement distinguishes the hospital person, login identity, permission to establish a login, existing sessions, administrative account state, failed-password lockout, roles, policies, and future exceptional per-user restrictions.

The 05A `IsActive` flag is **not sufficient** for the finalized model because it cannot independently represent account state and login permission. Before 05D generates the Identity migration, the conceptual account model becomes:

```csharp
public AccountSecurityState SecurityState { get; set; } = AccountSecurityState.Active;
public bool LoginAllowed { get; set; } = true;
```

`AccountSecurityState` is a dedicated `byte` enum in the same approved Identity source area and namespace as `ApplicationUser`: `ElsheiekhHMS.Infrastructure/Identity/Entities/AccountSecurityState.cs`, namespace `ElsheiekhHMS.Infrastructure.Identity.Entities`. Its persistence-sensitive values are fixed: `Active = 0`, `Suspended = 1`, and `Banned = 2`; they must not be reordered, renumbered, or reused without deliberate migration/compatibility review. `LoginAllowed` controls whether a new authenticated session may be established. `DisabledAt` and `DisabledBy` remain administrative metadata. The account-security service must reject invalid combinations: `Suspended` and `Banned` accounts cannot have `LoginAllowed = true`.

The resulting meanings are:

| Security state | LoginAllowed | Meaning |
|---|---:|---|
| Active | true | Normal account access |
| Active | false | Emergency login block; sessions are revoked separately |
| Suspended | false | Temporary administrative suspension |
| Banned | false | Indefinite/permanent administrative restriction |

Identity failed-password lockout remains a separate Identity-owned mechanism. No domain Doctor/staff status is changed by any account-security operation. The approved 05A security-model amendment implements this account shape; migration generation and physical SQL verification remain owned by 05D.

## 11. Role model and naming

The reconciled first-release role set is exactly five roles:

| Canonical role | Purpose | Initial boundary |
|---|---|---|
| `SystemAdministrator` | Technical/security/facility administration and privileged recovery | System configuration and security policies; no automatic clinical access |
| `Administrator` | Operational HMS administration | Users, approved roles, patients, appointments, billing, payments, reports, and explicit configuration policies |
| `Receptionist` | Reception workflows | Permitted patient demographics, appointments, arrival, queue, balances, and approved receipts/payments |
| `Provider` | General clinical provider across specialties | Clinical workflows subject to care/resource context |
| `Patient` | Authenticated patient portal access | Own permitted resources only |

`Doctor` remains a domain/provider classification; `Dentist` is a specialty, not an authorization role. `Nurse`, `Cashier`, `Pharmacist`, `LabTechnician`, and `Accountant` are deferred future roles. Names are persisted security identifiers and must not be casually renamed. Put constants in `ElsheiekhHMS.Application/Common/Security/RoleNames.cs` so policies and Web composition share plain strings without adding Identity dependencies to Application. The implementation must use constants rather than scattered literals.

## 12. Roles, claims, and policies

Roles provide broad organizational grouping and are seeded deterministically. Claims are reserved for identity-specific facts that are not roles; no permission-claim catalogue is created before workflows exist. Policies are the application-facing authorization boundary and should be used instead of scattering role strings through components.

The reconciled minimal policies are:

| Policy | Authorized requirement | Resource/context boundary |
|---|---|---|
| `CanManageUsers` | `Administrator`, `SystemAdministrator` | Target-account and final-admin rules |
| `CanManageUserSecurity` | `Administrator`, `SystemAdministrator` | Account state, self-targeting, restricted-account, and final-admin rules |
| `CanViewAuditTrail` | `Administrator`, `SystemAdministrator` | Redaction and administrative scope |
| `CanManagePatients` | `Administrator`, `Receptionist` | Permitted demographic fields and workflow context |
| `CanManageAppointments` | `Administrator`, `Receptionist`, `Provider` | Schedule, assignment, and care context |
| `CanAccessClinicalRecords` | `Provider`, `Patient` | Provider care context or Patient ownership |
| `CanManageBilling` | `Administrator`, `Receptionist` | Payment, refund, and financial workflow rules |
| `CanConfigureSystem` | `SystemAdministrator` | Facility/deployment context |

These are capability boundaries rather than button-level CRUD policies. Clinical access is never implied by administrative roles. Additional module policies require an approved Application contract.

## 13. Initial role/capability matrix

`ALLOW` means justified by the PRD now; `DENY` is an explicit security boundary; `DEFER` means the role or workflow is not yet modeled and must not be invented in Phase 05.

| Capability | SystemAdministrator | Administrator | Receptionist | Provider | Patient |
|---|---|---|---|---|---|
| Manage users and roles | ALLOW | ALLOW | DENY | DENY | DENY |
| Manage sensitive account security | ALLOW | ALLOW | DENY | DENY | DENY |
| View audit trail | ALLOW | ALLOW | DENY | DENY | DENY |
| Register/search patients | DEFER | ALLOW | ALLOW | DEFER | DENY |
| Manage appointments and operational queue | DEFER | ALLOW | ALLOW | ALLOW | OWN/ELIGIBLE |
| Clinical records and prescriptions | DENY | DENY | DENY | CONTEXT | OWN/PERMITTED |
| Billing and payments | DEFER | ALLOW | ALLOW | LIMITED STATUS | OWN |
| System/facility configuration | ALLOW | DEFER | DENY | DENY | DENY |

`OWN`, `CONTEXT`, and `LIMITED STATUS` require application/resource authorization. The public health endpoint and any explicitly approved public surface are separate anonymous endpoints, not role capabilities.

## 14. Security ownership by layer

**Core:** remains independent. No IdentityUser, ClaimsPrincipal, HttpContext, cookie, or authorization framework reference.

**Application:** owns `ICurrentUser` and plain role/policy-name constants when a use case needs them. It does not depend on HttpContext or Identity EF stores.

**Infrastructure:** owns `ApplicationUser`, Identity EF stores, Identity persistence configuration, account technical services, and the future audit persistence/interception implementation.

**Web:** owns authentication/authorization composition, middleware ordering, policy registration, and future login/logout endpoints or Blazor security surfaces. UI visibility never replaces backend authorization.

## 15. Current-user abstraction

Introduce the Application contract during the approved 05C subphase because audit attribution and use-case authorization need a stable actor boundary:

```csharp
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
}
```

The contract exposes no `HttpContext` or `ClaimsPrincipal`. A Web adapter reads the authenticated request and is registered at the composition root; Application services consume only the interface. Background jobs use an explicit system actor rather than pretending to be an interactive user.

## 16. Audit attribution design

Store the immutable Identity `UserId` string in existing `CreatedBy`, `UpdatedBy`, and `DeletedBy` fields. Do not store mutable usernames as the primary actor reference. The future `AuditLog` snapshot may additionally store `UserName` and the effective role for readable historical reporting.

Reserved non-user actors are stable values such as `system` and `background`; they must be used explicitly and never by silently treating an unauthenticated request as an administrator.

The future Infrastructure audit mechanism should use a `SaveChanges` interceptor or equivalent Infrastructure-owned unit around the existing context to populate timestamps and actor attribution together, capture before/after data, and append the audit record in the same transaction. It must exclude passwords, reset tokens, security stamps, cookies, and other secrets. Core constructors remain domain-validating; Core does not resolve the current user.

The full immutable AuditLog entity and transaction behavior require an implementation gate because no audit entity currently exists. Phase 05 establishes the actor contract and Identity event design; the cross-module audit implementation must remain consistent with P0-F009.

## 17. Password and lockout policy

- Identity’s built-in PBKDF2 password hasher; never custom password cryptography.
- Minimum length: 8 characters.
- Require at least one uppercase letter, one digit, and one non-alphanumeric character.
- Do not add arbitrary extra complexity or password history before a requirement exists.
- Five failed attempts trigger a fifteen-minute Identity lockout.
- Lockout applies to new users and administrator accounts; administrator recovery is an operational procedure, not a bypass.
- Account reset is administrator-initiated and displays a temporary credential once; reset tokens and passwords never enter logs or audit snapshots.

## 18. Cookie, session, and antiforgery security

The initial application cookie should use the PRD’s eight-hour expiry with sliding renewal, `HttpOnly = true`, and `SameSite = Lax`. Production requires HTTPS and `SecurePolicy = Always`; development may use the normal local HTTPS setup without weakening production settings. Security-stamp validation must support account blocking, suspension, banning, and sign-out/revocation behavior. The target for an administrative block is rejection on the next authenticated request; an in-flight request cannot be retroactively cancelled.

Blocking must set `LoginAllowed = false` and rotate the Identity security stamp. Cookie validation must check the security stamp and administrative account state using built-in Identity mechanisms. Restoring access sets the state back to `Active`, rotates the stamp again, and requires fresh authentication. No custom session-token infrastructure is introduced.

The existing `UseAntiforgery()` pipeline remains enabled. State-changing Razor/Blazor or endpoint operations require ASP.NET Core antiforgery protection; global antiforgery disabling is prohibited.

HTTPS redirection and HSTS already exist in the Web host. CSP, frame restrictions, content-type headers, and broader Web hardening belong to the later enterprise/backend hardening gate after actual UI assets and endpoints exist.

## 19. Role seeding and administrator bootstrap

Role records should be seeded idempotently and deterministically without passwords. Role seeding is separate from user creation.

The first administrator should be created by an explicit deployment/development bootstrap command or tool that reads the username, display name, and password from User Secrets or environment variables. It must:

- require an explicit bootstrap invocation;
- fail safely when required values are absent or invalid;
- refuse to overwrite an existing privileged account;
- never place a password in a migration, source file, committed appsettings file, or log;
- avoid silently recreating an administrator on every Web startup.

The exact command surface is an implementation detail for the approved plan; the policy is fixed here.

## 20. Persistence and migration strategy

`InitialCreate` remains unchanged. The proposed first Identity migration is `AddIdentityFoundation`, owned by Infrastructure and generated only after the DbContext and Identity model implementation is approved.

Expected Identity tables are:

- `AspNetUsers`
- `AspNetRoles`
- `AspNetUserClaims`
- `AspNetUserLogins`
- `AspNetUserRoles`
- `AspNetRoleClaims`
- `AspNetUserTokens`

The migration must preserve the existing six domain tables and their constraints. Before 05D generates `AddIdentityFoundation`, the finalized account model must be settled. The new Identity migration must add the administrative account-state and login-permission columns, while `InitialCreate` remains unchanged. A fresh integration database must apply `InitialCreate` followed by `AddIdentityFoundation`; the existing development database must not be changed during design review.

## 21. DbContext and DI impact

During implementation, `ElsheiekhHmsDbContext` is expected to derive from `IdentityDbContext<ApplicationUser>`, call the Identity base `OnModelCreating`, then apply the existing entity configurations. Identity table naming/schema, ApplicationUser scalar configuration, and the future account-state mapping remain in Infrastructure.

Infrastructure will register the Identity EF stores and options. Web will compose authentication, authorization, named policies, current-user adapter, and middleware. The expected middleware order is HTTPS/exception handling, authentication, authorization, then antiforgery and endpoint/component mapping according to the ASP.NET Core hosting pipeline. No code is changed by this design.

## 21A. Administrative security operations

The sensitive account-management boundary is the named `CanManageUserSecurity` policy, requiring the canonical `Administrator` or `SystemAdministrator` role. It covers login block/unblock, session revocation, credential-reset initiation, administrator username changes, suspension/restoration, banning, and privileged role changes. `CanManageUsers` remains the broader account-administration policy; the sensitive operations require the narrower security policy.

The approved operation sequence is:

1. Authorize the administrator with `CanManageUserSecurity`.
2. Change the target account's administrative state and `LoginAllowed` value.
3. Rotate the Identity security stamp when sessions must be revoked.
4. Record the security event through the future 05C audit boundary.

Password recovery uses `UserManager.GeneratePasswordResetTokenAsync` and `ResetPasswordAsync`; administrators never read the old password. Username changes use `UserManager.SetUserNameAsync`; the stable UserId does not change. No username or hard-coded UserId is an authorization bypass.

## 21B. Restrictions, precedence, and administrator protection

The normal authorization model remains roles plus named policies. A generic per-user explicit-deny engine is deferred because no current workflow requires it. If introduced later, an explicit deny must be evaluated before ordinary role/policy allow and must not be bypassed by a broad role without a separately approved break-glass rule.

Account-security restrictions take precedence over authentication, administrative state, roles, policies, and application authorization. A blocked, suspended, or banned account cannot regain access because it retains a Provider or Administrator role.

Users cannot assign roles to themselves. Role names must come from the canonical set. Granting or revoking the Administrator or SystemAdministrator role requires `CanManageUserSecurity`; blocked, suspended, or banned accounts cannot receive either role. Final-Administrator protection belongs to the later user-management/security-hardening workflow and must be atomic and auditable.

## 21C. Security-event ownership

Persistent security-event auditing belongs to 05C with current-user attribution. Events include `LoginBlocked`, `LoginRestored`, `SessionsRevoked`, `PasswordResetInitiated`, `UsernameChanged`, `RoleGranted`, `RoleRevoked`, `AccountSuspended`, `AccountRestored`, and `AccountBanned`. Each event records actor UserId, target UserId, action, timestamp, and reason where appropriate. Passwords, hashes, reset tokens, cookies, and security stamps are never recorded.

## 21D. Performance and reliability implications

The approved cross-cutting performance, reliability, and quality requirement applies to Phase 05. Identity security features must use framework-supported role/policy checks efficiently and must not trigger uncontrolled database work on every component render or trivial operation. Emergency session revocation must explicitly balance revocation latency against request/database overhead without weakening the authoritative block requirement. Password reset, username changes, role changes, and account-state changes must use appropriate async APIs, cancellation where meaningful, safe transaction boundaries, structured redacted logging, deliberate retries, and correct resource lifetimes. No cache, Redis, broker, custom token infrastructure, speculative background processing, or new package is authorized by this requirement. Phase 05 implementation gates must report performance, reliability, security, query, and regression-test impact.

## 22. Package requirements

| Package | Project | Version | Required | Reason |
|---|---|---|---|---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Infrastructure | `10.0.12` (installed in 05A) | YES | Identity entities, EF stores, and `IdentityDbContext` |
| `Microsoft.EntityFrameworkCore.Design` | Infrastructure/Web | Already `10.0.12` | YES, existing | Design-time migration tooling |
| `Microsoft.EntityFrameworkCore.SqlServer` | Infrastructure | Already `10.0.12` | YES, existing | Existing SQL Server provider |
| `Microsoft.AspNetCore.Identity.UI` | Web | None planned | NO | No Identity UI is implemented in Phase 05 design review |
| JWT bearer package | Web | None planned | NO | No token API requirement exists |

No package is required by the administrative account-control refinement. The approved Identity package is already installed by 05A; no additional package or version change is authorized.

## 23. Testing strategy

Phase 05 implementation should add tests in layers:

- **Unit:** Finalized account-state transitions, role/policy constants, account-block semantics, and current-user fallback behavior.
- **Identity integration:** fresh isolated database applies both migrations; Identity tables exist; roles are idempotent; user creation hashes passwords; duplicate usernames are rejected; blocked/suspended/banned accounts cannot authenticate; lockout follows 5/15; role assignment is restricted; security-stamp revocation rejects existing sessions.
- **Authorization integration:** each named policy allows its approved roles and denies unauthorized roles at the server boundary.
- **Audit integration:** login, failed login, role changes, account block/restoration, session revocation, password-reset initiation, username changes, and future entity writes capture stable actor attribution without secrets.
- **Web integration:** cookie login/logout, access denied behavior, antiforgery on state-changing operations, generic authentication failures, and security-cookie settings.
- **Bootstrap safety:** missing configuration fails safely, explicit invocation is required, existing administrator accounts are not overwritten, and no password is logged or persisted outside Identity hashing.

## 24. Integration database strategy

Continue using `ElsheiekhHMS_IntegrationTests`. Do not create another physical test database. Keep the current exact-target guard and fixture cleanup. After Identity implementation, the integration fixture must apply migrations from scratch in order and verify that the development database remains untouched.

## 25. Threat review

| Threat | Current design mitigation | Remaining owning phase |
|---|---|---|
| Credential theft | PBKDF2, HTTPS, secure HttpOnly cookies, external secrets | 05 implementation and deployment hardening |
| Brute-force login | Identity five-attempt/fifteen-minute lockout and failed-login audit event | 05E verification; rate limiting only if evidence requires it |
| Privilege escalation | Admin-only role management, policy checks, server-side enforcement, audit of changes | 05B/05E |
| Default admin credentials | Explicit secret-backed bootstrap; no migration/startup default | 05D/05E |
| Account enumeration | Generic public login/reset responses and protected diagnostics | 05E/Web security tests |
| CSRF | Existing antiforgery middleware retained; tokens required for writes | Web implementation gate |
| Session theft | HTTPS, Secure/HttpOnly/SameSite cookies, sliding expiry, security-stamp validation | 05E and later hardening |
| Authorization bypass | Default-deny policies and server-side endpoint/use-case tests | 05B/05E |
| Sensitive logging | Redact passwords, tokens, cookies, security stamps, and clinical secrets | 05C/05E |
| Database credential leakage | User Secrets/environment configuration; no committed credentials | All phases |

## 26. Proposed file structure

No implementation files are created by this design review. The minimum expected Phase 05 implementation structure is:

```text
ElsheiekhHMS.Infrastructure/
  Identity/
    Entities/ApplicationUser.cs
    IdentityConfiguration.cs
    IdentityRoleSeeder.cs
    CurrentUser/
    Auditing/

ElsheiekhHMS.Application/
  Common/Security/
    RoleNames.cs
    PolicyNames.cs
    ICurrentUser.cs

ElsheiekhHMS.Web/
  Security/
    CurrentUserAccessor.cs
    AuthenticationConfiguration.cs

ElsheiekhHMS.Tests/
  Unit/Identity/
  Integration/Identity/
  Integration/Persistence/
```

Login, registration, account-management, and role-dashboard UI remain deferred to the appropriate later Web/application work. No Login.razor or Register.razor is created in Phase 05 design review.

## 27. Proposed Phase 05 implementation subphases

### 05A — Identity foundation and security-model amendment

**Purpose:** Add the minimal ApplicationUser model, aligned package, same-context Identity base, options, stores, and the approved persisted account-security model.

**Allowed changes:** Infrastructure Identity files, existing context base, DI registration, and focused tests.

**Database impact:** New Identity migration model only; no database update until the separate migration gate.

**Test gate:** Model builds, Identity store integration passes against the isolated database, and Core remains dependency-free.

**Stop conditions:** Any Core dependency, migration edit to `InitialCreate`, package-version mismatch, development-database target, or implementation of account-control workflows beyond the approved data model.

### 05B — Roles and authorization

**Purpose:** Represent the five reconciled first-release roles, register the smallest named policies, define clinical confidentiality and resource-authorization boundaries, and finalize the `CanManageUserSecurity` boundary without implementing account-management workflows.

**Allowed changes:** Application role/policy constants, Infrastructure idempotent role-seeding design, Web policy composition, authorization tests, and security-operation policy vocabulary.

**Database impact:** Role rows only after explicit migration/database approval.

**Test gate:** Role seeding is idempotent; fallback/anonymous behavior and policy allow/deny boundaries pass server-side; resource-authorization tests remain with later Application workflows.

**Stop conditions:** New unapproved roles, scattered role literals, UI-only enforcement, or speculative permission claims.

### 05C — Current user and audit attribution

**Purpose:** Establish `ICurrentUser`, actor resolution, account lifecycle events, and the approved audit attribution mechanism.

**Allowed changes:** Application abstraction, Web adapter, Infrastructure interceptor/audit implementation, redaction tests.

**Database impact:** Any AuditLog schema must be separately reviewed; existing domain tables remain compatible with string actor IDs.

**Test gate:** Stable user ID attribution, system/background attribution, role snapshots, atomic audit behavior, and secret exclusion.

**Stop conditions:** HttpContext in Application/Core, username-only attribution, mutable historical references, or secrets in audit data.

### 05D — Identity migration and SQL verification

**Purpose:** Generate `AddIdentityFoundation`, apply it to the approved development database only after approval, and verify schema parity.

**Allowed changes:** New migration and migration verification tests.

**Database impact:** `InitialCreate` remains unchanged; the new migration adds Identity tables to the same physical database.

**Test gate:** Fresh integration database applies both migrations; development database is explicitly verified before/after.

**Stop conditions:** Any destructive migration, duplicate physical database, pending model drift, or Identity table outside the approved schema.

### 05E — Security integration tests and hardening

**Purpose:** Verify cookies, login/logout, lockout, disablement, role assignment, policies, antiforgery, bootstrap safety, and security logging.

**Allowed changes:** Tests and narrowly scoped security configuration fixes.

**Database impact:** Test database only until deployment approval.

**Test gate:** Full solution tests plus focused Web/Identity integration suite pass with no secret leakage.

**Stop conditions:** Any authorization bypass, default credential, database-target violation, or unverified security behavior.

## 28. Documentation decisions to record after approval

After the human design gate approves this artifact, record the accepted decisions in `docs/DECISIONS.md` and update `docs/ARCHITECTURE.md` only as part of the approved implementation checkpoint:

- same `ElsheiekhHmsDbContext` and same physical SQL Server database;
- opaque string Identity key independent of domain integer IDs;
- ApplicationUser is a login account, not a patient or staff aggregate;
- optional future staff/account association;
- five reconciled canonical role names and policy boundaries;
- Provider as the general clinical role, with Dentist as a specialty rather than a role;
- Patient as an authenticated first-release role with ownership checks;
- separate Administrator and SystemAdministrator boundaries without clinical bypass;
- fallback authentication with explicit anonymous exceptions;
- username login with optional email metadata and no public self-registration;
- stable user-ID audit attribution and explicit system/background actors;
- secret-backed administrator bootstrap;
- separate administrative account state (`Active`, `Suspended`, `Banned`) from `LoginAllowed` before 05D migration generation;
- security-stamp session revocation for account blocking and restoration;
- administrator-only `CanManageUserSecurity` policy for sensitive account operations;
- password reset through supported Identity token APIs without exposing old passwords;
- security-event auditing in 05C without recording passwords, tokens, cookies, or hashes;
- new Identity migration after unchanged `InitialCreate`.

## 29. Design blockers

No technical or design blocker remains. Phase 05 design is complete and 05A has been implemented under its separate human approval. The administrative account-control refinement and 05B design are finalized; implementation, current-user auditing, migration/database rollout, and security hardening remain separately gated.

## 30. Explicit non-actions

This design review does not:

- install `Microsoft.AspNetCore.Identity.EntityFrameworkCore`;
- change `ElsheiekhHmsDbContext`;
- modify Core, Application, Infrastructure, or Web production code;
- generate or edit a migration;
- update `ElsheiekhHMS_Dev` or any other database;
- create users, roles, cookies, or Identity tables;
- begin Phase 06;
- stage, commit, or push changes.
