namespace server.domain.battle;

public class BattleFieldContext(
    IEnumerable<BattleActorPosition>? positions = null,
    IEnumerable<BattleActorId>? bossActorIds = null,
    bool enableFormationRangeControl = true)
{
    private readonly BattleActorPosition[] positions = positions?.ToArray() ?? [];
    private readonly HashSet<BattleActorId> bossActorIds = bossActorIds?.ToHashSet() ?? [];

    public IReadOnlyList<BattleActorPosition> Positions => positions;
    public IReadOnlyCollection<BattleActorId> BossActorIds => bossActorIds;
    public bool EnableFormationRangeControl { get; } = enableFormationRangeControl;

    public bool IsBossActor(BattleActorId actorId)
    {
        return bossActorIds.Contains(actorId);
    }
}
