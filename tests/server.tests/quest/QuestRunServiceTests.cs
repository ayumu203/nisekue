using FluentAssertions;
using server.application.battle;
using server.application.quest;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class QuestRunServiceTests
{
    [Fact]
    public async Task SubmitCommandAsync_WithExistingRun_SavesPendingCommand()
    {
        var run = CreateRun();
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), []);

        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            run.TurnState.CurrentTurnNo,
            ActionKind.Wait,
            DateTimeOffset.UtcNow);

        var result = await service.SubmitCommandAsync(run.Id, run.PartySnapshots[0].ParticipantId, command);

        result.ResolvedInThisRequest.Should().BeTrue();
        result.Run.TurnState.PendingCommands.Should().BeEmpty();
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessExpiredRunsAsync_WhenDeadlineExceeded_SwitchesPartyToAutoAttack()
    {
        var run = CreateRun(deadlineAt: DateTimeOffset.UtcNow.AddSeconds(-1));
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), []);

        var updatedRuns = await service.ProcessExpiredRunsAsync(DateTimeOffset.UtcNow);

        updatedRuns.Should().ContainSingle();
        repository.StoredRun!.BattleState.PartyMembers.Should().OnlyContain(x => x.ActionMode == ActionMode.AutoAttackOnly);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task AddChatMessageAsync_WithInProgressRun_AppendsMessage()
    {
        var run = CreateRun();
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), []);

        var message = new QuestChatMessage(
            run.PartySnapshots[0].ParticipantId,
            "Owner",
            "/images/player.png",
            "hello",
            DateTimeOffset.UtcNow);

        var updatedRun = await service.AddChatMessageAsync(run.Id, message);

        updatedRun.ChatMessages.Should().ContainSingle(x => x.Message == "hello");
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenFinalFloorEnemyIsDefeated_MarksRunSucceeded()
    {
        var run = CreateRun();
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var stage = CreateStage(run.StageId);
        var service = CreateRunService(repository, roomRepository, stage, []);
        var target = run.BattleState.Enemies.Single().Position;

        var result = await service.SubmitCommandAsync(
            run.Id,
            run.PartySnapshots[0].ParticipantId,
            new QuestSubmittedCommand(
                run.PartySnapshots[0].ParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: target));

        result.ResolvedInThisRequest.Should().BeTrue();
        repository.StoredRun!.Status.Should().Be(QuestRunStatus.Succeeded);
        repository.SaveCount.Should().Be(1);
    }

    private static QuestRunService CreateRunService(
        FakeQuestRunRepository runRepository,
        FakeQuestRoomRepository roomRepository,
        QuestStageDefinition stage,
        IReadOnlyList<Move> moves)
    {
        return new QuestRunService(
            runRepository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository(moves),
            new FakePlayerRepository(),
            new FakeGrowthValueRepository(),
            new BattleService(),
            new QuestBattleFactory());
    }

    private static QuestRun CreateRun(DateTimeOffset? deadlineAt = null)
    {
        var participantId = QuestParticipantId.New();
        var moveSet = new MoveSet();
        var snapshot = new QuestRunPartyMemberSnapshot(
            participantId,
            ParticipantType.Player,
            "Owner",
            "/images/player.png",
            Job.Warrior,
            new Status(40, 10, 50, 5, 1, 1, 50),
            moveSet,
            new BattlePosition(BattleRow.Front, BattleColumn.Left),
            ActionMode.Manual);

        return new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            new QuestStageId(1),
            [snapshot],
            new QuestFloorState(
                1,
                false,
                [
                    new QuestEnemyPlacement(
                        1,
                        new QuestEnemyDefinitionId(1),
                        new BattlePosition(BattleRow.Front, BattleColumn.Right))
                ]),
            new QuestBattleState(
                [
                    new QuestRunPartyMemberState(
                        participantId,
                        currentHp: 40,
                        currentMp: 10,
                        isDead: false,
                        canActFromTurn: 1,
                        actionMode: ActionMode.Manual)
                ],
                [
                    new QuestEnemyState(
                        QuestEnemyInstanceId.New(),
                        new QuestEnemyDefinitionId(1),
                        new BattlePosition(BattleRow.Front, BattleColumn.Right),
                        currentHp: 1,
                        currentMp: 0,
                        isDead: false)
                ]),
            new QuestTurnState(1, deadlineAt ?? DateTimeOffset.UtcNow.AddSeconds(30)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: DateTimeOffset.UtcNow);
    }

    private static QuestRoom CreateRoom(QuestRun run)
    {
        var snapshot = run.PartySnapshots[0];
        var playerId = new PlayerId(Guid.NewGuid());

        return new QuestRoom(
            run.RoomId,
            playerId,
            run.StageId,
            QuestRoomMode.Solo,
            formation: new FormationLayout([snapshot.StartPosition]),
            participants:
            [
                new QuestParticipant(
                    snapshot.ParticipantId,
                    ParticipantType.Player,
                    snapshot.DisplayName,
                    snapshot.StartPosition,
                    DateTimeOffset.UtcNow,
                    isOwner: true,
                    playerId: playerId)
            ]);
    }

    private static QuestStageDefinition CreateStage(QuestStageId stageId)
    {
        return new QuestStageDefinition(
            stageId,
            "quest-001",
            "Test Quest",
            1,
            1,
            6,
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
            true);
    }

    private sealed class FakeQuestRunRepository(QuestRun run) : IQuestRunRepository
    {
        public QuestRun? StoredRun { get; private set; } = run;
        public int SaveCount { get; private set; }

        public Task<QuestRun?> GetAsync(QuestRunId id)
            => Task.FromResult(StoredRun?.Id == id ? StoredRun : null);

        public Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId)
            => Task.FromResult(StoredRun?.RoomId == roomId ? StoredRun : null);

        public Task<bool> ExistsActiveRunByPlayerAsync(PlayerId playerId)
            => Task.FromResult(false);

        public Task<IReadOnlyList<QuestRun>> ListExpiredAsync(DateTimeOffset now)
            => Task.FromResult<IReadOnlyList<QuestRun>>(StoredRun is not null && StoredRun.TurnState.ActionDeadlineAt <= now ? [StoredRun] : []);

        public Task SaveAsync(QuestRun run)
        {
            StoredRun = run;
            SaveCount++;
            return Task.CompletedTask;
        }
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

    private sealed class FakeQuestRoomRepository(QuestRoom room) : IQuestRoomRepository
    {
        private readonly QuestRoom room = room;

        public Task<QuestRoom?> GetAsync(QuestRoomId id)
            => Task.FromResult(this.room.Id == id ? this.room : null);

        public Task<QuestRoom?> GetRecruitingByOwnerAsync(PlayerId ownerId)
            => Task.FromResult<QuestRoom?>(this.room.OwnerId == ownerId && this.room.Status == QuestRoomStatus.Recruiting ? this.room : null);

        public Task<IReadOnlyList<QuestRoom>> SearchAsync(QuestRoomSearchCondition condition)
            => Task.FromResult<IReadOnlyList<QuestRoom>>([room]);

        public Task SaveAsync(QuestRoom room)
            => Task.CompletedTask;
    }

    private sealed class FakeMoveRepository(IReadOnlyList<Move> moves) : IMoveRepository
    {
        public Task<Move?> GetMoveAsync(MoveId moveId)
            => Task.FromResult(moves.FirstOrDefault(x => x.Id == moveId));

        public Task<IReadOnlyList<Move>> GetAllMovesAsync()
            => Task.FromResult(moves);
    }

    private sealed class FakeQuestEnemyDefinitionRepository : IQuestEnemyDefinitionRepository
    {
        private static readonly QuestEnemyDefinition Definition = new(
            new QuestEnemyDefinitionId(1),
            "Slime",
            1,
            new Status(10, 0, 4, 1, 1, 1, 1),
            "/images/slime.png",
            EnemyAiType.Aggressive,
            []);

        public Task<QuestEnemyDefinition?> GetAsync(QuestEnemyDefinitionId id)
            => Task.FromResult(Definition.Id == id ? Definition : null);

        public Task<IReadOnlyList<QuestEnemyDefinition>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<QuestEnemyDefinition>>([Definition]);
    }

    private sealed class FakePlayerRepository : IPlayerRepository
    {
        public Task<Player?> GetPlayerAsync(PlayerId id)
            => Task.FromResult<Player?>(new Player(
                id,
                "Owner",
                level: 1,
                exp: 0,
                status: new Status(10, 10, 10, 10, 10, 10, 10),
                job: Job.Warrior,
                imagePath: "/images/player.png",
                moveSet: new MoveSet()));

        public Task<bool> UpdateNameAsync(PlayerId id, string name)
            => Task.FromResult(true);

        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task SaveAsync(Player player)
            => Task.CompletedTask;
    }

    private sealed class FakeGrowthValueRepository : IGrowthValueRepository
    {
        public GrowthValue GetByJob(Job job) => new(1, 1, 1, 1, 1, 1, 1);
    }
}
