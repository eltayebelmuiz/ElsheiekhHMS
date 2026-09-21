namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class QueueTicketAllocation
{
    private QueueTicketAllocation()
    {
    }

    public QueueTicketAllocation(DateOnly queueDate, int nextSequenceNumber)
    {
        QueueDate = queueDate;
        NextSequenceNumber = nextSequenceNumber;
    }

    public DateOnly QueueDate { get; private set; }

    public int NextSequenceNumber { get; private set; }
}
