using server.domain.player;

namespace server.infrastructure.player;

internal static class PlayerItemStackEntityMapper
{
    public static PlayerItemStack MapToDomain(PlayerItemStackEntity entity)
    {
        return new PlayerItemStack(
            new PlayerItemStackId(entity.Id),
            new PlayerId(entity.PlayerId),
            new ItemId(entity.ItemId),
            entity.Quantity,
            entity.UpdatedAt);
    }

    public static void ApplyEntity(PlayerItemStackEntity entity, PlayerItemStack stack)
    {
        entity.Quantity = stack.Quantity;
        entity.UpdatedAt = stack.UpdatedAt;
    }
}
