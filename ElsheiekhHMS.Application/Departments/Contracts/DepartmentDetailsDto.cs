namespace ElsheiekhHMS.Application.Departments.Contracts;

public sealed record DepartmentDetailsDto(
    int Id,
    string Name,
    string? Description,
    string? PhoneExtension,
    bool IsActive);
