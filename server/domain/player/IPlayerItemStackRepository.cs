namespace server.domain.player;

public interface IPlayerItemStackRepository
{
    Task<IReadOnlyList<PlayerItemStack>> GetByPlayerAsync(PlayerId playerId);
    Task<PlayerItemStack?> GetAsync(PlayerItemStackId id);
    Task SaveAsync(IReadOnlyList<PlayerItemStack> playerItemStacks);
    Task DeleteAsync(PlayerItemStackId id);
}
