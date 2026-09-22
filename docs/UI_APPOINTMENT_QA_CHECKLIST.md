# Appointment UI external QA checklist

This checklist is the handoff for the external browser/visual QA pass after
Phase 12E. It does not replace backend authorization, validation, concurrency,
or integration tests.

## Registry

- [ ] Authorized Administrator/Receptionist can open `/appointments`.
- [ ] Unauthorized/anonymous users receive the existing access boundary.
- [ ] Date range, status, Patient ID, Department ID, and Doctor ID filters map
  to the server-side search contract; no full dataset is loaded in the browser.
- [ ] Sort direction and paging preserve the search state.
- [ ] AppointmentCode, Patient record, Department record, Doctor record,
  Kigali date/time, status, and View action remain readable at desktop,
  tablet, and 390px mobile widths.
- [ ] Loading, empty, no-results, validation, and error states are clear.

## Schedule

- [ ] Patient search is bounded and selecting a result is keyboard accessible.
- [ ] Department and Doctor record identifiers are clearly labelled and are
  validated by the backend; no invented provider lookup or ownership appears.
- [ ] Native date/time controls communicate Africa/Kigali semantics.
- [ ] Required fields, invalid values, past/ambiguous times, inactive records,
  and collision feedback are understandable and safe.
- [ ] AppointmentCode is shown only after the backend schedules the record.

## Details and lifecycle

- [ ] AppointmentCode is prominent; Patient, Department, Doctor, scheduled
  Kigali time, type, notes, and status have a clear hierarchy.
- [ ] Only valid actions for the current status are shown.
- [ ] Check In is presented as arrival/attendance and does not mention an
  Encounter or consultation.
- [ ] Cancel, Mark no-show, and Complete use clear confirmation language;
  submitting actions cannot be duplicated.
- [ ] A stale concurrency token produces safe reload guidance.
- [ ] There is no Reschedule, Delete, direct Queue creation, Encounter, or
  Provider ownership UI.

## Accessibility and responsive review

- [ ] Headings, labels, table captions, status text, alerts, and dialogs are
  announced appropriately.
- [ ] Keyboard focus and visible focus treatment work through search, lookup,
  forms, actions, and confirmation dialogs.
- [ ] Contrast and touch targets remain usable on desktop, tablet, and mobile.
- [ ] The existing Patient and Department external QA debt remains tracked;
  this checklist does not claim those passes occurred.
