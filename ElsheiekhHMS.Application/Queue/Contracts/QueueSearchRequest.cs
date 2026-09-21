using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Queue.Contracts;

public sealed record QueueSearchRequest
{
    public QueueSearchRequest(
        DateOnly queueDate,
        int? departmentId = null,
        int? doctorId = null,
        QueueStatus? status = null,
        QueuePriority? priority = null,
        PageRequest? page = null,
        QueueSortField sortBy = QueueSortField.RegisteredAt,
        SortDirection sortDirection = SortDirection.Ascending)
    {
        QueueDate = queueDate;
        DepartmentId = departmentId;
        DoctorId = doctorId;
        Status = status;
        Priority = priority;
        Page = page ?? new PageRequest();
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public DateOnly QueueDate { get; }
    public int? DepartmentId { get; }
    public int? DoctorId { get; }
    public QueueStatus? Status { get; }
    public QueuePriority? Priority { get; }
    public PageRequest Page { get; }
    public QueueSortField SortBy { get; }
    public SortDirection SortDirection { get; }
}
