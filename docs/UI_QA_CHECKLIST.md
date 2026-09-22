# Phase 12B UI QA Checklist

This checklist is a handoff for an external browser reviewer such as Google
Antigravity. It records what to inspect; it does not claim that browser QA has
already run.

## Anonymous access

- Open `/login` and confirm the anonymous layout has no operational sidebar.
- Confirm the username and password labels, visible focus, required-field
  feedback, generic invalid-login feedback, and safe return URL behavior.
- Confirm there is no Register, public patient registration, password reset, or
  external-login control.
- Open `/access-denied` and confirm the message is safe and actionable.

## Authenticated shell

- Sign in with a test account supplied by the environment; do not record or
  screenshot credentials, cookies, tokens, or patient data.
- Confirm the sidebar, header, active navigation state, current-user summary,
  account menu, and POST logout are visible and usable.
- Confirm role-aware navigation is only a UX aid and protected routes still
  enforce backend authorization.

## Responsive and keyboard checks

- Inspect a desktop width, a tablet-like width around 768px, and a mobile-like
  width around 390px.
- Open and close the mobile drawer; check scrim behavior, clipping, horizontal
  overflow, and access to the account/logout controls.
- Traverse links, form fields, the account menu, drawer control, and buttons by
  keyboard. Confirm focus is visible and there is no trap.
- Check reduced-motion behavior, contrast, labels, landmarks, and alert/status
  announcements.

## Scope guard

Do not expect Patient, Department, Appointment, Queue CRUD, Provider, or
clinical screens in Phase 12B. Do not test or request a direct database call
from a Razor component.
