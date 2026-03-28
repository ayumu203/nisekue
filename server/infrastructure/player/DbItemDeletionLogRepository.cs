using Microsoft.EntityFrameworkCore;
using server.domain.player;

namespace server.infrastructure.player;

public class DbItemDeletionLogRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IItemDeletionLogRepository
{
    public async Task AddAsync(ItemDeletionLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.ItemDeletionLogs.Add(new ItemDeletionLogEntity
        {
            Id = log.Id,
            PlayerId = log.PlayerId.Value,
            ItemIdentifier = log.ItemIdentifier,
            Quantity = log.Quantity,
            Reason = log.Reason,
            DeletedAt = log.DeletedAt
        });
        await dbContext.SaveChangesAsync();
    }
}
