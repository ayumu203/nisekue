namespace server.domain.treasuremap;

public interface ITreasureMapRepository
{
    Task<TreasureMap?> GetAsync(TreasureMapId id);
    Task<TreasureMap?> GetByCodeAsync(string code);
    Task<IReadOnlyList<TreasureMap>> GetAllAsync();
}
