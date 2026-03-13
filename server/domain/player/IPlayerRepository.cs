namespace server.domain.player;

public interface IPlayerRepository
{
    Task<Player?> GetPlayerAsync(PlayerId id);
    Task<IReadOnlyList<Player>> GetAllAsync();
    Task<bool> UpdateNameAsync(PlayerId id, string name);
    Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown);
    Task SaveAsync(Player player);
}
