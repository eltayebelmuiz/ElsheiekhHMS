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
    public const string PatientRegistered = "PATIENT_REGISTERED";
    public const string PatientDemographicsUpdated = "PATIENT_DEMOGRAPHICS_UPDATED";
    public const string PatientContactUpdated = "PATIENT_CONTACT_UPDATED";
    public const string PatientIdentifiersUpdated = "PATIENT_IDENTIFIERS_UPDATED";
    public const string DepartmentCreated = "DEPARTMENT_CREATED";
    public const string DepartmentUpdated = "DEPARTMENT_UPDATED";
    public const string DepartmentDeactivated = "DEPARTMENT_DEACTIVATED";
    public const string AppointmentScheduled = "APPOINTMENT_SCHEDULED";
    public const string AppointmentCancelled = "APPOINTMENT_CANCELLED";
    public const string AppointmentNoShow = "APPOINTMENT_NO_SHOW";
    public const string AppointmentCheckedIn = "APPOINTMENT_CHECKED_IN";
    public const string AppointmentCompleted = "APPOINTMENT_COMPLETED";
    public const string QueueEntryCreated = "QUEUE_ENTRY_CREATED";
    public const string QueueEntryCalled = "QUEUE_ENTRY_CALLED";
    public const string QueueEntryStarted = "QUEUE_ENTRY_STARTED";
    public const string QueueEntrySkipped = "QUEUE_ENTRY_SKIPPED";
    public const string QueueEntryHeld = "QUEUE_ENTRY_HELD";
    public const string QueueEntryResumed = "QUEUE_ENTRY_RESUMED";
    public const string QueueEntryCompleted = "QUEUE_ENTRY_COMPLETED";
    public const string QueueEntryCancelled = "QUEUE_ENTRY_CANCELLED";

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
        PrivilegedRoleRemoved,
        PatientRegistered,
        PatientDemographicsUpdated,
        PatientContactUpdated,
        PatientIdentifiersUpdated,
        DepartmentCreated,
        DepartmentUpdated,
        DepartmentDeactivated,
        AppointmentScheduled,
        AppointmentCancelled,
        AppointmentNoShow,
        AppointmentCheckedIn,
        AppointmentCompleted,
        QueueEntryCreated,
        QueueEntryCalled,
        QueueEntryStarted,
        QueueEntrySkipped,
        QueueEntryHeld,
        QueueEntryResumed,
        QueueEntryCompleted,
        QueueEntryCancelled
    ];
}
