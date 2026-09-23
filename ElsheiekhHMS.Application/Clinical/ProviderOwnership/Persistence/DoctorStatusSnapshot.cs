namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;

public sealed record DoctorStatusSnapshot(
    bool Exists,
    bool IsActive);
