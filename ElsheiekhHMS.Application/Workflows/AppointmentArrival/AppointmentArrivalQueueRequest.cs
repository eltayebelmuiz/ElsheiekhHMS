using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Workflows.AppointmentArrival;

public sealed record AppointmentArrivalQueueRequest(
    int AppointmentId,
    string? ExpectedConcurrencyToken,
    QueuePriority Priority,
    string? Notes)
{
    public string? ExpectedConcurrencyToken { get; } = Optional(ExpectedConcurrencyToken);
    public string? Notes { get; } = Optional(Notes);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
