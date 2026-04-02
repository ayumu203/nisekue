using server.domain.player;

namespace server.domain.treasuremap;

public interface ITreasureMapExpeditionRepository
{
    Task<TreasureMapExpedition?> GetAsync(TreasureMapExpeditionId id);
    Task<TreasureMapExpedition?> GetCurrentByPlayerAsync(PlayerId playerId);
    Task<IReadOnlyList<TreasureMapExpedition>> ListByPlayerAsync(PlayerId playerId);
    Task SaveAsync(TreasureMapExpedition expedition);
}
