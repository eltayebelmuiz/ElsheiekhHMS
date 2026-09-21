using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Application.Patients.Contracts;

public sealed record UpdatePatientDemographicsRequest
{
    public UpdatePatientDemographicsRequest(
        string firstName,
        string? middleName,
        string? thirdName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        BloodGroup? bloodGroup,
        string? expectedConcurrencyToken)
    {
        FirstName = Trim(firstName);
        MiddleName = Optional(middleName);
        ThirdName = Optional(thirdName);
        LastName = Trim(lastName);
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodGroup = bloodGroup;
        ExpectedConcurrencyToken = Optional(expectedConcurrencyToken);
    }

    public string FirstName { get; }
    public string? MiddleName { get; }
    public string? ThirdName { get; }
    public string LastName { get; }
    public DateOnly DateOfBirth { get; }
    public Gender Gender { get; }
    public BloodGroup? BloodGroup { get; }
    public string? ExpectedConcurrencyToken { get; }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
