using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestRoomSearchCondition(
    QuestStageId? stageId = null,
    QuestRoomMode? mode = null,
    QuestRoomStatus? status = null,
    PlayerId? ownerPlayerId = null,
    int page = 1,
    int pageSize = 20)
{
    public QuestStageId? StageId { get; } = stageId;
    public QuestRoomMode? Mode { get; } = mode;
    public QuestRoomStatus? Status { get; } = status;
    public PlayerId? OwnerPlayerId { get; } = ownerPlayerId;
    public int Page { get; } = page < 1 ? 1 : page;
    public int PageSize { get; } = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
}
