namespace server.domain.quest;

public readonly record struct QuestRoomId(Guid Value)
{
    public static QuestRoomId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
