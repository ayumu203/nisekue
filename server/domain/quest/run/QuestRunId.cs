namespace server.domain.quest;

public readonly record struct QuestRunId(Guid Value)
{
    public static QuestRunId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
