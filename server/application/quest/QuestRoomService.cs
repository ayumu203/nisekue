using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestRoomService(
    IQuestStageRepository questStageRepository,
    IQuestRoomRepository questRoomRepository,
    IQuestRunRepository questRunRepository,
    IPlayerRepository playerRepository,
    IPlayerEquipmentRepository playerEquipmentRepository,
    IEquipmentRepository equipmentRepository,
    QuestNpcAssignmentService questNpcAssignmentService,
    QuestSnapshotFactory questSnapshotFactory,
    QuestRunFactory questRunFactory)
{
    private static readonly TimeSpan QuestCooldown = TimeSpan.FromMinutes(3);

    public async Task<QuestRoom> CreateRoomAsync(
        PlayerId ownerId,
        QuestStageId stageId,
        QuestRoomMode mode,
        int? minRequiredLevel = null,
        IReadOnlyList<PlayerId>? allowedPlayerIds = null)
    {
        var player = await playerRepository.GetPlayerAsync(ownerId)
            ?? throw new KeyNotFoundException("オーナープレイヤーが見つかりません。");
        EnsureQuestCooldownExpired(player, DateTimeOffset.UtcNow);
        var stage = await questStageRepository.GetAsync(stageId)
            ?? throw new KeyNotFoundException("ステージが見つかりません。");
        if (!stage.IsActive)
        {
            throw new InvalidOperationException("無効化されたステージではルームを作成できません。");
        }

        if (await questRunRepository.ExistsActiveRunByPlayerAsync(ownerId))
        {
            throw new InvalidOperationException("進行中クエストに参加しているためルームを作成できません。");
        }

        var existingRecruitingRoom = await questRoomRepository.GetRecruitingByOwnerAsync(ownerId);
        if (existingRecruitingRoom is not null)
        {
            existingRecruitingRoom.CancelForOwnerRoomReplacement(DateTimeOffset.UtcNow);
            await questRoomRepository.SaveAsync(existingRecruitingRoom);
        }

        var room = new QuestRoom(
            QuestRoomId.New(),
            ownerId,
            stageId,
            mode,
            joinPolicy: new QuestRoomJoinPolicy(minRequiredLevel, EnsureOwnerIncluded(ownerId, allowedPlayerIds)));
        room.AddPlayer(ownerId, player.Name, player.Level);

        await questRoomRepository.SaveAsync(room);
        return room;
    }

    public async Task<QuestRoom> JoinRoomAsync(QuestRoomId roomId, PlayerId playerId)
    {
        var room = await questRoomRepository.GetAsync(roomId)
            ?? throw new KeyNotFoundException("ルームが見つかりません。");
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");
        EnsureQuestCooldownExpired(player, DateTimeOffset.UtcNow);
        if (await questRunRepository.ExistsActiveRunByPlayerAsync(playerId))
        {
            throw new InvalidOperationException("進行中クエストに参加しているためルームに参加できません。");
        }

        room.AddPlayer(playerId, player.Name, player.Level);
        await questRoomRepository.SaveAsync(room);
        return room;
    }

    public async Task<QuestRoom> CancelRoomAsync(QuestRoomId roomId, PlayerId ownerId)
    {
        var room = await questRoomRepository.GetAsync(roomId)
            ?? throw new KeyNotFoundException("ルームが見つかりません。");
        if (room.OwnerId != ownerId)
        {
            throw new InvalidOperationException("ルームのオーナーのみ募集をキャンセルできます。");
        }

        room.CancelByOwner(DateTimeOffset.UtcNow);
        await questRoomRepository.SaveAsync(room);
        return room;
    }

    public async Task<QuestRoom> UpdateRestrictionsAsync(
        QuestRoomId roomId,
        PlayerId ownerId,
        int? minRequiredLevel,
        IReadOnlyList<PlayerId>? allowedPlayerIds)
    {
        var room = await questRoomRepository.GetAsync(roomId)
            ?? throw new KeyNotFoundException("ルームが見つかりません。");
        if (room.OwnerId != ownerId)
        {
            throw new InvalidOperationException("ルームのオーナーのみ参加制限を更新できます。");
        }

        await EnsureActiveParticipantsMatchJoinPolicyAsync(room, minRequiredLevel, allowedPlayerIds);
        room.UpdateJoinPolicy(minRequiredLevel, EnsureOwnerIncluded(ownerId, allowedPlayerIds));
        await questRoomRepository.SaveAsync(room);
        return room;
    }

    public async Task<Player> GetViewerAsync(PlayerId playerId)
    {
        return await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");
    }

    public Task<bool> ViewerHasActiveRunAsync(PlayerId playerId) => questRunRepository.ExistsActiveRunByPlayerAsync(playerId);

    private async Task EnsureActiveParticipantsMatchJoinPolicyAsync(
        QuestRoom room,
        int? minRequiredLevel,
        IReadOnlyList<PlayerId>? allowedPlayerIds)
    {
        var nextPolicy = new QuestRoomJoinPolicy(minRequiredLevel, EnsureOwnerIncluded(room.OwnerId, allowedPlayerIds));
        var activePlayerIds = room.Participants
            .Where(x => x.Type == ParticipantType.Player && x.Status != ParticipantStatus.Left)
            .Select(x => x.PlayerId)
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

        foreach (var playerId in activePlayerIds)
        {
            var player = await playerRepository.GetPlayerAsync(playerId)
                ?? throw new KeyNotFoundException($"プレイヤーが見つかりません。 playerId={playerId.Value}");
            var joinDeniedReason = nextPolicy.GetJoinDeniedReason(player.Id, player.Level);
            if (joinDeniedReason == "LevelRequirementNotMet")
            {
                throw new InvalidOperationException("現在の参加者に参加可能レベルを満たさないプレイヤーが含まれるため更新できません。");
            }

            if (joinDeniedReason == "NotAllowedPlayer")
            {
                throw new InvalidOperationException("現在の参加者に参加対象外プレイヤーが含まれるため更新できません。");
            }
        }
    }

    private static IReadOnlyList<PlayerId> EnsureOwnerIncluded(PlayerId ownerId, IReadOnlyList<PlayerId>? allowedPlayerIds)
    {
        if (allowedPlayerIds is null || allowedPlayerIds.Count == 0)
        {
            return [];
        }

        return allowedPlayerIds
            .Append(ownerId)
            .Distinct()
            .ToArray();
    }

    public async Task<QuestRun> StartAsync(QuestRoomId roomId)
    {
        var room = await questRoomRepository.GetAsync(roomId)
            ?? throw new KeyNotFoundException("ルームが見つかりません。");
        if (!room.CanStart())
        {
            throw new InvalidOperationException("開始条件を満たしていないためクエストを開始できません。");
        }

        var existingRun = await questRunRepository.GetByRoomIdAsync(roomId);
        if (existingRun is not null)
        {
            throw new InvalidOperationException("このルームでは既にクエストが開始されています。");
        }

        var stage = await questStageRepository.GetAsync(room.StageId)
            ?? throw new KeyNotFoundException("ステージが見つかりません。");
        var npcTemplates = await questNpcAssignmentService.AssignForStart(room, stage.MinPartyMemberCount);
        room.AddNpcParticipants(npcTemplates);
        room.CloseRecruitment(QuestRoomCloseReason.Started, DateTimeOffset.UtcNow);

        var activeParticipants = room.Participants.Where(x => x.Status != ParticipantStatus.Left).ToArray();
        var playerParticipants = activeParticipants.Where(x => x.Type == ParticipantType.Player).ToArray();
        var playerTasks = playerParticipants.Select(async participant =>
        {
            if (participant.PlayerId is null)
            {
                throw new InvalidOperationException($"プレイヤー参加者に playerId がありません。 participantId={participant.Id}");
            }

            return await playerRepository.GetPlayerAsync(participant.PlayerId.Value)
                ?? throw new KeyNotFoundException($"プレイヤーが見つかりません。 participantId={participant.Id}");
        });
        var players = await Task.WhenAll(playerTasks);

        var snapshotNpcs = npcTemplates.Count == 0
            ? []
            : room.Participants
                .Where(x => x.Type == ParticipantType.Npc && x.NpcTemplateId is not null)
                .Select(x =>
                {
                    var template = npcTemplates.FirstOrDefault(t => t.Id == x.NpcTemplateId!.Value);
                    return template ?? throw new KeyNotFoundException($"NPC テンプレートが見つかりません。 participantId={x.Id}");
                })
                .ToArray();

        var playerEquipmentTasks = players.Select(player => playerEquipmentRepository.GetByPlayerAsync(player.Id));
        var playerEquipments = (await Task.WhenAll(playerEquipmentTasks))
            .SelectMany(x => x)
            .ToList();

        var equipments = await equipmentRepository.GetAllAsync();
        var snapshots = questSnapshotFactory.Create(activeParticipants, players, snapshotNpcs, playerEquipments, equipments);
        var run = await questRunFactory.Create(room, stage, snapshots, DateTimeOffset.UtcNow);

        await questRoomRepository.SaveAsync(room);
        await questRunRepository.SaveAsync(run);
        return run;
    }

    private static void EnsureQuestCooldownExpired(Player player, DateTimeOffset now)
    {
        if (player.QuestCooldownUntil is not null && player.QuestCooldownUntil.Value > now)
        {
            throw new InvalidOperationException($"クエスト終了後{(int)QuestCooldown.TotalMinutes}分間は再参加できません。");
        }
    }
}
