
# AGENTS.md

## Permanent Project Context Protocol

Before substantial architectural or implementation work, follow this sequence:

1. Read this `AGENTS.md` for repository operating rules and any applicable scoped instructions. The instruction filename is `AGENTS.md`, not `AGENT.md`; report a naming mismatch rather than assuming it is loaded.
2. Read `README.md` for the current phase, completed phases, next action, architectural decisions, and intentionally postponed work.
3. Read `PRD.md` for product goals, HMS modules, functional and non-functional requirements, business requirements, scope, and constraints. For follow-up work, revisit the relevant sections rather than repeatedly reading unrelated material.
4. Inspect applicable specifications, plans, tasks, decisions, implementation requirements, and acceptance criteria under `speckit/my-project/`.
5. Read `graphify-out/GRAPH_REPORT.md` for a fast architecture overview.
6. Consult `graphify-out/graph.json` when machine-readable projects, references, packages, files, namespaces, types, inheritance, or test relationships help the task.
7. Check graph freshness before relying on it: compare snapshot timestamp, Git commit, branch, and dirty-tree metadata with the current repository; run `.\tools\graphify.ps1 -Check` to compare source and generator fingerprints. A dirty tree alone does not prove staleness, and a matching commit alone does not prove freshness. If stale and architecture information is needed, regenerate with `.\tools\graphify.ps1` before using the snapshot. If checking is blocked, disclose this and verify relevant source directly.
8. Discover relevant installed skills/plugins using section 29. Use capabilities that materially improve the task.
9. Read the exact source, configuration, project, and test files needed for the task before editing them. Do not recursively read the entire repository by default.

Inspect `git status --short`, the current branch, and recent commits before significant changes. Preserve existing work. Missing context must be reported; do not fabricate specifications, phase completion, or verification results.

### Source-of-truth hierarchy

For resolving repository information conflicts, use:

1. Actual source code and project files: what exists and how it is wired.
2. Current approved specification under `speckit/my-project/`: intended detailed implementation.
3. `PRD.md`: product requirements and intended destination.
4. `AGENTS.md`: repository/development operating rules.
5. `README.md`: recorded development status and resume checkpoint.
6. `graphify-out/graph.json`: generated structural index.
7. `graphify-out/GRAPH_REPORT.md`: generated human summary.

This is an evidence hierarchy, not a replacement for system, developer, or user instruction precedence. Existing code is evidence of current behavior, not permission to ignore an approved requirement or preserve a bug. Report source/specification conflicts before making dependent architectural decisions. Source wins over an inaccurate graph. Report README inconsistencies without silently rewriting history.

### SpecKit workspace

Use `speckit/my-project/` as the detailed specification workspace. Before implementing a feature, determine whether an applicable approved specification exists and read its plan, tasks, decisions, and acceptance criteria. Follow it unless it conflicts with a higher-priority source; report ambiguity or conflicts. Do not invent requirements contrary to the PRD or modify a specification merely to make implementation easier.

Templates, installed workflow skills, and a constitution do not by themselves constitute an approved feature specification. Do not infer approval from file existence. Do not treat a nested duplicate scaffold as a second authoritative project or relocate/delete it without task authorization.

### Context findings to reconcile

Observed on 2026-09-20; recheck before relying on these findings:

- The user reports Phase 02 complete/passing, while README still records setup complete with final review pending. Reconcile the recorded checkpoint with the actual review evidence before declaring Phase 03 ready.
- `PRD.md` is marked Draft and names ASP.NET Core 9 / EF Core 9 and `docs/PRD.md`; the actual projects target .NET 10 and the PRD is at the repository root. Do not downgrade the solution or silently revise requirements.
- `speckit/my-project/` contains a constitution, standards, templates, and workflow skills, plus a nested `my-project/` scaffold. No feature specification/plan/tasks were found outside templates. The constitution's `Core -> Application -> Infrastructure -> Web` wording must not be interpreted as project references; it also needs clarification of domain rules versus application orchestration ownership.
- The existing Core concurrency interface and setup script remain present. Prior concurrency-deferral discussion requires reconciliation before changing the foundation; this context task does not authorize a production change.

### Architecture and phase boundaries

Verify the actual solution and project files. The expected five-project reference graph is:

```text
Core -> no project dependencies
Application -> Core
Infrastructure -> Application, Core
Web -> Application, Infrastructure
Tests -> Core, Application, Infrastructure
```

Core must remain independent of Application, Infrastructure, Web, Blazor, EF Core, SQL Server implementation, and Identity implementation. Existing foundation types must be inspected and reused, not recreated. Generic runtime flow diagrams elsewhere in this document do not reverse these project-reference rules.

The roadmap is: 01 Solution & Architecture; 02 Core Foundation; 03 Domain Entities; 04 EF Core & Database; 05 Identity & Security; 06 DTOs & Validation; 07 Application Services; 08 Business Workflows; 09 Enterprise Infrastructure; 10 Testing & Hardening; 11 Backend Review; 12 Blazor UI.

Stay within the authorized phase. Do not add EF Core during Phase 03, Identity before Phase 05, or substantial UI before backend readiness. Blazor calls Application contracts; domain rules belong in Core, use-case orchestration in Application, and persistence in Infrastructure. Do not put business logic or direct database access in Blazor. Plugin availability never authorizes future-phase application dependencies.

### Work, verification, and phase checkpoints

For substantial tasks: understand the request; read context; check the specification, graph, and relevant capabilities; inspect source; plan; implement; build and test; verify architecture; refresh the graph when needed; update progress only when the phase genuinely changes; report results. Avoid unrelated refactors and speculative abstractions.

After meaningful code changes, normally run against the actual solution:

```powershell
dotnet restore ElsheiekhHMS.slnx
dotnet build ElsheiekhHMS.slnx --no-restore
dotnet test ElsheiekhHMS.slnx --no-build
```

Use the required environment command wrapper where applicable. Adjust checks to scope; documentation-only changes normally need content/diff verification, not a new application build. Report commands actually run, warnings, errors, failures, skipped tests, and architecture violations. Never claim PASS from assumed or historical results.

README is the development resume checkpoint. At genuine phase completion: build, test, verify architecture, fix relevant findings, refresh Graphify, update README with completed/current phase, decisions, postponed work and next action, then review the Git diff and report checkpoint readiness. If the README edit changes a fingerprinted input, refresh/check Graphify again so the final snapshot remains current. Setup alone does not complete a phase. Never automatically stage, commit, or push.

## Role

Act as a senior software engineer working inside this repository.

Your job is not only to generate code, but to understand the existing project, make safe changes, verify them, and keep the codebase maintainable.

Prioritize:

1. Correctness
2. Simplicity
3. Maintainability
4. Security
5. Performance
6. Accessibility
7. Professional UI/UX

Avoid unnecessary abstractions, dependencies, rewrites, and over-engineering.

---

# 1. Understand the Project First

Before making significant changes:

* Inspect the repository structure.
* Read existing documentation.
* Read relevant source files.
* Identify the architecture and conventions already being used.
* Check existing dependencies before installing new ones.
* Look for existing reusable components before creating new ones.
* Understand how the requested feature fits into the current application.

Do not assume the project structure.

Prefer extending existing patterns over introducing competing patterns.

---

# 2. Development Environment

The development machine may provide the following tools.

## Core

* Git
* GitHub CLI (`gh`)
* .NET SDK
* .NET CLI (`dotnet`)
* EF Core CLI (`dotnet ef`)
* Node.js
* npm
* PowerShell

## Development / Testing

* Playwright CLI
* Docker
* Docker Compose

## Optional Web Development Tools

Depending on the project:

* pnpm
* shadcn CLI
* Supabase CLI
* Vercel CLI
* Stripe CLI

Before relying on a tool, verify that it is available when necessary.

Examples:

```powershell
git --version
gh --version
dotnet --info
dotnet ef --version
node --version
npm --version
docker --version
```

Do not install global software or modify the machine configuration unless explicitly requested.

---

# 3. .NET Development

For .NET projects, prefer the .NET CLI.

Common commands:

```powershell
dotnet restore
dotnet build
dotnet run
dotnet watch
dotnet test
dotnet publish
```

Before considering a .NET task complete, normally run:

```powershell
dotnet restore
dotnet build
dotnet test
```

If the repository contains multiple projects, determine the correct solution or project before running commands.

Do not ignore compiler errors.

Do not hide warnings simply to make the build appear successful.

---

# 4. ASP.NET Core

Follow modern ASP.NET Core conventions.

Prefer:

* Dependency Injection
* Configuration through `appsettings.json` and environment variables
* Strongly typed models
* DTOs where appropriate
* Async database/API operations
* Built-in logging
* Built-in validation
* ASP.NET Core Identity for authentication when applicable
* Authorization policies/roles where appropriate

Avoid:

* Hardcoded secrets
* Hardcoded connection strings
* Business logic inside Razor markup
* Large controllers
* Duplicate validation
* Unnecessary service layers
* Static global state

Keep responsibilities clear.

Typical separation:

```text
UI / Razor / Blazor
        ↓
Application / Services
        ↓
Domain / Business Logic
        ↓
Infrastructure
        ↓
Database / External Services
```

Follow the existing architecture if the repository already defines one.

---

# 5. Blazor

For Blazor applications:

* Prefer reusable Razor components.
* Keep components focused.
* Separate complex logic from markup.
* Use dependency injection.
* Use proper component lifecycle methods.
* Handle loading, empty, success, and error states.
* Avoid unnecessary re-rendering.
* Use async methods for I/O operations.
* Dispose resources and subscriptions when required.

For forms, prefer:

```razor
<EditForm>
```

with:

* Models or ViewModels
* DataAnnotationsValidator
* ValidationMessage
* ValidationSummary when useful

Avoid large pages containing unrelated responsibilities.

Extract reusable UI into components.

---

# 6. Entity Framework Core

Use EF Core carefully.

Useful commands:

```powershell
dotnet ef migrations list
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

Before creating a migration:

1. Inspect the current model.
2. Inspect the DbContext.
3. Inspect existing migrations.
4. Understand the intended schema change.

Never automatically delete production data.

Never drop a database unless explicitly requested.

Never remove migrations blindly.

For queries:

* Prefer async operations.
* Avoid unnecessary database round trips.
* Avoid N+1 queries.
* Use `AsNoTracking()` for read-only queries where appropriate.
* Project only required fields when practical.
* Use pagination for potentially large datasets.

---

# 7. SQL Server

When working with SQL Server:

* Use parameterized queries.
* Never concatenate untrusted input into SQL.
* Preserve referential integrity.
* Use appropriate data types.
* Use indexes intentionally.
* Avoid unnecessary `SELECT *`.
* Consider transaction boundaries.
* Be careful with destructive schema changes.

For sensitive operations, explain the impact before executing destructive commands.

---

# 8. API Development

For Web APIs:

* Follow REST conventions where appropriate.
* Use meaningful HTTP status codes.
* Validate incoming requests.
* Use DTOs rather than exposing database entities unnecessarily.
* Handle errors consistently.
* Use async I/O.
* Protect sensitive endpoints with authentication and authorization.

Typical responses should correctly use codes such as:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
500 Internal Server Error
```

Never expose stack traces, connection strings, tokens, or sensitive internal details to clients.

---

# 9. Security

Security is part of implementation, not an optional final step.

Never commit:

* Passwords
* API keys
* Access tokens
* Private keys
* Database credentials
* Production secrets

Use:

* Environment variables
* User Secrets during local .NET development
* Appropriate secret-management systems in production

Consider:

* Authentication
* Authorization
* Input validation
* SQL injection
* XSS
* CSRF
* File-upload validation
* Rate limiting
* Secure cookies
* HTTPS
* Data exposure
* Least privilege

Do not weaken security controls simply to make something work.

---

# 10. Frontend Development

For HTML, CSS, JavaScript, Razor, or Blazor UI:

Prioritize:

* Clear visual hierarchy
* Consistent spacing
* Responsive layouts
* Accessibility
* Keyboard navigation
* Readable typography
* Clear form validation
* Loading states
* Empty states
* Error states
* Useful feedback after actions

Prefer semantic HTML.

Use CSS Grid/Flexbox appropriately.

Avoid excessive animations.

Animations should improve understanding rather than distract users.

---

# 11. UI/UX Quality

For professional applications, especially dashboards and management systems, interfaces should feel like production software rather than demo projects.

Use:

* Consistent design tokens
* Predictable navigation
* Clear page titles
* Breadcrumbs where useful
* Search
* Filters
* Sorting
* Pagination
* Confirmation for destructive actions
* Toasts/alerts for operation results
* Responsive tables
* Accessible forms
* Helpful empty states
* Skeleton/loading indicators when appropriate

For tables containing many records, consider:

```text
Search
Filters
Sorting
Pagination
Column alignment
Row actions
Bulk actions when justified
Loading state
Empty state
Error state
Responsive behavior
```

Do not add features merely because they are common. They must serve the application's workflow.

---

# 12. Playwright / Browser Verification

For meaningful UI changes, use browser testing when Playwright is available.

Typical workflow:

```text
Modify UI
   ↓
Build application
   ↓
Run application
   ↓
Open with Playwright
   ↓
Inspect page
   ↓
Test interactions
   ↓
Check errors
   ↓
Fix issues
   ↓
Verify again
```

Check important viewport sizes when relevant:

* Desktop
* Tablet
* Mobile

Verify:

* Navigation
* Forms
* Buttons
* Dialogs
* Validation
* Tables
* Responsive behavior
* Important user flows

Do not claim a UI works merely because the code looks correct.

---

# 13. Testing

Add tests when they provide meaningful protection.

Prioritize testing:

* Business rules
* Validation
* Services
* APIs
* Authentication/authorization behavior
* Critical user flows
* Previously broken behavior

For bug fixes, when practical:

1. Reproduce the bug.
2. Identify the root cause.
3. Add a failing regression test.
4. Implement the fix.
5. Run the test.
6. Run related tests.

Do not change tests merely to hide broken application behavior.

---

# 14. Debugging

When debugging:

1. Reproduce the issue.
2. Read the actual error.
3. Inspect relevant logs.
4. Identify the root cause.
5. Make the smallest reasonable fix.
6. Re-run the failing scenario.
7. Run related tests/build checks.

Do not randomly modify multiple unrelated files hoping the problem disappears.

Fix root causes rather than symptoms.

---

# 15. Git

Before significant work, inspect:

```powershell
git status
git branch --show-current
```

After changes:

```powershell
git diff
git status
```

Do not:

* Commit automatically unless requested.
* Push automatically unless requested.
* Force-push unless explicitly requested and the consequences are understood.
* Rewrite unrelated code.
* Discard existing user changes.
* Commit generated secrets or local configuration.

Keep changes focused on the requested task.

---

# 16. GitHub

When GitHub CLI is available, it may be used for repository workflows.

Examples:

```powershell
gh repo view
gh issue list
gh issue view <number>
gh pr list
gh pr view <number>
gh run list
```

Creating or modifying remote GitHub resources should follow the user's request and repository workflow.

Before creating a pull request:

* Build the project.
* Run relevant tests.
* Review the diff.
* Ensure unrelated files are excluded.
* Summarize what changed.

---

# 17. Dependencies

Before adding a dependency:

1. Check whether the project already has equivalent functionality.
2. Determine whether built-in framework functionality is sufficient.
3. Consider maintenance and security implications.
4. Add the dependency only when it provides clear value.

Prefer fewer dependencies.

Never introduce a large framework for a small problem.

---

# 18. Refactoring

Do not perform large unrelated refactors during feature work.

Refactor when it:

* Removes meaningful duplication
* Improves clarity
* Fixes architectural problems
* Enables the requested feature
* Reduces maintenance risk

Preserve behavior unless behavior changes are explicitly intended.

---

# 19. File Editing

When modifying an existing project:

* Preserve existing conventions.
* Avoid unnecessary formatting changes.
* Avoid rewriting whole files for tiny changes.
* Do not rename public APIs without a reason.
* Do not move large numbers of files unnecessarily.

Prefer small, reviewable diffs.

---

# 20. Error Handling

Never silently swallow important exceptions.

Prefer:

* Structured logging
* Useful user-facing messages
* Detailed developer logs
* Centralized exception handling where appropriate

User-facing errors should not expose sensitive implementation details.

---

# 21. Performance

Optimize based on meaningful evidence.

Pay particular attention to:

* Database queries
* Network requests
* Large collections
* Rendering loops
* Images/assets
* Repeated API calls
* N+1 database queries
* Unnecessary component renders

Do not introduce complicated caching or optimization without a demonstrated need.

---

# 22. Documentation

Document decisions that future developers need to understand.

Good documentation explains:

* Why something exists
* Important constraints
* Setup requirements
* Non-obvious behavior

Avoid comments that merely repeat the code.

---

# 23. Working With Existing Code

Treat existing working code carefully.

Before replacing something:

* Understand why it exists.
* Search for usages.
* Check dependencies.
* Determine potential side effects.

Do not replace a working implementation simply because another approach looks newer.

---

# 24. Large Features

For substantial features:

1. Explore the existing implementation.
2. Identify affected areas.
3. Define the smallest sensible implementation.
4. Implement incrementally.
5. Build frequently.
6. Test important behavior.
7. Review the final diff.
8. Verify the completed feature.

Avoid attempting massive rewrites in one uncontrolled change.

---

# 25. Verification Before Completion

Never claim a task is complete without reasonable verification.

Depending on the project, verification may include:

```powershell
dotnet restore
dotnet build
dotnet test
```

and when relevant:

```powershell
git diff
git status
```

For frontend work, verify the application in a browser when possible.

For database changes:

* Inspect generated migrations.
* Verify expected schema changes.
* Avoid destructive changes unless explicitly intended.

For APIs:

* Verify important endpoints.
* Verify expected HTTP responses.
* Verify validation/error behavior.

---

# 26. Completion Report

After completing substantial work, provide a concise report containing:

### Changed

Explain the important changes.

### Files

List the main files changed.

### Verification

State what was actually run, for example:

```text
dotnet build     ✓
dotnet test      ✓
Playwright check ✓
```

Never claim a command passed unless it was actually executed successfully.

### Remaining Issues

Mention known limitations, warnings, TODOs, or follow-up work.

If nothing remains, say so briefly.

---

# 27. Core Principle

Work like a responsible engineer maintaining a real production codebase.

Do not optimize for generating the largest amount of code.

Optimize for:

> understanding the problem → making the smallest correct change → testing it → verifying the result.

# 28. graphify

This project has a generated architecture snapshot at `graphify-out/`. Its visual groups represent project ownership, not statistical community detection.

When the user types `/graphify`, use the installed graphify skill or instructions before doing anything else.

Rules:
- Follow the startup sequence above: read `GRAPH_REPORT.md`, consult `graph.json` as needed, and verify freshness before relying on architecture claims. `graph.html` is the interactive human explorer.
- For focused codebase questions, use compatible `graphify query`, `graphify path`, or `graphify explain` commands when useful after the freshness check. An existing wiki may help navigation. These are indexes, not replacements for reading the exact source before editing.
- Dirty graph files alone are not a reason to skip the graph. Report stale or incorrect output and verify source when necessary.
- Refresh after meaningful architecture changes using `.\tools\graphify.ps1`. Do not use generic `graphify update .` for this custom snapshot schema; it can replace required metadata.
- Do not regenerate for trivial documentation/comment edits unless fingerprinted inputs changed and the task needs a current snapshot. Static graph test counts are not evidence that tests executed.

# 29. Plugin / Skill Policy

Before substantial work, determine whether an installed skill or plugin materially improves the task.

Do NOT install plugins simply because they appear in the preferred catalog.

Use this process:

TASK
  ↓
Check installed capabilities
  ↓
Relevant capability exists?
  ├── YES → use it when beneficial
  └── NO
       ↓
Search available plugins/skills
       ↓
Relevant trusted capability exists?
       ├── YES → install/connect it only when permitted and appropriate
       └── NO → continue using built-in/local tooling

Never block normal development merely because an optional plugin is unavailable.

Never substitute a plugin's technology for the HMS technology stack without explicit approval.

Search trusted available capabilities if a relevant capability is missing. Use the normal supported installation/connection authorization flow only when appropriate; never bypass it. Report installed/available, available but not installed, not found in the searched catalog, and intentionally skipped accurately. Tool availability is not proof that an account connection works; a CLI is not an installed plugin.

Plugin installation and application dependency installation are separate decisions. Installing an EF Core skill does not authorize EF Core NuGet packages in Phase 02 or Phase 03. Do not install every preferred plugin, and do not persist a machine-specific availability list as a permanent guarantee.

   ## Preferred Plugin Catalog

### Priority — HMS Development

Prefer when relevant:

- Superpowers
- GitHub
- Playwright
- Context7
- .NET / ASP.NET Core
- Entity Framework Core
- SQL Server

### Conditional

Use only when the task requires them:

- Docker
- Sentry
- Azure
- AWS
- Figma
- Linear
- Notion
- Slack
- OpenAI Docs
- Terraform
- Kubernetes
- Vercel
- Cloudflare

### Do Not Introduce Without Explicit Approval

Skip these capabilities/technologies by default; they are not the HMS application stack:

- Build Web Apps
- shadcn/ui
- Supabase
- Stripe
- PostgreSQL
- Firebase
- Prisma
- Tailwind CSS
- ESLint
- Prettier
- Vitest
- Jest
- Cypress
- Storybook
- Next.js
- React
- TypeScript
- Node.js

Do not install, configure, or introduce these simply because a plugin is available.

If a future requirement genuinely requires one, explain why and its architectural impact, and obtain approval before changing the stack. Already-installed tooling does not authorize introducing its technology into HMS.
