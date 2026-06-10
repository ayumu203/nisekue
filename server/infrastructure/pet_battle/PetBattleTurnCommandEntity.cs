namespace server.infrastructure.pet_battle;

public class PetBattleTurnCommandEntity
{
    public Guid RunId { get; set; }
    public int TurnNo { get; set; }
    public Guid ParticipantId { get; set; }
    public int ActionKind { get; set; }
    public int? MoveId { get; set; }
    public int? TargetRow { get; set; }
    public int? TargetColumn { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public bool IsAutoSubmitted { get; set; }
}
