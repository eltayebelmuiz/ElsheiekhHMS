namespace ElsheiekhHMS.Application.Departments.Contracts;

public sealed record CreateDepartmentRequest
{
    public CreateDepartmentRequest(string name, string? description, string? phoneExtension)
    {
        Name = name?.Trim() ?? string.Empty;
        Description = Optional(description);
        PhoneExtension = Optional(phoneExtension);
    }

    public string Name { get; }
    public string? Description { get; }
    public string? PhoneExtension { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
