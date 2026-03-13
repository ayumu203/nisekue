using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestRoom(
    QuestRoomId id,
    PlayerId ownerId,
    QuestStageId stageId,
    QuestRoomMode mode,
    FormationLayout? formation = null,
    IEnumerable<QuestParticipant>? participants = null,
    QuestRoomStatus status = QuestRoomStatus.Recruiting,
    int version = 0,
    QuestRoomCloseReason? closeReason = null,
    DateTimeOffset? createdAt = null,
    DateTimeOffset? closedAt = null)
{
    private readonly List<QuestParticipant> participants = participants?.ToList() ?? [];

    public QuestRoomId Id { get; } = id;
    public PlayerId OwnerId { get; } = ownerId;
    public QuestStageId StageId { get; } = stageId;
    public QuestRoomMode Mode { get; } = mode;
    public QuestRoomStatus Status { get; private set; } = status;
    public int Version { get; private set; } = ValidateVersion(version);
    public FormationLayout Formation { get; private set; } = formation ?? new FormationLayout();
    public IReadOnlyList<QuestParticipant> Participants => participants;
    public QuestRoomCloseReason? CloseReason { get; private set; } = closeReason;
    public DateTimeOffset CreatedAt { get; } = createdAt ?? DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; private set; } = closedAt;

    public void AddPlayer(PlayerId playerId, string displayName)
    {
        EnsureRecruiting();
        EnsureCapacityAvailable();
        if (participants.Any(x => x.PlayerId == playerId && x.Status != ParticipantStatus.Left))
        {
            throw new InvalidOperationException("同じプレイヤーは同一ルームに重複参加できません。");
        }

        var position = Formation.FindFirstEmpty();
        var participant = new QuestParticipant(
            QuestParticipantId.New(),
            ParticipantType.Player,
            displayName,
            position,
            DateTimeOffset.UtcNow,
            isOwner: playerId == OwnerId,
            playerId: playerId);

        Formation.Assign(participant.Id, position);
        participants.Add(participant);
        EnsureSingleOwner();
    }

    public void AssignPosition(QuestParticipantId participantId, server.domain.battle.BattlePosition position)
    {
        EnsureRecruiting();
        var participant = FindParticipant(participantId);
        if (participant.Status == ParticipantStatus.Left)
        {
            throw new InvalidOperationException("離脱済み参加者は再配置できません。");
        }

        if (participant.Position == position)
        {
            return;
        }

        var other = participants.FirstOrDefault(x => x.Id != participantId && x.Status != ParticipantStatus.Left && x.Position == position);
        if (other is not null)
        {
            throw new InvalidOperationException("指定されたマスはすでに使用中です。");
        }

        Formation.Release(participant.Position);
        participant.AssignPosition(position);
        Formation.Assign(participant.Id, position);
    }

    public void MarkDisconnected(QuestParticipantId participantId, DateTimeOffset at)
    {
        EnsureRecruiting();
        FindParticipant(participantId).MarkDisconnected(at);
    }

    public void MarkReconnected(QuestParticipantId participantId, DateTimeOffset at)
    {
        EnsureRecruiting();
        FindParticipant(participantId).MarkReconnected(at);
    }

    public void RemoveParticipant(QuestParticipantId participantId, DateTimeOffset at)
    {
        EnsureRecruiting();
        var participant = FindParticipant(participantId);
        Formation.Release(participant.Position);
        participant.Remove(at);
        EnsureSingleOwner();
    }

    public bool CanStart()
    {
        if (Status != QuestRoomStatus.Recruiting)
        {
            return false;
        }

        if (participants.Count(x => x.Status != ParticipantStatus.Left) == 0)
        {
            return false;
        }

        if (participants.Count(x => x.IsOwner && x.Status != ParticipantStatus.Left) != 1)
        {
            return false;
        }

        var joinedHumanCount = participants.Count(x => x.Type == ParticipantType.Player && x.Status != ParticipantStatus.Left);
        return Mode switch
        {
            QuestRoomMode.Solo => joinedHumanCount >= 1,
            QuestRoomMode.Multi => joinedHumanCount >= 2,
            _ => false
        };
    }

    public void AddNpcParticipants(IEnumerable<QuestNpcTemplate> npcTemplates)
    {
        EnsureRecruiting();
        ArgumentNullException.ThrowIfNull(npcTemplates);

        foreach (var template in npcTemplates)
        {
            EnsureCapacityAvailable();
            var position = Formation.FindFirstEmpty(template.PreferredRow);
            var participant = new QuestParticipant(
                QuestParticipantId.New(),
                ParticipantType.Npc,
                template.Name,
                position,
                DateTimeOffset.UtcNow,
                npcTemplateId: template.Id);

            Formation.Assign(participant.Id, position);
            participants.Add(participant);
        }
    }

    public void CloseRecruitment(QuestRoomCloseReason reason, DateTimeOffset at)
    {
        EnsureRecruiting();
        Status = QuestRoomStatus.Closed;
        CloseReason = reason;
        ClosedAt = at;
    }

    public void CancelForOwnerRoomReplacement(DateTimeOffset at)
    {
        CloseRecruitment(QuestRoomCloseReason.Cancelled, at);
    }

    public void CancelByOwner(DateTimeOffset at)
    {
        CloseRecruitment(QuestRoomCloseReason.Cancelled, at);
    }

    public void SyncVersion(int version)
    {
        Version = ValidateVersion(version);
    }

    private QuestParticipant FindParticipant(QuestParticipantId participantId)
    {
        return participants.FirstOrDefault(x => x.Id == participantId)
            ?? throw new KeyNotFoundException("参加者が見つかりません。");
    }

    private void EnsureRecruiting()
    {
        if (Status != QuestRoomStatus.Recruiting)
        {
            throw new InvalidOperationException("募集中のルームでのみ実行できます。");
        }
    }

    private void EnsureSingleOwner()
    {
        var owners = participants.Count(x => x.IsOwner && x.Status != ParticipantStatus.Left);
        if (owners != 1)
        {
            throw new InvalidOperationException("ルームのオーナーは常に1人である必要があります。");
        }
    }

    private void EnsureCapacityAvailable()
    {
        var activeCount = participants.Count(x => x.Status != ParticipantStatus.Left);
        if (activeCount >= 6)
        {
            throw new InvalidOperationException("ルームの参加上限は6人です。");
        }
    }

    private static int ValidateVersion(int version)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "バージョンは0以上である必要があります。");
        }

        return version;
    }
}
