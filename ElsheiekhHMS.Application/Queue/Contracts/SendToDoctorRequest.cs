namespace ElsheiekhHMS.Application.Queue.Contracts;

public sealed record SendToDoctorRequest(
    int QueueEntryId,
    int DoctorId,
    string? ExpectedConcurrencyToken)
{
    public string? ExpectedConcurrencyToken { get; } = Optional(ExpectedConcurrencyToken);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
