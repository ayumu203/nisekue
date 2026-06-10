namespace server.infrastructure.pet_battle;

public class PetBattleRoomEntity
{
    public Guid Id { get; set; }
    public Guid OwnerPlayerId { get; set; }
    public Guid OpponentPlayerId { get; set; }
    public int Status { get; set; }
    public int Version { get; set; }
    public int? CloseReason { get; set; }
    public string SlotsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
