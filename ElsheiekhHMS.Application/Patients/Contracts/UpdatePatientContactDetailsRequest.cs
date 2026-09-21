namespace ElsheiekhHMS.Application.Patients.Contracts;

public sealed record UpdatePatientContactDetailsRequest
{
    public UpdatePatientContactDetailsRequest(
        string phone,
        string address,
        string? city,
        string? emergencyContactName,
        string? emergencyContactPhone,
        string? emergencyContactRelationship,
        string? insuranceProvider,
        string? expectedConcurrencyToken)
    {
        Phone = Trim(phone);
        Address = Trim(address);
        City = Optional(city);
        EmergencyContactName = Optional(emergencyContactName);
        EmergencyContactPhone = Optional(emergencyContactPhone);
        EmergencyContactRelationship = Optional(emergencyContactRelationship);
        InsuranceProvider = Optional(insuranceProvider);
        ExpectedConcurrencyToken = Optional(expectedConcurrencyToken);
    }

    public string Phone { get; }
    public string Address { get; }
    public string? City { get; }
    public string? EmergencyContactName { get; }
    public string? EmergencyContactPhone { get; }
    public string? EmergencyContactRelationship { get; }
    public string? InsuranceProvider { get; }
    public string? ExpectedConcurrencyToken { get; }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
