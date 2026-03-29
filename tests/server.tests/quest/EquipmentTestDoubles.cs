using server.domain.player;

namespace server.tests.quest;

internal sealed class FakePlayerEquipmentRepository(params PlayerEquipment[] equipments) : IPlayerEquipmentRepository
{
    private readonly Dictionary<PlayerId, IReadOnlyList<PlayerEquipment>> equipmentsByPlayerId = equipments
        .GroupBy(x => x.PlayerId)
        .ToDictionary(x => x.Key, x => (IReadOnlyList<PlayerEquipment>)x.ToArray());

    public Task<IReadOnlyList<PlayerEquipment>> GetByPlayerAsync(PlayerId playerId)
    {
        return Task.FromResult(equipmentsByPlayerId.TryGetValue(playerId, out var playerEquipments)
            ? playerEquipments
            : (IReadOnlyList<PlayerEquipment>)[]);
    }

    public Task<PlayerEquipment?> GetAsync(PlayerEquipmentId playerEquipmentId)
    {
        var equipment = equipmentsByPlayerId.Values
            .SelectMany(x => x)
            .FirstOrDefault(x => x.Id == playerEquipmentId);
        return Task.FromResult(equipment);
    }

    public Task SaveAsync(IReadOnlyList<PlayerEquipment> playerEquipments)
    {
        foreach (var group in playerEquipments.GroupBy(x => x.PlayerId))
        {
            equipmentsByPlayerId[group.Key] = group.ToArray();
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(PlayerEquipmentId playerEquipmentId)
    {
        foreach (var group in equipmentsByPlayerId.ToArray())
        {
            var filtered = group.Value.Where(x => x.Id != playerEquipmentId).ToArray();
            if (filtered.Length != group.Value.Count)
            {
                equipmentsByPlayerId[group.Key] = filtered;
                break;
            }
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeEquipmentRepository(params Equipment[] equipments) : IEquipmentRepository
{
    private readonly IReadOnlyList<Equipment> storedEquipments = equipments;

    public Task<Equipment?> GetAsync(EquipmentId id)
    {
        return Task.FromResult(storedEquipments.FirstOrDefault(x => x.Id == id));
    }

    public Task<IReadOnlyList<Equipment>> GetAllAsync()
    {
        return Task.FromResult(storedEquipments);
    }
}
