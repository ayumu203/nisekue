namespace server.infrastructure.quest.room;

public class QuestRoomParticipantEntity
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public int ParticipantType { get; set; }
    public Guid? PlayerId { get; set; }
    public int? NpcTemplateId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int BattleRow { get; set; }
    public int BattleColumn { get; set; }
    public int ParticipantStatus { get; set; }
    public bool IsOwner { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
}
