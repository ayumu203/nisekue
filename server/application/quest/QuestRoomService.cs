using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestRoomService(
    IQuestStageRepository questStageRepository,
    IQuestRoomRepository questRoomRepository,
    IQuestRunRepository questRunRepository,
    IPlayerRepository playerRepository,
    QuestNpcAssignmentService questNpcAssignmentService,
    QuestSnapshotFactory questSnapshotFactory,
    QuestRunFactory questRunFactory)
{
    private static readonly TimeSpan QuestCooldown = TimeSpan.FromMinutes(3);

    public async Task<QuestRoom> CreateRoomAsync(PlayerId ownerId, QuestStageId stageId, QuestRoomMode mode)
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
            mode);
        room.AddPlayer(ownerId, player.Name);

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

        room.AddPlayer(playerId, player.Name);
        await questRoomRepository.SaveAsync(room);
        return room;
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
        var players = new List<Player>(activeParticipants.Length);
        foreach (var participant in activeParticipants.Where(x => x.Type == ParticipantType.Player))
        {
            if (participant.PlayerId is null)
            {
                throw new InvalidOperationException($"プレイヤー参加者に playerId がありません。 participantId={participant.Id}");
            }

            var player = await playerRepository.GetPlayerAsync(participant.PlayerId.Value)
                ?? throw new KeyNotFoundException($"プレイヤーが見つかりません。 participantId={participant.Id}");
            players.Add(player);
        }

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

        var snapshots = questSnapshotFactory.Create(activeParticipants, players, snapshotNpcs);
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
