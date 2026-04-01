namespace server.domain.player;

public sealed class CombatIndexWeight(string statusKey, double weight)
{
    public string StatusKey { get; } = ValidateStatusKey(statusKey);
    public double Weight { get; } = ValidateWeight(weight);

    private static string ValidateStatusKey(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("statusKey は必須です。", nameof(value));
        }

        return normalized;
    }

    private static double ValidateWeight(double value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "weight は0以上である必要があります。");
        }

        return value;
    }
}
