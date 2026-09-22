using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Core.Domain.Patients.Entities;

namespace ElsheiekhHMS.Application.Patients.Persistence;

public interface IPatientPersistence
{
    Task<string> AllocatePatientCodeAsync(int year, CancellationToken cancellationToken);
    Task<PatientDetailsDto?> GetDetailsAsync(int patientId, CancellationToken cancellationToken);
    Task<PagedResult<PatientSummaryDto>> SearchAsync(PatientSearchRequest request, CancellationToken cancellationToken);
    Task<Patient?> LoadTrackedAsync(int patientId, CancellationToken cancellationToken);
    Task<bool> ExistsByNationalIdAsync(string value, int? excludingPatientId, CancellationToken cancellationToken);
    Task<bool> ExistsByPassportNumberAsync(string value, int? excludingPatientId, CancellationToken cancellationToken);
    void Add(Patient patient);
    Task<PatientPersistenceSaveStatus> SaveChangesAsync(CancellationToken cancellationToken);
}
