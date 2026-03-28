namespace server.domain.player;

public interface IMarketTradeHistoryRepository
{
    Task AddAsync(MarketTradeHistory history);
}
