using server.domain.player;

namespace server.infrastructure.player;

internal static class PlayerEquipmentEntityMapper
{
    public static PlayerEquipment MapToDomain(PlayerEquipmentEntity entity)
    {
        return new PlayerEquipment(
            new PlayerEquipmentId(entity.Id),
            new PlayerId(entity.PlayerId),
            new EquipmentId(entity.EquipmentId),
            (EquipmentType)entity.EquipmentType,
            (EquipmentStatus)entity.EquipmentStatus,
            entity.Durability,
            entity.Mastery,
            entity.PlusValue,
            entity.AcquiredAt,
            entity.UpdatedAt);
    }

    public static void ApplyEntity(PlayerEquipmentEntity entity, PlayerEquipment playerEquipment)
    {
        entity.PlayerId = playerEquipment.PlayerId.Value;
        entity.EquipmentStatus = (int)playerEquipment.Status;
        entity.Durability = playerEquipment.Durability;
        entity.Mastery = playerEquipment.Mastery;
        entity.PlusValue = playerEquipment.PlusValue;
        entity.UpdatedAt = playerEquipment.UpdatedAt;
    }
}
