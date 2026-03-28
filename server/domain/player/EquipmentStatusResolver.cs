namespace server.domain.player;

public class EquipmentStatusResolver
{
    public Status BuildEffectiveStatus(
        Status baseStatus,
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

            bonusMaxHp += equipment.BonusValues.MaxHp;
            bonusMaxMp += equipment.BonusValues.MaxMp;
            bonusStrength += equipment.BonusValues.Strength;
            bonusDefense += equipment.BonusValues.Defense;
            bonusIntelligence += equipment.BonusValues.Intelligence;
            bonusLuck += equipment.BonusValues.Luck;
            bonusSpeed += equipment.BonusValues.Speed;
        }

        return new Status(
            baseStatus.MaxHp + bonusMaxHp,
            baseStatus.MaxMp + bonusMaxMp,
            baseStatus.Strength + bonusStrength,
            baseStatus.Defense + bonusDefense,
            baseStatus.Intelligence + bonusIntelligence,
            baseStatus.Luck + bonusLuck,
            baseStatus.Speed + bonusSpeed);
    }
}
