namespace server.domain.player;

public class EquipmentStatusResolver
{

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

            // 熟練度は隠しステータスとして保持するのみで、ステータス計算には用いない。
            bonusMaxHp += ApplyBonuses(equipment.BonusValues.MaxHp, playerEquipment.PlusValue);
            bonusMaxMp += ApplyBonuses(equipment.BonusValues.MaxMp, playerEquipment.PlusValue);
            bonusStrength += ApplyBonuses(equipment.BonusValues.Strength, playerEquipment.PlusValue);
            bonusDefense += ApplyBonuses(equipment.BonusValues.Defense, playerEquipment.PlusValue);
            bonusIntelligence += ApplyBonuses(equipment.BonusValues.Intelligence, playerEquipment.PlusValue);
            bonusLuck += ApplyBonuses(equipment.BonusValues.Luck, playerEquipment.PlusValue);
            bonusSpeed += ApplyBonuses(equipment.BonusValues.Speed, playerEquipment.PlusValue);
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

    private static int ApplyBonuses(int baseBonus, int plusValue)
    {
        if (baseBonus == 0)
        {
            return 0;
        }

        return baseBonus * (100 + plusValue) / 100;
    }
}
