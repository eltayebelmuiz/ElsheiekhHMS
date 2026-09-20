<!--
Sync Impact Report
- Version change: unversioned scaffold -> 1.0.0
- Modified principles: placeholder principles -> I. Layered Architecture and Application-Owned Behavior;
  II. Clinical and Financial Data Integrity; III. Transactional Auditability;
  IV. Server-Enforced Authorization and Secret Handling; V. Type-Safe, Asynchronous, Explicit Code
- Added sections: Implementation Constraints; Compliance and Delivery Workflow
- Removed sections: none
- Follow-up TODOs: Determine the original ratification date from project history.
-->

# Elsheiekh Hospital Management System Constitution

## Core Principles

### I. Layered Architecture and Application-Owned Behavior
The system MUST maintain the dependency direction Core -> Application -> Infrastructure -> Web.
Core contains intrinsic domain invariants, domain behavior, and abstractions. Application contains
use-case orchestration and rules requiring coordination, persistence queries, authorization, or
external dependencies. Infrastructure contains persistence and external-system implementations;
Web contains endpoints, Blazor presentation, and transport concerns. Blazor components MUST NOT
contain business logic or
direct database access. Components MUST delegate behavior to Application services and remain focused
on presentation, input binding, and user interaction. This separation keeps clinical behavior
testable and prevents the UI from becoming an alternative business layer.

### II. Clinical and Financial Data Integrity
Clinical and financial records MUST use soft deletion only. No application record may ever be
hard-deleted through EF Core, SQL, background jobs, administrative tools, or endpoints. Soft-deleted data
MUST remain excluded from normal reads while remaining available for authorized audit and reporting
purposes. Every monetary value MUST use `decimal`; `float` and `double` MUST NOT be used for money.
These rules preserve medical and financial history and prevent avoidable rounding and reconciliation
errors.

### III. Transactional Auditability
Every operation that writes application state MUST create a corresponding `AuditLog` entry in the
same database transaction. This includes creation, modification, and soft deletion of records, as
well as security-sensitive state changes. The audit entry MUST identify the actor, action, affected
record, and operation time without storing passwords, tokens, or other prohibited secrets. If either
the state change or its audit entry fails, the transaction MUST roll back as a unit. This provides a
complete, reliable history for clinical accountability and financial review.

### IV. Server-Enforced Authorization and Secret Handling
Every endpoint and server-side operation that exposes or changes application data MUST enforce
authorization on the server using the applicable identity, role, or policy. Hiding a menu item,
button, route, or component is a usability measure and MUST NEVER be treated as a security control.
Passwords, access tokens, refresh tokens, and equivalent credentials MUST be excluded from logs,
exceptions, telemetry, and audit trail values. Sensitive values MUST be redacted or omitted before
any diagnostic or audit record is written. This protects patient, financial, and account data even
when clients are modified or requests are forged.

### V. Type-Safe, Asynchronous, Explicit Code
Nullable reference types MUST be enabled for all applicable projects. The null-forgiving operator
(`!`) MUST NOT be used without a documented, locally verifiable justification. All database and EF
Core operations MUST use `async`/`await` with cancellation support where available; synchronous EF
Core calls MUST NOT be introduced. Magic strings MUST NOT encode domain behavior, authorization
policies, entity identifiers, or persistence keys: use constants, enums, named options, and strongly
typed keys instead. These constraints make invalid states visible during development and keep
contracts consistent across layers.

## Implementation Constraints

- Core MUST remain independent of Infrastructure and Web concerns.
- Core MUST own intrinsic domain invariants and domain behavior. Application services MUST own
  use-case orchestration and rules requiring coordination, persistence queries, authorization, or
  external dependencies.
- Infrastructure MUST encapsulate EF Core, transactions, audit persistence, and external integrations.
- Web endpoints and Blazor components MUST call Application contracts rather than `DbContext`
  directly.
- Persistence mappings MUST preserve decimal precision for monetary fields and apply soft-delete
  filtering without removing historical rows.
- Authorization policies, audit action names, record categories, and other cross-layer identifiers
  MUST be defined through shared constants, enums, or strongly typed keys.

## Compliance and Delivery Workflow

- Every change MUST be reviewed against this constitution before merge.
- Changes affecting persistence, authorization, audit behavior, or monetary calculations MUST include
  focused automated coverage for the affected rule and its failure path.
- Reviewers MUST verify that new write paths create audit records transactionally and that new
  clinical or financial paths cannot hard-delete data.
- Builds and tests MUST pass before release, and compliance findings MUST be resolved before merge.
- A change that cannot satisfy a principle MUST be treated as a constitution amendment, not as an
  undocumented exception.

## Governance

This constitution is the governing standard for architecture, data handling, security, and delivery
decisions in the Elsheiekh Hospital Management System. It supersedes conflicting local conventions.
Amendments require a written proposal describing the motivation, affected principles, compatibility
impact, migration or remediation plan, and verification approach. The proposal MUST be reviewed and
approved by the project owners before the amended constitution is merged.

The constitution follows Semantic Versioning. A MAJOR version removes or weakens a non-negotiable
principle, a MINOR version adds a principle or materially expands the governance contract, and a
PATCH version clarifies wording without changing obligations. Every amendment MUST update the sync
impact report, last-amended date, and version line together.

Compliance MUST be checked during code review and at release readiness. Reviewers MUST inspect new
endpoints, write operations, EF Core calls, monetary fields, null suppression, cross-layer references,
and logging or audit payloads. Violations MUST be corrected before approval; unresolved findings
require a constitution amendment rather than an informal waiver.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date unknown | **Last Amended**: 2026-09-20
