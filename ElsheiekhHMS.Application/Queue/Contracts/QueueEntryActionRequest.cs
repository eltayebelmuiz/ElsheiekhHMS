namespace ElsheiekhHMS.Application.Queue.Contracts;

public sealed record QueueEntryActionRequest(
    int QueueEntryId,
    string? ExpectedConcurrencyToken,
    string? Reason = null)
{
    public string? ExpectedConcurrencyToken { get; } = Normalize(ExpectedConcurrencyToken);
    public string? Reason { get; } = Normalize(Reason);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
