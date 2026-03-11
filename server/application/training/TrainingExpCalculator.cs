using server.domain.player;
using server.domain.training;
using server.shared.constants.training;

namespace server.application.training;

public class TrainingExpCalculator
{
    public int Calculate(Player player, TrainingEnemy enemy, TrainingOutcome outcome, int playerDealtTotalDamage)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(enemy);

        var levelDiff = Math.Abs(player.Level - enemy.Level);
        var resultBonus = outcome switch
        {
            TrainingOutcome.Win => TrainingConstants.Exp.WinMultiplier,
            TrainingOutcome.Lose => TrainingConstants.Exp.LoseMultiplier,
            _ => TrainingConstants.Exp.DrawMultiplier
        };

        var baseExp = Math.Max(
            TrainingConstants.Exp.BaseExpMin,
            playerDealtTotalDamage / TrainingConstants.Exp.DamageBaseDivisor);

        var levelBonus = levelDiff switch
        {
            <= TrainingConstants.Exp.LevelBonusHighThreshold => TrainingConstants.Exp.LevelBonusHigh,
            <= TrainingConstants.Exp.LevelBonusMidThreshold => TrainingConstants.Exp.LevelBonusMid,
            _ => TrainingConstants.Exp.LevelBonusLow
        };

        var exp = (int)Math.Floor(baseExp * (levelBonus + resultBonus));
        return Math.Max(1, exp);
    }
}
