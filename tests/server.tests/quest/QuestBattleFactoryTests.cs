using FluentAssertions;
using server.application.quest;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class QuestBattleFactoryTests
{
    [Fact]
    public async Task CreateTurnInputsAsync_WhenUseMoveCommandHasTargetPosition_PreservesSelectedPosition()
    {
        var participantId = QuestParticipantId.New();
        var moveId = new MoveId(99);
        var targetPosition = new BattlePosition(BattleRow.Front, BattleColumn.Right);
        var moveSet = new MoveSet();
        moveSet.SetSlot(0, moveId);

        var run = new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            new QuestStageId(1),
            [
                new QuestRunPartyMemberSnapshot(
                    participantId,
                    ParticipantType.Player,
                    "Player",
                    "/images/player.png",
                    Job.Warrior,
                    new Status(40, 10, 10, 5, 3, 3, 8),
                    moveSet,
                    new BattlePosition(BattleRow.Front, BattleColumn.Left),
                    ActionMode.Manual)
            ],
            new QuestFloorState(
                1,
                false,
                [
                    new QuestEnemyPlacement(1, new QuestEnemyDefinitionId(1), new BattlePosition(BattleRow.Front, BattleColumn.Left)),
                    new QuestEnemyPlacement(2, new QuestEnemyDefinitionId(1), targetPosition)
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
                    new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), new BattlePosition(BattleRow.Front, BattleColumn.Left), 10, 0, false),
                    new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), targetPosition, 10, 0, false)
                ]),
            new QuestTurnState(
                1,
                DateTimeOffset.UtcNow.AddSeconds(30),
                [
                    new QuestSubmittedCommand(
                        participantId,
                        1,
                        ActionKind.UseMove,
                        DateTimeOffset.UtcNow,
                        moveId,
                        selectedTargetPosition: targetPosition)
                ]),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: DateTimeOffset.UtcNow);

        var move = new Move(
            moveId,
            "Row Attack",
            "row",
            TargetType.Enemy,
            AttackRange.Row,
            3,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    moveId,
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1m, 1, 0m, ElementType.None))
            ]);

        var factory = new QuestBattleFactory();

        var (actions, moves) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([move]));

        actions.Should().ContainSingle(x =>
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == moveId.Id &&
            x.SelectedPosition == targetPosition);
        moves.Should().ContainSingle(x => x.Id.Id == moveId.Id);
    }

    private sealed class FakeMoveRepository(IReadOnlyList<Move> moves) : IMoveRepository
    {
        private readonly IReadOnlyDictionary<int, Move> moveById = moves.ToDictionary(x => x.Id.Id);

        public Task<Move?> GetMoveAsync(MoveId moveId)
        {
            moveById.TryGetValue(moveId.Id, out var move);
            return Task.FromResult(move);
        }

        public Task<IReadOnlyList<Move>> GetAllMovesAsync()
        {
            return Task.FromResult(moves);
        }
    }
}
