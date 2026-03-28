namespace server.domain.player;

public interface IItemDeletionLogRepository
{
    Task AddAsync(ItemDeletionLog log);
}
