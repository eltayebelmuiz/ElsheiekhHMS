namespace ElsheiekhHMS.Application.Common.Contracts;

/// <summary>
/// The minimal immutable result shape for a bounded page of items.
/// </summary>
public sealed class PagedResult<T>
{
    public PagedResult(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = items.ToArray();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int PageNumber { get; }

    public int PageSize { get; }
}
