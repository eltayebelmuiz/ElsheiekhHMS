namespace ElsheiekhHMS.Application.Appointments.Persistence;

public enum AppointmentPersistenceSaveStatus
{
    Saved,
    ConcurrencyConflict,
    Collision
}
