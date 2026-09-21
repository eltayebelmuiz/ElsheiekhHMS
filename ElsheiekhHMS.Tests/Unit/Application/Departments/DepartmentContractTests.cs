using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Application.Departments.Validation;

namespace ElsheiekhHMS.Tests.Unit.Application.Departments;

public sealed class DepartmentContractTests
{
    [Fact]
    public void Create_contract_trims_text_and_converts_blank_optionals_to_null()
    {
        var request = new CreateDepartmentRequest("  Outpatient  ", "  General care  ", "  ");

        Assert.Equal("Outpatient", request.Name);
        Assert.Equal("General care", request.Description);
        Assert.Null(request.PhoneExtension);
    }

    [Fact]
    public void Department_read_contracts_expose_only_approved_fields()
    {
        var summaryNames = typeof(DepartmentSummaryDto).GetProperties().Select(property => property.Name);
        var detailNames = typeof(DepartmentDetailsDto).GetProperties().Select(property => property.Name);

        Assert.Equal(["Id", "Name", "IsActive", "PhoneExtension"], summaryNames);
        Assert.Contains("Description", detailNames);
        Assert.DoesNotContain("FacilityId", detailNames);
        Assert.DoesNotContain("HeadDoctorId", detailNames);
    }
}
