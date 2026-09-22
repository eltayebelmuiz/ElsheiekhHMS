# Queue UI external QA checklist

This checklist is the external browser/visual QA handoff for Phase 12F. It
does not replace backend authorization, workflow, concurrency, or SQL tests.

## Queue registry

- [ ] Authorized Administrator/Receptionist can open `/queue`; the existing
  authorization boundary remains intact.
- [ ] Queue date, status, priority, Department ID, and Doctor ID filters map to
  bounded server-side search; history remains date-selectable.
- [ ] Priority, registered time, and queue number sorting plus paging preserve
  the search state.
- [ ] Queue number, Patient, Department, Walk-in/Appointment-linked state,
  priority, status, and actions remain readable at desktop, tablet, and mobile.
- [ ] Loading, empty, no-results, validation, and error states are clear.

## Walk-in workflow

- [ ] Patient lookup is bounded, labelled, and keyboard accessible.
- [ ] Department record selection is clear; inactive departments receive safe
  backend feedback.
- [ ] No QueueDate, sequence, QueuePosition, or status input is present.
- [ ] Duplicate active Patient feedback is clear and does not create a second
  entry.
- [ ] Backend-assigned queue number is shown after success.

## Appointment→Queue workflow

- [ ] Appointment Details shows Add to queue only when no linked Queue is known
  and the Appointment is eligible.
- [ ] Handoff context shows AppointmentCode, Patient, Department, time, and
  status read-only.
- [ ] The handoff offers only priority and notes; Patient/Department cannot be
  overridden.
- [ ] The workflow checks in eligible Scheduled/Confirmed Appointments and
  creates the linked Queue through the approved orchestrator.
- [ ] Existing, Completed, and Cancelled linked Queue entries remain
  authoritative and are not requeued.
- [ ] Partial success clearly preserves the checked-in Appointment and permits
  a safe retry.

## Queue lifecycle

- [ ] Waiting: Call to nurse, Hold, Cancel.
- [ ] At nurse: Send to doctor, Hold, Cancel.
- [ ] At doctor: Complete, Cancel.
- [ ] On hold: Resume, Cancel.
- [ ] Completed and Cancelled entries expose no further actions.
- [ ] Queue actions do not mention Encounter, Start Consultation, or automatic
  Appointment completion.
- [ ] Stale concurrency feedback is safe and asks staff to reload.

## Accessibility and responsive review

- [ ] Semantic headings, labels, table captions, status text, alerts, and
  confirmation dialogs are usable with assistive technology.
- [ ] Keyboard navigation and visible focus work through lookup, filters,
  forms, lifecycle actions, and dialogs.
- [ ] Critical queue identity and actions remain reachable at 390px width.
- [ ] Patient, Department, and Appointment external QA checklists remain
  tracked as existing debt; this checklist does not claim those passes.
