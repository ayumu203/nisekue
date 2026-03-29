namespace server.application.chat;

public sealed record ThreadListPageView(
    IReadOnlyList<ThreadSummaryView> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage);
