using ElsheiekhHMS.Application.Common.Contracts;

namespace ElsheiekhHMS.Application.Departments.Contracts;

public sealed record DepartmentSearchRequest
{
    public DepartmentSearchRequest(
        string? searchText = null,
        bool? isActive = null,
        PageRequest? page = null,
        DepartmentSortField sortBy = DepartmentSortField.Name,
        SortDirection sortDirection = SortDirection.Ascending)
    {
        SearchText = Optional(searchText);
        IsActive = isActive;
        Page = page ?? new PageRequest();
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public string? SearchText { get; }
    public bool? IsActive { get; }
    public PageRequest Page { get; }
    public DepartmentSortField SortBy { get; }
    public SortDirection SortDirection { get; }

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
