namespace server.domain.player;

public class StatusBonusPercent(
    int maxHpPercent,
    int maxMpPercent,
    int strengthPercent,
    int defensePercent,
    int intelligencePercent,
    int luckPercent,
    int speedPercent)
{
    public int MaxHpPercent { get; } = ValidateNonNegative(maxHpPercent, nameof(maxHpPercent));
    public int MaxMpPercent { get; } = ValidateNonNegative(maxMpPercent, nameof(maxMpPercent));
    public int StrengthPercent { get; } = ValidateNonNegative(strengthPercent, nameof(strengthPercent));
    public int DefensePercent { get; } = ValidateNonNegative(defensePercent, nameof(defensePercent));
    public int IntelligencePercent { get; } = ValidateNonNegative(intelligencePercent, nameof(intelligencePercent));
    public int LuckPercent { get; } = ValidateNonNegative(luckPercent, nameof(luckPercent));
    public int SpeedPercent { get; } = ValidateNonNegative(speedPercent, nameof(speedPercent));

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "割合に負数は指定できません。");
        }

        return value;
    }
}
