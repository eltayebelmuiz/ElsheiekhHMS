using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Application.Patients.Contracts;

public sealed record PatientSummaryDto(
    int Id,
    string PatientCode,
    string FullName,
    DateOnly DateOfBirth,
    Gender Gender,
    string Phone);
