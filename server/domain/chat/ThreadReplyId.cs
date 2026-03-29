namespace server.domain.chat;

public readonly record struct ThreadReplyId(Guid Value)
{
    public static ThreadReplyId New() => new(Guid.NewGuid());
}
