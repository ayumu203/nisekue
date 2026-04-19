using server.domain.player;
using server.domain.training;
using server.shared.constants.training;

namespace server.application.training;

public class TrainingExpCalculator
{
    public int Calculate(Player player, TrainingEnemy enemy, TrainingContributionMetrics metrics, TrainingOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(metrics);

        return CalculateInternal(player, enemy.Level, enemy.Status.MaxHp, metrics, outcome);
    }

    public int CalculatePvp(Player player, int opponentLevel, int opponentMaxHp, TrainingContributionMetrics metrics, TrainingOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(metrics);

        return CalculateInternal(player, opponentLevel, opponentMaxHp, metrics, outcome);
    }

    private int CalculateInternal(Player player, int enemyLevel, int enemyMaxHp, TrainingContributionMetrics metrics, TrainingOutcome outcome)
    {
        var outcomeRate = outcome switch
        {
            TrainingOutcome.Win => TrainingConstants.Exp.WinMultiplier,
            TrainingOutcome.Lose => TrainingConstants.Exp.LoseMultiplier,
            _ => TrainingConstants.Exp.DrawMultiplier
        };

        var baseExp = enemyLevel * TrainingConstants.Exp.BaseExpPerEnemyLevel;
        var levelDiff = enemyLevel - player.Level;
        var levelRate = levelDiff switch
        {
            <= TrainingConstants.Exp.LevelRateVeryLowThreshold => TrainingConstants.Exp.LevelRateVeryLow,
            <= TrainingConstants.Exp.LevelRateLowThreshold => TrainingConstants.Exp.LevelRateLow,
            <= TrainingConstants.Exp.LevelRateNormalThreshold => TrainingConstants.Exp.LevelRateNormal,
            <= TrainingConstants.Exp.LevelRateHighThreshold => TrainingConstants.Exp.LevelRateHigh,
            _ => TrainingConstants.Exp.LevelRateVeryHigh
        };

        var attackScore = ClampRatio(metrics.PlayerDealtTotalDamage, enemyMaxHp);
        var healScore = ClampRatio(metrics.PlayerEffectiveHealTotal, player.Status.MaxHp);
        var survivalScore = ClampRatio(metrics.CurrentPlayerHp, player.Status.MaxHp);
        var contributionRate = TrainingConstants.Exp.BaseContribution
            + (TrainingConstants.Exp.AttackContributionWeight * attackScore)
            + (TrainingConstants.Exp.HealContributionWeight * healScore)
            + (TrainingConstants.Exp.SurvivalContributionWeight * survivalScore);

        var exp = (int)Math.Floor(baseExp * levelRate * outcomeRate * contributionRate);
        return Math.Max(TrainingConstants.Exp.MinimumExp, exp);
    }

    private static decimal ClampRatio(int value, int maxValue)
    {
        if (maxValue <= 0)
        {
            return 0m;
        }

        return Math.Clamp((decimal)Math.Max(0, value) / maxValue, 0m, 1m);
    }
}
