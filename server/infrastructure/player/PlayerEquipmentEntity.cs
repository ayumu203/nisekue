namespace server.infrastructure.player;

public class PlayerEquipmentEntity
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public int EquipmentId { get; set; }
    public int EquipmentType { get; set; }
    public int EquipmentStatus { get; set; }
    public int Durability { get; set; }
    public int Mastery { get; set; }
    public DateTimeOffset AcquiredAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
