---
name: elsheiekh-hms-task-execution
description: Repository-specific procedure for safe ElsheiekhHMS engineering tasks. Use for substantial code, persistence, architecture, documentation, or phase-gate work in this repository.
---

# ElsheiekhHMS task execution

Use this procedure with the repository `AGENTS.md`. It describes how to work,
not what product features to invent.

1. Verify branch, HEAD, working tree, expected checkpoint, and current tests.
2. Read applicable `AGENTS.md`, README checkpoint, relevant PRD/roadmap sections,
   decisions, specifications, acceptance records, and exact source/tests.
3. Discover relevant installed skills. Use Graphify after a freshness check when
   architecture or relationships matter; use Speckit for substantial new
   capabilities whose requirements need resolution.
4. Search for an existing equivalent before creating a type, service, port,
   migration, document, or skill.
5. Inspect applicable migrations, model snapshot, DbContext, and database target
   before persistence work. Never use the development database for automated tests.
6. Classify affected layers and record protected invariants, authorization,
   concurrency, time, audit, privacy, and transaction boundaries.
7. Plan the smallest change that fits the approved phase. Stop and report source,
   specification, or product ambiguity instead of guessing.
8. Implement only the approved scope. Keep Core persistence-independent and
   Application EF-free. Do not add generic repositories/UoW, CQRS/MediatR,
   brokers, caches, or speculative abstractions without explicit approval.
9. Run focused tests first, then the full solution test suite and build. Run
   migration/pending-model, SQL, Graphify, or browser checks when the change
   requires them.
10. Review the complete diff, `git diff --check`, forbidden files, secrets,
    generated artifacts, migration immutability, and phase boundaries.
11. Update only the authoritative documentation required by the completed phase.
12. Commit and push only when the task explicitly authorizes those actions; never
    force-push or stage `Phases.md`.
13. Report commands actually run, results, changed files, remaining risks, and
    the next approved milestone. Stop at that boundary.
