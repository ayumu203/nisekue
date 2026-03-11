using server.domain.battle;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestParticipant(
    QuestParticipantId id,
    ParticipantType type,
    string displayName,
    BattlePosition position,
    DateTimeOffset joinedAt,
    bool isOwner = false,
    PlayerId? playerId = null,
    QuestNpcTemplateId? npcTemplateId = null,
    ParticipantStatus status = ParticipantStatus.Joined,
    DateTimeOffset? lastSeenAt = null,
    DateTimeOffset? leftAt = null)
{
    public QuestParticipantId Id { get; } = id;
    public ParticipantType Type { get; } = type;
    public PlayerId? PlayerId { get; } = ValidatePlayerId(type, playerId, npcTemplateId);
    public QuestNpcTemplateId? NpcTemplateId { get; } = ValidateNpcTemplateId(type, playerId, npcTemplateId);
    public string DisplayName { get; private set; } = ValidateDisplayName(displayName);
    public ParticipantStatus Status { get; private set; } = status;
    public BattlePosition Position { get; private set; } = position;
    public bool IsOwner { get; } = isOwner;
    public DateTimeOffset JoinedAt { get; } = joinedAt;
    public DateTimeOffset? LastSeenAt { get; private set; } = lastSeenAt;
    public DateTimeOffset? LeftAt { get; private set; } = leftAt;

    public void AssignPosition(BattlePosition position)
    {
        EnsureJoined();
        Position = position;
    }

    public void MarkDisconnected(DateTimeOffset at)
    {
        EnsureNotLeft();
        Status = ParticipantStatus.Disconnected;
        LastSeenAt = at;
    }

    public void MarkReconnected(DateTimeOffset at)
    {
        EnsureNotLeft();
        Status = ParticipantStatus.Joined;
        LastSeenAt = at;
    }

    public void Remove(DateTimeOffset at)
    {
        if (IsOwner)
        {
            throw new InvalidOperationException("オーナーはルームから離脱できません。");
        }

        EnsureNotLeft();
        Status = ParticipantStatus.Left;
        LeftAt = at;
    }

    private void EnsureJoined()
    {
        EnsureNotLeft();
    }

    private void EnsureNotLeft()
    {
        if (Status == ParticipantStatus.Left)
        {
            throw new InvalidOperationException("離脱済みの参加者には操作できません。");
        }
    }

    private static string ValidateDisplayName(string displayName)
    {
        var normalized = displayName?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("表示名は必須です。", nameof(displayName));
        }

        return normalized;
    }

    private static PlayerId? ValidatePlayerId(ParticipantType type, PlayerId? playerId, QuestNpcTemplateId? npcTemplateId)
    {
        if (type == ParticipantType.Player)
        {
            return playerId ?? throw new ArgumentException("プレイヤー参加者には playerId が必要です。", nameof(playerId));
        }

        if (npcTemplateId is null)
        {
            throw new ArgumentException("NPC 参加者には npcTemplateId が必要です。", nameof(npcTemplateId));
        }

        return null;
    }

    private static QuestNpcTemplateId? ValidateNpcTemplateId(ParticipantType type, PlayerId? playerId, QuestNpcTemplateId? npcTemplateId)
    {
        if (type == ParticipantType.Npc)
        {
            return npcTemplateId ?? throw new ArgumentException("NPC 参加者には npcTemplateId が必要です。", nameof(npcTemplateId));
        }

        if (playerId is null)
        {
            throw new ArgumentException("プレイヤー参加者には playerId が必要です。", nameof(playerId));
        }

        return null;
    }
}
