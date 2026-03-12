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
        var service = CreateRunService(repository, CreateStage(run.StageId), []);

        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            run.TurnState.CurrentTurnNo,
            ActionKind.Wait,
            DateTimeOffset.UtcNow);

        var updatedRun = await service.SubmitCommandAsync(run.Id, run.PartySnapshots[0].ParticipantId, command);

        updatedRun.TurnState.PendingCommands.Should().ContainSingle();
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task ResolveTimeoutAsync_WhenDeadlineExceeded_SwitchesPartyToAutoAttack()
    {
        var run = CreateRun(deadlineAt: DateTimeOffset.UtcNow.AddSeconds(-1));
        var repository = new FakeQuestRunRepository(run);
        var service = CreateRunService(repository, CreateStage(run.StageId), []);

        var updatedRun = await service.ResolveTimeoutAsync(run.Id, DateTimeOffset.UtcNow);

        updatedRun.BattleState.PartyMembers.Should().OnlyContain(x => x.ActionMode == ActionMode.AutoAttackOnly);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task AddChatMessageAsync_WithInProgressRun_AppendsMessage()
    {
        var run = CreateRun();
        var repository = new FakeQuestRunRepository(run);
        var service = CreateRunService(repository, CreateStage(run.StageId), []);

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
        var stage = CreateStage(run.StageId);
        var service = CreateRunService(repository, stage, []);
        var target = run.BattleState.Enemies.Single().Position;

        await service.SubmitCommandAsync(
            run.Id,
            run.PartySnapshots[0].ParticipantId,
            new QuestSubmittedCommand(
                run.PartySnapshots[0].ParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: target));

        var summary = await service.ResolveTurnAsync(run.Id);

        summary.IsQuestCompleted.Should().BeTrue();
        summary.IsFloorCleared.Should().BeTrue();
        repository.StoredRun!.Status.Should().Be(QuestRunStatus.Succeeded);
        repository.SaveCount.Should().Be(2);
    }

    private static QuestRunService CreateRunService(
        FakeQuestRunRepository runRepository,
        QuestStageDefinition stage,
        IReadOnlyList<Move> moves)
    {
        return new QuestRunService(
            runRepository,
            new FakeQuestStageRepository(stage),
            new FakeMoveRepository(moves),
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
            [],
            DateTimeOffset.UtcNow);
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

    private sealed class FakeMoveRepository(IReadOnlyList<Move> moves) : IMoveRepository
    {
        public Task<Move?> GetMoveAsync(MoveId moveId)
            => Task.FromResult(moves.FirstOrDefault(x => x.Id == moveId));

        public Task<IReadOnlyList<Move>> GetAllMovesAsync()
            => Task.FromResult(moves);
    }
}
