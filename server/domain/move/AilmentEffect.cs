using server.domain.move.enums;
using server.shared.constants.move;

namespace server.domain.move;

public class AilmentEffect(AilmentType ailmentType, decimal ailmentRate)
{
    public AilmentType AilmentType { get; } = ailmentType;
    public decimal AilmentRate { get; } = ValidateRateRange(ailmentRate);

    private static decimal ValidateRateRange(decimal value)
    {
        if (value < MoveConstants.Constraints.MinRate || value > MoveConstants.Constraints.MaxRate)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "付与率は0.00〜1.00である必要があります。");
        }

        return value;
    }
}
