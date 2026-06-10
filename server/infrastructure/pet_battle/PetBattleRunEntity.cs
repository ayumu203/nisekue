namespace server.infrastructure.pet_battle;

public class PetBattleRunEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Guid OwnerPlayerId { get; set; }
    public Guid OpponentPlayerId { get; set; }
    public int Status { get; set; }
    public int CurrentTurnNo { get; set; }
    public DateTimeOffset ActionDeadlineAt { get; set; }
    public int? LastResolvedTurnNo { get; set; }
    public string OwnerSnapshotsJson { get; set; } = "[]";
    public string OpponentSnapshotsJson { get; set; } = "[]";
    public string OwnerMembersJson { get; set; } = "[]";
    public string OpponentMembersJson { get; set; } = "[]";
    public string? LastTurnResultsJson { get; set; }
    public Guid? WinnerPlayerId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
