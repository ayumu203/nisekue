namespace server.domain.player;

public sealed class StatusRankEvaluator(IStatusRankThresholdRepository thresholdRepository)
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<StatusRankThreshold>> thresholdsByStatus = thresholdRepository
        .GetAll()
        .GroupBy(x => x.StatusKey, StringComparer.Ordinal)
        .ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<StatusRankThreshold>)g.OrderBy(x => x.MaxValue).ToArray(),
            StringComparer.Ordinal);

    public StatusRank Evaluate(string statusKey, int value)
    {
        if (!thresholdsByStatus.TryGetValue(statusKey, out var thresholds))
        {
            throw new InvalidOperationException($"statusKey={statusKey} のランクしきい値が未定義です。");
        }

        foreach (var threshold in thresholds)
        {
            if (value <= threshold.MaxValue)
            {
                return threshold.Rank;
            }
        }

        return thresholds[^1].Rank;
    }
}
