namespace server.domain.player;

public interface IPlayerEquipmentRepository
{
    Task<IReadOnlyList<PlayerEquipment>> GetByPlayerAsync(PlayerId playerId);
    Task<IReadOnlyList<PlayerEquipment>> GetEquippedByPlayerAsync(PlayerId playerId);
    Task<PlayerEquipment?> GetAsync(PlayerEquipmentId playerEquipmentId);
    Task SaveAsync(IReadOnlyList<PlayerEquipment> playerEquipments);
    Task DeleteAsync(PlayerEquipmentId playerEquipmentId);
}
