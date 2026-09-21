using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Application.Queue.Validation;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Queue;

public sealed class QueueValidationTests
{
    [Fact]
    public void Add_accepts_valid_registered_patient_request()
    {
        var request = new AddWalkInQueueEntryRequest(7, 3, QueuePriority.Normal, null);

        var result = new AddWalkInQueueEntryRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Add_rejects_invalid_ids_priority_and_notes_length()
    {
        var request = new AddWalkInQueueEntryRequest(0, -1, (QueuePriority)99, new string('n', 2001));

        var result = new AddWalkInQueueEntryRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "queue.PatientId.positive");
        Assert.Contains(result.Errors, error => error.Code == "queue.priority.invalid");
        Assert.Contains(result.Errors, error => error.Code == "queue.notes.maximum");
    }

    [Fact]
    public void Send_to_doctor_preserves_opaque_concurrency_token()
    {
        var request = new SendToDoctorRequest(4, 9, "  token  ");

        var result = new SendToDoctorRequestValidator().Validate(request);

        Assert.True(result.IsValid);
        Assert.Equal("token", request.ExpectedConcurrencyToken);
    }

    [Fact]
    public void Search_rejects_invalid_filters_sort_and_paging()
    {
        var request = new QueueSearchRequest(
            DateOnly.MinValue, departmentId: 0, doctorId: -1,
            status: (QueueStatus)99, priority: (QueuePriority)99,
            page: new PageRequest(0, 251), sortBy: (QueueSortField)99,
            sortDirection: (SortDirection)99);

        var result = new QueueSearchRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "queue.date.required");
        Assert.Contains(result.Errors, error => error.Code == "queue.Status.invalid");
        Assert.Contains(result.Errors, error => error.Code == "queue.Priority.invalid");
        Assert.Contains(result.Errors, error => error.Code == "queue.SortBy.invalid");
        Assert.Contains(result.Errors, error => error.Code == "pagination.page_size.maximum");
    }
}
