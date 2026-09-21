namespace ElsheiekhHMS.Application.Departments.Contracts;

public sealed record DepartmentSummaryDto(
    int Id,
    string Name,
    bool IsActive,
    string? PhoneExtension);
