using server.domain.battle.enums;
using server.domain.player;
using server.domain.quest.enums;

namespace server.endpoints;

public record CreateQuestRoomRequest(int StageId, QuestRoomMode Mode, int? MinRequiredLevel, IReadOnlyList<Guid>? AllowedPlayerIds);
public record UpdateQuestRoomRestrictionsRequest(int? MinRequiredLevel, IReadOnlyList<Guid>? AllowedPlayerIds);
public record UpdateQuestRoomPositionRequest(Guid ParticipantId, BattleRow Row, BattleColumn Column);
public record SubmitQuestCommandRequest(Guid ParticipantId, int TurnNo, ActionKind ActionKind, int? MoveId, BattleRow? TargetRow, BattleColumn? TargetColumn);
public record ManualControlRequest(Guid ParticipantId);
public record ManualControlApproveRequest(Guid ParticipantId);
public record PostQuestChatMessageRequest(Guid ParticipantId, string Message);
public record CreatePlayerRequest(string UserName);
public record UpdatePlayerNameRequest(string UserName);
public record UpdatePlayerImageRequest(int ImageNo);
public record UpdatePlayerJobRequest(Job Job);
public record UpdatePlayerEquipmentRequest(EquipmentType EquipmentType, Guid? PlayerEquipmentId);
public record PostChatMessageRequest(Guid OwnerId, string Text);
public record CreateThreadRequest(string Title, string Body);
public record CreateThreadReplyRequest(string Body);
public record ExecuteTrainingRequest(int EnemyId, IReadOnlyList<int?> MoveIds);
public record UseItemRequest(int Quantity);
public record CreateMarketListingRequest(Guid? PlayerEquipmentId, Guid? ItemStackId, int Quantity, int UnitPrice);
public record PurchaseMarketListingRequest(int Quantity);
