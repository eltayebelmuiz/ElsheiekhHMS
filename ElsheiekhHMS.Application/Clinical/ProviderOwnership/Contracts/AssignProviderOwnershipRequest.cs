namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;

public sealed record AssignProviderOwnershipRequest(
    int DoctorId,
    string UserId);
