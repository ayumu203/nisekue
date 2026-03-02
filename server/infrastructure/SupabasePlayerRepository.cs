namespace server.infrastructure;
using server.domain.player;

public class SupabasePlayerRepository : IPlayerRepository
{
    public Task<Player?> GetPlayerAsync(PlayerId id)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(Player player)
    {
        throw new NotImplementedException();
    }
}
