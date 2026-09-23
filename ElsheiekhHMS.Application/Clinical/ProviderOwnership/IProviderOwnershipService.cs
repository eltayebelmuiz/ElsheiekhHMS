using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;
using ElsheiekhHMS.Application.Common.Results;

namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership;

public interface IProviderOwnershipService
{
    Task<ServiceResult<ProviderOwnershipDto>> AssignAsync(
        AssignProviderOwnershipRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<ProviderOwnershipDto>> UnassignAsync(
        int doctorId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> ResolveCurrentDoctorIdAsync(
        CancellationToken cancellationToken = default);
}
