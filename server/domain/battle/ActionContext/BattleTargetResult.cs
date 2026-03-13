using server.domain.move.enums;

namespace server.domain.battle;

public class BattleTargetResult(
    BattleActorId targetActorId,
    int damage,
    bool isDefeated,
    AilmentType? appliedAilment)
{
    public BattleActorId TargetActorId { get; } = targetActorId;
    public int Damage { get; } = ValidateNonNegative(damage);
    public bool IsDefeated { get; } = isDefeated;
    public AilmentType? AppliedAilment { get; } = appliedAilment;

    private static int ValidateNonNegative(int damage)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "damage に負数は指定できません。");
        }

        return damage;
    }
}
