namespace ElsheiekhHMS.Application.Appointments.Contracts;

public sealed record CancelAppointmentRequest(
    int AppointmentId,
    string? Reason,
    string? ExpectedConcurrencyToken)
{
    public string? Reason { get; } = Optional(Reason);

    public string? ExpectedConcurrencyToken { get; } = Optional(ExpectedConcurrencyToken);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
