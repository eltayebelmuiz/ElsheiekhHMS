using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Application.Patients.Contracts;

public sealed record RegisterPatientRequest
{
    public RegisterPatientRequest(
        string firstName,
        string? middleName,
        string? thirdName,
        string lastName,
        DateOnly dateOfBirth,
        Gender gender,
        BloodGroup? bloodGroup,
        string? nationalId,
        string? passportNumber,
        string phone,
        string address,
        string? city,
        string? emergencyContactName,
        string? emergencyContactPhone,
        string? emergencyContactRelationship,
        string? insuranceProvider)
    {
        FirstName = Trim(firstName);
        MiddleName = Optional(middleName);
        ThirdName = Optional(thirdName);
        LastName = Trim(lastName);
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodGroup = bloodGroup;
        NationalId = Optional(nationalId);
        PassportNumber = Optional(passportNumber);
        Phone = Trim(phone);
        Address = Trim(address);
        City = Optional(city);
        EmergencyContactName = Optional(emergencyContactName);
        EmergencyContactPhone = Optional(emergencyContactPhone);
        EmergencyContactRelationship = Optional(emergencyContactRelationship);
        InsuranceProvider = Optional(insuranceProvider);
    }

    public string FirstName { get; }
    public string? MiddleName { get; }
    public string? ThirdName { get; }
    public string LastName { get; }
    public DateOnly DateOfBirth { get; }
    public Gender Gender { get; }
    public BloodGroup? BloodGroup { get; }
    public string? NationalId { get; }
    public string? PassportNumber { get; }
    public string Phone { get; }
    public string Address { get; }
    public string? City { get; }
    public string? EmergencyContactName { get; }
    public string? EmergencyContactPhone { get; }
    public string? EmergencyContactRelationship { get; }
    public string? InsuranceProvider { get; }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
