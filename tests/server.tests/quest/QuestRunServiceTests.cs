using FluentAssertions;
using server.application.battle;
using server.application.player;
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
            run.TurnState.CurrentTurnNo,
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

    [Fact]
    public async Task ResolveTurnAsync_WhenRunEnds_AppliesQuestCooldownToRewardedPlayers()
    {
        var run = CreateRun();
        var repository = new FakeQuestRunRepository(run);
        var room = CreateRoom(run);
        var roomRepository = new FakeQuestRoomRepository(room);
        var playerRepository = new FakePlayerRepository(room.Participants.Single().PlayerId!.Value);
        var stage = CreateStage(run.StageId);
        var service = CreateRunService(repository, roomRepository, playerRepository, stage, []);
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

        var player = await playerRepository.GetPlayerAsync(room.Participants.Single().PlayerId!.Value);
        player.Should().NotBeNull();
        player!.QuestCooldownUntil.Should().NotBeNull();
    }

    [Fact]
    public async Task EscapeAsync_WhenOwnerMatches_MarksRunFailedAndAppliesQuestCooldown()
    {
        var run = CreateRun();
        var repository = new FakeQuestRunRepository(run);
        var room = CreateRoom(run);
        var roomRepository = new FakeQuestRoomRepository(room);
        var playerRepository = new FakePlayerRepository(roomRepository.PlayerIds.ToArray());
        var service = CreateRunService(repository, roomRepository, playerRepository, CreateStage(run.StageId), []);

        var escapedRun = await service.EscapeAsync(run.Id, room.OwnerId);

        escapedRun.Status.Should().Be(QuestRunStatus.Failed);
        var player = await playerRepository.GetPlayerAsync(room.OwnerId);
        player.Should().NotBeNull();
        player!.QuestCooldownUntil.Should().NotBeNull();
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenNpcPriestIsPresent_ResolvesPrayerAutomatically()
    {
        var playerParticipantId = QuestParticipantId.New();
        var priestParticipantId = QuestParticipantId.New();
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 10),
                new PartyMemberSeed(priestParticipantId, ParticipantType.Npc, "Priest", Job.Priest, new BattlePosition(BattleRow.Back, BattleColumn.Left), ActionMode.AutoAttackOnly, new Status(30, 10, 4, 4, 12, 3, 8), new MoveSet(), 30, 10)
            ],
            enemyHp: 20);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), []);

        var result = await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.Wait,
                DateTimeOffset.UtcNow));

        result.ResolvedInThisRequest.Should().BeTrue();
        repository.StoredRun!.LastTurnResults.Should().NotBeNull();
        repository.StoredRun.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorParticipantId == priestParticipantId.Value &&
            x.ActionKind == ActionKind.Prayer.ToString() &&
            x.Logs.Contains("PriestはOwnerに祈りを捧げた") &&
            x.TargetSummaries.Any(t => t.TargetParticipantId == playerParticipantId.Value));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenNpcRangerIsPresent_UsesTrapMoveAutomatically()
    {
        var playerParticipantId = QuestParticipantId.New();
        var rangerParticipantId = QuestParticipantId.New();
        var trapMoveId = new MoveId(12);
        var rangerMoveSet = new MoveSet();
        rangerMoveSet.SetSlot(0, trapMoveId);

        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 10),
                new PartyMemberSeed(rangerParticipantId, ParticipantType.Npc, "Ranger", Job.Ranger, new BattlePosition(BattleRow.Middle, BattleColumn.Left), ActionMode.AutoAttackOnly, new Status(30, 10, 8, 4, 4, 3, 8), rangerMoveSet, 30, 10)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Right),
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ],
            enemyHp: 20);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
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
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 0.1m, 0, 0m, ElementType.None)),
                new MoveEffect(
                    new MoveEffectId(2),
                    trapMoveId,
                    2,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.DamageTrap, 1m, 2, new DamageEffect(1, 0.1m, 0, 0m, ElementType.None)))
            ]);
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), [trapMove]);

        var result = await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.Wait,
                DateTimeOffset.UtcNow));

        result.ResolvedInThisRequest.Should().BeTrue();
        repository.StoredRun!.BattleState.Enemies.Should().Contain(x => x.Ailments.Any(a => a.Type == AilmentType.DamageTrap));
        repository.StoredRun.LastTurnResults.Should().NotBeNull();
        repository.StoredRun.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorParticipantId == rangerParticipantId.Value &&
            x.MoveId == trapMoveId.Id &&
            x.Logs.Contains("RangerはSlimeにTrapを使って1ダメージを与え、DamageTrapを付与した"));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenPlayerUsesNormalAttack_StoresDamageLog()
    {
        var playerParticipantId = QuestParticipantId.New();
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 10)
            ],
            enemyHp: 20);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), []);

        await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorParticipantId == playerParticipantId.Value &&
            x.ActionKind == ActionKind.NormalAttack.ToString() &&
            x.Logs.Contains("OwnerはSlimeに9ダメージを与えた"));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenPlayerUsesDamageMove_StoresMoveDamageLog()
    {
        var playerParticipantId = QuestParticipantId.New();
        var moveId = new MoveId(101);
        var moveSet = new MoveSet();
        moveSet.SetSlot(0, moveId);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 10, 5, 3, 3, 8), moveSet, 40, 10)
            ],
            enemyHp: 20);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var move = new Move(
            moveId,
            "Slash",
            "slash",
            TargetType.Enemy,
            AttackRange.Single,
            3,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Damage, damage: new DamageEffect(1, 1m, 1, 0m, ElementType.None))
            ]);
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), [move]);

        await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.UseMove,
                DateTimeOffset.UtcNow,
                moveId,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorParticipantId == playerParticipantId.Value &&
            x.MoveId == moveId.Id &&
            x.Logs.Contains("OwnerはSlimeにSlashを使って10ダメージを与えた"));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenPlayerUsesHealMove_StoresHealLog()
    {
        var healerId = QuestParticipantId.New();
        var targetId = QuestParticipantId.New();
        var moveId = new MoveId(102);
        var healerMoveSet = new MoveSet();
        healerMoveSet.SetSlot(0, moveId);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(healerId, ParticipantType.Player, "Healer", Job.Priest, new BattlePosition(BattleRow.Back, BattleColumn.Left), ActionMode.Manual, new Status(30, 10, 3, 3, 12, 3, 8), healerMoveSet, 30, 10),
                new PartyMemberSeed(targetId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.AutoAttackOnly, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 10, 10)
            ],
            enemyHp: 20);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var move = new Move(
            moveId,
            "Heal",
            "heal",
            TargetType.Ally,
            AttackRange.Single,
            3,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.Heal, damage: new DamageEffect(1, 1m, 5, 0m, ElementType.Holy, BuffStat.Intelligence))
            ]);
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), [move]);

        await service.SubmitCommandAsync(
            run.Id,
            healerId,
            new QuestSubmittedCommand(
                healerId,
                run.TurnState.CurrentTurnNo,
                ActionKind.UseMove,
                DateTimeOffset.UtcNow,
                moveId,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Left)));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorParticipantId == healerId.Value &&
            x.MoveId == moveId.Id &&
            x.Logs.Contains("HealerはOwnerにHealを使って17回復した"));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenPlayerUsesRestoreMpMove_StoresRestoreMpLog()
    {
        var mageId = QuestParticipantId.New();
        var targetId = QuestParticipantId.New();
        var moveId = new MoveId(103);
        var mageMoveSet = new MoveSet();
        mageMoveSet.SetSlot(0, moveId);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(mageId, ParticipantType.Player, "Mage", Job.Mage, new BattlePosition(BattleRow.Back, BattleColumn.Left), ActionMode.Manual, new Status(24, 10, 3, 3, 12, 3, 8), mageMoveSet, 24, 10),
                new PartyMemberSeed(targetId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.AutoAttackOnly, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 2)
            ],
            enemyHp: 20);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var move = new Move(
            moveId,
            "Meditate",
            "restore mp",
            TargetType.Ally,
            AttackRange.Single,
            1,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(new MoveEffectId(1), moveId, 1, MoveEffectType.RestoreMp, damage: new DamageEffect(1, 1m, 5, 0m, ElementType.None, BuffStat.Intelligence))
            ]);
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), [move]);

        await service.SubmitCommandAsync(
            run.Id,
            mageId,
            new QuestSubmittedCommand(
                mageId,
                run.TurnState.CurrentTurnNo,
                ActionKind.UseMove,
                DateTimeOffset.UtcNow,
                moveId,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Left)));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorParticipantId == mageId.Value &&
            x.MoveId == moveId.Id &&
            x.Logs.Contains("MageはOwnerにMeditateを使ってMPを8回復した"));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenEnemyHasPoisonAndTrap_StoresSeparateAilmentLogs()
    {
        var playerParticipantId = QuestParticipantId.New();
        var poisonMoveId = new MoveId(201);
        var trapMoveId = new MoveId(202);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 10)
            ],
            enemyHp: 20,
            enemyAilments:
            [
                new BattleAilmentState(AilmentType.Poison, 1, sourceMoveId: poisonMoveId),
                new BattleAilmentState(AilmentType.DamageTrap, 1, new DamageEffect(1, 0.1m, 0, 0m, ElementType.None), trapMoveId)
            ]);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var poisonMove = new Move(
            poisonMoveId,
            "Poison Mist",
            "poison",
            TargetType.Enemy,
            AttackRange.Single,
            0,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(1),
                    poisonMoveId,
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.Poison, 1m, 1))
            ]);
        var trapMove = new Move(
            trapMoveId,
            "Spike Trap",
            "trap",
            TargetType.Enemy,
            AttackRange.Single,
            0,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(2),
                    trapMoveId,
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.DamageTrap, 1m, 1, new DamageEffect(1, 0.1m, 0, 0m, ElementType.None)))
            ]);
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), [poisonMove, trapMove]);

        await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.Wait,
                DateTimeOffset.UtcNow));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorEnemyInstanceId != null &&
            x.MoveId == poisonMoveId.Id &&
            x.Logs.Contains("SlimeはPoison Mistによる毒で2ダメージを受けた"));
        repository.StoredRun.LastTurnResults.Actions.Should().Contain(x =>
            x.ActorEnemyInstanceId != null &&
            x.MoveId == trapMoveId.Id &&
            x.Logs.Contains("SlimeはSpike Trapによるトラップで2ダメージを受けた"));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenPoisonAndMaxHpBuffExpire_LogsOnlyPoisonTickDamage()
    {
        var playerParticipantId = QuestParticipantId.New();
        var poisonMoveId = new MoveId(203);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 10)
            ],
            enemyHp: 10,
            enemyAilments:
            [
                new BattleAilmentState(AilmentType.Poison, 1, sourceMoveId: poisonMoveId)
            ],
            enemyBuffs:
            [
                new BattleBuffState(BuffStat.MaxHp, BuffCalculationType.Mul, 2.0m, 1)
            ]);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var poisonMove = new Move(
            poisonMoveId,
            "Venom",
            "venom",
            TargetType.Enemy,
            AttackRange.Single,
            0,
            0,
            MoveCategory.Support,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(3),
                    poisonMoveId,
                    1,
                    MoveEffectType.Ailment,
                    ailment: new AilmentEffect(AilmentType.Poison, 1m, 1))
            ]);
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), [poisonMove]);

        await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.Wait,
                DateTimeOffset.UtcNow));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorEnemyInstanceId != null &&
            x.MoveId == poisonMoveId.Id &&
            x.Logs.Contains("SlimeはVenomによる毒で1ダメージを受けた"));
        repository.StoredRun.LastTurnResults.Actions.Should().NotContain(x =>
            x.ActorEnemyInstanceId != null &&
            x.Logs.Any(log => log.Contains("6ダメージ")));
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenRunEnds_ConsumesSnapshottedEquipmentDurability()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1,
            weaponEquipmentId: PlayerEquipmentId.New());
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var playerEquipment = new PlayerEquipment(
            run.PartySnapshots[0].WeaponEquipmentId!.Value,
            playerId,
            new EquipmentId(1001),
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: 2,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(CreateStage(run.StageId)),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        var stored = await playerEquipmentRepository.GetAsync(run.PartySnapshots[0].WeaponEquipmentId!.Value);
        stored.Should().NotBeNull();
        stored!.Durability.Should().Be(1);
        stored.Status.Should().Be(EquipmentStatus.Equipped);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenRunEndsWithLastDurabilityEquipment_MarksBroken()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1,
            weaponEquipmentId: PlayerEquipmentId.New());
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var playerEquipment = new PlayerEquipment(
            run.PartySnapshots[0].WeaponEquipmentId!.Value,
            playerId,
            new EquipmentId(1001),
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: 1,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(CreateStage(run.StageId)),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        var stored = await playerEquipmentRepository.GetAsync(run.PartySnapshots[0].WeaponEquipmentId!.Value);
        stored.Should().NotBeNull();
        stored!.Durability.Should().Be(0);
        stored.Status.Should().Be(EquipmentStatus.Broken);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenQuestSucceedsWithGuaranteedEquipmentReward_GrantsRewardToPlayer()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var rewardEquipment = new Equipment(
            new EquipmentId(2001),
            "報酬の剣",
            "勝者に授けられる剣。",
            EquipmentType.Weapon,
            12,
            20,
            10,
            new EquipmentStatusBonus(0, 0, 2, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice, Job.Warrior });
        var stage = CreateStage(
            new QuestStageId(1),
            [new QuestStageEquipmentRewardEntry(rewardEquipment.Id, weight: 100, isMiss: false)]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository();
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(rewardEquipment),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.Rewards.EquipmentRewardId.Should().Be(rewardEquipment.Id);
        repository.StoredRun.Rewards.SkippedRewardPlayerIds.Should().BeEmpty();
        var grantedEquipments = await playerEquipmentRepository.GetByPlayerAsync(playerId);
        grantedEquipments.Should().ContainSingle();
        grantedEquipments[0].EquipmentId.Should().Be(rewardEquipment.Id);
        grantedEquipments[0].Status.Should().Be(EquipmentStatus.Inventory);
        grantedEquipments[0].Durability.Should().Be(rewardEquipment.MaxDurability);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenRewardPlayerInventoryIsFull_SkipsGrantAndRecordsPlayerId()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var rewardEquipment = new Equipment(
            new EquipmentId(2001),
            "報酬の剣",
            "勝者に授けられる剣。",
            EquipmentType.Weapon,
            12,
            20,
            10,
            new EquipmentStatusBonus(0, 0, 2, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice, Job.Warrior });
        var stage = CreateStage(
            new QuestStageId(1),
            [new QuestStageEquipmentRewardEntry(rewardEquipment.Id, weight: 100, isMiss: false)]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var inventoryEquipments = Enumerable.Range(1, 20)
            .Select(i => new PlayerEquipment(
                PlayerEquipmentId.New(),
                playerId,
                new EquipmentId(1000 + i),
                EquipmentType.Weapon,
                EquipmentStatus.Inventory,
                durability: 10,
                mastery: 0,
                acquiredAt: DateTimeOffset.UtcNow,
                updatedAt: DateTimeOffset.UtcNow))
            .ToArray();
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(inventoryEquipments);
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(rewardEquipment),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.Rewards.EquipmentRewardId.Should().Be(rewardEquipment.Id);
        repository.StoredRun.Rewards.SkippedRewardPlayerIds.Should().Contain(playerId);
        var grantedEquipments = await playerEquipmentRepository.GetByPlayerAsync(playerId);
        grantedEquipments.Should().HaveCount(20);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenRewardRollIsMiss_DoesNotGrantEquipment()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var stage = CreateStage(
            new QuestStageId(1),
            [new QuestStageEquipmentRewardEntry(equipmentId: null, weight: 100, isMiss: true)]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository();
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.Rewards.EquipmentRewardId.Should().BeNull();
        repository.StoredRun.Rewards.SkippedRewardPlayerIds.Should().BeEmpty();
        var grantedEquipments = await playerEquipmentRepository.GetByPlayerAsync(playerId);
        grantedEquipments.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenQuestSucceedsWithGuaranteedItemReward_GrantsItemStackToPlayer()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var rewardItem = new Item(
            new ItemId(3001),
            "命脈の種",
            "最大HPが1上がる",
            99,
            ItemEffectType.StatBoost,
            statusBonus: new StatusBonus(1, 0, 0, 0, 0, 0, 0));
        var stage = CreateStage(
            new QuestStageId(1),
            itemRewards: [new QuestStageItemRewardEntry(rewardItem.Id, weight: 100, isMiss: false)]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var playerItemStackRepository = new FakePlayerItemStackRepository();
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            new FakePlayerEquipmentRepository(),
            playerItemStackRepository,
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(),
            new FakeItemRepository(rewardItem),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.Rewards.ItemRewardId.Should().Be(rewardItem.Id);
        var grantedStacks = await playerItemStackRepository.GetByPlayerAsync(playerId);
        grantedStacks.Should().ContainSingle();
        grantedStacks[0].ItemId.Should().Be(rewardItem.Id);
        grantedStacks[0].Quantity.Should().Be(1);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenItemRewardMatchesExistingStack_AddsQuantityToExistingStack()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var rewardItem = new Item(
            new ItemId(3001),
            "命脈の種",
            "最大HPが1上がる",
            99,
            ItemEffectType.StatBoost,
            statusBonus: new StatusBonus(1, 0, 0, 0, 0, 0, 0));
        var existingStack = new PlayerItemStack(PlayerItemStackId.New(), playerId, rewardItem.Id, 3, DateTimeOffset.UtcNow);
        var stage = CreateStage(
            new QuestStageId(1),
            itemRewards: [new QuestStageItemRewardEntry(rewardItem.Id, weight: 100, isMiss: false)]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var playerItemStackRepository = new FakePlayerItemStackRepository(existingStack);
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            new FakePlayerEquipmentRepository(),
            playerItemStackRepository,
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(),
            new FakeItemRepository(rewardItem),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        var grantedStacks = await playerItemStackRepository.GetByPlayerAsync(playerId);
        grantedStacks.Should().ContainSingle();
        grantedStacks[0].Quantity.Should().Be(4);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenQuestSucceedsWithTwoPlayers_GrantsSameRewardToBothPlayers()
    {
        var ownerPlayerId = new PlayerId(Guid.NewGuid());
        var memberPlayerId = new PlayerId(Guid.NewGuid());
        var ownerParticipantId = QuestParticipantId.New();
        var memberParticipantId = QuestParticipantId.New();
        var rewardEquipment = new Equipment(
            new EquipmentId(2001),
            "報酬の剣",
            "勝者に授けられる剣。",
            EquipmentType.Weapon,
            12,
            20,
            10,
            new EquipmentStatusBonus(0, 0, 2, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice, Job.Warrior });
        var stage = CreateStage(
            new QuestStageId(1),
            [new QuestStageEquipmentRewardEntry(rewardEquipment.Id, weight: 100, isMiss: false)]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(ownerParticipantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10),
                new PartyMemberSeed(memberParticipantId, ParticipantType.Player, "Member", Job.Apprentice, new BattlePosition(BattleRow.Middle, BattleColumn.Left), ActionMode.AutoAttackOnly, new Status(40, 10, 10, 5, 1, 1, 10), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var room = CreateRoom(run, ownerPlayerId);
        room = ReplaceSecondPlayer(room, memberPlayerId);
        var roomRepository = new FakeQuestRoomRepository(room);
        var playerRepository = new FakePlayerRepository(ownerPlayerId, memberPlayerId);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository();
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(rewardEquipment),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            ownerParticipantId,
            new QuestSubmittedCommand(
                ownerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        var ownerEquipments = await playerEquipmentRepository.GetByPlayerAsync(ownerPlayerId);
        var memberEquipments = await playerEquipmentRepository.GetByPlayerAsync(memberPlayerId);
        ownerEquipments.Should().ContainSingle(x => x.EquipmentId == rewardEquipment.Id);
        memberEquipments.Should().ContainSingle(x => x.EquipmentId == rewardEquipment.Id);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenGreatThiefIsInParty_RollsRewardTwiceForSharedReward()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var rewardEquipment = new Equipment(
            new EquipmentId(2001),
            "報酬の剣",
            "勝者に授けられる剣。",
            EquipmentType.Weapon,
            12,
            20,
            10,
            new EquipmentStatusBonus(0, 0, 2, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice, Job.GreatThief });
        var stage = CreateStage(
            new QuestStageId(1),
            [
                new QuestStageEquipmentRewardEntry(equipmentId: null, weight: 1, isMiss: true),
                new QuestStageEquipmentRewardEntry(rewardEquipment.Id, weight: 1, isMiss: false)
            ]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.GreatThief, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 10, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository();
        var rolls = new Queue<int>([1, 2]);
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(rewardEquipment),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory(),
            _ => rolls.Dequeue());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.Rewards.EquipmentRewardId.Should().Be(rewardEquipment.Id);
        var grantedEquipments = await playerEquipmentRepository.GetByPlayerAsync(playerId);
        grantedEquipments.Should().ContainSingle(x => x.EquipmentId == rewardEquipment.Id);
    }

    [Fact]
    public async Task ResolveTurnAsync_WhenOneInventoryEquipmentIsListed_ExcludesItFromCapacityCheckAndStillGrantsReward()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var participantId = QuestParticipantId.New();
        var rewardEquipment = new Equipment(
            new EquipmentId(2001),
            "報酬の剣",
            "勝者に授けられる剣。",
            EquipmentType.Weapon,
            12,
            20,
            10,
            new EquipmentStatusBonus(0, 0, 2, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice, Job.Warrior });
        var stage = CreateStage(
            new QuestStageId(1),
            [new QuestStageEquipmentRewardEntry(rewardEquipment.Id, weight: 100, isMiss: false)]);
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(participantId, ParticipantType.Player, "Owner", Job.Apprentice, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 50, 5, 1, 1, 50), new MoveSet(), 40, 10)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run, playerId));
        var playerRepository = new FakePlayerRepository(playerId);
        var listedEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            new EquipmentId(9999),
            EquipmentType.Weapon,
            EquipmentStatus.Inventory,
            durability: 10,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var otherEquipments = Enumerable.Range(1, 19)
            .Select(i => new PlayerEquipment(
                PlayerEquipmentId.New(),
                playerId,
                new EquipmentId(1000 + i),
                EquipmentType.Weapon,
                EquipmentStatus.Inventory,
                durability: 10,
                mastery: 0,
                acquiredAt: DateTimeOffset.UtcNow,
                updatedAt: DateTimeOffset.UtcNow))
            .ToArray();
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(otherEquipments.Append(listedEquipment).ToArray());
        var listedMarketEntry = new MarketListing(
            MarketListingId.New(),
            playerId,
            listedEquipment.Id,
            itemId: null,
            itemName: "出品中の剣",
            flavorText: "出品中。",
            quantity: 1,
            remainingQuantity: 1,
            unitPrice: 50,
            listedAt: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddDays(15));
        var service = new QuestRunService(
            repository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository([]),
            playerRepository,
            playerEquipmentRepository,
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(listedMarketEntry),
            new FakeEquipmentRepository(rewardEquipment),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());

        await service.SubmitCommandAsync(
            run.Id,
            participantId,
            new QuestSubmittedCommand(
                participantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.Rewards.SkippedRewardPlayerIds.Should().BeEmpty();
        var grantedEquipments = await playerEquipmentRepository.GetByPlayerAsync(playerId);
        grantedEquipments.Should().Contain(x => x.EquipmentId == rewardEquipment.Id);
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenNpcCannotReachEnemyWithNormalAttack_StoresWaitLog()
    {
        var playerParticipantId = QuestParticipantId.New();
        var warriorParticipantId = QuestParticipantId.New();
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Mage, new BattlePosition(BattleRow.Back, BattleColumn.Left), ActionMode.Manual, new Status(24, 10, 3, 3, 12, 3, 8), new MoveSet(), 24, 10),
                new PartyMemberSeed(warriorParticipantId, ParticipantType.Npc, "Warrior", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.AutoAttackOnly, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 10)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Back, BattleColumn.Right)
            ],
            enemyHp: 20);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), []);

        await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.Wait,
                DateTimeOffset.UtcNow));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorParticipantId == warriorParticipantId.Value &&
            x.ActionKind == ActionKind.Wait.ToString() &&
            x.Logs.Contains("Warriorは様子を見ている"));
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenEnemyDiesBeforeItsTurn_StoresActorUnavailableLog()
    {
        var playerParticipantId = QuestParticipantId.New();
        var run = CreateRunWithParty(
            [
                new PartyMemberSeed(playerParticipantId, ParticipantType.Player, "Owner", Job.Warrior, new BattlePosition(BattleRow.Front, BattleColumn.Left), ActionMode.Manual, new Status(40, 10, 10, 5, 3, 3, 8), new MoveSet(), 40, 10)
            ],
            enemyPositions:
            [
                new BattlePosition(BattleRow.Front, BattleColumn.Right)
            ],
            enemyHp: 1);
        var repository = new FakeQuestRunRepository(run);
        var roomRepository = new FakeQuestRoomRepository(CreateRoom(run));
        var service = CreateRunService(repository, roomRepository, CreateStage(run.StageId), []);

        await service.SubmitCommandAsync(
            run.Id,
            playerParticipantId,
            new QuestSubmittedCommand(
                playerParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.NormalAttack,
                DateTimeOffset.UtcNow,
                selectedTargetPosition: new BattlePosition(BattleRow.Front, BattleColumn.Right)));

        repository.StoredRun!.LastTurnResults!.Actions.Should().Contain(x =>
            x.ActorDisplayName == "Slime" &&
            x.ActionKind == ActionKind.NormalAttack.ToString() &&
            x.Logs.Contains("Slimeは行動前に倒れた"));
    }

    private static QuestRunService CreateRunService(
        FakeQuestRunRepository runRepository,
        FakeQuestRoomRepository roomRepository,
        FakePlayerRepository playerRepository,
        QuestStageDefinition stage,
        IReadOnlyList<Move> moves)
    {
        return new QuestRunService(
            runRepository,
            roomRepository,
            new FakeQuestStageRepository(stage),
            new FakeQuestEnemyDefinitionRepository(),
            new FakeMoveRepository(moves),
            playerRepository,
            new FakePlayerEquipmentRepository(),
            new FakePlayerItemStackRepository(),
            new FakeMarketListingRepository(),
            new FakeEquipmentRepository(),
            new FakeItemRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new BattleService(),
            new QuestBattleFactory());
    }

    private static QuestRunService CreateRunService(
        FakeQuestRunRepository runRepository,
        FakeQuestRoomRepository roomRepository,
        QuestStageDefinition stage,
        IReadOnlyList<Move> moves)
    {
        return CreateRunService(
            runRepository,
            roomRepository,
            new FakePlayerRepository(roomRepository.PlayerIds.ToArray()),
            stage,
            moves);
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
            weaponEquipmentId: null,
            armorEquipmentId: null,
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

    private static QuestRun CreateRunWithParty(
        IReadOnlyList<PartyMemberSeed> partyMembers,
        IReadOnlyList<BattlePosition>? enemyPositions = null,
        int enemyHp = 1,
        DateTimeOffset? deadlineAt = null,
        PlayerEquipmentId? weaponEquipmentId = null,
        PlayerEquipmentId? armorEquipmentId = null,
        IReadOnlyList<BattleAilmentState>? enemyAilments = null,
        IReadOnlyList<BattleBuffState>? enemyBuffs = null)
    {
        enemyPositions ??= [new BattlePosition(BattleRow.Front, BattleColumn.Right)];

        var snapshots = partyMembers.Select(member => new QuestRunPartyMemberSnapshot(
            member.ParticipantId,
            member.Type,
            member.DisplayName,
            member.Type == ParticipantType.Player ? "/images/player.png" : null,
            member.Job,
            member.Status,
            weaponEquipmentId: weaponEquipmentId,
            armorEquipmentId: armorEquipmentId,
            member.MoveSet,
            member.Position,
            member.ActionMode)).ToArray();

        var partyStates = partyMembers.Select(member => new QuestRunPartyMemberState(
            member.ParticipantId,
            currentHp: member.CurrentHp,
            currentMp: member.CurrentMp,
            isDead: false,
            canActFromTurn: 1,
            actionMode: member.ActionMode)).ToArray();

        return new QuestRun(
            QuestRunId.New(),
            QuestRoomId.New(),
            new QuestStageId(1),
            snapshots,
            new QuestFloorState(
                1,
                false,
                enemyPositions.Select((position, index) => new QuestEnemyPlacement(index + 1, new QuestEnemyDefinitionId(1), position))),
            new QuestBattleState(
                partyStates,
                enemyPositions.Select(position => new QuestEnemyState(
                    QuestEnemyInstanceId.New(),
                    new QuestEnemyDefinitionId(1),
                    position,
                    currentHp: enemyHp,
                    currentMp: 0,
                    isDead: false,
                    ailments: enemyAilments,
                    buffs: enemyBuffs)).ToArray()),
            new QuestTurnState(1, deadlineAt ?? DateTimeOffset.UtcNow.AddSeconds(30)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: DateTimeOffset.UtcNow);
    }

    private static QuestRoom CreateRoom(QuestRun run, PlayerId? ownerPlayerId = null)
    {
        var playerIds = run.PartySnapshots
            .Where(x => x.Type == ParticipantType.Player)
            .Select(_ => new PlayerId(Guid.NewGuid()))
            .ToArray();
        var playerIndex = 0;
        var resolvedOwnerPlayerId = ownerPlayerId ?? playerIds[0];
        if (playerIds.Length > 0)
        {
            playerIds[0] = resolvedOwnerPlayerId;
        }

        return new QuestRoom(
            run.RoomId,
            resolvedOwnerPlayerId,
            run.StageId,
            QuestRoomMode.Solo,
            formation: new FormationLayout(run.PartySnapshots.Select(x => x.StartPosition)),
            participants: run.PartySnapshots.Select((snapshot, index) =>
            {
                PlayerId? playerId = null;
                var isOwner = false;
                if (snapshot.Type == ParticipantType.Player)
                {
                    playerId = playerIds[playerIndex++];
                    isOwner = playerId == resolvedOwnerPlayerId;
                }

                return new QuestParticipant(
                    snapshot.ParticipantId,
                    snapshot.Type,
                    snapshot.DisplayName,
                    snapshot.StartPosition,
                    DateTimeOffset.UtcNow,
                    isOwner: isOwner,
                    playerId: playerId,
                    npcTemplateId: snapshot.Type == ParticipantType.Npc ? new QuestNpcTemplateId(index + 1) : null);
            }).ToArray());
    }

    private static QuestRoom ReplaceSecondPlayer(QuestRoom room, PlayerId secondPlayerId)
    {
        var participants = room.Participants.ToArray();
        var secondPlayer = participants.First(x => x.PlayerId is not null && !x.IsOwner);
        participants[Array.IndexOf(participants, secondPlayer)] = new QuestParticipant(
            secondPlayer.Id,
            secondPlayer.Type,
            secondPlayer.DisplayName,
            secondPlayer.Position,
            secondPlayer.JoinedAt,
            isOwner: false,
            playerId: secondPlayerId,
            npcTemplateId: secondPlayer.NpcTemplateId);

        return new QuestRoom(
            room.Id,
            room.OwnerId,
            room.StageId,
            room.Mode,
            formation: room.Formation,
            participants: participants,
            status: room.Status,
            version: room.Version,
            closeReason: room.CloseReason,
            createdAt: room.CreatedAt,
            closedAt: room.ClosedAt);
    }

    private static QuestStageDefinition CreateStage(
        QuestStageId stageId,
        IReadOnlyList<QuestStageEquipmentRewardEntry>? equipmentRewards = null,
        IReadOnlyList<QuestStageItemRewardEntry>? itemRewards = null)
    {
        return new QuestStageDefinition(
            stageId,
            "quest-001",
            "Test Quest",
            "image/quest/test-battlefield.svg",
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
            equipmentRewards: equipmentRewards ?? [],
            itemRewards: itemRewards ?? [],
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

        public Task<QuestRun?> GetActiveByPlayerAsync(PlayerId playerId)
            => Task.FromResult(StoredRun);

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
        public IReadOnlyList<PlayerId> PlayerIds => this.room.Participants
            .Where(x => x.PlayerId is not null)
            .Select(x => x.PlayerId!.Value)
            .ToArray();

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
        private readonly Dictionary<PlayerId, Player> players;

        public FakePlayerRepository(params PlayerId[] playerIds)
        {
            players = playerIds.ToDictionary(
                x => x,
                x => new Player(
                    x,
                    "Owner",
                    level: 1,
                    exp: 0,
                    jobLevel: 1,
                    jobExp: 0,
                    gold: 100,
                    status: new Status(10, 10, 10, 10, 10, 10, 10),
                    job: Job.Warrior,
                    imagePath: "/images/player.png",
                    moveSet: new MoveSet()));
        }

        public Task<Player?> GetPlayerAsync(PlayerId id)
            => Task.FromResult(players.GetValueOrDefault(id));

        public Task<IReadOnlyList<Player>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Player>>(players.Values.ToArray());

        public Task<bool> UpdateNameAsync(PlayerId id, string name)
            => Task.FromResult(true);

        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown)
            => Task.FromResult<DateTimeOffset?>(null);

        public Task SaveAsync(Player player)
        {
            players[player.Id] = player;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlayerItemStackRepository(params PlayerItemStack[] stacks) : IPlayerItemStackRepository
    {
        private readonly Dictionary<PlayerId, List<PlayerItemStack>> stacksByPlayerId = stacks
            .GroupBy(x => x.PlayerId)
            .ToDictionary(x => x.Key, x => x.ToList());

        public Task<IReadOnlyList<PlayerItemStack>> GetByPlayerAsync(PlayerId playerId)
            => Task.FromResult((IReadOnlyList<PlayerItemStack>)(stacksByPlayerId.TryGetValue(playerId, out var playerStacks)
                ? playerStacks.ToArray()
                : []));

        public Task<PlayerItemStack?> GetAsync(PlayerItemStackId id)
            => Task.FromResult(stacksByPlayerId.Values.SelectMany(x => x).FirstOrDefault(x => x.Id == id));

        public Task SaveAsync(IReadOnlyList<PlayerItemStack> playerItemStacks)
        {
            foreach (var group in playerItemStacks.GroupBy(x => x.PlayerId))
            {
                stacksByPlayerId[group.Key] = group.ToList();
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(PlayerItemStackId id)
        {
            foreach (var group in stacksByPlayerId.ToArray())
            {
                var filtered = group.Value.Where(x => x.Id != id).ToList();
                if (filtered.Count != group.Value.Count)
                {
                    stacksByPlayerId[group.Key] = filtered;
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeItemRepository(params Item[] items) : IItemRepository
    {
        private readonly Dictionary<ItemId, Item> itemsById = items.ToDictionary(x => x.Id);

        public Task<Item?> GetAsync(ItemId id)
            => Task.FromResult(itemsById.TryGetValue(id, out var item) ? item : null);

        public Task<IReadOnlyList<Item>> GetAllAsync()
            => Task.FromResult((IReadOnlyList<Item>)itemsById.Values.OrderBy(x => x.Id.Value).ToArray());
    }

    private sealed class FakeMarketListingRepository(params MarketListing[] listings) : IMarketListingRepository
    {
        private readonly Dictionary<MarketListingId, MarketListing> listingsById = listings.ToDictionary(x => x.Id);

        public Task<MarketListing?> GetAsync(MarketListingId id)
            => Task.FromResult(listingsById.GetValueOrDefault(id));

        public Task<IReadOnlyList<MarketListing>> GetActiveAsync(DateTimeOffset now)
            => Task.FromResult((IReadOnlyList<MarketListing>)listingsById.Values.Where(x => !x.IsExpired(now) && !x.IsSoldOut).ToArray());

        public Task<IReadOnlyList<MarketListing>> GetBySellerAsync(PlayerId sellerId, DateTimeOffset now)
            => Task.FromResult((IReadOnlyList<MarketListing>)listingsById.Values
                .Where(x => x.SellerId == sellerId && !x.IsExpired(now) && !x.IsSoldOut)
                .ToArray());

        public Task<IReadOnlyList<MarketListing>> GetExpiredAsync(DateTimeOffset now)
            => Task.FromResult((IReadOnlyList<MarketListing>)listingsById.Values
                .Where(x => x.IsExpired(now) && !x.IsSoldOut)
                .ToArray());

        public Task SaveAsync(MarketListing listing)
        {
            listingsById[listing.Id] = listing;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(MarketListingId id)
        {
            listingsById.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJobProfileRepository : IJobProfileRepository
    {
        public JobProfile GetByJob(Job job)
            => new(
                job,
                $"{job} profile",
                5,
                [],
                new GrowthValue(1, 1, 1, 1, 1, 1, 1));

        public IReadOnlyList<JobProfile> GetAll() => [GetByJob(Job.Apprentice)];
    }

    private sealed class FakeJobMoveLearningRuleRepository : IJobMoveLearningRuleRepository
    {
        public JobMoveLearningRule GetByJob(Job job) => new(job, []);
    }

    private readonly record struct PartyMemberSeed(
        QuestParticipantId ParticipantId,
        ParticipantType Type,
        string DisplayName,
        Job Job,
        BattlePosition Position,
        ActionMode ActionMode,
        Status Status,
        MoveSet MoveSet,
        int CurrentHp,
        int CurrentMp);
}
