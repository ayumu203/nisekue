namespace server.domain.player;

public interface IPlayerRepository
{
    Task<Player?> GetPlayerAsync(PlayerId id);
    Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel);
    Task<IReadOnlyList<Player>> GetPvpOpponentsAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null);
    Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids);
    Task<IReadOnlyList<Player>> GetAllAsync(int? offset = null, int? limit = null);
    Task<bool> UpdateNameAsync(PlayerId id, string name);
    Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown);
    Task SaveAsync(Player player);
}
