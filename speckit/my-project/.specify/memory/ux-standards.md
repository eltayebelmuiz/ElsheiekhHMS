# Elsheiekh Hospital Management System UX Consistency Standards

These standards define the minimum interaction and responsive behavior for all Blazor screens and
shared UI components. They apply consistently across clinical, administrative, and financial
modules.

## Forms and Actions

- Every form MUST validate each field inline on blur. Validation errors MUST appear below the related
  field before submission is attempted.
- Save and submit buttons MUST show a spinner and become disabled immediately when processing starts.
  The action MUST prevent double submissions until it completes or fails.
- Every successful create, update, delete, or submit action MUST display a top-right toast
  notification. Success toasts MUST auto-dismiss after four seconds and remain readable to assistive
  technology.
- Every destructive action MUST require a confirmation modal before execution. The modal MUST show
  the affected entity name and entity code, identify the consequence, and provide explicit cancel and
  confirm actions.

## Loading, Empty, and Error States

- Every loading state MUST communicate progress without making the page appear frozen.
- Every empty state MUST contain an icon, a descriptive message explaining what is empty, and a
  primary call-to-action button appropriate to the current workflow.
- Every error state MUST contain an icon, a clear error message, and a retry button. A failed load
  or action MUST NEVER leave a blank screen.
- Error and success feedback MUST preserve the user's entered data and relevant context when retrying
  is safe.

## Live Screens

- Queue boards and dashboards MUST auto-refresh without a full page reload.
- Live updates MUST use Blazor real-time mechanisms and MUST NOT require client-side polling or a
  manual browser refresh for normal updates.
- Refreshes MUST preserve the current filter, sort, selection, and scroll context whenever possible.
- Live-update failures MUST surface the standard error state and retry behavior rather than silently
  presenting stale data.

## Shared Controls and Visual Language

### Patient Search

Patient search MUST use an enhanced dropdown that provides:

- Avatar initials for quick visual identification.
- A visible patient status badge.
- Full keyboard navigation, including opening, moving through results, selecting, and dismissing
  the dropdown.
- An accessible name and clear highlighted-result state for assistive technology and keyboard users.

### Status Badges

- Status badges MUST use the same color, label, capitalization, and meaning everywhere the same status
  appears.
- Status-to-style mappings MUST come from a shared component or centralized definition; individual
  modules MUST NOT redefine them locally.
- Color MUST supplement, not replace, the visible status label so status remains understandable to
  users with color-vision differences.

## Responsive Tables

- All tables MUST convert to a readable card layout below `640px` viewport width.
- Card layouts MUST expose each value's field name through a `data-label` attribute or an equivalent
  semantic accessible label.
- Card layouts MUST retain record identity, status, primary actions, and destructive-action
  confirmation behavior.
- Responsive conversion MUST avoid horizontal scrolling for ordinary record views; exceptionally
  wide data MUST provide an intentional accessible alternative.

## Consistency and Verification

- Shared controls for validation, spinners, toasts, confirmation modals, empty states, error states,
  badges, and patient search MUST be reused across modules.
- New or changed screens MUST be checked at desktop and below `640px` widths, including keyboard
  navigation, focus movement, validation, processing, success, empty, error, and retry states.
- UX review MUST verify that visual consistency does not weaken server-side authorization or expose
  sensitive patient, financial, password, or token data.
