# Phase 03A Department Foundation Design

**Status:** Implemented and verified after approval on 2026-09-20.

## Goal

Introduce the first Phase 03 domain entity: a focused Department model that protects its intrinsic state while leaving cross-record rules and persistence to later layers.

## Scope

Create `ElsheiekhHMS.Core.Domain.Organization.Entities.Department` and focused xUnit tests. Correct the two contradictory statements in the project-owned SpecKit constitution so it clearly assigns intrinsic domain invariants to Core and use-case coordination to Application.

Phase 03A does not introduce staff, doctors, specialties, patients, appointments, EF Core, Identity, DTOs, repositories, services, UI, persistence configuration, or seed data.

## Department model

`Department` is organizational reference data and inherits `AuditableEntity`.

Properties:

- `Name`: required and trimmed at construction and update.
- `Description`: optional.
- `PhoneExtension`: optional.
- `IsActive`: starts `true` and becomes `false` through domain behavior.

The constructor accepts the department values plus `CreatedAt` and `CreatedBy`. Mutating behavior accepts `UpdatedAt` and `UpdatedBy` and updates inherited audit metadata.

Behavior:

- Construct a valid active department.
- `UpdateDetails` changes the three descriptive values while active.
- `Deactivate` moves Active to Inactive.
- Updating an inactive department or deactivating it again raises `BusinessRuleException`.
- A null, empty, or whitespace-only name raises `DomainValidationException`.

The entity has private setters and no persistence attributes. It has no factory, value object, domain event, interface, navigation collection, or separate lifecycle enum.

## Lifecycle and deletion

The lifecycle is one-way for this batch:

```text
Active -> Inactive
```

Inactive preserves historical references. A second deleted/administratively removed state has no approved distinct meaning, so Department does not inherit `SoftDeletableEntity` and exposes no delete behavior. Reactivation is not included because it has not been required.

## Concurrency

Department does not implement `IHasConcurrencyToken`. No demonstrated collision warrants optimistic concurrency for this initial reference entity. The choice can be revisited with concrete workflows.

## Responsibility boundary

- Core owns intrinsic invariants and domain behavior that use the entity's own state and domain inputs.
- Application owns use-case orchestration and rules requiring persistence queries, authorization, other aggregates, transactions, or external dependencies.
- Infrastructure later implements persistence and technical integrations.
- Web may provide early feedback but is not authoritative.

Department-name uniqueness is therefore outside the entity. Phase 03A does not implement it.

## Tests

Tests exercise real domain behavior without mocks:

- valid construction, initial lifecycle, and creation audit metadata;
- name trimming;
- null/empty/whitespace name rejection;
- successful details update and update audit metadata;
- invalid update name;
- deactivation and update audit metadata;
- repeated deactivation rejection;
- update after deactivation rejection.

No EF or database behavior is tested.

## Verification

Run restore, build, and the full test suite. Verify Core still has no project, framework, or package references. Refresh the repository's custom Graphify snapshot after all checks pass and verify Department and its inheritance appear.
