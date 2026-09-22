# Phase 12 frontend acceptance and freeze

Status: **accepted and frozen** at commit `docs(phase12): accept and freeze frontend baseline`.

## Accepted scope

Phase 12 includes the authentication presentation and anonymous/authenticated
shell, focused navigation, the Patient, Department, Appointment, Queue, and
Appointment-to-Queue screens, shared feedback/form/list patterns, and the
bounded operations dashboard. The Web project remains a Blazor Interactive
Server client of the accepted Application contracts.

The design system is documented in `docs/UI_DESIGN_SYSTEM.md`. The final
cross-module checklist is `docs/UI_PHASE12G_QA.md`; the module checklists are
`docs/UI_PATIENT_QA_CHECKLIST.md`, `docs/UI_DEPARTMENT_QA_CHECKLIST.md`,
`docs/UI_APPOINTMENT_QA_CHECKLIST.md`, and `docs/UI_QUEUE_QA_CHECKLIST.md`.

## Final acceptance evidence

- The dashboard uses only bounded `IAppointmentService` and `IQueueService`
  searches for the current Africa/Kigali date. Counts come from server
  `TotalCount`; no fake metrics, unbounded query, N+1 loop, reporting layer,
  or production SLA claim was added.
- Login, access denied, and anonymous protected-route behavior were exercised
  against the local development host with synthetic invalid credentials. The
  global authenticated fallback policy now leaves fingerprinted static assets
  anonymous, so the login and error surfaces receive their CSS, scripts, and
  favicon.
- The in-app browser checked the login surface at 1440px, 1024px, 768px, and
  390px widths; desktop and mobile screenshots were observed; keyboard focus
  moved through username, password, and sign-in in logical order.
- Google Antigravity and authenticated browser workflows were unavailable in
  this environment. No privileged credentials were created or used, and no
  screenshot archive was committed. This is an accepted non-blocking external
  QA limitation; source review and automated tests remain the evidence for
  protected module behavior.

## Boundaries and limitations

The backend remains frozen: Core, Application, Infrastructure, Identity,
schema, migrations, snapshots, and packages are unchanged. The UI does not
invent Patient ownership, Provider ownership, duplicate detection, Department
reactivation, Appointment rescheduling, QueuePosition, Encounter, or future
clinical, billing, reporting, pharmacy, or notification modules. Appointment
codes, queue dates, queue sequence, and Appointment-to-Queue authority remain
server controlled. Production performance targets still require production
measurement.

## Verification

`dotnet restore`, `dotnet build`, three consecutive full `dotnet test` runs
(418 passed each), `git diff --check`, the EF pending-model check, and the
Graphify freshness check all passed. The final frontend baseline is frozen for
future roadmap work; Phase 13 and future clinical features have not started.
