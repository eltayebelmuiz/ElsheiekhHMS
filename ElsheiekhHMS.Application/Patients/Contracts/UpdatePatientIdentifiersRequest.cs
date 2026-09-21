namespace ElsheiekhHMS.Application.Patients.Contracts;

public sealed record UpdatePatientIdentifiersRequest
{
    public UpdatePatientIdentifiersRequest(
        string? nationalId,
        string? passportNumber,
        string? expectedConcurrencyToken)
    {
        NationalId = Optional(nationalId);
        PassportNumber = Optional(passportNumber);
        ExpectedConcurrencyToken = Optional(expectedConcurrencyToken);
    }

    public string? NationalId { get; }
    public string? PassportNumber { get; }
    public string? ExpectedConcurrencyToken { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
