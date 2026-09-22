namespace ElsheiekhHMS.Application.Queue.Persistence;

public enum QueuePersistenceSaveStatus
{
    Saved,
    DuplicateActive,
    DuplicateAppointmentLink,
    ConcurrencyConflict,
    TicketAllocationFailure
}
