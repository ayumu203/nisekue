using server.domain.move.enums;
using server.shared.constants.move;

namespace server.domain.move;

public class DamageEffect(int hitCount, decimal powerRate, int fixedValue, decimal criticalRate, ElementType elementType)
{
    public int HitCount { get; } = ValidateHitCount(hitCount);
    public decimal PowerRate { get; } = ValidateNonNegative(powerRate, nameof(powerRate));
    public int FixedValue { get; } = ValidateFixedValue(fixedValue);
    public decimal CriticalRate { get; } = ValidateRateRange(criticalRate, nameof(criticalRate));
    public ElementType ElementType { get; } = elementType;

    private static int ValidateHitCount(int hitCount)
    {
        if (hitCount < MoveConstants.Constraints.MinHitCount)
        {
            throw new ArgumentOutOfRangeException(nameof(hitCount), "hitCount は1以上である必要があります。");
        }

        return hitCount;
    }

    private static int ValidateFixedValue(int fixedValue)
    {
        if (fixedValue < MoveConstants.Constraints.MinFixedValue)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedValue), "fixedValue に負数は設定できません。");
        }

        return fixedValue;
    }

    private static decimal ValidateRateRange(decimal value, string paramName)
    {
        if (value < MoveConstants.Constraints.MinRate || value > MoveConstants.Constraints.MaxRate)
        {
            throw new ArgumentOutOfRangeException(paramName, "倍率は0.00〜1.00である必要があります。");
        }

        return value;
    }

    private static decimal ValidateNonNegative(decimal value, string paramName)
    {
        if (value < MoveConstants.Constraints.MinRate)
        {
            throw new ArgumentOutOfRangeException(paramName, "負数は設定できません。");
        }

        return value;
    }
}
