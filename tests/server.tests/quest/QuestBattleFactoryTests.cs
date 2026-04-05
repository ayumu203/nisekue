using System.Linq;
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
        var moveId = new MoveId(519);
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
                    weaponEquipmentId: null,
                    armorEquipmentId: null,
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

        var (actions, moves) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([move]), CreateEnemyDefinitions());

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

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([]), CreateEnemyDefinitions());

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.Prayer &&
            x.SelectedPosition == new BattlePosition(BattleRow.Front, BattleColumn.Left));
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcRangerHasNoTrap_UsesTrapMove()
    {
        var participantId = QuestParticipantId.New();
        var trapMoveId = new MoveId(504);
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

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([trapMove]), CreateEnemyDefinitions());

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
        var attackMoveId = new MoveId(421);
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

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([attackMove]), CreateEnemyDefinitions());

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == attackMoveId.Id);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcGuardianWithoutTaunt_UsesTauntMove()
    {
        var participantId = QuestParticipantId.New();
        var tauntMoveId = new MoveId(501);
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
        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([tauntMove]), CreateEnemyDefinitions());

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == tauntMoveId.Id);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcMageHasHalfMp_SelectsFireAttack()
    {
        var participantId = QuestParticipantId.New();
        var areaMoveId = new MoveId(108);
        var singleMoveId = new MoveId(109);
        var restoreMoveId = new MoveId(509);
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
            AttackRange.Single,
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
        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([areaMove, singleMove, restoreMove]), CreateEnemyDefinitions());

        var fireMoveIds = new[] { areaMoveId.Id, singleMoveId.Id };
        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId.HasValue &&
            fireMoveIds.Contains(x.MoveId.Value));
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenNpcCannotReachEnemyWithNormalAttack_UsesWait()
    {
        var participantId = QuestParticipantId.New();
        var run = CreateNpcRun(
            participantId,
            Job.Warrior,
            actionMode: ActionMode.AutoAttackOnly,
            initialActionMode: ActionMode.AutoAttackOnly,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Npc, "Warrior", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 12, 5, 3)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Right)
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([]), CreateEnemyDefinitions());

        actions.Should().ContainSingle(x =>
            x.ActorId == participantId.Value &&
            x.Kind == BattleActionKind.Wait);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenEnemyCannotReachAnyAlly_UsesWait()
    {
        var participantId = QuestParticipantId.New();
        var run = CreateNpcRun(
            participantId,
            Job.Warrior,
            actionMode: ActionMode.Manual,
            initialActionMode: ActionMode.Manual,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Back, BattleColumn.Left), 40, 10, 12, 5, 3)
            ],
            partyStateFactory: member => new QuestRunPartyMemberState(
                member.ParticipantId,
                currentHp: 0,
                currentMp: member.MaxMp,
                isDead: true,
                canActFromTurn: 1,
                actionMode: ActionMode.Manual),
            enemyPositions:
            [
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([]), CreateEnemyDefinitions());

        actions.Should().Contain(x =>
            x.ActorId != participantId.Value &&
            x.Kind == BattleActionKind.Wait);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenEnemyHasRearReachDamageMove_UsesMoveAgainstBackRow()
    {
        var participantId = QuestParticipantId.New();
        var enemyMoveId = new MoveId(407);
        var run = CreateNpcRun(
            participantId,
            Job.Warrior,
            actionMode: ActionMode.Manual,
            initialActionMode: ActionMode.Manual,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Front", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 12, 5, 3),
                new PartyMemberSeed(QuestParticipantId.New(), ParticipantType.Player, "Back", Job.Mage, new BattlePosition(BattleRow.Back, BattleColumn.Right), 20, 12, 3, 3, 12)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Left)
            ]);

        var rowMove = new Move(
            enemyMoveId,
            "Enemy Row",
            "enemy-row",
            TargetType.Enemy,
            AttackRange.Row,
            0,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    enemyMoveId,
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1m, 1, 0m, ElementType.Fire))
            ]);

        var factory = new QuestBattleFactory();

        var (actions, moves) = await factory.CreateTurnInputsAsync(
            run,
            new FakeMoveRepository([rowMove]),
            CreateEnemyDefinitions(moveIds: [enemyMoveId]));

        actions.Should().Contain(x =>
            x.ActorId != participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == enemyMoveId.Id &&
            x.SelectedPosition == new BattlePosition(BattleRow.Back, BattleColumn.Right));
        moves.Should().Contain(x => x.Id.Id == enemyMoveId.Id);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenEnemyCanReachTauntingTarget_PrioritizesTauntOverBackRow()
    {
        var tauntParticipantId = QuestParticipantId.New();
        var run = CreateNpcRun(
            tauntParticipantId,
            Job.Warrior,
            actionMode: ActionMode.Manual,
            initialActionMode: ActionMode.Manual,
            party:
            [
                new PartyMemberSeed(tauntParticipantId, ParticipantType.Player, "Taunter", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 12, 5, 3),
                new PartyMemberSeed(QuestParticipantId.New(), ParticipantType.Player, "Back", Job.Mage, new BattlePosition(BattleRow.Back, BattleColumn.Right), 20, 12, 3, 3, 12)
            ],
            partyStateFactory: member => new QuestRunPartyMemberState(
                member.ParticipantId,
                currentHp: member.MaxHp,
                currentMp: member.MaxMp,
                isDead: false,
                canActFromTurn: 1,
                actionMode: member.ParticipantId == tauntParticipantId ? ActionMode.Manual : ActionMode.Manual,
                ailments: member.ParticipantId == tauntParticipantId
                    ? [new BattleAilmentState(AilmentType.Taunt, 2)]
                    : []),
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Left)
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(run, new FakeMoveRepository([]), CreateEnemyDefinitions());

        actions.Should().Contain(x =>
            x.ActorId != tauntParticipantId.Value &&
            x.Kind == BattleActionKind.NormalAttack &&
            x.SelectedPosition == new BattlePosition(BattleRow.Front, BattleColumn.Left));
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenEnemySelfHpIsHalfOrLess_UsesSelfHeal()
    {
        var participantId = QuestParticipantId.New();
        var enemyMoveId = new MoveId(408);
        var run = CreateNpcRun(
            participantId,
            Job.Warrior,
            actionMode: ActionMode.Manual,
            initialActionMode: ActionMode.Manual,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Front", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 12, 5, 3)
            ],
            enemyStatesFactory: position =>
                new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), position, 10, 10, false),
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Left)
            ]);

        var healMove = new Move(
            enemyMoveId,
            "Self Heal",
            "self-heal",
            TargetType.Self,
            AttackRange.Single,
            2,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    enemyMoveId,
                    1,
                    MoveEffectType.Heal,
                    damage: new DamageEffect(1, 0m, 12, 0m, ElementType.None, BuffStat.Intelligence))
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(
            run,
            new FakeMoveRepository([healMove]),
            CreateEnemyDefinitions(moveIds: [enemyMoveId]));

        actions.Should().Contain(x =>
            x.ActorId != participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == enemyMoveId.Id &&
            x.TargetType == TargetType.Self);
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenEnemyFrontAllyHpIsHalfOrLess_UsesAllyHealOnFrontTarget()
    {
        var participantId = QuestParticipantId.New();
        var enemyMoveId = new MoveId(409);
        var run = CreateNpcRun(
            participantId,
            Job.Warrior,
            actionMode: ActionMode.Manual,
            initialActionMode: ActionMode.Manual,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Front", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 12, 5, 3)
            ],
            enemyStatesFactory: position => position == new BattlePosition(BattleRow.Front, BattleColumn.Right)
                ? new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), position, 10, 10, false)
                : new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), position, 20, 10, false),
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Left),
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ]);

        var healMove = new Move(
            enemyMoveId,
            "Ally Heal",
            "ally-heal",
            TargetType.Ally,
            AttackRange.Single,
            2,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    enemyMoveId,
                    1,
                    MoveEffectType.Heal,
                    damage: new DamageEffect(1, 0m, 12, 0m, ElementType.None, BuffStat.Intelligence))
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(
            run,
            new FakeMoveRepository([healMove]),
            CreateEnemyDefinitions(moveIds: [enemyMoveId]));

        actions.Should().Contain(x =>
            x.ActorId != participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == enemyMoveId.Id &&
            x.SelectedPosition == new BattlePosition(BattleRow.Front, BattleColumn.Right));
    }

    [Fact]
    public async Task CreateTurnInputsAsync_WhenEnemyHasOtherAlly_UsesBuffBeforeAttack()
    {
        var participantId = QuestParticipantId.New();
        var enemyMoveId = new MoveId(203);
        var run = CreateNpcRun(
            participantId,
            Job.Warrior,
            actionMode: ActionMode.Manual,
            initialActionMode: ActionMode.Manual,
            party:
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Front", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), 40, 10, 12, 5, 3)
            ],
            enemyStatesFactory: position =>
                new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), position, 20, 10, false),
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Left),
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ]);

        var buffMove = new Move(
            enemyMoveId,
            "Enemy Buff",
            "enemy-buff",
            TargetType.Ally,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    enemyMoveId,
                    1,
                    MoveEffectType.Buff,
                    buff: new BuffEffect(BuffStat.Defense, BuffCalculationType.Add, 5m, 2, 1m, false))
            ]);

        var factory = new QuestBattleFactory();

        var (actions, _) = await factory.CreateTurnInputsAsync(
            run,
            new FakeMoveRepository([buffMove]),
            CreateEnemyDefinitions(moveIds: [enemyMoveId]));

        actions.Should().Contain(x =>
            x.ActorId != participantId.Value &&
            x.Kind == BattleActionKind.UseMove &&
            x.MoveId == enemyMoveId.Id &&
            x.SelectedPosition == new BattlePosition(BattleRow.Front, BattleColumn.Right));
    }

    private static QuestRun CreateNpcRun(
        QuestParticipantId actorId,
        Job actorJob,
        ActionMode actionMode,
        ActionMode initialActionMode,
        IReadOnlyList<MoveId>? moveIds = null,
        IReadOnlyList<PartyMemberSeed>? party = null,
        IReadOnlyList<BattlePosition>? enemyPositions = null,
        bool isBossFloor = false,
        Func<PartyMemberSeed, QuestRunPartyMemberState>? partyStateFactory = null,
        Func<BattlePosition, QuestEnemyState>? enemyStatesFactory = null)
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
                weaponEquipmentId: null,
                armorEquipmentId: null,
                set,
                member.Position,
                member.ParticipantId == actorId ? initialActionMode : ActionMode.Manual);
        }).ToArray();

        partyStateFactory ??= member => new QuestRunPartyMemberState(
            member.ParticipantId,
            currentHp: member.MaxHp,
            currentMp: member.MaxMp,
            isDead: false,
            canActFromTurn: 1,
            actionMode: member.ParticipantId == actorId ? actionMode : ActionMode.Manual);

        var partyStates = party.Select(partyStateFactory).ToArray();

        enemyStatesFactory ??= position =>
            new QuestEnemyState(QuestEnemyInstanceId.New(), new QuestEnemyDefinitionId(1), position, 20, 0, false);

        var enemies = enemyPositions.Select(enemyStatesFactory).ToArray();

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

    private static IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> CreateEnemyDefinitions(
        IReadOnlyList<MoveId>? moveIds = null)
    {
        moveIds ??= [];
        var definition = new QuestEnemyDefinition(
            new QuestEnemyDefinitionId(1),
            "Enemy",
            1,
            new Status(30, 10, 8, 6, 6, 3, 8),
            "/image/battle/enemy.png",
            EnemyAiType.Aggressive,
            moveIds);

        return new Dictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>
        {
            [definition.Id] = definition
        };
    }
}
