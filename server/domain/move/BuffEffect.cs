using server.domain.move.enums;
using server.shared.constants.move;

namespace server.domain.move;

public class BuffEffect(BuffStat buffStat, BuffCalculationType buffCalculationType, decimal buffValue, int buffTurns, decimal buffRate, bool canStack)
{
    public BuffStat BuffStat { get; } = buffStat;
    public BuffCalculationType BuffCalculationType { get; } = buffCalculationType;
    public decimal BuffValue { get; } = ValidateBuffValue(buffValue, buffCalculationType);
    public int BuffTurns { get; } = ValidateTurns(buffTurns);
    public decimal BuffRate { get; } = ValidateRateRange(buffRate);
    public bool CanStack { get; } = canStack;

    private static decimal ValidateBuffValue(decimal value, BuffCalculationType calculationType)
    {
        if (calculationType == BuffCalculationType.Mul && value <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Mul 指定時の buffValue は0より大きい必要があります。");
        }

        return value;
    }

    private static int ValidateTurns(int turns)
    {
        if (turns < MoveConstants.Constraints.MinBuffTurns)
        {
            throw new ArgumentOutOfRangeException(nameof(turns), "buffTurns は1以上である必要があります。");
        }

        return turns;
    }

    private static decimal ValidateRateRange(decimal value)
    {
        if (value < MoveConstants.Constraints.MinRate || value > MoveConstants.Constraints.MaxRate)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "付与率は0.00〜1.00である必要があります。");
        }

        return value;
    }
}
