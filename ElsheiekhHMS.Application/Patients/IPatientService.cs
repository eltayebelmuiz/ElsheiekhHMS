using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Patients.Contracts;

namespace ElsheiekhHMS.Application.Patients;

public interface IPatientService
{
    Task<ServiceResult<PatientDetailsDto>> GetByIdAsync(int patientId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<PatientSummaryDto>>> SearchAsync(PatientSearchRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PatientDetailsDto>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PatientDetailsDto>> UpdateDemographicsAsync(int patientId, UpdatePatientDemographicsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PatientDetailsDto>> UpdateContactDetailsAsync(int patientId, UpdatePatientContactDetailsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PatientDetailsDto>> UpdateIdentifiersAsync(int patientId, UpdatePatientIdentifiersRequest request, CancellationToken cancellationToken = default);
}
