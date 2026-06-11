namespace server.shared.pagination;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage);

public static class PagedResponseFactory
{
    public static PagedResponse<T> Create<T>(
        IReadOnlyList<T> items,
        int totalCount,
        int? page,
        int? pageSize,
        int? offset,
        int? limit)
    {
        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? limit ?? Math.Max(1, items.Count);
        var hasNextPage = offset is not null
            && limit is not null
            && offset.Value + items.Count < totalCount;

        return new PagedResponse<T>(
            Items: items,
            Page: resolvedPage,
            PageSize: resolvedPageSize,
            TotalCount: totalCount,
            HasNextPage: hasNextPage);
    }
}
