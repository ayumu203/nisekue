using server.domain.player;

namespace server.application.player;

public class ItemStatBoostService
{
    public Status Apply(Status currentStatus, Item item, int quantity)
    {
        ArgumentNullException.ThrowIfNull(currentStatus);
        ArgumentNullException.ThrowIfNull(item);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "使用数は1以上である必要があります。");
        }

        var nextStatus = currentStatus;
        for (var i = 0; i < quantity; i++)
        {
            nextStatus = ApplySingle(nextStatus, item);
        }

        return nextStatus;
    }

    private static Status ApplySingle(Status currentStatus, Item item)
    {
        var flatBonus = item.StatusBonus ?? new StatusBonus(0, 0, 0, 0, 0, 0, 0);
        var percentBonus = item.StatusBonusPercent ?? new StatusBonusPercent(0, 0, 0, 0, 0, 0, 0);

        return new Status(
            maxHp: currentStatus.MaxHp + flatBonus.MaxHp + CalculatePercentIncrease(currentStatus.MaxHp, percentBonus.MaxHpPercent),
            maxMp: currentStatus.MaxMp + flatBonus.MaxMp + CalculatePercentIncrease(currentStatus.MaxMp, percentBonus.MaxMpPercent),
            strength: currentStatus.Strength + flatBonus.Strength + CalculatePercentIncrease(currentStatus.Strength, percentBonus.StrengthPercent),
            defense: currentStatus.Defense + flatBonus.Defense + CalculatePercentIncrease(currentStatus.Defense, percentBonus.DefensePercent),
            intelligence: currentStatus.Intelligence + flatBonus.Intelligence + CalculatePercentIncrease(currentStatus.Intelligence, percentBonus.IntelligencePercent),
            luck: currentStatus.Luck + flatBonus.Luck + CalculatePercentIncrease(currentStatus.Luck, percentBonus.LuckPercent),
            speed: currentStatus.Speed + flatBonus.Speed + CalculatePercentIncrease(currentStatus.Speed, percentBonus.SpeedPercent),
            accuracy: currentStatus.Accuracy,
            evasion: currentStatus.Evasion,
            criticalChance: currentStatus.CriticalChance,
            damageReduction: currentStatus.DamageReduction);
    }

    private static int CalculatePercentIncrease(int currentValue, int percent)
    {
        if (percent <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling(currentValue * percent / 100m);
    }
}
