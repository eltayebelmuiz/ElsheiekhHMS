# Cross-Cutting Performance, Reliability, and Quality Requirement

> **Status:** Human approved. This is a cross-cutting design requirement; it does not authorize implementation work by itself.

## Priorities

Remaining phases prioritize correctness, security, data integrity, reliability, predictable performance, maintainability, and scalability based on measured need. Correctness and security must not be traded away for latency.

## Performance boundary

Do not introduce Redis, distributed caching, message brokers, microservices, CQRS frameworks, event buses, additional databases, generic repositories, custom thread or connection pools, or speculative background processing without a measured and separately approved need. The existing modular architecture remains the default.

Future persistence work must use query/business-justified indexes, bounded result sets, server-side filtering and sorting, pagination, projections, efficient joins, short transactions, deliberate uniqueness/concurrency design, and query-plan inspection where performance matters. Avoid N+1 queries, unnecessary `Include` chains, full-table materialization, client-side filtering, repeated `SaveChanges`, and long-lived `DbContext` instances. Use `AsNoTracking()` for genuinely read-only queries when tracking is unnecessary.

## Async, reliability, and resource safety

Database, network, file, and other I/O-bound operations should use appropriate async APIs and meaningful cancellation. Avoid `.Result` and `.Wait()` in ASP.NET request/application flows and do not wrap CPU-bound synchronous work in fake async APIs.

Future architecture must fail safely through centralized exception handling, structured redacted logging, health checks, dependency-failure handling, cancellation, timeouts, safe transaction boundaries, deliberate retries, and correct dependency-injection/resource lifetimes. Do not blindly retry non-idempotent operations or create retry storms. Never expose stack traces, passwords, tokens, cookies, or sensitive secrets.

## Identity and Blazor implications

Identity authorization must use framework-supported role/policy checks efficiently. Account blocking, suspension, banning, session revocation, and authorization remain authoritative; security must not be weakened to reduce database or request overhead. The implementation must explicitly balance emergency revocation latency against validation cost.

When Phase 12 begins, patient, appointment, queue, audit, and report screens must use bounded server-side search/filter/sort, pagination, projections/DTOs, deliberate component state, and virtualization where useful. Large entity graphs must not be sent to components merely for convenience.

## Measurement and phase gates

No build or unit-test result alone proves high performance. Important workflows require realistic baselines for request latency, query duration/count, throughput, error rate, memory, CPU, and connection usage before optimization claims. Future hardening must include slow-query detection and regression review.

Every remaining implementation phase must report:

- performance impact review;
- reliability impact review;
- security impact review;
- database-query impact; and
- regression-test status.

Where relevant, also report query count, pagination, index justification, concurrency, and resource lifetimes. Phase 06/07 must settle the shared pagination contract before list services proliferate. Phase 10/11 must perform a dedicated performance and reliability review covering representative SQL, indexes, N+1 detection, bounded queries, pagination, latency, Identity/authentication overhead, concurrency, exception handling, logging, health checks, resource lifetimes, realistic load testing, and performance regressions.

## Current-phase boundary

This requirement does not authorize refactoring, caching, Redis, new packages, indexes, Identity changes, migrations, database updates, Phase 05B implementation, or Phase 06. It records the standard future phases must satisfy.
