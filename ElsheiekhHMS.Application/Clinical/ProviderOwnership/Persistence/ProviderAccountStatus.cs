namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;

public sealed record ProviderAccountStatus(
    bool Exists,
    bool HasProviderRole,
    bool IsActive);
