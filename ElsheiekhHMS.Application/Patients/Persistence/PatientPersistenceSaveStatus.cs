namespace ElsheiekhHMS.Application.Patients.Persistence;

public enum PatientPersistenceSaveStatus
{
    Saved,
    ConcurrencyConflict,
    NationalIdConflict,
    PassportNumberConflict
}
