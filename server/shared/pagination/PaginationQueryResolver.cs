namespace server.shared.pagination;

public static class PaginationQueryResolver
{
    private const int MaxPageSize = 100;

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

        if (pageSize!.Value > MaxPageSize)
        {
            errorMessage = $"pageSize は {MaxPageSize} 以下を指定してください。";
            return false;
        }

        var effectivePageSize = pageSize.Value;
        try
        {
            offset = checked((page!.Value - 1) * effectivePageSize);
        }
        catch (OverflowException)
        {
            errorMessage = "page と pageSize の組み合わせが大きすぎます。";
            return false;
        }

        effectiveLimit = effectiveLimit is null
            ? effectivePageSize
            : Math.Min(effectivePageSize, effectiveLimit.Value);
        return true;
    }
}
