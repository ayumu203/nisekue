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

            bonusMaxHp += ApplyMasteryBonus(equipment.BonusValues.MaxHp, masteryRate);
            bonusMaxMp += ApplyMasteryBonus(equipment.BonusValues.MaxMp, masteryRate);
            bonusStrength += ApplyMasteryBonus(equipment.BonusValues.Strength, masteryRate);
            bonusDefense += ApplyMasteryBonus(equipment.BonusValues.Defense, masteryRate);
            bonusIntelligence += ApplyMasteryBonus(equipment.BonusValues.Intelligence, masteryRate);
            bonusLuck += ApplyMasteryBonus(equipment.BonusValues.Luck, masteryRate);
            bonusSpeed += ApplyMasteryBonus(equipment.BonusValues.Speed, masteryRate);
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

    private static int ApplyMasteryBonus(int baseBonus, decimal masteryRate)
    {
        if (baseBonus == 0 || masteryRate <= 0)
        {
            return baseBonus;
        }

        return baseBonus + (int)Math.Floor(baseBonus * masteryRate);
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
