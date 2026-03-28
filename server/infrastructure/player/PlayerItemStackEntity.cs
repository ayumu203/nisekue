namespace server.infrastructure.player;

public class PlayerItemStackEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
