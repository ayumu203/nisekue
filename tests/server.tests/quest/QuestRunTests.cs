using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class QuestRunTests
{
    [Fact]
    public void SubmitCommand_WhenDeadlineExceeded_ThrowsInvalidOperationException()
    {
        var run = CreateRun();
        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            1,
            ActionKind.Wait,
            DateTimeOffset.UtcNow);

        var act = () => run.SubmitCommand(run.PartySnapshots[0].ParticipantId, command, run.TurnState.ActionDeadlineAt.AddSeconds(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SwitchToAutoActionForTimeout_WhenDeadlineExceeded_ChangesUnsubmittedManualMembers()
    {
        var run = CreateRun();

        run.SwitchToAutoActionForTimeout(run.TurnState.ActionDeadlineAt.AddSeconds(1));

        run.BattleState.PartyMembers.Should().OnlyContain(x => x.ActionMode == ActionMode.AutoAttackOnly);
    }

    [Fact]
    public void ResolveTurn_WhenEscapeCommandExists_MarksRunFailed()
    {
        var run = CreateRun();
        run.SubmitCommand(
            run.PartySnapshots[0].ParticipantId,
            new QuestSubmittedCommand(run.PartySnapshots[0].ParticipantId, 1, ActionKind.Escape, DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow);

        var summary = run.ResolveTurn(DateTimeOffset.UtcNow.AddSeconds(60));

        run.Status.Should().Be(QuestRunStatus.Failed);
        summary.IsQuestFailed.Should().BeTrue();
    }

    [Fact]
    public void SetLastTurnResults_MarksDirty_AndMarkPersistedClearsIt()
    {
        var run = CreateRun();
        run.LastTurnResultsDirty.Should().BeFalse();

        run.SetLastTurnResults(new QuestLastTurnResults(1, DateTimeOffset.UtcNow));
        run.LastTurnResultsDirty.Should().BeTrue();

        run.MarkLastTurnResultsPersisted();
        run.LastTurnResultsDirty.Should().BeFalse();
    }

    [Fact]
    public void ResolveTurn_WhenEscapeEndsAsSuccess_MarksRunSucceeded()
    {
        var run = CreateRun();
        run.SubmitCommand(
            run.PartySnapshots[0].ParticipantId,
            new QuestSubmittedCommand(run.PartySnapshots[0].ParticipantId, 1, ActionKind.Escape, DateTimeOffset.UtcNow),
            DateTimeOffset.UtcNow);

        var summary = run.ResolveTurn(DateTimeOffset.UtcNow.AddSeconds(60), escapeEndsAsSuccess: true);

        run.Status.Should().Be(QuestRunStatus.Succeeded);
        run.EndedAt.Should().NotBeNull();
        summary.IsQuestCompleted.Should().BeTrue();
        summary.IsQuestFailed.Should().BeFalse();
    }

    [Fact]
    public void ApplyBattleResolution_WhenFinalFloorEnemiesAreAllDefeated_MarksRunSucceeded()
    {
        var run = CreateRun();
        var enemy = run.BattleState.Enemies.Single();
        var resolution = new BattleTurnResolution(
            [],
            [
                new BattleActorState(new BattleActorId(enemy.Id.Value), currentHp: 0, currentMp: 0)
            ]);

        var summary = run.ApplyBattleResolution(
            resolution,
            new Dictionary<BattleActorId, QuestParticipantId>(),
            new Dictionary<BattleActorId, QuestEnemyInstanceId>
            {
                [new BattleActorId(enemy.Id.Value)] = enemy.Id
            },
            finalFloorNo: run.FloorState.CurrentFloorNo,
            nextDeadlineAt: DateTimeOffset.UtcNow.AddSeconds(60));

        run.Status.Should().Be(QuestRunStatus.Succeeded);
        summary.IsQuestCompleted.Should().BeTrue();
    }

    [Fact]
    public void AddChatMessage_WhenRunIsInProgress_AppendsMessage()
    {
        var run = CreateRun();
        var message = new QuestChatMessage(
            run.TurnState.CurrentTurnNo,
            run.PartySnapshots[0].ParticipantId,
            "Owner",
            "owner.png",
            "hello",
            DateTimeOffset.UtcNow);

        run.AddChatMessage(message);

        run.ChatMessages.Should().ContainSingle().Which.Message.Should().Be("hello");
    }

    [Fact]
    public void EscapeByOwner_WhenRunIsInProgress_MarksRunFailed()
    {
        var run = CreateRun();

        run.EscapeByOwner();

        run.Status.Should().Be(QuestRunStatus.Failed);
        run.EndedAt.Should().NotBeNull();
    }

    [Fact]
    public void SubmitCommand_CaptureWithoutTarget_ThrowsArgumentException()
    {
        var run = CreateRun();
        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            1,
            ActionKind.Capture,
            DateTimeOffset.UtcNow);

        var act = () => run.SubmitCommand(run.PartySnapshots[0].ParticipantId, command, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SubmitCommand_CaptureWithTarget_AcceptsCommand()
    {
        var run = CreateRun();
        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            1,
            ActionKind.Capture,
            DateTimeOffset.UtcNow,
            selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right));

        run.SubmitCommand(run.PartySnapshots[0].ParticipantId, command, DateTimeOffset.UtcNow);

        run.TurnState.PendingCommands.Should().ContainSingle().Which.ActionKind.Should().Be(ActionKind.Capture);
    }

    [Fact]
    public void SubmitCommand_SummonPetWithoutStandbyPet_ThrowsInvalidOperationException()
    {
        var run = CreateRun();
        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            1,
            ActionKind.SummonPet,
            DateTimeOffset.UtcNow);

        var act = () => run.SubmitCommand(run.PartySnapshots[0].ParticipantId, command, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>().WithMessage("*スタンバイのペット*");
    }

    [Fact]
    public void SubmitCommand_SummonPetWithStandbyPet_AcceptsCommand()
    {
        var run = CreateRun(pet: CreatePetSnapshot());
        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            1,
            ActionKind.SummonPet,
            DateTimeOffset.UtcNow);

        run.SubmitCommand(run.PartySnapshots[0].ParticipantId, command, DateTimeOffset.UtcNow);

        run.TurnState.PendingCommands.Should().ContainSingle().Which.ActionKind.Should().Be(ActionKind.SummonPet);
    }

    [Fact]
    public void SubmitCommand_SummonPetWhenLimitReached_ThrowsInvalidOperationException()
    {
        var run = CreateRun(pet: CreatePetSnapshot(), petSummonsUsed: 3);
        var command = new QuestSubmittedCommand(
            run.PartySnapshots[0].ParticipantId,
            1,
            ActionKind.SummonPet,
            DateTimeOffset.UtcNow);

        var act = () => run.SubmitCommand(run.PartySnapshots[0].ParticipantId, command, DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>().WithMessage("*上限*");
    }

    private static QuestPetSnapshot CreatePetSnapshot()
    {
        return new QuestPetSnapshot(
            new QuestEnemyDefinitionId(1),
            new Status(maxHp: 30, maxMp: 10, strength: 5, defense: 3, intelligence: 1, luck: 1, speed: 4));
    }

    private static QuestRun CreateRun(QuestPetSnapshot? pet = null, int petSummonsUsed = 0)
    {
        var participantId = QuestParticipantId.New();
        var snapshot = new QuestRunPartyMemberSnapshot(
            participantId,
            ParticipantType.Player,
            "Owner",
            "owner.png",
            Job.Warrior,
            new Status(20, 5, 5, 3, 1, 1, 2),
            weaponEquipmentId: null,
            armorEquipmentId: null,
            new MoveSet(),
            new BattlePosition(BattleRow.Front, BattleColumn.Left),
            ActionMode.Manual,
            pet);

        var enemyId = QuestEnemyInstanceId.New();
        return new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            new QuestStageId(1),
            [snapshot],
            new QuestFloorState(1, false, []),
            new QuestBattleState(
                [
                    new QuestRunPartyMemberState(participantId, 20, 5, false, 1, ActionMode.Manual, petSummonsUsed: petSummonsUsed)
                ],
                [
                    new QuestEnemyState(
                        enemyId,
                        new server.domain.quest.QuestEnemyDefinitionId(1),
                        new BattlePosition(BattleRow.Front, BattleColumn.Right),
                        10,
                        0,
                        false)
                ]),
            new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: DateTimeOffset.UtcNow);
    }
}
