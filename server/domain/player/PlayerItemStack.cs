namespace server.domain.player;

public class PlayerItemStack(
    PlayerItemStackId id,
    PlayerId playerId,
    ItemId itemId,
    int quantity,
    DateTimeOffset updatedAt)
{
    public PlayerItemStackId Id { get; } = id;
    public PlayerId PlayerId { get; } = playerId;
    public ItemId ItemId { get; } = itemId;
    public int Quantity { get; private set; } = ValidatePositive(quantity, nameof(quantity));
    public DateTimeOffset UpdatedAt { get; private set; } = updatedAt;

    public void AddQuantity(int value, int maxStack, DateTimeOffset now)
    {
        if (maxStack < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxStack), "スタック上限は1以上である必要があります。");
        }

        Quantity = Math.Min(maxStack, checked(Quantity + ValidatePositive(value, nameof(value))));
        UpdatedAt = now;
    }

    public void ConsumeQuantity(int value, DateTimeOffset now)
    {
        var validated = ValidatePositive(value, nameof(value));
        if (validated > Quantity)
        {
            throw new InvalidOperationException("所持数を超えて消費できません。");
        }

        Quantity -= validated;
        UpdatedAt = now;
    }

    private static int ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1以上である必要があります。");
        }

        return value;
    }
}
