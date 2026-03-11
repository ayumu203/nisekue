namespace server.domain.quest;

public readonly record struct QuestParticipantId(Guid Value)
{
    public static QuestParticipantId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
