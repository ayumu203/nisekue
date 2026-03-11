namespace server.domain.battle;

public class BattleActionResult(
    BattleActorId actorId,
    bool succeeded,
    IEnumerable<BattleTargetResult>? targetResults = null)
{
    private readonly BattleTargetResult[] _targetResults = targetResults?.ToArray() ?? [];

    public BattleActorId ActorId { get; } = actorId;
    public bool Succeeded { get; } = succeeded;
    public IReadOnlyList<BattleTargetResult> TargetResults => _targetResults;
}
