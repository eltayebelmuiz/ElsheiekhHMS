using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;

namespace ElsheiekhHMS.Application.Queue.Persistence;

public interface IQueuePersistence
{
    Task<QueueEntryDetailsDto?> GetDetailsAsync(int queueEntryId, CancellationToken cancellationToken);
    Task<PagedResult<QueueEntrySummaryDto>> SearchAsync(QueueSearchRequest request, CancellationToken cancellationToken);
    Task<bool> PatientExistsAsync(int patientId, CancellationToken cancellationToken);
    Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken);
    Task<bool> DepartmentIsActiveAsync(int departmentId, CancellationToken cancellationToken);
    Task<bool> DoctorIsActiveInDepartmentAsync(int doctorId, int departmentId, CancellationToken cancellationToken);
    Task<bool> HasActiveEntryAsync(int patientId, DateOnly queueDate, CancellationToken cancellationToken);
    Task<(DateOnly QueueDate, int SequenceNumber, string QueueNumber)> AllocateTicketAsync(DateOnly queueDate, CancellationToken cancellationToken);
    Task<WalkInQueueEntry?> LoadTrackedAsync(int queueEntryId, CancellationToken cancellationToken);
    void Add(WalkInQueueEntry entry);
    Task<QueuePersistenceSaveStatus> SaveChangesAsync(CancellationToken cancellationToken);
}
