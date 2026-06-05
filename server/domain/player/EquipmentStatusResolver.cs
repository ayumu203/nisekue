namespace server.domain.player;

public class EquipmentStatusResolver
{
    private const int MasteryStep = 10;

    public Status BuildEffectiveStatus(
        Status baseStatus,
        Job playerJob,
        IEnumerable<PlayerEquipment> playerEquipments,
        IEnumerable<Equipment> equipments)
    {
        ArgumentNullException.ThrowIfNull(baseStatus);
        ArgumentNullException.ThrowIfNull(playerEquipments);
        ArgumentNullException.ThrowIfNull(equipments);

        var equipmentById = equipments.ToDictionary(x => x.Id);
        var bonusMaxHp = 0;
        var bonusMaxMp = 0;
        var bonusStrength = 0;
        var bonusDefense = 0;
        var bonusIntelligence = 0;
        var bonusLuck = 0;
        var bonusSpeed = 0;

        foreach (var playerEquipment in playerEquipments.Where(x => x.Status == EquipmentStatus.Equipped && !x.IsBroken))
        {
            if (!equipmentById.TryGetValue(playerEquipment.EquipmentId, out var equipment))
            {
                continue;
            }

            if (!equipment.CanEquip(playerJob))
            {
                continue;
            }

            var masteryRate = equipment.Type == EquipmentType.Weapon
                ? ResolveMasteryRate(playerEquipment.Mastery)
                : 0m;

            bonusMaxHp += ApplyBonuses(equipment.BonusValues.MaxHp, masteryRate, playerEquipment.PlusValue);
            bonusMaxMp += ApplyBonuses(equipment.BonusValues.MaxMp, masteryRate, playerEquipment.PlusValue);
            bonusStrength += ApplyBonuses(equipment.BonusValues.Strength, masteryRate, playerEquipment.PlusValue);
            bonusDefense += ApplyBonuses(equipment.BonusValues.Defense, masteryRate, playerEquipment.PlusValue);
            bonusIntelligence += ApplyBonuses(equipment.BonusValues.Intelligence, masteryRate, playerEquipment.PlusValue);
            bonusLuck += ApplyBonuses(equipment.BonusValues.Luck, masteryRate, playerEquipment.PlusValue);
            bonusSpeed += ApplyBonuses(equipment.BonusValues.Speed, masteryRate, playerEquipment.PlusValue);
        }

        return new Status(
            baseStatus.MaxHp + bonusMaxHp,
            baseStatus.MaxMp + bonusMaxMp,
            baseStatus.Strength + bonusStrength,
            baseStatus.Defense + bonusDefense,
            baseStatus.Intelligence + bonusIntelligence,
            baseStatus.Luck + bonusLuck,
            baseStatus.Speed + bonusSpeed,
            baseStatus.Accuracy,
            baseStatus.Evasion,
            baseStatus.CriticalChance,
            baseStatus.DamageReduction);
    }

    private static int ApplyBonuses(int baseBonus, decimal masteryRate, int plusValue)
    {
        if (baseBonus == 0)
        {
            return 0;
        }

        var plusApplied = baseBonus * (100 + plusValue) / 100;

        if (masteryRate <= 0)
        {
            return plusApplied;
        }

        return plusApplied + (int)Math.Floor(plusApplied * masteryRate);
    }

    private static decimal ResolveMasteryRate(int mastery)
    {
        if (mastery < MasteryStep)
        {
            return 0m;
        }

        var masteryTier = mastery / MasteryStep;
        return masteryTier / 100m;
    }
}
