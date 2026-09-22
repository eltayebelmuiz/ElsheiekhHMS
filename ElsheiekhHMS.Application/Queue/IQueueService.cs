using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Queue.Contracts;

namespace ElsheiekhHMS.Application.Queue;

public interface IQueueService
{
    Task<ServiceResult<QueueEntryDetailsDto>> GetByIdAsync(int queueEntryId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<QueueEntrySummaryDto>>> SearchAsync(QueueSearchRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<QueueEntryDetailsDto>> AddAsync(AddWalkInQueueEntryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<QueueEntryDetailsDto>> CallToNurseAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<QueueEntryDetailsDto>> SendToDoctorAsync(SendToDoctorRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<QueueEntryDetailsDto>> HoldAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<QueueEntryDetailsDto>> ResumeAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<QueueEntryDetailsDto>> CompleteAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<QueueEntryDetailsDto>> CancelAsync(QueueEntryActionRequest request, CancellationToken cancellationToken = default);
}
