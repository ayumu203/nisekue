using server.domain.move.enums;

namespace server.domain.battle;

public class BattleBuffState(BuffStat stat, BuffCalculationType calculationType, decimal value, int remainingTurns)
{
    public BuffStat Stat { get; } = stat;
    public BuffCalculationType CalculationType { get; } = calculationType;
    public decimal Value { get; } = ValidateValue(value, calculationType);
    public int RemainingTurns { get; } = ValidateTurns(remainingTurns);

    private static decimal ValidateValue(decimal value, BuffCalculationType calculationType)
    {
        if (calculationType == BuffCalculationType.Mul && value <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Mul 指定時の value は0より大きい必要があります。");
        }

        return value;
    }

    private static int ValidateTurns(int remainingTurns)
    {
        if (remainingTurns <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingTurns), "remainingTurns は1以上である必要があります。");
        }

        return remainingTurns;
    }
}
