namespace server.domain.quest;

public readonly record struct QuestTrapId(Guid Value)
{
    public static QuestTrapId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
