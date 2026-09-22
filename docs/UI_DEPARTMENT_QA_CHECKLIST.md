# Phase 12D Department UI QA Checklist

This handoff is for external browser review. It records checks and does not
claim that browser QA has run.

## Authorization and registry

- Sign in with a SystemAdministrator test account. Confirm `/departments` is
  unavailable to unauthorized roles through the existing policy.
- Confirm the heading, Add Department action, labelled search, status filter,
  Name/Status sorting, loading, error, empty/no-results states, result count,
  table, View, Edit, and active-only Deactivate actions.
- Confirm search, filtering, sorting, and paging are server-side.

## Create and edit

- Confirm department name required feedback, description/extension controls,
  Save/Cancel behavior, submitting state, and success feedback.
- Confirm edit prepopulation and that only name, description, and phone
  extension are editable.
- Confirm no technical ID, audit fields, provider assignment, or workflow
  relationship controls are present.

## Deactivation

- Confirm the dialog identifies the department and consequence clearly.
- Confirm Cancel leaves the record unchanged and Deactivate changes status to
  Inactive with success feedback.
- Confirm inactive records remain visible, cannot be edited, and expose no
  Activate/Reactivate/Restore action. Historical records are not altered.

## Responsive/accessibility

- Inspect desktop, tablet-like (~768px), and mobile-like (~390px) widths.
- Check table/action reachability, visible focus, keyboard operation, labels,
  status text independent of color, dialog semantics, contrast, and shell
  navigation.

## Scope guard

Appointment, Queue, Provider, Department analytics, hard delete, and
reactivation are outside Phase 12D.
