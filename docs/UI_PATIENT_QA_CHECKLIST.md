# Phase 12C Patient UI QA Checklist

This handoff is for external browser review (Google Antigravity or an
equivalent authenticated browser session). It records checks; it does not
claim that browser QA has run.

## Registry

- Sign in with an approved Administrator or Receptionist test account.
- Confirm `/patients` has a clear heading, Register action, labelled search,
  code/phone fields, sorting, loading, error, empty, no-results, result count,
  responsive table, and View/Edit row actions.
- Confirm search and paging remain server-side and no patient data is loaded in
  bulk into the browser.

## Registration

- Confirm labels, required indicators, keyboard order, date and enum controls,
  validation feedback, submitting state, Save, and Cancel.
- Use synthetic development data only. Patient phone duplicates remain allowed.
- The accepted registration contract exposes no duplicate-candidate result, so
  no duplicate warning or merge action should appear.

## Details and edit

- Confirm PatientCode is the primary identifier and only supported demographic,
  contact, and identifier data are shown.
- Confirm each edit section preserves its concurrency token. A stale save must
  show a conflict message and must not overwrite silently.
- Confirm no audit actor, ownership, clinical, queue, appointment, billing, or
  delete controls are present.

## Responsive/accessibility

- Inspect desktop, tablet-like (~768px), and mobile-like (~390px) widths.
- Check table overflow, visible focus, keyboard navigation, labels, alert/status
  announcements, contrast, and the authenticated shell/sidebar.

## Scope guard

Patient self-service, clinical history, Patient↔ApplicationUser ownership, and
Appointment→Queue behavior are outside Phase 12C.
