using server.domain.move.enums;

namespace server.domain.battle;

public class BattleTargetResult(
    BattleActorId targetActorId,
    int damage,
    int recoveredHp,
    bool isDefeated,
    AilmentType? appliedAilment)
{
    public BattleActorId TargetActorId { get; } = targetActorId;
    public int Damage { get; } = ValidateNonNegative(damage, nameof(damage));
    public int RecoveredHp { get; } = ValidateNonNegative(recoveredHp, nameof(recoveredHp));
    public bool IsDefeated { get; } = isDefeated;
    public AilmentType? AppliedAilment { get; } = appliedAilment;

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} に負数は指定できません。");
        }

        return value;
    }
}
