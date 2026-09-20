# Phase 03A Department Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a minimal, invariant-protecting Department entity with focused tests and correct the Core/Application responsibility wording.

**Architecture:** Department lives in Core under the approved domain namespace and inherits `AuditableEntity`. It has one Active-to-Inactive lifecycle, no persistence attributes, no soft-delete state, and no concurrency token.

**Tech Stack:** .NET 10, C#, xUnit, PowerShell, custom Graphify generator.

**Spec:** `docs/superpowers/specs/2026-09-20-phase03a-department-foundation-design.md`

## Global Constraints

- Implement Department only; introduce no other Phase 03 domain.
- Add no packages, project references, EF Core, SQL Server, Identity, DTOs, services, repositories, controllers, or UI.
- Core owns intrinsic invariants; Application owns use-case orchestration.
- Do not stage, commit, or push.
- Write and observe failing tests before production code.
- Refresh Graphify only after implementation and tests pass.

## Review Focus

- Null, empty, and whitespace-only names must fail without mutation.
- Valid names must be trimmed.
- Inactive Departments must reject repeated deactivation and detail changes.
- Failed operations must preserve the last successful audit metadata.
- Optional description and phone extension must accept null.

---

### Task 1: Correct the constitution boundary

**Files:**
- Modify: `speckit/my-project/.specify/memory/constitution.md`

**Interfaces:**
- Consumes: approved Core/Application responsibility rule.
- Produces: unambiguous guidance for subsequent domain work.

- [ ] Replace `Application contains use cases and business logic` in Principle I with wording that Core owns intrinsic domain invariants/behavior and Application owns orchestration and coordinated rules.
- [ ] Replace `Application services MUST be the only location for use-case orchestration and business rules` with the same responsibility split.
- [ ] Run `rtk proxy git diff -- speckit/my-project/.specify/memory/constitution.md`; verify no unrelated constitution text changed.

### Task 2: Add Department test-first

**Files:**
- Create first: `ElsheiekhHMS.Tests/Unit/Domain/Organization/DepartmentTests.cs`
- Create only after RED: `ElsheiekhHMS.Core/Domain/Organization/Entities/Department.cs`

**Interfaces:**
- Consumes: `AuditableEntity`, `BusinessRuleException`, and `DomainValidationException`.
- Produces: the constructor and behavior shown below.

- [ ] Create the failing test file:

```csharp
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Exceptions;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Organization;

public class DepartmentTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_creates_active_department_with_normalized_name_and_audit_metadata()
    {
        var department = new Department(
            "  Emergency  ", "Emergency care", "101", CreatedAt, "admin-1");

        Assert.Equal("Emergency", department.Name);
        Assert.Equal("Emergency care", department.Description);
        Assert.Equal("101", department.PhoneExtension);
        Assert.True(department.IsActive);
        Assert.Equal(CreatedAt, department.CreatedAt);
        Assert.Equal("admin-1", department.CreatedBy);
        Assert.Null(department.UpdatedAt);
        Assert.Null(department.UpdatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_missing_name(string? name)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Department(name!, null, null, CreatedAt, null));
    }

    [Fact]
    public void UpdateDetails_changes_values_and_records_update_metadata()
    {
        var department = CreateDepartment();
        var updatedAt = CreatedAt.AddHours(1);

        department.UpdateDetails(
            "  Clinical Laboratory  ", "Diagnostic testing", "205", updatedAt, "admin-2");

        Assert.Equal("Clinical Laboratory", department.Name);
        Assert.Equal("Diagnostic testing", department.Description);
        Assert.Equal("205", department.PhoneExtension);
        Assert.Equal(updatedAt, department.UpdatedAt);
        Assert.Equal("admin-2", department.UpdatedBy);
    }

    [Fact]
    public void UpdateDetails_rejects_missing_name_without_mutating_department()
    {
        var department = CreateDepartment();

        Assert.Throws<DomainValidationException>(() =>
            department.UpdateDetails(
                " ", "Changed", "999", CreatedAt.AddHours(1), "admin-2"));

        Assert.Equal("Emergency", department.Name);
        Assert.Equal("Emergency care", department.Description);
        Assert.Equal("101", department.PhoneExtension);
        Assert.Null(department.UpdatedAt);
        Assert.Null(department.UpdatedBy);
    }

    [Fact]
    public void Deactivate_marks_department_inactive_and_records_update_metadata()
    {
        var department = CreateDepartment();
        var updatedAt = CreatedAt.AddHours(1);

        department.Deactivate(updatedAt, "admin-2");

        Assert.False(department.IsActive);
        Assert.Equal(updatedAt, department.UpdatedAt);
        Assert.Equal("admin-2", department.UpdatedBy);
    }

    [Fact]
    public void Deactivate_rejects_repeated_transition_without_replacing_audit_metadata()
    {
        var department = CreateDepartment();
        var firstUpdate = CreatedAt.AddHours(1);
        department.Deactivate(firstUpdate, "admin-2");

        Assert.Throws<BusinessRuleException>(() =>
            department.Deactivate(CreatedAt.AddHours(2), "admin-3"));

        Assert.Equal(firstUpdate, department.UpdatedAt);
        Assert.Equal("admin-2", department.UpdatedBy);
    }

    [Fact]
    public void UpdateDetails_rejects_inactive_department()
    {
        var department = CreateDepartment();
        department.Deactivate(CreatedAt.AddHours(1), "admin-2");

        Assert.Throws<BusinessRuleException>(() =>
            department.UpdateDetails(
                "Cardiology", null, null, CreatedAt.AddHours(2), "admin-3"));
    }

    private static Department CreateDepartment() =>
        new("Emergency", "Emergency care", "101", CreatedAt, "admin-1");
}
```

- [ ] Run `rtk proxy dotnet test ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj --filter FullyQualifiedName~DepartmentTests`.
- [ ] Verify RED: compilation fails specifically because `Department` does not exist.
- [ ] Create the minimal implementation:

```csharp
using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Exceptions;

namespace ElsheiekhHMS.Core.Domain.Organization.Entities;

public sealed class Department : AuditableEntity
{
    public Department(
        string name,
        string? description,
        string? phoneExtension,
        DateTimeOffset createdAt,
        string? createdBy)
    {
        Name = NormalizeName(name);
        Description = description;
        PhoneExtension = phoneExtension;
        IsActive = true;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? PhoneExtension { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateDetails(
        string name,
        string? description,
        string? phoneExtension,
        DateTimeOffset updatedAt,
        string? updatedBy)
    {
        EnsureActive();

        var normalizedName = NormalizeName(name);
        Name = normalizedName;
        Description = description;
        PhoneExtension = phoneExtension;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureActive();

        IsActive = false;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new BusinessRuleException("Inactive departments cannot be changed.");
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Department name is required.");
        }

        return name.Trim();
    }
}
```

- [ ] Run the focused command again; verify all nine Department cases pass.
- [ ] Run `rtk proxy dotnet test ElsheiekhHMS.slnx --no-restore`; verify the complete suite passes.

### Task 3: Verify architecture and refresh Graphify

**Files:**
- Regenerate: `graphify-out/graph.json`
- Regenerate: `graphify-out/graph.html`
- Regenerate: `graphify-out/GRAPH_REPORT.md`

**Interfaces:**
- Consumes: completed source and `tools/graphify.ps1`.
- Produces: a fresh snapshot containing Department and its AuditableEntity inheritance.

- [ ] Run `rtk proxy dotnet restore ElsheiekhHMS.slnx`.
- [ ] Run `rtk proxy dotnet build ElsheiekhHMS.slnx --no-restore`; require zero warnings/errors.
- [ ] Run `rtk proxy dotnet test ElsheiekhHMS.slnx --no-build`; require 19 passed, zero failed/skipped.
- [ ] Inspect Core project/source for project, framework, package, EF Core, SQL Server, Identity, Blazor, Application, Infrastructure, and Web dependencies.
- [ ] Run `rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\graphify.ps1`.
- [ ] Run the generator with `-Check`; verify fresh fingerprints.
- [ ] Verify the graph contains `ElsheiekhHMS.Core.Domain.Organization.Entities.Department`, its inheritance edge to `AuditableEntity`, and no forbidden Core dependency.
- [ ] Run `rtk proxy git diff --check`, `rtk proxy git diff --stat`, and `rtk proxy git status --short`.
- [ ] Confirm the diff contains only approved source/tests, two focused constitution changes, these planning artifacts, and regenerated Graphify outputs.

