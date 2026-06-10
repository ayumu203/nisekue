using server.domain.quest;

namespace server.domain.pet_battle;

public class PetBattleLastTurnResults(
    int turnNo,
    DateTimeOffset resolvedAt,
    IEnumerable<QuestResolvedAction> actions)
{
    public int TurnNo { get; } = turnNo;
    public DateTimeOffset ResolvedAt { get; } = resolvedAt;
    public IReadOnlyList<QuestResolvedAction> Actions { get; } = actions?.ToArray()
        ?? throw new ArgumentNullException(nameof(actions));
}
