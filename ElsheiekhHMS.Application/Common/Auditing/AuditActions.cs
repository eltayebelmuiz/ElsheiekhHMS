namespace ElsheiekhHMS.Application.Common.Auditing;

public static class AuditActions
{
    public const string AccountLoginBlocked = "ACCOUNT_LOGIN_BLOCKED";
    public const string AccountLoginUnblocked = "ACCOUNT_LOGIN_UNBLOCKED";
    public const string AccountSuspended = "ACCOUNT_SUSPENDED";
    public const string AccountRestored = "ACCOUNT_RESTORED";
    public const string AccountBanned = "ACCOUNT_BANNED";
    public const string AccountBanReversed = "ACCOUNT_BAN_REVERSED";
    public const string SessionsRevoked = "SESSIONS_REVOKED";
    public const string UsernameChanged = "USERNAME_CHANGED";
    public const string PasswordResetByAdmin = "PASSWORD_RESET_BY_ADMIN";
    public const string RoleAssigned = "ROLE_ASSIGNED";
    public const string RoleRemoved = "ROLE_REMOVED";
    public const string PrivilegedRoleAssigned = "PRIVILEGED_ROLE_ASSIGNED";
    public const string PrivilegedRoleRemoved = "PRIVILEGED_ROLE_REMOVED";

    public static IReadOnlyList<string> All { get; } =
    [
        AccountLoginBlocked,
        AccountLoginUnblocked,
        AccountSuspended,
        AccountRestored,
        AccountBanned,
        AccountBanReversed,
        SessionsRevoked,
        UsernameChanged,
        PasswordResetByAdmin,
        RoleAssigned,
        RoleRemoved,
        PrivilegedRoleAssigned,
        PrivilegedRoleRemoved
    ];
}
