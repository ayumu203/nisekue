namespace server.domain.player;

public sealed class CombatIndexCalculator(ICombatIndexWeightRepository weightRepository)
{
    private readonly IReadOnlyDictionary<string, double> weights = weightRepository
        .GetAll()
        .ToDictionary(x => x.StatusKey, x => x.Weight, StringComparer.Ordinal);

    public int Calculate(Status status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var score =
            status.MaxHp * GetWeight(RankingStatusKeys.MaxHp)
            + status.MaxMp * GetWeight(RankingStatusKeys.MaxMp)
            + status.Strength * GetWeight(RankingStatusKeys.Strength)
            + status.Defense * GetWeight(RankingStatusKeys.Defense)
            + status.Intelligence * GetWeight(RankingStatusKeys.Intelligence)
            + status.Luck * GetWeight(RankingStatusKeys.Luck)
            + status.Speed * GetWeight(RankingStatusKeys.Speed);

        return (int)Math.Floor(score);
    }

    private double GetWeight(string statusKey)
    {
        if (weights.TryGetValue(statusKey, out var weight))
        {
            return weight;
        }

        throw new InvalidOperationException($"statusKey={statusKey} の重みが未定義です。");
    }
}
