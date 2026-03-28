namespace server.domain.player;

public class EquipmentStatusBonus(
    int maxHp,
    int maxMp,
    int strength,
    int defense,
    int intelligence,
    int luck,
    int speed)
{
    public int MaxHp { get; } = ValidateNonNegative(maxHp, nameof(maxHp));
    public int MaxMp { get; } = ValidateNonNegative(maxMp, nameof(maxMp));
    public int Strength { get; } = ValidateNonNegative(strength, nameof(strength));
    public int Defense { get; } = ValidateNonNegative(defense, nameof(defense));
    public int Intelligence { get; } = ValidateNonNegative(intelligence, nameof(intelligence));
    public int Luck { get; } = ValidateNonNegative(luck, nameof(luck));
    public int Speed { get; } = ValidateNonNegative(speed, nameof(speed));

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "装備補正値に負数は代入できないです.");
        }

        return value;
    }
}
