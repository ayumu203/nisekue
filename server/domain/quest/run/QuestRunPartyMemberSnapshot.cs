using server.domain.battle;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestRunPartyMemberSnapshot(
    QuestParticipantId participantId,
    ParticipantType type,
    string displayName,
    string? imagePath,
    Job job,
    Status baseStatus,
    MoveSet moveSet,
    BattlePosition startPosition,
    ActionMode initialActionMode)
{
    public QuestParticipantId ParticipantId { get; } = participantId;
    public ParticipantType Type { get; } = type;
    public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
        ? throw new ArgumentException("表示名は必須です。", nameof(displayName))
        : displayName.Trim();
    public string? ImagePath { get; } = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
    public Job Job { get; } = job;
    public Status BaseStatus { get; } = baseStatus ?? throw new ArgumentNullException(nameof(baseStatus));
    public MoveSet MoveSet { get; } = moveSet ?? throw new ArgumentNullException(nameof(moveSet));
    public BattlePosition StartPosition { get; } = startPosition;
    public ActionMode InitialActionMode { get; } = initialActionMode;
}
