using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Application.Patients.Validation;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Patients;

public sealed class PatientContractTests
{
    [Fact]
    public void Registration_contract_trims_text_and_preserves_unicode()
    {
        var request = new RegisterPatientRequest(
            "  Élise  ", "  Marie  ", "   ", "  Uwase  ",
            new DateOnly(1990, 1, 2), Gender.Female, BloodGroup.APositive,
            "  N-1  ", "  ", "  0780000000  ", "  Kigali  ", "  ",
            "  Aline  ", "  ", "  Sister  ", "  Health Plan  ");

        Assert.Equal("Élise", request.FirstName);
        Assert.Equal("Marie", request.MiddleName);
        Assert.Null(request.ThirdName);
        Assert.Equal("Uwase", request.LastName);
        Assert.Equal("N-1", request.NationalId);
        Assert.Null(request.PassportNumber);
        Assert.Equal("0780000000", request.Phone);
        Assert.Equal("Aline", request.EmergencyContactName);
        Assert.Null(request.EmergencyContactPhone);
        Assert.Equal("Sister", request.EmergencyContactRelationship);
        Assert.Equal("Health Plan", request.InsuranceProvider);
    }

    [Fact]
    public void Patient_read_contracts_exclude_audit_and_security_fields()
    {
        var summaryNames = typeof(PatientSummaryDto).GetProperties().Select(property => property.Name);
        var detailNames = typeof(PatientDetailsDto).GetProperties().Select(property => property.Name);

        Assert.DoesNotContain("CreatedBy", summaryNames);
        Assert.DoesNotContain("UpdatedBy", detailNames);
        Assert.DoesNotContain("DeletedBy", detailNames);
        Assert.DoesNotContain("RowVersion", detailNames);
        Assert.Contains("ConcurrencyToken", detailNames);
    }

    [Fact]
    public void Search_contract_defaults_to_bounded_paging_and_explicit_sorting()
    {
        var request = new PatientSearchRequest();

        Assert.Equal(1, request.Page.PageNumber);
        Assert.Equal(25, request.Page.PageSize);
        Assert.Equal(PatientSortField.PatientCode, request.SortBy);
        Assert.Equal(SortDirection.Ascending, request.SortDirection);
    }
}
