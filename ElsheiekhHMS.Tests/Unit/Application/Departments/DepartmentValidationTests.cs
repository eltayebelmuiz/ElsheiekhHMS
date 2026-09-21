using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Application.Departments.Validation;

namespace ElsheiekhHMS.Tests.Unit.Application.Departments;

public sealed class DepartmentValidationTests
{
    [Fact]
    public void Create_accepts_valid_unicode_department_data()
    {
        var request = new CreateDepartmentRequest("  Thérapie  ", "  General care  ", "  101  ");

        var result = new CreateDepartmentRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_rejects_missing_name_and_storage_limit_overflow()
    {
        var request = new CreateDepartmentRequest("", new string('d', 2001), new string('1', 33));

        var result = new CreateDepartmentRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "department.name.required");
        Assert.Contains(result.Errors, error => error.Code == "department.Description.maximum");
        Assert.Contains(result.Errors, error => error.Code == "department.PhoneExtension.maximum");
    }

    [Fact]
    public void Update_does_not_accept_or_change_lifecycle_state()
    {
        var propertyNames = typeof(UpdateDepartmentRequest).GetProperties().Select(property => property.Name);

        Assert.DoesNotContain("IsActive", propertyNames);
        Assert.DoesNotContain("Activate", propertyNames);
        Assert.DoesNotContain("Deactivate", propertyNames);
    }
}
