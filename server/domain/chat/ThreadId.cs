namespace server.domain.chat;

public readonly record struct ThreadId(Guid Value)
{
    public static ThreadId New() => new(Guid.NewGuid());
}
