using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Application.Patients.Validation;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Patients;

public sealed class PatientValidationTests
{
    [Fact]
    public void Registration_accepts_unicode_and_duplicate_phone_values()
    {
        var request = new RegisterPatientRequest(
            "Amina", null, null, "Mukamana", new DateOnly(1988, 5, 4),
            Gender.Female, null, null, null, "0780000000", "Address", null,
            null, null, null, null);

        var result = new RegisterPatientRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Registration_rejects_required_and_undefined_enum_values()
    {
        var request = new RegisterPatientRequest(
            "", null, null, "", DateOnly.MinValue, (Gender)99, (BloodGroup)99,
            null, null, "", "", null, null, null, null, null);

        var result = new RegisterPatientRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "patient.FirstName.required");
        Assert.Contains(result.Errors, error => error.Code == "patient.Gender.invalid");
        Assert.Contains(result.Errors, error => error.Code == "patient.BloodGroup.invalid");
    }

    [Fact]
    public void Registration_rejects_values_over_actual_storage_limits()
    {
        var request = new RegisterPatientRequest(
            new string('a', 101), null, null, "Last", new DateOnly(1990, 1, 1),
            Gender.Other, null, null, null, "Phone", "Address", null, null, null, null, null);

        var result = new RegisterPatientRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.Code == "patient.FirstName.maximum");
    }

    [Fact]
    public void Search_rejects_unbounded_page_and_invalid_sort_values()
    {
        var request = new PatientSearchRequest(
            page: new PageRequest(0, 251),
            sortBy: (PatientSortField)99,
            sortDirection: (SortDirection)99);

        var result = new PatientSearchRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "pagination.page_number.positive");
        Assert.Contains(result.Errors, error => error.Code == "pagination.page_size.maximum");
        Assert.Contains(result.Errors, error => error.Code == "patient.sort.invalid");
        Assert.Contains(result.Errors, error => error.Code == "patient.sort_direction.invalid");
    }

    [Fact]
    public void Update_identifiers_allows_clearing_optional_values()
    {
        var request = new UpdatePatientIdentifiersRequest("  ", null, "  ");

        var result = new UpdatePatientIdentifiersRequestValidator().Validate(request);

        Assert.True(result.IsValid);
        Assert.Null(request.NationalId);
        Assert.Null(request.PassportNumber);
        Assert.Null(request.ExpectedConcurrencyToken);
    }
}
