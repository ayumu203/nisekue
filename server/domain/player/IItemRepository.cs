namespace server.domain.player;

public interface IItemRepository
{
    Task<Item?> GetAsync(ItemId id);
    Task<IReadOnlyList<Item>> GetAllAsync();
}
