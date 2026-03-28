using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbMarketTradeHistoryRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IMarketTradeHistoryRepository
{
    public async Task AddAsync(MarketTradeHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.MarketTradeHistories.Add(new MarketTradeHistoryEntity
        {
            Id = history.Id,
            SellerId = history.SellerId.Value,
            BuyerId = history.BuyerId.Value,
            ItemIdentifier = history.ItemIdentifier,
            Quantity = history.Quantity,
            UnitPrice = history.UnitPrice,
            PurchasedAt = history.PurchasedAt
        });
        await dbContext.SaveChangesAsync();
    }
}
