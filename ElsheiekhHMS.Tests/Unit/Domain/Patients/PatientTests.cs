using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Exceptions;
using ElsheiekhHMS.Core.Interfaces;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Patients;

public class PatientTests
{
    private static readonly DateOnly AsOfDate = new(2026, 9, 21);
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 9, 21, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Construction_preserves_durable_identity_and_audit_metadata()
    {
        var patient = CreatePatient();

        Assert.Equal("PT-2026-00001", patient.PatientCode);
        Assert.Equal("أحمد", patient.FirstName);
        Assert.Equal("محمد", patient.LastName);
        Assert.Equal("أحمد محمد", patient.FullName);
        Assert.Equal("أحمد محمد", patient.ShortName);
        Assert.Equal(new DateOnly(1990, 1, 1), patient.DateOfBirth);
        Assert.Equal(Gender.Male, patient.Gender);
        Assert.Null(patient.BloodGroup);
        Assert.Equal("N-42", patient.NationalId);
        Assert.Null(patient.PassportNumber);
        Assert.Equal("0912345678", patient.Phone);
        Assert.Equal("Khartoum", patient.Address);
        Assert.Null(patient.City);
        Assert.Null(patient.InsuranceProvider);
        Assert.False(patient.IsDeleted);
        Assert.Equal(CreatedAt, patient.CreatedAt);
        Assert.Equal("registrar-1", patient.CreatedBy);
        Assert.Null(patient.UpdatedAt);
    }

    [Fact]
    public void Four_part_name_and_optional_contact_values_are_normalized()
    {
        var patient = CreatePatient(middleName: "  عبد  ", thirdName: "  الرحمن  ",
            bloodGroup: BloodGroup.ABPositive, city: "  أم درمان  ",
            emergencyContactName: "  فاطمة  ", emergencyContactPhone: "  09876  ",
            emergencyContactRelationship: "  أخت  ", insuranceProvider: "  Provider  ");

        Assert.Equal("أحمد عبد الرحمن محمد", patient.FullName);
        Assert.Equal("أحمد محمد", patient.ShortName);
        Assert.Equal("عبد", patient.MiddleName);
        Assert.Equal("الرحمن", patient.ThirdName);
        Assert.Equal(BloodGroup.ABPositive, patient.BloodGroup);
        Assert.Equal("أم درمان", patient.City);
        Assert.Equal("فاطمة", patient.EmergencyContactName);
        Assert.Equal("09876", patient.EmergencyContactPhone);
        Assert.Equal("أخت", patient.EmergencyContactRelationship);
        Assert.Equal("Provider", patient.InsuranceProvider);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Required_names_cannot_be_blank(string? value)
    {
        Assert.Throws<DomainValidationException>(() => CreatePatient(firstName: value!));
        Assert.Throws<DomainValidationException>(() => CreatePatient(lastName: value!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Code_phone_and_address_cannot_be_blank(string? value)
    {
        Assert.Throws<DomainValidationException>(() => CreatePatient(patientCode: value!));
        Assert.Throws<DomainValidationException>(() => CreatePatient(phone: value!));
        Assert.Throws<DomainValidationException>(() => CreatePatient(address: value!));
    }

    [Theory]
    [InlineData("PT-2026-0001")]
    [InlineData("PT-2026-000001")]
    [InlineData("pt-2026-00001")]
    [InlineData("PT-202A-00001")]
    [InlineData("PT-2026-٠٠٠٠١")]
    public void Patient_code_requires_exact_ASCII_format(string code) =>
        Assert.Throws<DomainValidationException>(() => CreatePatient(patientCode: code));

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(2026, 9, 22)]
    public void Invalid_birth_date_is_rejected(int year, int month, int day)
    {
        var birth = year == 0 ? DateOnly.MinValue : new DateOnly(year, month, day);
        Assert.Throws<DomainValidationException>(() => CreatePatient(dateOfBirth: birth));
    }

    [Fact]
    public void Undefined_enums_are_rejected()
    {
        Assert.Throws<DomainValidationException>(() => CreatePatient(gender: (Gender)100));
        Assert.Throws<DomainValidationException>(() => CreatePatient(bloodGroup: (BloodGroup)100));
    }

    [Fact]
    public void Distinct_patients_can_share_a_phone_number()
    {
        var first = CreatePatient();
        var second = CreatePatient(firstName: "فاطمة");
        Assert.Equal(first.Phone, second.Phone);
    }

    [Theory]
    [InlineData(BloodGroup.APositive, 0)]
    [InlineData(BloodGroup.ANegative, 1)]
    [InlineData(BloodGroup.BPositive, 2)]
    [InlineData(BloodGroup.BNegative, 3)]
    [InlineData(BloodGroup.ABPositive, 4)]
    [InlineData(BloodGroup.ABNegative, 5)]
    [InlineData(BloodGroup.OPositive, 6)]
    [InlineData(BloodGroup.ONegative, 7)]
    public void Approved_blood_group_values_remain_stable(BloodGroup group, int value)
    {
        Assert.Equal(8, Enum.GetValues<BloodGroup>().Length);
        Assert.Equal(value, (int)group);
    }

    [Fact]
    public void Demographic_update_preserves_code_and_recomputes_names()
    {
        var patient = CreatePatient();
        patient.UpdateDemographics("  فاطمة  ", "  عبد  ", "  الله  ", "  علي  ",
            new DateOnly(2001, 3, 4), Gender.Female, BloodGroup.ONegative,
            AsOfDate, UpdatedAt, "registrar-2");

        Assert.Equal("PT-2026-00001", patient.PatientCode);
        Assert.Equal("فاطمة عبد الله علي", patient.FullName);
        Assert.Equal("فاطمة علي", patient.ShortName);
        Assert.Equal(new DateOnly(2001, 3, 4), patient.DateOfBirth);
        Assert.Equal(Gender.Female, patient.Gender);
        Assert.Equal(BloodGroup.ONegative, patient.BloodGroup);
        Assert.Equal(UpdatedAt, patient.UpdatedAt);
        Assert.Equal("registrar-2", patient.UpdatedBy);
    }

    [Fact]
    public void Future_birth_date_update_preserves_all_demographics_and_audit()
    {
        var patient = CreatePatient();
        Assert.Throws<DomainValidationException>(() => patient.UpdateDemographics(
            "Changed", null, null, "Name", new DateOnly(2026, 9, 22),
            Gender.Other, BloodGroup.BPositive, AsOfDate, UpdatedAt, "registrar-2"));

        Assert.Equal("أحمد محمد", patient.FullName);
        Assert.Equal(new DateOnly(1990, 1, 1), patient.DateOfBirth);
        Assert.Equal(Gender.Male, patient.Gender);
        Assert.Null(patient.BloodGroup);
        Assert.Null(patient.UpdatedAt);
        Assert.Null(patient.UpdatedBy);
    }

    [Fact]
    public void Invalid_name_or_enum_update_preserves_original_state()
    {
        var patient = CreatePatient();
        Assert.Throws<DomainValidationException>(() => patient.UpdateDemographics(
            "  ", null, null, "Valid", new DateOnly(2000, 1, 1),
            Gender.Female, null, AsOfDate, UpdatedAt, "registrar-2"));
        Assert.Throws<DomainValidationException>(() => patient.UpdateDemographics(
            "Valid", null, null, "Name", new DateOnly(2000, 1, 1),
            (Gender)99, null, AsOfDate, UpdatedAt, "registrar-2"));
        Assert.Equal("أحمد محمد", patient.FullName);
        Assert.Equal(Gender.Male, patient.Gender);
        Assert.Null(patient.UpdatedAt);
    }

    [Fact]
    public void Contact_update_normalizes_all_fields_and_allows_a_shared_phone()
    {
        var patient = CreatePatient();
        var otherPatient = CreatePatient(firstName: "ليلى", phone: "  09876  ");
        patient.UpdateContactDetails("  09876  ", "  New address  ", "  Omdurman  ",
            "  علي  ", "  09999  ", "  أخ  ", "  Insurance  ",
            UpdatedAt, "registrar-2");

        Assert.Equal(otherPatient.Phone, patient.Phone);
        Assert.Equal("New address", patient.Address);
        Assert.Equal("Omdurman", patient.City);
        Assert.Equal("علي", patient.EmergencyContactName);
        Assert.Equal("09999", patient.EmergencyContactPhone);
        Assert.Equal("أخ", patient.EmergencyContactRelationship);
        Assert.Equal("Insurance", patient.InsuranceProvider);
        Assert.Equal(UpdatedAt, patient.UpdatedAt);
        Assert.Equal("registrar-2", patient.UpdatedBy);
    }

    [Fact]
    public void Invalid_contact_update_preserves_values_and_audit()
    {
        var patient = CreatePatient();
        Assert.Throws<DomainValidationException>(() => patient.UpdateContactDetails(
            "  New phone  ", "  ", "City", "Name", "Phone", "Sister", "Insurance",
            UpdatedAt, "registrar-2"));
        Assert.Equal("0912345678", patient.Phone);
        Assert.Equal("Khartoum", patient.Address);
        Assert.Null(patient.City);
        Assert.Null(patient.EmergencyContactName);
        Assert.Null(patient.UpdatedAt);
    }

    [Fact]
    public void Optional_fields_can_be_cleared_and_identifiers_normalize_to_null()
    {
        var patient = CreatePatient();
        patient.UpdateIdentifiers("  ", "  P-123  ", UpdatedAt, "registrar-2");
        Assert.Null(patient.NationalId);
        Assert.Equal("P-123", patient.PassportNumber);

        patient.UpdateContactDetails("0912345678", "Khartoum", "  ",
            "  ", " ", null, "  ", UpdatedAt, "registrar-2");
        Assert.Null(patient.City);
        Assert.Null(patient.EmergencyContactName);
        Assert.Null(patient.EmergencyContactPhone);
        Assert.Null(patient.EmergencyContactRelationship);
        Assert.Null(patient.InsuranceProvider);
        Assert.Equal("registrar-2", patient.UpdatedBy);
    }

    [Fact]
    public void Soft_deletion_sets_consistent_metadata_and_rejects_repetition()
    {
        var patient = CreatePatient();
        Assert.IsAssignableFrom<IHasConcurrencyToken>(patient);
        patient.MarkDeleted(UpdatedAt, "registrar-2");

        Assert.True(patient.IsDeleted);
        Assert.Equal(UpdatedAt, patient.DeletedAt);
        Assert.Equal("registrar-2", patient.DeletedBy);
        Assert.Equal(UpdatedAt, patient.UpdatedAt);
        Assert.Equal("registrar-2", patient.UpdatedBy);
        Assert.Throws<BusinessRuleException>(() => patient.MarkDeleted(
            UpdatedAt.AddMinutes(1), "registrar-3"));
        Assert.Equal(UpdatedAt, patient.DeletedAt);
        Assert.Equal("registrar-2", patient.DeletedBy);
    }

    [Fact]
    public void Deleted_patient_rejects_every_update_without_changing_state()
    {
        var patient = CreatePatient();
        patient.MarkDeleted(UpdatedAt, "registrar-2");
        var later = UpdatedAt.AddMinutes(1);

        Assert.Throws<BusinessRuleException>(() => patient.UpdateDemographics(
            "Different", null, null, "Person", new DateOnly(2000, 1, 1),
            Gender.Female, null, AsOfDate, later, "registrar-3"));
        Assert.Throws<BusinessRuleException>(() => patient.UpdateContactDetails(
            "09999", "New address", null, null, null, null, null,
            later, "registrar-3"));
        Assert.Throws<BusinessRuleException>(() => patient.UpdateIdentifiers(
            "New ID", "New passport", later, "registrar-3"));

        Assert.Equal("أحمد محمد", patient.FullName);
        Assert.Equal("0912345678", patient.Phone);
        Assert.Equal("N-42", patient.NationalId);
        Assert.Equal(UpdatedAt, patient.UpdatedAt);
        Assert.Equal("registrar-2", patient.UpdatedBy);
    }

    private static Patient CreatePatient(
        string patientCode = "PT-2026-00001", string firstName = "أحمد",
        string? middleName = null, string? thirdName = null, string lastName = "محمد",
        DateOnly? dateOfBirth = null, Gender gender = Gender.Male,
        BloodGroup? bloodGroup = null, string? nationalId = "  N-42  ",
        string? passportNumber = "  ", string phone = "0912345678",
        string address = "  Khartoum  ", string? city = null,
        string? emergencyContactName = null, string? emergencyContactPhone = null,
        string? emergencyContactRelationship = null, string? insuranceProvider = null)
        => new(patientCode, firstName, middleName, thirdName, lastName,
            dateOfBirth ?? new DateOnly(1990, 1, 1), gender, bloodGroup,
            nationalId, passportNumber, phone, address, city,
            emergencyContactName, emergencyContactPhone, emergencyContactRelationship,
            insuranceProvider, AsOfDate, CreatedAt, "registrar-1");
}
