namespace server.domain.quest;

public readonly record struct QuestStageId(int Value)
{
    public override string ToString() => Value.ToString();
}
