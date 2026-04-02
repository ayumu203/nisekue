namespace server.domain.treasuremap;

public interface ITreasureMapRewardPoolRepository
{
    Task<TreasureMapRewardPool?> GetAsync(TreasureMapRewardPoolId id);
    Task<IReadOnlyList<TreasureMapRewardPool>> GetAllAsync();
}
