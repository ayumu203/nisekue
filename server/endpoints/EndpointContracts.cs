using server.domain.battle.enums;
using server.domain.player;
using server.domain.quest.enums;

namespace server.endpoints;

public record CreateQuestRoomRequest(int StageId, QuestRoomMode Mode);
public record UpdateQuestRoomPositionRequest(Guid ParticipantId, BattleRow Row, BattleColumn Column);
public record SubmitQuestCommandRequest(Guid ParticipantId, int TurnNo, ActionKind ActionKind, int? MoveId, BattleRow? TargetRow, BattleColumn? TargetColumn);
public record ManualControlRequest(Guid ParticipantId);
public record ManualControlApproveRequest(Guid ParticipantId);
public record PostQuestChatMessageRequest(Guid ParticipantId, string Message);
public record CreatePlayerRequest(string UserName);
public record UpdatePlayerNameRequest(string UserName);
public record UpdatePlayerJobRequest(Job Job);
public record PostChatMessageRequest(Guid OwnerId, string Text);
public record ExecuteTrainingRequest(int EnemyId, IReadOnlyList<int?> MoveIds);
