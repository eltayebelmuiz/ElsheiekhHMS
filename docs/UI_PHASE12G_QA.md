# Phase 12G consolidated UI QA handoff

Status: source-level review complete; external browser execution required.

This document consolidates the Phase 12A–12F module checklists for the final
UI acceptance gate. It records what was verified from source and what still
needs a browser-in-loop run. It does not certify production performance or
replace backend authorization and workflow tests.

## Route inventory

| Area | Routes reviewed |
| --- | --- |
| Authentication/access | `/login`, `/access-denied`, `/Error`, `/not-found` |
| Dashboard | `/` |
| Patients | `/patients`, `/patients/new`, `/patients/{id}`, `/patients/{id}/edit` |
| Departments | `/departments`, `/departments/new`, `/departments/{id}`, `/departments/{id}/edit` |
| Appointments | `/appointments`, `/appointments/new`, `/appointments/{id}` |
| Queue | `/queue`, `/queue/new`, `/queue/{id}`, `/appointments/{id}/queue` |

No Weather, Counter, or future clinical, billing, pharmacy, reporting, or
provider portal route is present in the current Web components.

## Source review completed

- The dashboard uses only two bounded service searches for the current
  Africa/Kigali date: five appointment rows and five Waiting queue rows. Counts
  come from `PagedResult.TotalCount`; no fake KPI values, unbounded query, N+1
  loop, chart, polling, cache, or reporting layer was added.
- Dashboard quick actions link to existing Patient registration, Appointment
  scheduling, walk-in Queue creation, and Queue registry workflows. A
  SystemAdministrator receives only the existing Department configuration link.
- Patient, Department, Appointment, and Queue pages use the shared PageHeader,
  alert, loading, empty, error, status, form, table, details, pagination, and
  confirmation patterns. The accepted appointment-to-queue orchestrator and
  walk-in service entry points remain unchanged.
- The static foundation showcase and its unused CSS selectors were removed
  after confirming they had no consumers. The undefined `--hms-text-muted`
  references were replaced with the defined `--hms-muted` token.
- Dashboard loading is cancellable on component disposal. Retry, loading,
  empty, no-result, validation, safe-error, stale-concurrency, and terminal
  workflow states remain represented by existing shared components.
- Semantic headings, labels, table captions, status text, alert roles,
  visible focus outlines, reduced-motion CSS, logical positioning, and the
  mobile navigation relationship are present in source.

## Browser handoff checklist

### Authentication and shell

- [ ] Login validation and safe error text at desktop and mobile widths.
- [ ] Authenticated shell, active navigation, account menu, logout, and access
  denied route.
- [ ] Sidebar drawer opens/closes at tablet/mobile widths with keyboard access,
  visible focus, and no content trap.

### Dashboard

- [ ] Administrator or Receptionist sees real appointment and Waiting queue
  totals for the current Kigali date.
- [ ] Dashboard totals and five-row lists match the corresponding registries.
- [ ] Retry, empty, and operational error states render without a blank flash.
- [ ] Quick actions navigate to the approved existing workflows.
- [ ] SystemAdministrator sees Department configuration access without
  unsupported operational metrics.

### Module consistency

- [ ] Patient registry, lookup, registration, details, edit, validation, and
  stale-concurrency recovery.
- [ ] Department registry, create, edit, deactivation confirmation, inactive
  state, and no reactivation control.
- [ ] Appointment registry, bounded filters, Kigali date/time, scheduling,
  lifecycle actions, stale-concurrency recovery, and no reschedule/Encounter.
- [ ] Queue registry, bounded filters, walk-in creation, backend ticket,
  lifecycle actions, linked handoff, historical link, and no requeue/
  QueuePosition/Encounter behavior.

### Viewports and keyboard

- [ ] 1440px desktop.
- [ ] Approximately 1024px laptop.
- [ ] Approximately 768px tablet.
- [ ] Approximately 390px mobile.
- [ ] Tab/Shift+Tab order, Enter/Space activation, form submission, dialogs,
  cancellation, pagination, visible focus, and no keyboard traps.
- [ ] Table identity and actions remain reachable through horizontal overflow;
  forms and dialogs do not clip.

## Findings

Source-level findings fixed in Phase 12G: **2** (static dashboard showcase/
placeholder value; undefined muted-token references), plus the related unused
showcase CSS removal and shell/dashboard accessibility hardening.

Critical findings: **none identified from source**.

High findings: **none identified from source**.

Medium/low findings: **none requiring a code change**.

Google Antigravity and a browser screenshot pass were not available in this
repository session. External browser execution and representative screenshot
evidence remain required before Phase 12H final acceptance.
