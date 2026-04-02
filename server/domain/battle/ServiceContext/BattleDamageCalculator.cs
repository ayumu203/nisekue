using server.domain.move.enums;
using server.domain.player;
using server.shared.constants.battle;

namespace server.domain.battle;

public class BattleDamageCalculator(Func<double>? randomProvider = null)
{
    private readonly Func<double> randomProvider = randomProvider ?? Random.Shared.NextDouble;

    public BattleDamageResult Calculate(BattleDamageInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var attackPower = ResolveAttackPower(input.AttackerStatus, input.AttackStat);
        var defensePower = input.AttackStat == BuffStat.Intelligence
            ? input.DefenderStatus.Intelligence
            : input.DefenderStatus.Defense;

        var baseDamage = input.FixedPower + (int)Math.Round(attackPower * input.PowerRate, MidpointRounding.AwayFromZero);
        var rawDamage = input.AttackStat == BuffStat.Intelligence
            ? CalculateIntelligenceDamage(baseDamage, attackPower, defensePower)
            : Math.Max(1, baseDamage - defensePower);

        var isCritical = input.CriticalRate > 0m && randomProvider() < CalculateCriticalChance(input);
        var criticalMultiplier = isCritical ? 1m + input.CriticalRate : 1m;
        var reducedDamage = Math.Max(1, (int)Math.Round(rawDamage * criticalMultiplier, MidpointRounding.AwayFromZero));
        var reductionRate = Math.Clamp(input.DefenderStatus.DamageReduction, 0, 95) / 100m;
        var damage = Math.Max(1, (int)Math.Round(reducedDamage * (1m - reductionRate), MidpointRounding.AwayFromZero));

        return new BattleDamageResult(damage, isCritical);
    }

    private static double CalculateCriticalChance(BattleDamageInput input)
    {
        var luckAdvantage = Math.Max(0, input.AttackerStatus.Luck - input.DefenderStatus.Luck);
        var normalizedAdvantage = Math.Min(luckAdvantage, BattleConstants.Critical.MaxLuckAdvantageForChance)
            / (double)BattleConstants.Critical.MaxLuckAdvantageForChance;

        return BattleConstants.Critical.MinChance
            + ((BattleConstants.Critical.MaxChance - BattleConstants.Critical.MinChance) * normalizedAdvantage)
            + (double)input.CriticalChanceBonus;
    }

    private static int CalculateIntelligenceDamage(int baseDamage, int attackPower, int defensePower)
    {
        if (attackPower + defensePower <= 0)
        {
            return 1;
        }

        var ratio = (decimal)attackPower / (attackPower + defensePower);
        return Math.Max(1, (int)Math.Round(baseDamage * ratio, MidpointRounding.AwayFromZero));
    }

    private static int ResolveAttackPower(Status attackerStatus, BuffStat attackStat)
    {
        return attackStat switch
        {
            BuffStat.MaxHp => attackerStatus.MaxHp,
            BuffStat.MaxMp => attackerStatus.MaxMp,
            BuffStat.Strength => attackerStatus.Strength,
            BuffStat.Intelligence => attackerStatus.Intelligence,
            BuffStat.Defense => attackerStatus.Defense,
            BuffStat.Luck => attackerStatus.Luck,
            BuffStat.Speed => attackerStatus.Speed,
            BuffStat.Accuracy => attackerStatus.Accuracy,
            BuffStat.StrengthIntelligence => attackerStatus.Strength + attackerStatus.Intelligence,
            _ => throw new ArgumentOutOfRangeException(nameof(attackStat), $"未対応の attackStat: {attackStat}")
        };
    }
}
