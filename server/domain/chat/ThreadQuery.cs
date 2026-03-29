using server.shared.constants.chat;

namespace server.domain.chat;

public class ThreadQuery(int page = 1, int pageSize = ThreadConstants.ListPageSize)
{
    public int Page { get; } = page < 1 ? 1 : page;
    public int PageSize { get; } = pageSize < 1 ? ThreadConstants.ListPageSize : Math.Min(pageSize, ThreadConstants.ListPageSize);
}
