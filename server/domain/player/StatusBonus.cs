namespace server.domain.player;

public class StatusBonus(
    int maxHp,
    int maxMp,
    int strength,
    int defense,
    int intelligence,
    int luck,
    int speed)
{
    public int MaxHp { get; } = maxHp;
    public int MaxMp { get; } = maxMp;
    public int Strength { get; } = strength;
    public int Defense { get; } = defense;
    public int Intelligence { get; } = intelligence;
    public int Luck { get; } = luck;
    public int Speed { get; } = speed;
}
