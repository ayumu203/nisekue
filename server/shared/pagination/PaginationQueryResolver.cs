namespace server.shared.pagination;

public static class PaginationQueryResolver
{
    public static bool TryResolve(
        int? page,
        int? pageSize,
        int? limit,
        out int? offset,
        out int? effectiveLimit,
        out string? errorMessage)
    {
        offset = null;
        effectiveLimit = limit;
        errorMessage = null;

        if (page is <= 0 || pageSize is <= 0 || limit is <= 0)
        {
            errorMessage = "page/pageSize/limit は1以上を指定してください。";
            return false;
        }

        var hasPage = page is not null;
        var hasPageSize = pageSize is not null;
        if (hasPage != hasPageSize)
        {
            errorMessage = "page と pageSize は両方指定するか、どちらも省略してください。";
            return false;
        }

        if (!hasPage || !hasPageSize)
        {
            return true;
        }

        offset = (page!.Value - 1) * pageSize!.Value;
        effectiveLimit = effectiveLimit is null
            ? pageSize.Value
            : Math.Min(pageSize.Value, effectiveLimit.Value);
        return true;
    }
}
