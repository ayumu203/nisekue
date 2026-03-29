namespace server.domain.player;

public class ItemDeletionLog(
    Guid id,
    PlayerId playerId,
    string itemIdentifier,
    int quantity,
    string reason,
    DateTimeOffset deletedAt)
{
    public Guid Id { get; } = id;
    public PlayerId PlayerId { get; } = playerId;
    public string ItemIdentifier { get; } = string.IsNullOrWhiteSpace(itemIdentifier) ? throw new ArgumentException("itemIdentifier は必須です。", nameof(itemIdentifier)) : itemIdentifier.Trim();
    public int Quantity { get; } = quantity > 0 ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity), "1以上である必要があります。");
    public string Reason { get; } = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("reason は必須です。", nameof(reason)) : reason.Trim();
    public DateTimeOffset DeletedAt { get; } = deletedAt;
}
