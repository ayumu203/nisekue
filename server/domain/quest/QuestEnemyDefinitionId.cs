namespace server.domain.quest;

public readonly record struct QuestEnemyDefinitionId(int Value)
{
    public override string ToString() => Value.ToString();
}
