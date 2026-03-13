namespace server.domain.quest;

public readonly record struct QuestEnemyInstanceId(Guid Value)
{
    public static QuestEnemyInstanceId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
