using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Departments.Contracts;

namespace ElsheiekhHMS.Application.Departments;

public interface IDepartmentService
{
    Task<ServiceResult<DepartmentDetailsDto>> GetByIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResult<DepartmentSummaryDto>>> SearchAsync(
        DepartmentSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<DepartmentDetailsDto>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<DepartmentDetailsDto>> UpdateAsync(
        int departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<DepartmentDetailsDto>> DeactivateAsync(
        int departmentId,
        CancellationToken cancellationToken = default);
}
