using server.domain.move;
using server.domain.move.enums;

namespace server.domain.battle;

public class BattleAilmentState(
    AilmentType type,
    int remainingTurns,
    DamageEffect? triggerDamage = null,
    MoveId? sourceMoveId = null)
{
    public AilmentType Type { get; } = type;
    public int RemainingTurns { get; } = ValidateTurns(remainingTurns);
    public DamageEffect? TriggerDamage { get; } = triggerDamage;
    public MoveId? SourceMoveId { get; } = sourceMoveId;

    private static int ValidateTurns(int remainingTurns)
    {
        if (remainingTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingTurns), "remainingTurns は1以上である必要があります。");
        }

        return remainingTurns;
    }
}
