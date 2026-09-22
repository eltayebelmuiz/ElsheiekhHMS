# ElsheiekhHMS UI Design System

This document records the implemented Phase 12A foundation and Phase 12B
authentication shell. It is the UI reference for later feature screens; it does
not grant permission to add backend capabilities.

## Product language

The interface is calm, operational, and data-oriented. Pages favor clear
headings, short descriptions, restrained surfaces, predictable actions, and
useful loading, empty, error, and confirmation states. Avoid marketing layouts,
decorative gradients, large illustrations, emoji navigation, and animation that
does not improve understanding.

## Foundations

- Bootstrap **5.3.3** is the low-level CSS foundation.
- `wwwroot/app.css` owns HMS tokens and reusable visual patterns.
- Component-scoped CSS owns layout-specific rules such as the shell and nav.
- Logical properties (`margin-inline`, `padding-inline`, and `inset-inline`)
  keep the system ready for future localization and RTL work.

The main tokens cover page and surface colors, primary/accent states, text and
muted text, borders, semantic success/warning/danger/information surfaces,
focus color, sidebar colors, spacing, radii, shadows, content width, and shell
dimensions. Use a token before introducing a one-off value.

Typography uses the system UI stack with a readable base size and strong,
compact headings. Buttons, fields, tables, status badges, alerts, dialogs, and
pagination use the existing Bootstrap primitives with HMS colors, borders,
radii, and focus treatment.

## Shell and authentication

Authenticated operational routes use `MainLayout`: a persistent desktop
sidebar, sticky header, content region, and footer. At narrow widths the
sidebar becomes an off-canvas drawer with a scrim. `NavLink` supplies the active
location state; labels and CSS markers remain understandable without an icon
font.

Anonymous authentication routes use `AnonymousLayout`, which intentionally has
no operational sidebar. `/login` accepts the administrator-assigned username
and password through the existing Identity mechanism. Public registration,
password reset, external login, and remember-me are not part of this shell.
`UserAccountMenu` presents the available username/role summary and sends logout
through an antiforgery-protected POST. Backend authentication, account state,
security-stamp revalidation, and authorization remain authoritative.

## Accessibility and responsive behavior

Use semantic landmarks, associated labels, native links/buttons/forms, visible
`:focus-visible` outlines, alert/status roles, readable contrast, and reduced
motion support. Keep interactive targets usable on touch devices. Verify the
desktop, tablet-like, and mobile-like layouts for clipping, overflow, focus, and
drawer behavior before accepting a feature screen.

The current shell is English and LTR, but the token and logical-property
strategy is intentionally ready for future localization. Do not duplicate the
stylesheet or add localization infrastructure without an approved requirement.

## Boundaries for future screens

Patient, Department, Appointment, and Queue screens must call Application
contracts through DI; Razor components must not inject a DbContext or an
Infrastructure persistence implementation. Phase 12C provides the authorized
staff Patient registry, registration, details, and concurrency-safe edit
sections; its external browser handoff is `docs/UI_PATIENT_QA_CHECKLIST.md`.
Patient ownership and Provider self-service remain deferred. Appointment check-in is not an Encounter, and an
Appointment-linked Queue entry must be created through the approved workflow so
PatientId and DepartmentId come from the Appointment. Do not add clinical,
billing, laboratory, pharmacy, inpatient, or Provider navigation until their
backend scope is approved.

## Tooling guidance

The repository has no checked-in `.vscode` configuration. Recommended editor
support is C# Dev Kit, C#, EditorConfig, and CSS Peek; IntelliCode, Error Lens,
GitLens, and REST Client are optional. Stylelint is unnecessary for the current
small centralized CSS workflow. These are recommendations only; extensions are
not installed by the application build.
