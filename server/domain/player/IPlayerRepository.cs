namespace server.domain.player;

public interface IPlayerRepository
{
    Task<Player?> GetPlayerAsync(PlayerId id);
    Task SaveAsync(Player player);
}
