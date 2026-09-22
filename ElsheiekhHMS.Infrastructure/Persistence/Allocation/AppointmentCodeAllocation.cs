namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class AppointmentCodeAllocation
{
    private AppointmentCodeAllocation()
    {
    }

    public AppointmentCodeAllocation(int allocationYear, int nextSequenceNumber)
    {
        AllocationYear = allocationYear;
        NextSequenceNumber = nextSequenceNumber;
    }

    public int AllocationYear { get; private set; }

    public int NextSequenceNumber { get; private set; }
}
