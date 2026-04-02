namespace server.domain.player;

public sealed class StatusRankThreshold(string statusKey, StatusRank rank, int maxValue)
{
    public string StatusKey { get; } = ValidateStatusKey(statusKey);
    public StatusRank Rank { get; } = rank;
    public int MaxValue { get; } = ValidateMaxValue(maxValue);

    private static string ValidateStatusKey(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("statusKey は必須です。", nameof(value));
        }

        return normalized;
    }

    private static int ValidateMaxValue(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "maxValue は0以上である必要があります。");
        }

        return value;
    }
}
