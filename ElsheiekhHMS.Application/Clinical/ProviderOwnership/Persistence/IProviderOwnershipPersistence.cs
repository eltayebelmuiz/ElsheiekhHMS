using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;

namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;

public interface IProviderOwnershipPersistence
{
    Task<ProviderAccountStatus> GetAccountStatusAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<DoctorStatusSnapshot> GetDoctorStatusAsync(
        int doctorId,
        CancellationToken cancellationToken);

    Task<ProviderOwnershipDto?> GetByDoctorIdAsync(
        int doctorId,
        CancellationToken cancellationToken);

    Task<ProviderOwnershipDto?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken);

    void Add(int doctorId, string userId);

    Task<bool> RemoveByDoctorIdAsync(
        int doctorId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
