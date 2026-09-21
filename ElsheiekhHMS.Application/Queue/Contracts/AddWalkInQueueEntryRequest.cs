using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Queue.Contracts;

public sealed record AddWalkInQueueEntryRequest(
    int PatientId,
    int DepartmentId,
    QueuePriority Priority,
    string? Notes)
{
    public string? Notes { get; } = Optional(Notes);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
