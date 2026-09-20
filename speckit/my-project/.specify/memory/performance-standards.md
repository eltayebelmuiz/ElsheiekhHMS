# Elsheiekh Hospital Management System Performance Standards

These standards define measurable performance targets and mandatory query practices for the
hospital management system. Measurements MUST use representative hospital LAN conditions, realistic
data volumes, and production-equivalent configuration unless a test explicitly states otherwise.

## User-Visible Performance

- Page load time on the hospital LAN MUST remain under 2 seconds at P95.
- Patient search response time MUST remain under 500 milliseconds at P95.
- Patient search filtering MUST execute in SQL. The application MUST NOT load an unbounded patient
  set and filter it in memory.
- Dashboard data lag MUST remain under 30 seconds at P95. Blazor SignalR push MUST be preferred over
  polling for dashboard updates.

## Query Bounds and Data Access

- Every list query MUST be paginated. The default page size MUST be 25 rows and the maximum page size
  MUST be 250 rows.
- Unbounded list queries MUST NOT be introduced, including queries used by exports, dropdowns,
  background jobs, or administrative screens. Large exports MUST use an explicit batching strategy.
- N+1 queries are prohibited. Repository queries MUST use `Include()` where related entities are
  required and MUST project results to DTOs at the repository layer.
- EF Core entities MUST NOT be exposed directly to Blazor components. Repositories or Application
  services MUST map entities to DTOs before data reaches the Web layer.
- Read-only queries MUST avoid unnecessary tracking and MUST select only the fields required by the
  consuming use case.

## Required Database Indexes

SQL Server indexes MUST exist and remain effective for the following access paths:

- `PatientCode`
- `NationalId`
- `Phone`
- `AppointmentDate`
- The composite key `(QueueDate, Status)`
- `InvoiceNumber`

Index changes MUST be evaluated against actual query plans and representative data volume. Duplicate
or unused indexes MUST NOT be added without measured justification.

## Refresh and Availability Behavior

- The queue display board MUST refresh no less often than every 12 seconds when polling is required.
- SignalR push MUST be used for the queue display board when the deployment supports it; polling is
  the fallback mechanism and MUST NOT exceed a 12-second interval.
- The queue display board MUST cache and continue displaying its last known state when the server is
  unavailable, with a visible stale-data indicator and a retry or reconnect path.
- Refresh and reconnect behavior MUST NOT trigger a full page reload or reset the user's display
  context.

## Audit and Database Timing

- Audit log writes MUST add less than 50 milliseconds of overhead at P95, measured as the duration
  delta between the same operation with auditing enabled and the operation without auditing.
- SQL Server query time MUST remain under 200 milliseconds at P95 for application queries.
- Slow queries MUST be observable through EF Core command logging, including duration and operation
  context without recording passwords, tokens, or other sensitive values.
- Performance measurements MUST identify the query, data volume, database configuration, and whether
  caching, auditing, or related-entity loading was enabled.

## Performance Verification

- New or changed list, search, dashboard, queue, audit, or persistence behavior MUST include a
  representative performance measurement before release.
- Performance regressions MUST be investigated when any P95 target is exceeded; increasing timeouts
  or hiding the operation behind a spinner does not satisfy these standards.
- Code review MUST check pagination bounds, SQL-side filtering, query count, DTO projection, index
  coverage, refresh intervals, and EF Core command logging for affected paths.
