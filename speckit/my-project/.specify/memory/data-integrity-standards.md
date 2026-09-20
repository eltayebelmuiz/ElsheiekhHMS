# Elsheiekh Hospital Management System Data Integrity Standards

These standards define mandatory database schema, persistence, relationship, concurrency, and
identifier rules. They apply to EF Core models, migrations, repositories, services, and database
administration.

## Monetary Values and Timestamps

- Every monetary SQL Server column MUST use `decimal(12,3)`.
- Every monetary CLR property MUST use `decimal`; `float` and `double` MUST NOT be used for money.
- EF Core precision configuration and generated migrations MUST preserve `decimal(12,3)` without
  silent rounding or type widening.
- All timestamps MUST be stored in UTC. Application code MUST write UTC values explicitly, and local
  time conversion MUST occur only at the display or presentation layer.
- Client-provided local timestamps MUST NOT be persisted as authoritative event times.

## Uniqueness and Indexes

SQL Server MUST enforce unique indexes for the following fields:

- `PatientCode`
- `NationalId`, using a filtered unique index that excludes null values
- `PassportNumber`, using a filtered unique index that excludes null values
- `InvoiceNumber`

Uniqueness MUST be enforced in the database as well as validated in Application services. Concurrent
requests MUST receive a controlled conflict result when a unique constraint is violated.

## Optimistic Concurrency

- `Patient`, `Invoice`, and `Appointment` MUST each contain a `RowVersion` property mapped to a SQL
  Server `rowversion` column.
- EF Core MUST configure each `RowVersion` as a generated concurrency token.
- Updates MUST use the original `RowVersion` value in the concurrency check and MUST NOT silently
  overwrite a record changed by another user.
- A concurrency conflict MUST return a safe conflict result, preserve the already-committed data,
  and follow the application's audit requirements.

## Clinical Relationships and Deletion

- Cascade delete MUST be disabled for every clinical relationship.
- Clinical foreign keys MUST use `Restrict` or `SetNull` delete behavior; `SetNull` is permitted only
  when the foreign key is nullable and preserving the related record remains valid.
- Deletion behavior MUST never remove clinical history implicitly through a parent delete.
- Clinical records MUST remain subject to the project's soft-delete-only rules.

## Global Soft-Delete Filters

EF Core global query filters MUST exclude soft-deleted rows by default for all of the following
entities:

- `Patient`
- `Doctor`
- `Appointment`
- `WalkInQueue`
- `Invoice`
- `LabTest`

Repositories MUST use an explicit, authorized bypass only for audit, recovery, or approved reporting
workflows. Normal Application and Web queries MUST NOT return soft-deleted rows.

## Server-Generated Identifiers

- `PatientCode` MUST match `PT-YYYY-NNNNN`, where `YYYY` is the four-digit year and `NNNNN` is a
  zero-padded sequence. It MUST be generated server-side by `PatientService` only.
- `InvoiceNumber` MUST match `INV-YYYY-NNNNN`, where `YYYY` is the four-digit year and `NNNNN` is a
  zero-padded sequence. It MUST be generated server-side by `BillingService` only.
- Queue tickets MUST match `A-NNN`, where `NNN` is a three-digit sequence that is sequential per day
  and resets at midnight.
- Clients MUST NOT choose, override, or submit authoritative values for these generated identifiers.
- Identifier generation MUST be concurrency-safe and MUST preserve uniqueness across simultaneous
  requests.

## EF Core Fluent Configuration

- Every entity MUST have its EF Core Fluent API configuration in a separate
  `IEntityTypeConfiguration<T>` file for that entity.
- Entity configuration files MUST define keys, required fields, precision, indexes, relationships,
  delete behavior, query filters, and concurrency tokens for their entity.
- The DbContext MUST apply all entity configuration classes consistently during model creation.
- Inline entity-specific configuration in the DbContext MUST NOT replace the dedicated configuration
  file.

## Integrity Verification

- Migrations MUST be reviewed for decimal precision, UTC-compatible columns, filtered unique indexes,
  RowVersion mappings, delete behaviors, and query filters before application.
- Automated tests MUST cover duplicate identifiers, nullable unique fields, concurrent updates,
  soft-delete filtering, identifier formats, daily queue reset behavior, and prohibited cascade
  deletion.
