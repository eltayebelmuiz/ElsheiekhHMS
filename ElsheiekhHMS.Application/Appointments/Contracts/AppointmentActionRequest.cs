namespace ElsheiekhHMS.Application.Appointments.Contracts;

public sealed record AppointmentActionRequest(
    int AppointmentId,
    string? ExpectedConcurrencyToken)
{
    public string? ExpectedConcurrencyToken { get; } = Optional(ExpectedConcurrencyToken);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
