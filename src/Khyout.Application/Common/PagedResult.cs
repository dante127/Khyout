namespace Khyout.Application.Common;

/// <summary>Standard paged response. Page size is clamped to a hard maximum of 50 (platform invariant).</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    long TotalCount)
{
    public long TotalPages => PageSize == 0 ? 0 : (long)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, long totalCount)
        => new(items, pageNumber, pageSize, totalCount);
}

/// <summary>Pagination conventions for the whole API.</summary>
public static class Pagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
    {
        var page = pageNumber <= 0 ? 1 : pageNumber;
        var size = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (page, size);
    }
}
