using FluentAssertions;
using server.application.quest;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class QuestRoomServiceTests
{
    [Fact]
    public async Task CreateRoomAsync_WithActiveStage_CreatesOwnerRoomAndSaves()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);

        room.OwnerId.Should().Be(owner.Id);
        room.StageId.Should().Be(stage.Id);
        room.Participants.Should().ContainSingle();
        room.Participants[0].PlayerId.Should().Be(owner.Id);
        roomRepository.SavedRooms.Should().ContainSingle(x => x.Id == room.Id);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenOwnerHasRecruitingRoom_CancelsOldRoomBeforeCreatingNewRoom()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();
        var existingRoom = new QuestRoom(QuestRoomId.New(), owner.Id, stage.Id, QuestRoomMode.Solo);
        existingRoom.AddPlayer(owner.Id, owner.Name);
        await roomRepository.SaveAsync(existingRoom);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var newRoom = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Multi);

        existingRoom.Status.Should().Be(QuestRoomStatus.Closed);
        existingRoom.CloseReason.Should().Be(QuestRoomCloseReason.Cancelled);
        newRoom.Id.Should().NotBe(existingRoom.Id);
        roomRepository.SavedRooms.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenOwnerIsInActiveRun_ThrowsInvalidOperationException()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            new FakeQuestRoomRepository(),
            new FakeQuestRunRepository { HasActiveRunForAnyPlayer = true },
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var act = () => service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("進行中クエストに参加しているためルームを作成できません。");
    }

    [Fact]
    public async Task CreateRoomAsync_WhenOwnerIsInQuestCooldown_ThrowsInvalidOperationException()
    {
        var owner = CreatePlayer("Owner");
        owner.SetQuestCooldownUntil(DateTimeOffset.UtcNow.AddMinutes(3));
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            new FakeQuestRoomRepository(),
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var act = () => service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("クエスト終了後1分間は再参加できません。");
    }

    [Fact]
    public async Task JoinRoomAsync_WhenPlayerIsInQuestCooldown_ThrowsInvalidOperationException()
    {
        var owner = CreatePlayer("Owner");
        var guest = CreatePlayer("Guest");
        guest.SetQuestCooldownUntil(DateTimeOffset.UtcNow.AddMinutes(3));
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();
        var room = new QuestRoom(QuestRoomId.New(), owner.Id, stage.Id, QuestRoomMode.Multi);
        room.AddPlayer(owner.Id, owner.Name);
        await roomRepository.SaveAsync(room);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner, guest),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var act = () => service.JoinRoomAsync(room.Id, guest.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("クエスト終了後1分間は再参加できません。");
    }

    [Fact]
    public async Task CancelRoomAsync_WhenOwnerMatches_ClosesRoomAsCancelled()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();
        var room = new QuestRoom(QuestRoomId.New(), owner.Id, stage.Id, QuestRoomMode.Solo);
        room.AddPlayer(owner.Id, owner.Name);
        await roomRepository.SaveAsync(room);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var cancelledRoom = await service.CancelRoomAsync(room.Id, owner.Id);

        cancelledRoom.Status.Should().Be(QuestRoomStatus.Closed);
        cancelledRoom.CloseReason.Should().Be(QuestRoomCloseReason.Cancelled);
    }

    [Fact]
    public async Task StartAsync_WhenPartyBelowMinimum_AddsNpcAndCreatesRun()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateStage(minPartyMemberCount: 2, maxPartyMemberCount: 6, isActive: true);
        var enemyDefinition = CreateEnemyDefinition();
        var npcTemplate = CreateNpcTemplate();
        var roomRepository = new FakeQuestRoomRepository();
        var runRepository = new FakeQuestRunRepository();

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            runRepository,
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([npcTemplate]),
            new FakeQuestEnemyDefinitionRepository(enemyDefinition));

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);

        var run = await service.StartAsync(room.Id);

        room.Status.Should().Be(QuestRoomStatus.Closed);
        room.Participants.Should().HaveCount(2);
        room.Participants.Should().Contain(x => x.Type == ParticipantType.Npc);
        run.RoomId.Should().Be(room.Id);
        run.PartySnapshots.Should().HaveCount(2);
        run.BattleState.Enemies.Should().ContainSingle();
        runRepository.SavedRuns.Should().ContainSingle(x => x.Id == run.Id);
    }

    private static QuestRoomService CreateRoomService(
        IQuestStageRepository stageRepository,
        IQuestRoomRepository roomRepository,
        IQuestRunRepository runRepository,
        IPlayerRepository playerRepository,
        IQuestNpcTemplateRepository npcTemplateRepository,
        IQuestEnemyDefinitionRepository enemyDefinitionRepository)
    {
        return new QuestRoomService(
            stageRepository,
            roomRepository,
            runRepository,
            playerRepository,
            new QuestNpcAssignmentService(npcTemplateRepository),
            new QuestSnapshotFactory(),
            new QuestRunFactory(enemyDefinitionRepository));
    }

    private static Player CreatePlayer(string name)
    {
        var moveSet = new MoveSet();
        return new Player(
            new PlayerId(Guid.NewGuid()),
            name,
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            status: new Status(30, 10, 12, 8, 6, 3, 10),
            job: Job.Warrior,
            imagePath: "/images/player.png",
            moveSet: moveSet);
    }

    private static QuestStageDefinition CreateStage(int minPartyMemberCount, int maxPartyMemberCount, bool isActive)
    {
        return new QuestStageDefinition(
            new QuestStageId(1),
            "quest-001",
            "Test Quest",
            1,
            minPartyMemberCount,
            maxPartyMemberCount,
            [
                new QuestFloorDefinition(
                    1,
                    FloorType.Normal,
                    [
                        new QuestEnemyPlacement(
                            1,
                            new QuestEnemyDefinitionId(1),
                            new BattlePosition(BattleRow.Front, BattleColumn.Right))
                    ],
                    new QuestFloorRewardRule(0, 0))
            ],
            isActive);
    }

    private static QuestEnemyDefinition CreateEnemyDefinition()
    {
        return new QuestEnemyDefinition(
            new QuestEnemyDefinitionId(1),
            "Slime",
            1,
            new Status(10, 0, 4, 1, 1, 1, 1),
            "/images/slime.png",
            EnemyAiType.Aggressive,
            []);
    }

    private static QuestNpcTemplate CreateNpcTemplate()
    {
        return new QuestNpcTemplate(
            new QuestNpcTemplateId(1),
            "Support NPC",
            Job.Warrior,
            BattleRow.Front,
            1,
            new Status(20, 0, 6, 4, 1, 1, 3),
            [],
            NpcRole.FrontGuard);
    }

    private sealed class FakeQuestStageRepository(QuestStageDefinition stage) : IQuestStageRepository
    {
        public Task<QuestStageDefinition?> GetAsync(QuestStageId id)
            => Task.FromResult(stage.Id == id ? stage : null);

        public Task<QuestStageDefinition?> GetByStageCodeAsync(string stageCode)
            => Task.FromResult(stage.StageCode == stageCode ? stage : null);

        public Task<IReadOnlyList<QuestStageDefinition>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<QuestStageDefinition>>([stage]);
    }

    private sealed class FakeQuestRoomRepository : IQuestRoomRepository
    {
        private readonly Dictionary<Guid, QuestRoom> rooms = [];

        public List<QuestRoom> SavedRooms { get; } = [];

        public Task<QuestRoom?> GetAsync(QuestRoomId id)
        {
            rooms.TryGetValue(id.Value, out var room);
            return Task.FromResult(room);
        }

        public Task<QuestRoom?> GetRecruitingByOwnerAsync(PlayerId ownerId)
        {
            var room = rooms.Values
                .Where(x => x.OwnerId == ownerId && x.Status == QuestRoomStatus.Recruiting)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();
            return Task.FromResult(room);
        }

        public Task<IReadOnlyList<QuestRoom>> SearchAsync(QuestRoomSearchCondition condition)
            => Task.FromResult<IReadOnlyList<QuestRoom>>(rooms.Values.ToArray());

        public Task SaveAsync(QuestRoom room)
        {
            rooms[room.Id.Value] = room;
            SavedRooms.Add(room);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeQuestRunRepository : IQuestRunRepository
    {
        private readonly Dictionary<Guid, QuestRun> runs = [];

        public List<QuestRun> SavedRuns { get; } = [];
        public bool HasActiveRunForAnyPlayer { get; init; }

        public Task<QuestRun?> GetAsync(QuestRunId id)
        {
            runs.TryGetValue(id.Value, out var run);
            return Task.FromResult(run);
        }

        public Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId)
        {
            var run = runs.Values.FirstOrDefault(x => x.RoomId == roomId);
            return Task.FromResult(run);
        }

        public Task<bool> ExistsActiveRunByPlayerAsync(PlayerId playerId)
            => Task.FromResult(HasActiveRunForAnyPlayer);

        public Task<IReadOnlyList<QuestRun>> ListExpiredAsync(DateTimeOffset now)
            => Task.FromResult<IReadOnlyList<QuestRun>>([]);

        public Task SaveAsync(QuestRun run)
        {
            runs[run.Id.Value] = run;
            SavedRuns.Add(run);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlayerRepository(params Player[] players) : IPlayerRepository
    {
        private readonly Dictionary<PlayerId, Player> players = players.ToDictionary(x => x.Id);

        public Task<Player?> GetPlayerAsync(PlayerId id)
            => Task.FromResult(players.GetValueOrDefault(id));

        public Task<IReadOnlyList<Player>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Player>>(players.Values.ToArray());

        public Task<bool> UpdateNameAsync(PlayerId id, string name)
            => Task.FromResult(false);

        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task SaveAsync(Player player)
        {
            players[player.Id] = player;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeQuestNpcTemplateRepository(IReadOnlyList<QuestNpcTemplate> templates) : IQuestNpcTemplateRepository
    {
        public Task<IReadOnlyList<QuestNpcTemplate>> GetForStartAsync(QuestStageId stageId, int count)
            => Task.FromResult<IReadOnlyList<QuestNpcTemplate>>(templates.Take(count).ToArray());

        public Task<IReadOnlyList<QuestNpcTemplate>> GetAllAsync()
            => Task.FromResult(templates);
    }

    private sealed class FakeQuestEnemyDefinitionRepository(QuestEnemyDefinition definition) : IQuestEnemyDefinitionRepository
    {
        public Task<QuestEnemyDefinition?> GetAsync(QuestEnemyDefinitionId id)
            => Task.FromResult(definition.Id == id ? definition : null);

        public Task<IReadOnlyList<QuestEnemyDefinition>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<QuestEnemyDefinition>>([definition]);
    }
}
