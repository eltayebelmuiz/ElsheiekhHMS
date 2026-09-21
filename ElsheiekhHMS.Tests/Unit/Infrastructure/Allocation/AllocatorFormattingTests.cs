using ElsheiekhHMS.Infrastructure.Persistence.Allocation;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Allocation;

public sealed class AllocatorFormattingTests
{
    [Fact]
    public void PatientCode_format_is_the_approved_year_scoped_shape()
    {
        Assert.Equal("PT-2026-00001", PatientCodeAllocator.Format(2026, 1));
        Assert.Equal("PT-2026-99999", PatientCodeAllocator.Format(2026, 99999));
    }

    [Fact]
    public void PatientCode_format_rejects_values_outside_the_five_digit_sequence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PatientCodeAllocator.Format(2026, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PatientCodeAllocator.Format(2026, 100000));
    }

    [Fact]
    public void QueueTicket_format_is_the_approved_daily_shape()
    {
        Assert.Equal("A-001", QueueTicketAllocator.Format(1));
        Assert.Equal("A-999", QueueTicketAllocator.Format(999));
    }

    [Fact]
    public void QueueTicket_format_rejects_values_outside_the_daily_capacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => QueueTicketAllocator.Format(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => QueueTicketAllocator.Format(1000));
    }
}
