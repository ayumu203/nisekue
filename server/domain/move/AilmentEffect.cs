using server.domain.move.enums;
using server.shared.constants.move;

namespace server.domain.move;

public class AilmentEffect(AilmentType ailmentType, decimal ailmentRate, int ailmentTurns, DamageEffect? triggerDamage = null)
{
    public AilmentType AilmentType { get; } = ailmentType;
    public decimal AilmentRate { get; } = ValidateRateRange(ailmentRate);
    public int AilmentTurns { get; } = ValidateTurns(ailmentTurns);
    public DamageEffect? TriggerDamage { get; } = ValidateTriggerDamage(ailmentType, triggerDamage);

    private static decimal ValidateRateRange(decimal value)
    {
        if (value < MoveConstants.Constraints.MinRate || value > MoveConstants.Constraints.MaxRate)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "付与率は0.00〜1.00である必要があります。");
        }

        return value;
    }

    private static int ValidateTurns(int turns)
    {
        if (turns < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(turns), "状態異常ターンは1以上である必要があります。");
        }

        return turns;
    }

    private static DamageEffect? ValidateTriggerDamage(AilmentType ailmentType, DamageEffect? triggerDamage)
    {
        if (ailmentType == AilmentType.DamageTrap && triggerDamage is null)
        {
            throw new ArgumentException("DamageTrap には triggerDamage が必要です。", nameof(triggerDamage));
        }

        return triggerDamage;
    }
}
