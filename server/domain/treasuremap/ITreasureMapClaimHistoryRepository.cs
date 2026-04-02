using server.domain.player;

namespace server.domain.treasuremap;

public interface ITreasureMapClaimHistoryRepository
{
    Task<bool> ExistsByExpeditionAsync(TreasureMapExpeditionId expeditionId);
    Task<IReadOnlyList<TreasureMapClaimHistory>> ListByPlayerAsync(PlayerId playerId, int take = 50);
    Task AddAsync(TreasureMapClaimHistory history);
}
