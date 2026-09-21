namespace ElsheiekhHMS.Application.Common.Security;

public static class RoleNames
{
    public const string SystemAdministrator = "SystemAdministrator";
    public const string Administrator = "Administrator";
    public const string Receptionist = "Receptionist";
    public const string Provider = "Provider";
    public const string Patient = "Patient";

    public static IReadOnlyList<string> All { get; } =
    [
        SystemAdministrator,
        Administrator,
        Receptionist,
        Provider,
        Patient
    ];
}
