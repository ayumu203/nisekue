namespace server.domain.player;

public interface IEquipmentRepository
{
    Task<Equipment?> GetAsync(EquipmentId id);
    Task<IReadOnlyList<Equipment>> GetAllAsync();
}
