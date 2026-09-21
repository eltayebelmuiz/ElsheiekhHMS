using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Queue;

public sealed class QueueContractTests
{
    [Fact]
    public void Add_contract_references_registered_patient_and_normalizes_notes()
    {
        var request = new AddWalkInQueueEntryRequest(7, 3, QueuePriority.Urgent, "  Needs review  ");

        Assert.Equal(7, request.PatientId);
        Assert.Equal("Needs review", request.Notes);
    }

    [Fact]
    public void Queue_read_contracts_do_not_expose_anonymous_demographics_or_position()
    {
        var summaryNames = typeof(QueueEntrySummaryDto).GetProperties().Select(property => property.Name);
        var detailNames = typeof(QueueEntryDetailsDto).GetProperties().Select(property => property.Name);

        Assert.Contains("PatientId", summaryNames);
        Assert.Contains("QueueNumber", summaryNames);
        Assert.Contains("ConcurrencyToken", detailNames);
        Assert.DoesNotContain("QueuePosition", summaryNames);
        Assert.DoesNotContain("FirstName", detailNames);
        Assert.DoesNotContain("Phone", detailNames);
    }

    [Fact]
    public void Search_contract_requires_queue_date_and_uses_explicit_sorting()
    {
        var request = new QueueSearchRequest(new DateOnly(2026, 10, 1));

        Assert.Equal(25, request.Page.PageSize);
        Assert.Equal(QueueSortField.RegisteredAt, request.SortBy);
        Assert.Equal(SortDirection.Ascending, request.SortDirection);
    }
}
