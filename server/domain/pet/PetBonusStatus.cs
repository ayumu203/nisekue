namespace server.domain.pet;

public class PetBonusStatus(
    int maxHp = 0,
    int maxMp = 0,
    int strength = 0,
    int defense = 0,
    int intelligence = 0,
    int luck = 0,
    int speed = 0)
{
    public int MaxHp { get; } = ValidateNonNegative(maxHp, nameof(maxHp));
    public int MaxMp { get; } = ValidateNonNegative(maxMp, nameof(maxMp));
    public int Strength { get; } = ValidateNonNegative(strength, nameof(strength));
    public int Defense { get; } = ValidateNonNegative(defense, nameof(defense));
    public int Intelligence { get; } = ValidateNonNegative(intelligence, nameof(intelligence));
    public int Luck { get; } = ValidateNonNegative(luck, nameof(luck));
    public int Speed { get; } = ValidateNonNegative(speed, nameof(speed));

    public PetBonusStatus Add(PetBonusStatus other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return new PetBonusStatus(
            maxHp: ClampedAdd(MaxHp, other.MaxHp),
            maxMp: ClampedAdd(MaxMp, other.MaxMp),
            strength: ClampedAdd(Strength, other.Strength),
            defense: ClampedAdd(Defense, other.Defense),
            intelligence: ClampedAdd(Intelligence, other.Intelligence),
            luck: ClampedAdd(Luck, other.Luck),
            speed: ClampedAdd(Speed, other.Speed));
    }

    private static int ClampedAdd(int left, int right)
    {
        var sum = (long)left + right;
        return sum > int.MaxValue ? int.MaxValue : (int)sum;
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
    }
}
