namespace server.domain.battle;

public class BattleFieldContext(IEnumerable<BattleActorPosition>? positions = null, bool enableFormationRangeControl = true)
{
    private readonly BattleActorPosition[] positions = positions?.ToArray() ?? [];

    public IReadOnlyList<BattleActorPosition> Positions => positions;
    public bool EnableFormationRangeControl { get; } = enableFormationRangeControl;
}
