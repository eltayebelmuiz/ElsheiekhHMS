namespace ElsheiekhHMS.Application.Common.Security;

public static class PolicyNames
{
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageUserSecurity = "CanManageUserSecurity";
    public const string CanViewAuditTrail = "CanViewAuditTrail";
    public const string CanManagePatients = "CanManagePatients";
    public const string CanManageAppointments = "CanManageAppointments";
    public const string CanAccessClinicalRecords = "CanAccessClinicalRecords";
    public const string CanManageBilling = "CanManageBilling";
    public const string CanConfigureSystem = "CanConfigureSystem";

    public static IReadOnlyList<string> All { get; } =
    [
        CanManageUsers,
        CanManageUserSecurity,
        CanViewAuditTrail,
        CanManagePatients,
        CanManageAppointments,
        CanAccessClinicalRecords,
        CanManageBilling,
        CanConfigureSystem
    ];
}
