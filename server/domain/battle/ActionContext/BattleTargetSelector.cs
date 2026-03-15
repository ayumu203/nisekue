using server.domain.move.enums;

namespace server.domain.battle;

public class BattleTargetSelector(
    TargetType targetType,
    AttackRange attackRange,
    IEnumerable<BattleActorId>? targetActorIds = null,
    BattlePosition? selectedPosition = null)
{
    private readonly BattleActorId[] _targetActorIds = targetActorIds?.ToArray() ?? [];

    public TargetType TargetType { get; } = targetType;
    public AttackRange AttackRange { get; } = attackRange;
    public IReadOnlyList<BattleActorId> TargetActorIds => _targetActorIds;
    public BattlePosition? SelectedPosition { get; } = selectedPosition;
}
