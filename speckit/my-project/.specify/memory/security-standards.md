# Elsheiekh Hospital Management System Security Standards

These standards define mandatory protections for identity, sessions, requests, authorization,
clinical records, and audit data. They apply to every deployment and server-side operation, with
production-specific transport and cookie requirements called out explicitly.

## Passwords and Account Protection

- All passwords MUST be hashed with ASP.NET Identity's PBKDF2 password hasher. Plaintext, reversible,
  encrypted, or otherwise recoverable password storage is prohibited.
- The minimum password policy MUST require at least 8 characters, 1 uppercase character, 1 digit,
  and 1 special character.
- An account MUST lock out after 5 failed login attempts and remain locked for at least 15 minutes.
- Passwords, password reset values, tokens, and security stamps MUST be excluded from logs,
  exceptions, telemetry, diagnostics, and audit trail entries.

## Request and Transport Security

- CSRF anti-forgery tokens MUST be validated on every state-changing Blazor operation and every
  state-changing API operation.
- Production deployments MUST enforce HTTPS for all application traffic.
- IIS MUST automatically redirect HTTP requests to HTTPS in production.
- Production session cookies MUST use `HttpOnly=true`, `SameSite=Lax`, and `Secure=true`.
- Authentication and session configuration MUST reject insecure production defaults during deployment
  validation.

## Server-Side Authorization

- Authorization MUST be verified server-side for every action, endpoint, and Application service that
  reads or changes protected data.
- Authorization MUST use the authenticated identity and the applicable role or policy at the point
  of action; a prior UI check MUST NOT be treated as authorization.
- Hidden navigation, hidden components, disabled buttons, and route visibility MUST NEVER be the only
  control preventing an unauthorized action.
- Unauthorized requests MUST fail with the appropriate server-side response and MUST NOT disclose
  whether protected data exists beyond the defined contract.

## Clinical Record Protection

- Clinical records MUST be soft-deleted only. Hard deletion is prohibited for Patient, EMR, LabTest,
  and all other clinical records.
- The application MUST expose no hard-delete endpoint for `Patient`, `EMR`, or `LabTest`.
- Authorized reads MUST exclude soft-deleted clinical records by default while preserving them for
  approved audit and reporting workflows.
- Tests and code review MUST verify that repositories, services, endpoints, background jobs, and
  administrative actions cannot bypass the soft-delete rule.

## Audit Trail Integrity

- `AuditLog` MUST be append-only. The application MUST expose no update or delete endpoint for
  `AuditLog`.
- Audit log storage MUST reject mutation and deletion paths, including through administrative APIs or
  ordinary application services.
- Every write operation MUST produce its audit entry within the same transaction, consistent with
  the project constitution.
- Audit entries MUST omit passwords, tokens, security stamps, and equivalent credentials from all
  values, metadata, exception details, and serialized payloads.
- Audit access MUST be authorized and read-only for consumers that do not own the append operation.

## Security Verification

- Automated tests MUST cover password hashing, password policy, lockout thresholds and duration,
  CSRF rejection, HTTPS and cookie configuration, server-side authorization, clinical soft deletion,
  and AuditLog append-only behavior.
- Security review MUST inspect every new state-changing operation for CSRF protection, authorization,
  secret leakage, and correct audit behavior before release.
