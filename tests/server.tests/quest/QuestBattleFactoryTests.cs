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

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcPriestHasNoHealTarget_UsesPrayer()
    {
        var participantId = QuestParticipantId.New();
        var run = CreateNpcRun(
            participantId,
            Job.Priest,
            actionMode: ActionMode.AutoAttackOnly,
            initialActionMode: ActionMode.AutoAttackOnly,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Npc, "Priest", Job.Priest, new BattlePosition(BattleRow.Back, BattleColumn.Left), 30, 10, 4, 4, 12),
                new PartyMemberSeed(QuestParticipantId.New(), ParticipantType.Player, "Warrior", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 5, 10, 4, 3)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([]));

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.Prayer &&
            x.SelectedPosition == new BattlePosition(BattleRow.Front, BattleColumn.Left));
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcRangerHasNoTrap_UsesTrapMove()
    {
        var participantId = QuestParticipantId.New();
        var trapMoveId = new MoveId(12);
        var run = CreateNpcRun(
            participantId,
            Job.Ranger,
            actionMode: ActionMode.AutoAttackOnly,
            initialActionMode: ActionMode.AutoAttackOnly,
            moveIds: [trapMoveId],
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Npc, "Ranger", Job.Ranger, new BattlePosition(BattleRow.Middle, BattleColumn.Left), 30, 10, 8, 4, 4)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Right),
                new BattlePosition(BattleRow.Front, BattleColumn.Left)
            ]);

        var trapMove = new Move(
            trapMoveId,
            "Trap",
            "trap",
            TargetType.Enemy,
            AttackRange.Single,
            3,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    trapMoveId,
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.DamageTrap, 1m, 2, new DamageEffect(1, 0.1m, 0, 0m, ElementType.None)))
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([trapMove]));

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == trapMoveId.Id &&
            x.SelectedPosition == new BattlePosition(BattleRow.Back, BattleColumn.Right));
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcWarriorOnBossFloor_UsesSingleAttackMove()
    {
        var participantId = QuestParticipantId.New();
        var attackMoveId = new MoveId(1);
        var run = CreateNpcRun(
            participantId,
            Job.Warrior,
            actionMode: ActionMode.AutoAttackOnly,
            initialActionMode: ActionMode.AutoAttackOnly,
            moveIds: [attackMoveId],
            isBossFloor: true,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Npc, "Warrior", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 12, 5, 3)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ]);

        var attackMove = new Move(
            attackMoveId,
            "Strike",
            "strike",
            TargetType.Enemy,
            AttackRange.Single,
            4,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    attackMoveId,
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1m, 1, 0m, ElementType.None))
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([attackMove]));

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == attackMoveId.Id);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcGuardianWithoutTaunt_UsesTauntMove()
    {
        var participantId = QuestParticipantId.New();
        var tauntMoveId = new MoveId(7);
        var run = CreateNpcRun(
            participantId,
            Job.Guardian,
            actionMode: ActionMode.AutoAttackOnly,
            initialActionMode: ActionMode.AutoAttackOnly,
            moveIds: [tauntMoveId],
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Npc, "Guardian", Job.Guardian, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 8, 10, 3)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ]);

        var tauntMove = new Move(
            tauntMoveId,
            "Taunt",
            "taunt",
            TargetType.Self,
            AttackRange.Single,
            3,
            1,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    tauntMoveId,
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.Taunt, 1m, 1))
            ]);

        var factory = new QuestBattleFactory();
        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([tauntMove]));

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == tauntMoveId.Id);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcMageHasHalfMp_UsesAreaAttack()
    {
        var participantId = QuestParticipantId.New();
        var areaMoveId = new MoveId(18);
        var singleMoveId = new MoveId(19);
        var restoreMoveId = new MoveId(50);
        var run = CreateNpcRun(
            participantId,
            Job.Mage,
            actionMode: ActionMode.AutoAttackOnly,
            initialActionMode: ActionMode.AutoAttackOnly,
            moveIds: [areaMoveId, singleMoveId, restoreMoveId],
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Npc, "Mage", Job.Mage, new BattlePosition(BattleRow.Back, BattleColumn.Left), 24, 10, 3, 3, 12)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Right),
                new BattlePosition(BattleRow.Middle, BattleColumn.Right)
            ]);

        var areaMove = new Move(
            areaMoveId,
            "Area",
            "area",
            TargetType.Enemy,
            AttackRange.Row,
            6,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(new MoveEffectId(1), areaMoveId, 1, MoveEffectType.Damage, damage: new DamageEffect(1, 1m, 1, 0m, ElementType.Fire))
            ]);
        var singleMove = new Move(
            singleMoveId,
            "Single",
            "single",
            TargetType.Enemy,
            AttackRange.Single,
            3,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(new MoveEffectId(2), singleMoveId, 1, MoveEffectType.Damage, damage: new DamageEffect(1, 1m, 1, 0m, ElementType.Fire))
            ]);
        var restoreMove = new Move(
            restoreMoveId,
            "Restore",
            "restore",
            TargetType.Self,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(new MoveEffectId(3), restoreMoveId, 1, MoveEffectType.RestoreMp, damage: new DamageEffect(1, 1m, 5, 0m, ElementType.None, BuffStat.Intelligence))
            ]);

        var factory = new QuestBattleFactory();
        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([areaMove, singleMove, restoreMove]));

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == areaMoveId.Id);
    }

    private static QuestRun CreateNpcRun(
        QuestParticipantId actorId,
        Job actorJob,
        ActionMode actionMode,
        ActionMode initialActionMode,
        IReadOnlyList<MoveId>? moveIds = null,
        IReadOnlyList<PartyMemberSeed>? party = null,
        IReadOnlyList<BattlePosition>? enemyPositions = null,
        bool isBossFloor = false)
    {
        moveIds ??= [];
        party ??=
        [
            new PartyMemberSeed(actorId, ParticipantType.Npc, actorJob.ToString(), actorJob, new BattlePosition(BattleRow.Front, BattleColumn.Left), 30, 10, 10, 5, 5)
        ];
        enemyPositions ??= [new BattlePosition(BattleRow.Front, BattleColumn.Right)];

        var snapshots = party.Select((member, index) =>
        {
            var set = new MoveSet();
            if (member.ParticipantId == actorId)
            {
                for (var i = 0; i < moveIds.Count; i++)
                {
                    set.SetSlot(i, moveIds[i]);
                }
            }

            return new QuestRunPartyMemberSnapshot(
                member.ParticipantId,
                member.Type,
                member.DisplayName,
                member.Type == ParticipantType.Player ? "/images/player.png" : null,
                member.Job,
                new Status(member.MaxHp, member.MaxMp, member.Strength, member.Defense, member.Intelligence, 3, 8),
                set,
                member.Position,
                member.ParticipantId == actorId ? initialActionMode : ActionMode.Manual);
        }).ToArray();

        var partyStates = party.Select(member => new QuestRunPartyMemberState(
            member.ParticipantId,
            currentHp: member.MaxHp,
            currentMp: member.MaxMp,
            isDead: false,
            canActFromTurn: 1,
            actionMode: member.ParticipantId == actorId ? actionMode : ActionMode.Manual)).ToArray();

        var enemies = enemyPositions.Select(position =>
            new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), position, 20, 0, false)).ToArray();

        return new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            new QuestStageId(1),
            snapshots,
            new QuestFloorState(
                1,
                isBossFloor,
                enemyPositions.Select((position, index) => new QuestEnemyPlacement(index + 1, new QuestEnemyDefinitionId(1), position))),
            new QuestBattleState(partyStates, enemies),
            new QuestTurnState(1, DateTimeOffset.UtcNow.AddSeconds(30)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: DateTimeOffset.UtcNow);
    }

    private readonly record struct PartyMemberSeed(
        QuestParticipantId ParticipantId,
        ParticipantType Type,
        string DisplayName,
        Job Job,
        BattlePosition Position,
        int MaxHp,
        int MaxMp,
        int Strength,
        int Defense,
        int Intelligence);

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
