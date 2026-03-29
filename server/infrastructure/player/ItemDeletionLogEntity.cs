namespace server.infrastructure.player;

public class ItemDeletionLogEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string ItemIdentifier { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset DeletedAt { get; set; }
}
