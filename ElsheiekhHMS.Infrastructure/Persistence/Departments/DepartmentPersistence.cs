using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Application.Departments.Persistence;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Infrastructure.Persistence.Departments;

public sealed class DepartmentPersistence(ElsheiekhHmsDbContext context) : IDepartmentPersistence
{
    public async Task<DepartmentDetailsDto?> GetDetailsAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        return await context.Departments
            .AsNoTracking()
            .Where(department => department.Id == departmentId)
            .Select(department => new DepartmentDetailsDto(
                department.Id,
                department.Name,
                department.Description,
                department.PhoneExtension,
                department.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<DepartmentSummaryDto>> SearchAsync(
        DepartmentSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.Departments.AsNoTracking();
        if (request.SearchText is not null)
        {
            var searchText = request.SearchText;
            query = query.Where(department =>
                department.Name.Contains(searchText) ||
                (department.Description != null && department.Description.Contains(searchText)) ||
                (department.PhoneExtension != null && department.PhoneExtension.Contains(searchText)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(department => department.IsActive == request.IsActive.Value);
        }

        var ordered = request.SortBy switch
        {
            DepartmentSortField.IsActive => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(department => department.IsActive)
                : query.OrderByDescending(department => department.IsActive),
            _ => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(department => department.Name)
                : query.OrderByDescending(department => department.Name)
        };

        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .ThenBy(department => department.Id)
            .Skip((request.Page.PageNumber - 1) * request.Page.PageSize)
            .Take(request.Page.PageSize)
            .Select(department => new DepartmentSummaryDto(
                department.Id,
                department.Name,
                department.IsActive,
                department.PhoneExtension))
            .ToListAsync(cancellationToken);

        return new PagedResult<DepartmentSummaryDto>(
            items,
            totalCount,
            request.Page.PageNumber,
            request.Page.PageSize);
    }

    public Task<Department?> LoadTrackedAsync(
        int departmentId,
        CancellationToken cancellationToken) =>
        context.Departments.SingleOrDefaultAsync(
            department => department.Id == departmentId,
            cancellationToken);

    public void Add(Department department) => context.Departments.Add(department);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
