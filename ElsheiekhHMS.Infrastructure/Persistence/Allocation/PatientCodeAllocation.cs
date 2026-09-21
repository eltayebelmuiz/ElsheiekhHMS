namespace ElsheiekhHMS.Infrastructure.Persistence.Allocation;

public sealed class PatientCodeAllocation
{
    private PatientCodeAllocation()
    {
    }

    public PatientCodeAllocation(int codeYear, int nextSequenceNumber)
    {
        CodeYear = codeYear;
        NextSequenceNumber = nextSequenceNumber;
    }

    public int CodeYear { get; private set; }

    public int NextSequenceNumber { get; private set; }
}
