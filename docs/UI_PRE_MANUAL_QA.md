# HMS pre-manual QA checklist

Status: source-level UI/UX polish completed. This checklist is the handoff for
manual browser acceptance against the Development database; it does not replace
backend authorization, workflow, or production performance verification.

## Test accounts

Use only the seeded Development accounts below. Passwords are intentionally not
documented or committed.

| Role | Username |
| --- | --- |
| SystemAdministrator | `sysadmin.dev@elsheiekh.local` |
| Administrator | `admin.dev@elsheiekh.local` |
| Receptionist | `reception.dev@elsheiekh.local` |
| Provider | `provider.dev@elsheiekh.local` |
| Patient | `patient.dev@elsheiekh.local` |

## Browser matrix

Run each applicable flow at 1440px, 1280px, 1024px, 768px, and 390px.

- [ ] Light theme loads with readable text, tables, forms, badges, alerts,
      sidebar, login, error, and dialog surfaces.
- [ ] Dark theme loads with the same readable states.
- [ ] Theme control has a name, visible focus, keyboard activation, current
      state, and persists the explicit choice after reload.
- [ ] A first visit follows the operating-system theme when no choice exists.
- [ ] Desktop sidebar and sticky header remain stable while content scrolls.
- [ ] Mobile menu opens, exposes `aria-expanded`, receives focus, closes with
      Escape, closes from the scrim or a route link, and does not overflow the
      page.
- [ ] Skip to main content is the first meaningful keyboard target and moves
      focus to the page main landmark.
- [ ] Tab/Shift+Tab, Enter, Space, and Escape work through navigation, forms,
      pagination, account/logout, theme, search, and confirmation dialogs.
- [ ] Focus, hover, pressed, disabled, and loading states are visible and
      consistent; critical touch targets are comfortable to tap.
- [ ] Reduced-motion preference removes decorative transitions and spinner
      motion remains understandable.

## Operational routes

- [ ] **Login:** labels, password visibility control, safe invalid-login text,
      required fields, keyboard order, theme, and no public registration.
- [ ] **Dashboard:** real bounded appointment and waiting-queue totals, retry,
      empty/no-result states, quick links, and role-appropriate configuration.
- [ ] **Patients:** search, clear/filter behavior, result count, pagination,
      registration, details, edit validation, stale-concurrency recovery, and
      clear patient identity without excessive PII.
- [ ] **Departments:** search/filter/sort, result count, create, details, edit,
      deactivation confirmation, inactive state, and no reactivation control.
- [ ] **Appointments:** bounded patient lookup, scheduling validation, Kigali
      presentation, lifecycle actions, stale-concurrency recovery, and no
      rescheduling, Queue creation, or Encounter UI.
- [ ] **Queue:** bounded registry, walk-in lookup, server ticket, lifecycle
      actions, appointment handoff, linked history, confirmation, and no
      QueuePosition or Encounter behavior.
- [ ] **404:** clear recovery to the workspace with the appropriate shell.
- [ ] **403/unauthorized:** calm, actionable access messaging without exposing
      internal details.
- [ ] **Unexpected error:** safe message and request identifier only.
- [ ] **Print:** operational record content is readable; shell, navigation,
      controls, and dialogs are excluded.

## Feature decision matrix

| Feature | Status | Decision | Reason |
| --- | --- | --- | --- |
| Light theme | IMPLEMENTED | KEEP | Baseline operational surface. |
| Dark theme | IMPLEMENTED | KEEP | Useful for varied staff environments. |
| Theme persistence/system preference | IMPLEMENTED | KEEP | Preference only; no sensitive storage. |
| Accessible theme toggle | IMPLEMENTED | KEEP | Named, keyboard-ready control with state. |
| Application header | IMPLEMENTED | KEEP | Stable context, account, and theme access. |
| Sticky header | IMPLEMENTED | KEEP | Operational navigation stays available. |
| Hide-on-scroll header | SKIPPED | NOT APPROPRIATE | Stable navigation is safer for operations. |
| Sidebar navigation | IMPLEMENTED | KEEP | Existing shell with active/focus/mobile polish. |
| Mobile navigation drawer | IMPLEMENTED | KEEP | Required for tablet/mobile access. |
| Active navigation | IMPLEMENTED | KEEP | Route state is clear without color alone. |
| Skip link | IMPLEMENTED | KEEP | Keyboard access to main content. |
| Focus states | IMPLEMENTED | KEEP | Visible focus is a safety and accessibility aid. |
| Button states | IMPLEMENTED | KEEP | Shared Bootstrap/HMS interaction treatment. |
| Touch targets | IMPLEMENTED | KEEP | Critical controls are usable on touch layouts. |
| Typography | IMPLEMENTED | KEEP | Compact, readable operational hierarchy. |
| Color/contrast tokens | IMPLEMENTED | KEEP | Light and dark surfaces use shared tokens. |
| Responsive layout | IMPLEMENTED | KEEP | Forms, shell, and tables reflow or scroll safely. |
| Loading states | IMPLEMENTED | KEEP | Existing shared loading and disabled-submit patterns. |
| Reduced motion | IMPLEMENTED | KEEP | Honors user motion preference. |
| Decorative animations | SKIPPED | NOT APPROPRIATE | Operational HMS does not need spectacle. |
| Offscreen animation control | SKIPPED | NOT APPROPRIATE | No continuous decorative animation exists. |
| Scroll progress | SKIPPED | NOT APPROPRIATE | CRUD screens have no useful progress measure. |
| Back to top | SKIPPED | NOT APPROPRIATE | Current screens are not meaningfully long. |
| Global HMS search | DEFERRED | CONTRACT NEEDED | No approved safe cross-module contract. |
| Module search UX | IMPLEMENTED | KEEP | Existing bounded searches remain authoritative. |
| Search dialog | SKIPPED | NOT APPROPRIATE | No dialog is needed for current searches. |
| Confirmation dialogs | IMPLEMENTED | KEEP | Reserved for consequential lifecycle actions. |
| Native/dialog accessibility | IMPLEMENTED | KEEP | Existing dialog gains focus and Escape behavior. |
| 404/not found | IMPLEMENTED | KEEP | Recovery path is explicit. |
| Error experience | IMPLEMENTED | KEEP | Safe, specific, actionable states. |
| Print styles | IMPLEMENTED | KEEP | Useful for operational record review. |
| Password visibility | IMPLEMENTED | KEEP | Reduces sign-in errors without logging secrets. |
| Cookie banner | SKIPPED | NOT APPROPRIATE | Only necessary application cookies are used. |
| Storage notice | DOCUMENTED | KEEP | Theme preference storage is factual and non-sensitive. |
| WhatsApp/contact float | SKIPPED | NOT APPROPRIATE | Internal operations, not a public website. |
| Newsletter | SKIPPED | NOT APPROPRIATE | No operational use. |
| FAQ | SKIPPED | NOT APPROPRIATE | No marketing content in the HMS. |
| Program/training guide | SKIPPED | NOT APPROPRIATE | Separate training-site concern. |
| UTM tracking | SKIPPED | NOT APPROPRIATE | No marketing analytics. |
| Code-copy controls | SKIPPED | NOT APPROPRIATE | No code/documentation screen. |
| Blog last-updated | SKIPPED | NOT APPROPRIATE | No blog content. |
| Image gallery | SKIPPED | NOT APPROPRIATE | No marketing gallery. |
| Promotional video | SKIPPED | NOT APPROPRIATE | No marketing content. |
| JavaScript resilience | IMPLEMENTED | KEEP | Small browser helpers; Blazor remains authoritative. |
| External resource failure | IMPLEMENTED | KEEP | Existing local Bootstrap/assets retained. |
| RTL readiness | IMPLEMENTED | KEEP | Logical properties remain in use. |
| Table UX | IMPLEMENTED | KEEP | Bounded tables scroll internally and expose state. |
| Form UX | IMPLEMENTED | KEEP | Labels, validation, submit, cancel, and loading states. |
| Patient safety cues | IMPLEMENTED | KEEP | Existing identifiers are clear without extra PII. |
| Status system | IMPLEMENTED | KEEP | Text and visual marker are both present. |
| Empty/no-result states | IMPLEMENTED | KEEP | Shared states distinguish no data from no matches. |
| Concurrency UX | IMPLEMENTED | KEEP | Existing stale-record recovery is preserved. |
| Authorization UX | IMPLEMENTED | KEEP | Role-aware navigation remains non-authoritative. |
| Provider ownership UI | SKIPPED | OUT OF CURRENT UI SCOPE | Phase13C provides the backend ownership foundation; no ownership-management UI is approved. |
| Encounter/clinical UI | SKIPPED | BLOCKED BY ACCEPTED BACKEND | Encounter persistence and clinical workflow remain deferred after Phase13B/13C. |
| Performance | IMPLEMENTED | KEEP | No unbounded query, library, or animation added. |
| New packages | SKIPPED | NOT REQUIRED | Existing stack is sufficient. |
| CSS organization | IMPLEMENTED | KEEP | Tokens and existing shared stylesheet remain authoritative. |
| JS organization | IMPLEMENTED | KEEP | One small purpose-specific browser helper. |
| Browser storage | IMPLEMENTED | KEEP | Only theme preference is stored locally. |
| Accessibility review | SOURCE VERIFIED | MANUAL QA REQUIRED | Browser evidence is the next gate. |
| Antigravity review | UNAVAILABLE | MANUAL/EXTERNAL QA REQUIRED | Tool is not available in this environment. |

## Scope freeze verification

- Core, Application contracts, Infrastructure business behavior,
  ApplicationUser, DbContext, migrations, snapshots, and schema are unchanged.
- No Encounter, Provider ownership, global search contract, package, or
  `Phases.md` change is part of this pass.
- Browser acceptance is the next gate. This document does not certify
  production performance or formal WCAG conformance.
