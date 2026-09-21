using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Application.Patients.Contracts;

public sealed record PatientDetailsDto(
    int Id,
    string PatientCode,
    string FullName,
    DateOnly DateOfBirth,
    Gender Gender,
    string Phone,
    string? MiddleName,
    string? ThirdName,
    BloodGroup? BloodGroup,
    string? NationalId,
    string? PassportNumber,
    string Address,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelationship,
    string? InsuranceProvider,
    string? ConcurrencyToken);
