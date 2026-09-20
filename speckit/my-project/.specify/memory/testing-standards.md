# Elsheiekh Hospital Management System Testing Standards

These standards define the minimum automated coverage for the Elsheiekh Hospital Management
System. They supplement the project constitution and apply to all new and changed behavior.

## Test Layers

### Application Unit Tests

- Every business rule in the Application layer MUST have unit tests.
- Unit tests MUST execute without a database, EF Core provider, network, filesystem, or external
  service.
- Tests MUST cover successful outcomes, validation failures, authorization failures exposed by the
  service contract, and relevant boundary conditions.
- Dependencies MUST be replaced with focused fakes or mocks at the Application boundary; tests MUST
  not verify EF Core implementation details.

### Repository and Service Integration Tests

- Repository and service integration tests MUST use EF Core InMemory.
- Each test MUST use an isolated database name or a newly created in-memory store and MUST clean up
  its service scope and data after execution.
- Integration tests MUST verify persistence, query filters, soft deletion, service orchestration,
  transaction-aware audit behavior, and the mapping of domain outcomes to application results.
- Tests MUST use `decimal` values for all monetary assertions.

## Required Workflow Coverage

### Critical Clinical and Financial Workflow

At least one end-to-end application workflow test MUST cover the complete sequence:

`patient registration -> queue -> EMR -> lab -> billing -> payment`

The test MUST assert that each stage creates the expected state, passes the correct identifiers to
the next stage, enforces authorization, and produces the required audit entries. It MUST also cover
the failure behavior when a stage rejects the request, including the absence of invalid downstream
records.

### Status Transitions

For each of `Queue`, `Appointment`, `Lab`, and `Invoice`, tests MUST cover:

- Every permitted status transition.
- Every prohibited transition from each terminal or incompatible status.
- Repeated transition attempts and idempotency behavior where applicable.
- Audit entries and user-facing outcomes for accepted and rejected transitions.

Status tests MUST assert the resulting status and MUST NOT rely only on UI state or displayed labels.

## Concurrency and Authorization

### Concurrent Edits

Concurrency tests MUST use two independent user scopes or context instances editing the same
`Patient` and the same `Invoice` concurrently. Each scenario MUST assert that:

- The system detects the stale write or conflicting version.
- At most one conflicting update is accepted.
- The losing operation returns a safe conflict result rather than silently overwriting data.
- The final record, monetary totals, and AuditLog entries remain consistent.

### Authorization

For every protected Application service and endpoint, each defined role MUST attempt actions both
inside and outside its permission scope. Tests MUST assert server-side allow or deny behavior for
each attempt. Tests MUST NOT treat hidden navigation, disabled controls, or component visibility as
authorization coverage.

## Audit Trail Coverage

Every write-operation test MUST verify that exactly the correct `AuditLog` entry is produced,
including actor, action, affected record, timestamp presence, and relevant before/after values.
Audit tests MUST cover creates, updates, soft deletes, status changes, payments, authorization-
sensitive changes, and rejected writes where the system records an attempted action.

Audit assertions MUST verify that passwords, access tokens, refresh tokens, and equivalent secrets
never appear in audit values, logs, exceptions, or telemetry payloads. When a write fails, tests MUST
verify that the state change and its AuditLog entry roll back together.

## Financial and Edge-Case Coverage

Financial tests MUST cover all of the following using exact `decimal` assertions:

- Partial payment reduces the outstanding balance correctly.
- Zero payment is rejected or handled according to the defined business rule without changing the
  invoice balance.
- Overpayment is rejected or handled according to the defined business rule without corrupting the
  invoice balance or payment total.
- Cancelled invoice payment is rejected and creates no invalid payment record.

Edge-case tests MUST cover at least:

- Duplicate national ID registration.
- Assignment of an occupied bed.
- Payment against a cancelled invoice.
- Repeated requests where the operation must be idempotent.
- Missing, stale, or unauthorized record references.

## Naming and Quality Rules

- Test names MUST follow `MethodName_Scenario_ExpectedResult`.
- Test names MUST describe observable behavior, not implementation details.
- Test data MUST be deterministic, minimal, and explicit about the scenario it represents.
- Tests MUST assert outcomes and important side effects; broad non-null or no-exception assertions
  alone are insufficient.
- New or changed business behavior MUST include the applicable unit tests first, followed by
  integration or workflow coverage where persistence, authorization, auditing, or cross-module
  behavior is involved.
- A change MUST NOT be considered complete while its required tests are missing, disabled, flaky,
  or dependent on shared mutable test state.
