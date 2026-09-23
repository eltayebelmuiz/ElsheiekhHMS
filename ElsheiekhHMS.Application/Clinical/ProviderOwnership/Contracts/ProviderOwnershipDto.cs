namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;

public sealed record ProviderOwnershipDto(
    int DoctorId,
    string UserId);
