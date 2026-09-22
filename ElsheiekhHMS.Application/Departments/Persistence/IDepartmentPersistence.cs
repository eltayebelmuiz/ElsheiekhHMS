using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Core.Domain.Organization.Entities;

namespace ElsheiekhHMS.Application.Departments.Persistence;

public interface IDepartmentPersistence
{
    Task<DepartmentDetailsDto?> GetDetailsAsync(
        int departmentId,
        CancellationToken cancellationToken);

    Task<PagedResult<DepartmentSummaryDto>> SearchAsync(
        DepartmentSearchRequest request,
        CancellationToken cancellationToken);

    Task<Department?> LoadTrackedAsync(
        int departmentId,
        CancellationToken cancellationToken);

    void Add(Department department);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
