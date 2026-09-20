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
