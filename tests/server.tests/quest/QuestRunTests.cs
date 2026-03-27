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

    private static QuestRun CreateRun()
    {
        var participantId = QuestParticipantId.New();
        var snapshot = new QuestRunPartyMemberSnapshot(
            participantId,
            ParticipantType.Player,
            "Owner",
            "owner.png",
            Job.Warrior,
            new Status(20, 5, 5, 3, 1, 1, 2),
            new MoveSet(),
            new BattlePosition(BattleRow.Front, BattleColumn.Left),
            ActionMode.Manual);

        var enemyId = QuestEnemyInstanceId.New();
        return new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            new QuestStageId(1),
            [snapshot],
            new QuestFloorState(1, false, []),
            new QuestBattleState(
                [
                    new QuestRunPartyMemberState(participantId, 20, 5, false, 1, ActionMode.Manual)
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
