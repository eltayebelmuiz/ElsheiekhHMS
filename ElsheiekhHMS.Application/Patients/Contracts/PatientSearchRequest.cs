using ElsheiekhHMS.Application.Common.Contracts;

namespace ElsheiekhHMS.Application.Patients.Contracts;

public sealed record PatientSearchRequest
{
    public PatientSearchRequest(
        string? searchText = null,
        string? patientCode = null,
        string? phone = null,
        string? nationalId = null,
        string? passportNumber = null,
        DateOnly? dateOfBirth = null,
        PageRequest? page = null,
        PatientSortField sortBy = PatientSortField.PatientCode,
        SortDirection sortDirection = SortDirection.Ascending)
    {
        SearchText = Optional(searchText);
        PatientCode = Optional(patientCode);
        Phone = Optional(phone);
        NationalId = Optional(nationalId);
        PassportNumber = Optional(passportNumber);
        DateOfBirth = dateOfBirth;
        Page = page ?? new PageRequest();
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public string? SearchText { get; }
    public string? PatientCode { get; }
    public string? Phone { get; }
    public string? NationalId { get; }
    public string? PassportNumber { get; }
    public DateOnly? DateOfBirth { get; }
    public PageRequest Page { get; }
    public PatientSortField SortBy { get; }
    public SortDirection SortDirection { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
