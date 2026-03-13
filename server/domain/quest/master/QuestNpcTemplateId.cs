namespace server.domain.quest;

public readonly record struct QuestNpcTemplateId(int Value)
{
    public override string ToString() => Value.ToString();
}
