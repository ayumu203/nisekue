namespace server.domain.player;

public sealed class CombatIndexRankEvaluator(ICombatIndexRankThresholdRepository thresholdRepository)
{
    private readonly IReadOnlyList<StatusRankThreshold> thresholds = thresholdRepository
        .GetAll()
        .OrderBy(x => x.MaxValue)
        .ToArray();

    public StatusRank Evaluate(int combatIndex)
    {
        foreach (var threshold in thresholds)
        {
            if (combatIndex <= threshold.MaxValue)
            {
                return threshold.Rank;
            }
        }

        return thresholds[^1].Rank;
    }
}
