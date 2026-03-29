namespace server.domain.chat;

public sealed record ThreadPageResult(
    IReadOnlyList<ThreadListItem> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public bool HasNextPage => Page * PageSize < TotalCount;
}
