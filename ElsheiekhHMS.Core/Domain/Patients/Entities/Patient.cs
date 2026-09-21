using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Exceptions;
using ElsheiekhHMS.Core.Interfaces;

namespace ElsheiekhHMS.Core.Domain.Patients.Entities;

public sealed class Patient : SoftDeletableEntity, IHasConcurrencyToken
{
    // EF materializes persisted values after invoking this constructor. Normal
    // domain creation continues through the validated public constructor below.
    private Patient()
    {
        PatientCode = null!;
        FirstName = null!;
        LastName = null!;
        Phone = null!;
        Address = null!;
    }

    public Patient(
        string patientCode, string firstName, string? middleName, string? thirdName,
        string lastName, DateOnly dateOfBirth, Gender gender, BloodGroup? bloodGroup,
        string? nationalId, string? passportNumber, string phone, string address,
        string? city, string? emergencyContactName, string? emergencyContactPhone,
        string? emergencyContactRelationship, string? insuranceProvider,
        DateOnly asOfDate, DateTimeOffset createdAt, string? createdBy)
    {
        var code = Required(patientCode, "Patient code");
        if (!IsPatientCode(code))
        {
            throw new DomainValidationException("Patient code must match PT-YYYY-NNNNN.");
        }

        var normalizedFirstName = Required(firstName, "First name");
        var normalizedLastName = Required(lastName, "Last name");
        ValidateBirthDate(dateOfBirth, asOfDate);
        ValidateEnums(gender, bloodGroup);
        var normalizedPhone = Required(phone, "Phone");
        var normalizedAddress = Required(address, "Address");

        PatientCode = code;
        FirstName = normalizedFirstName;
        MiddleName = Optional(middleName);
        ThirdName = Optional(thirdName);
        LastName = normalizedLastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodGroup = bloodGroup;
        NationalId = Optional(nationalId);
        PassportNumber = Optional(passportNumber);
        Phone = normalizedPhone;
        Address = normalizedAddress;
        City = Optional(city);
        EmergencyContactName = Optional(emergencyContactName);
        EmergencyContactPhone = Optional(emergencyContactPhone);
        EmergencyContactRelationship = Optional(emergencyContactRelationship);
        InsuranceProvider = Optional(insuranceProvider);
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string PatientCode { get; }
    public string FirstName { get; private set; }
    public string? MiddleName { get; private set; }
    public string? ThirdName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => string.Join(" ",
        new[] { FirstName, MiddleName, ThirdName, LastName }
            .Where(part => !string.IsNullOrEmpty(part)));
    public string ShortName => $"{FirstName} {LastName}";
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public BloodGroup? BloodGroup { get; private set; }
    public string? NationalId { get; private set; }
    public string? PassportNumber { get; private set; }
    public string Phone { get; private set; }
    public string Address { get; private set; }
    public string? City { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public string? EmergencyContactRelationship { get; private set; }
    public string? InsuranceProvider { get; private set; }
    public byte[] RowVersion { get; set; } = [];

    public void UpdateDemographics(
        string firstName, string? middleName, string? thirdName, string lastName,
        DateOnly dateOfBirth, Gender gender, BloodGroup? bloodGroup,
        DateOnly asOfDate, DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureNotDeleted();
        var nextFirstName = Required(firstName, "First name");
        var nextLastName = Required(lastName, "Last name");
        ValidateBirthDate(dateOfBirth, asOfDate);
        ValidateEnums(gender, bloodGroup);

        FirstName = nextFirstName;
        MiddleName = Optional(middleName);
        ThirdName = Optional(thirdName);
        LastName = nextLastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        BloodGroup = bloodGroup;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void UpdateContactDetails(
        string phone, string address, string? city,
        string? emergencyContactName, string? emergencyContactPhone,
        string? emergencyContactRelationship, string? insuranceProvider,
        DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureNotDeleted();
        var nextPhone = Required(phone, "Phone");
        var nextAddress = Required(address, "Address");

        Phone = nextPhone;
        Address = nextAddress;
        City = Optional(city);
        EmergencyContactName = Optional(emergencyContactName);
        EmergencyContactPhone = Optional(emergencyContactPhone);
        EmergencyContactRelationship = Optional(emergencyContactRelationship);
        InsuranceProvider = Optional(insuranceProvider);
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void UpdateIdentifiers(
        string? nationalId, string? passportNumber,
        DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureNotDeleted();
        NationalId = Optional(nationalId);
        PassportNumber = Optional(passportNumber);
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void MarkDeleted(DateTimeOffset deletedAt, string? deletedBy)
    {
        EnsureNotDeleted();
        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new BusinessRuleException("Deleted patients cannot be changed.");
        }
    }

    private static string Required(string? value, string label) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainValidationException($"{label} is required.")
            : value.Trim();

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateBirthDate(DateOnly birth, DateOnly asOfDate)
    {
        if (birth == DateOnly.MinValue || birth > asOfDate)
        {
            throw new DomainValidationException("Date of birth must not be in the future.");
        }
    }

    private static void ValidateEnums(Gender gender, BloodGroup? bloodGroup)
    {
        if (!Enum.IsDefined(gender) ||
            (bloodGroup.HasValue && !Enum.IsDefined(bloodGroup.Value)))
        {
            throw new DomainValidationException("Patient demographic value is invalid.");
        }
    }

    private static bool IsPatientCode(string code) =>
        code.Length == 13 && code.StartsWith("PT-", StringComparison.Ordinal) &&
        code[7] == '-' && code[3..7].All(c => c is >= '0' and <= '9') &&
        code[8..].All(c => c is >= '0' and <= '9');
}
