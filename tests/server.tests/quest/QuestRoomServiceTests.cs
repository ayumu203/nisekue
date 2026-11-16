using FluentAssertions;
using server.application.quest;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.pet;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.infrastructure.player;
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
            .WithMessage("クエスト終了後3分間は再参加できません。再参加まであと*です。");
    }

    [Fact]
    public async Task CreateRoomAsync_WhenOwnerLevelIsFarBelowRecommended_ThrowsInvalidOperationException()
    {
        var owner = CreatePlayer("Owner", level: 5);
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true, recommendedLevel: 20);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            new FakeQuestRoomRepository(),
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var act = () => service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("推奨レベル20に対してレベルが低すぎるためルームを作成できません。最低レベルは14です。");
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
            .WithMessage("クエスト終了後3分間は再参加できません。再参加まであと*です。");
    }

    [Fact]
    public async Task JoinRoomAsync_WhenPlayerLevelIsFarBelowRecommended_ThrowsInvalidOperationException()
    {
        var owner = CreatePlayer("Owner", level: 20);
        var guest = CreatePlayer("Guest", level: 5);
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true, recommendedLevel: 20);
        var roomRepository = new FakeQuestRoomRepository();
        var room = new QuestRoom(QuestRoomId.New(), owner.Id, stage.Id, QuestRoomMode.Multi);
        room.AddPlayer(owner.Id, owner.Name, owner.Level);
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
            .WithMessage("推奨レベル20に対してレベルが低すぎるためルームに参加できません。最低レベルは14です。");
    }

    [Fact]
    public async Task CreateRoomAsync_WithJoinRestrictions_SavesJoinPolicy()
    {
        var owner = CreatePlayer("Owner");
        var allowedPlayer = CreatePlayer("Allowed");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner, allowedPlayer),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Multi, owner.Level, [owner.Id, allowedPlayer.Id]);

        room.JoinPolicy.MinRequiredLevel.Should().Be(owner.Level);
        room.JoinPolicy.AllowedPlayerIds.Should().Contain(allowedPlayer.Id);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenOwnerIsNotIncludedInAllowedPlayers_AddsOwnerAutomatically()
    {
        var owner = CreatePlayer("Owner");
        var other = CreatePlayer("Other");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner, other),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Multi, null, [other.Id]);

        room.JoinPolicy.AllowedPlayerIds.Should().Contain(owner.Id);
        room.JoinPolicy.AllowedPlayerIds.Should().Contain(other.Id);
    }

    [Fact]
    public async Task JoinRoomAsync_WhenPlayerIsNotAllowedByRestriction_ThrowsInvalidOperationException()
    {
        var owner = CreatePlayer("Owner");
        var guest = CreatePlayer("Guest");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();
        var room = new QuestRoom(
            QuestRoomId.New(),
            owner.Id,
            stage.Id,
            QuestRoomMode.Multi,
            joinPolicy: new QuestRoomJoinPolicy(allowedPlayerIds: [owner.Id]));
        room.AddPlayer(owner.Id, owner.Name, owner.Level);
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
            .WithMessage("このルームへの参加対象プレイヤーに含まれていません。");
    }

    [Fact]
    public async Task UpdateRestrictionsAsync_WhenExistingParticipantWouldBecomeNotAllowed_ThrowsInvalidOperationException()
    {
        var owner = CreatePlayer("Owner");
        var guest = CreatePlayer("Guest");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();
        var room = new QuestRoom(QuestRoomId.New(), owner.Id, stage.Id, QuestRoomMode.Multi);
        room.AddPlayer(owner.Id, owner.Name, owner.Level);
        room.AddPlayer(guest.Id, guest.Name, guest.Level);
        await roomRepository.SaveAsync(room);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner, guest),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var act = () => service.UpdateRestrictionsAsync(room.Id, owner.Id, null, [owner.Id]);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("現在の参加者に参加対象外プレイヤーが含まれるため更新できません。");
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

    [Fact]
    public async Task StartAsync_WithEquippedPlayerEquipment_ReflectsEquipmentInSnapshot()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true);
        var roomRepository = new FakeQuestRoomRepository();
        var runRepository = new FakeQuestRunRepository();
        var equipment = new Equipment(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            20,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Warrior });
        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            owner.Id,
            equipment.Id,
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: equipment.MaxDurability,
            mastery: 0,
            plusValue: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            runRepository,
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()),
            new FakePlayerEquipmentRepository(playerEquipment),
            new FakeEquipmentRepository(equipment));

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);
        var run = await service.StartAsync(room.Id);

        run.PartySnapshots.Should().ContainSingle();
        run.PartySnapshots[0].WeaponEquipmentId.Should().Be(playerEquipment.Id);
        run.PartySnapshots[0].BaseStatus.Strength.Should().Be(owner.Status.Strength + equipment.BonusValues.Strength);
    }

    [Fact]
    public async Task JoinRoomAsync_WhenStageRequiresMapUnlock_ParticipantCanJoinRegardlessOfOwnFlag()
    {
        // マップ解放は作成者のみ必要。参加者はフラグ不問で参加可能
        var owner = CreatePlayer("Owner");
        owner.SetMapUnlockFlag(MapUnlockFlag.Map1);
        var guest = CreatePlayer("Guest");
        var stage = CreateStage(minPartyMemberCount: 1, maxPartyMemberCount: 6, isActive: true, requiredMapUnlockFlag: MapUnlockFlag.Map1);
        var roomRepository = new FakeQuestRoomRepository();
        var room = new QuestRoom(QuestRoomId.New(), owner.Id, stage.Id, QuestRoomMode.Multi);
        room.AddPlayer(owner.Id, owner.Name, owner.Level);
        await roomRepository.SaveAsync(room);

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner, guest),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var updatedRoom = await service.JoinRoomAsync(room.Id, guest.Id);

        updatedRoom.Participants.Should().Contain(x => x.PlayerId == guest.Id);
    }

    [Fact]
    public async Task CreateRoomAsync_WhenMirrorStageAndMultiMode_Throws()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateMirrorStage();

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            new FakeQuestRoomRepository(),
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var act = () => service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Multi);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_WhenMirrorStage_CreatesRunFromPlayerEnemyPlan()
    {
        var owner = CreatePlayer("Owner");
        var top = CreatePlayer("Top", level: 99);
        var stage = CreateMirrorStage();
        var playerRepository = new FakePlayerRepository(owner, top);
        var runRepository = new FakeQuestRunRepository();
        var mirrorEnemyPlanService = new QuestMirrorEnemyPlanService(
            new FakeMirrorCandidateRepository(playerRepository, top.Id, owner.Id),
            new FakePlayerEquipmentRepository(),
            new FakeEquipmentRepository(),
            new FakeMoveRepository(),
            new EquipmentStatusResolver(),
            new CombatIndexCalculator(new CsvCombatIndexWeightRepository()));

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            new FakeQuestRoomRepository(),
            runRepository,
            playerRepository,
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()),
            mirrorEnemyPlanService: mirrorEnemyPlanService);

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);
        var run = await service.StartAsync(room.Id);

        run.MirrorEnemyPlan.Should().NotBeNull();
        var plan = run.MirrorEnemyPlan!;
        run.FloorState.CurrentFloorNo.Should().Be(1);
        run.FloorState.IsBossFloor.Should().BeFalse();
        var firstFloorEnemyIds = run.BattleState.Enemies.Select(x => x.EnemyDefinitionId).ToArray();
        firstFloorEnemyIds.Should().NotBeEmpty();
        firstFloorEnemyIds.Should().OnlyContain(id => id.Value >= MirrorEnemyPlanBuilder.FirstDefinitionId);
        plan.GetFloor(2).IsBoss.Should().BeTrue();
        plan.Enemies.Should().Contain(x => x.SourcePlayerId == owner.Id);
        foreach (var enemy in run.BattleState.Enemies)
        {
            enemy.CurrentHp.Should().Be(plan.FindEnemy(enemy.EnemyDefinitionId)!.Status.MaxHp);
        }
        runRepository.SavedRuns.Should().ContainSingle(x => x.Id == run.Id);
    }

    [Fact]
    public async Task StartAsync_WhenMirrorStageNeedsAllies_FillsWithPlayersBeforeNpcTemplates()
    {
        var owner = CreatePlayer("Owner");
        var ally = CreatePlayer("Ally", level: 8);
        ally.MoveSet.AddLearnedMove(new MoveId(101));
        var stage = CreateMirrorStage(minPartyMemberCount: 3, maxPartyMemberCount: 3);
        var playerRepository = new FakePlayerRepository(owner, ally);
        var roomRepository = new FakeQuestRoomRepository();
        var runRepository = new FakeQuestRunRepository();
        var candidateRepository = new FakeMirrorCandidateRepository(playerRepository, owner.Id) { AllyIds = [ally.Id] };
        var mirrorEnemyPlanService = new QuestMirrorEnemyPlanService(
            candidateRepository,
            new FakePlayerEquipmentRepository(),
            new FakeEquipmentRepository(),
            new FakeMoveRepository(),
            new EquipmentStatusResolver(),
            new CombatIndexCalculator(new CsvCombatIndexWeightRepository()));

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            roomRepository,
            runRepository,
            playerRepository,
            new FakeQuestNpcTemplateRepository([CreateNpcTemplate()]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()),
            mirrorEnemyPlanService: mirrorEnemyPlanService,
            mirrorAllyService: new QuestMirrorAllyService(candidateRepository));

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);
        var run = await service.StartAsync(room.Id);

        run.PartySnapshots.Should().HaveCount(3);
        var allySnapshot = run.PartySnapshots.Single(x => x.DisplayName == "Ally");
        allySnapshot.Type.Should().Be(ParticipantType.Npc);
        allySnapshot.InitialActionMode.Should().Be(ActionMode.AutoAttackOnly);
        allySnapshot.Job.Should().Be(ally.Job);
        allySnapshot.ImagePath.Should().Be(ally.ImagePath);
        allySnapshot.MoveSet.GetLearnedMoveIds().Select(x => x.Id).Should().Equal(101);
        allySnapshot.Pets.Should().BeEmpty();
        run.PartySnapshots.Should().ContainSingle(x => x.DisplayName == "Support NPC");

        var savedRoom = roomRepository.SavedRooms.Last();
        var allyParticipant = savedRoom.Participants.Single(x => x.DisplayName == "Ally");
        allyParticipant.SourcePlayerId.Should().Be(ally.Id);
        allyParticipant.NpcTemplateId.Should().BeNull();
        allyParticipant.PlayerId.Should().BeNull();
    }

    [Fact]
    public async Task StartAsync_WhenMirrorStageWithoutPlanService_Throws()
    {
        var owner = CreatePlayer("Owner");
        var stage = CreateMirrorStage();

        var service = CreateRoomService(
            new FakeQuestStageRepository(stage),
            new FakeQuestRoomRepository(),
            new FakeQuestRunRepository(),
            new FakePlayerRepository(owner),
            new FakeQuestNpcTemplateRepository([]),
            new FakeQuestEnemyDefinitionRepository(CreateEnemyDefinition()));

        var room = await service.CreateRoomAsync(owner.Id, stage.Id, QuestRoomMode.Solo);
        var act = () => service.StartAsync(room.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static QuestStageDefinition CreateMirrorStage(int minPartyMemberCount = 1, int maxPartyMemberCount = 1)
    {
        return new QuestStageDefinition(
            new QuestStageId(14),
            "meikan-shrine",
            "名鑑の祠",
            "image/quest/mirror-shrine-battlefield.svg",
            recommendedLevel: 1,
            minimumEntryLevel: null,
            minPartyMemberCount: minPartyMemberCount,
            maxPartyMemberCount: maxPartyMemberCount,
            [
                new QuestFloorDefinition(1, FloorType.Normal, [], new QuestFloorRewardRule(0, 0)),
                new QuestFloorDefinition(2, FloorType.Boss, [], new QuestFloorRewardRule(0, 0))
            ],
            equipmentRewards: [],
            itemRewards: [],
            isActive: true,
            progressionType: ProgressionType.Mirror);
    }

    private static QuestRoomService CreateRoomService(
        IQuestStageRepository stageRepository,
        IQuestRoomRepository roomRepository,
        IQuestRunRepository runRepository,
        IPlayerRepository playerRepository,
        IQuestNpcTemplateRepository npcTemplateRepository,
        IQuestEnemyDefinitionRepository enemyDefinitionRepository,
        IPlayerEquipmentRepository? playerEquipmentRepository = null,
        IEquipmentRepository? equipmentRepository = null,
        QuestMirrorEnemyPlanService? mirrorEnemyPlanService = null,
        QuestMirrorAllyService? mirrorAllyService = null)
    {
        return new QuestRoomService(
            stageRepository,
            roomRepository,
            runRepository,
            playerRepository,
            playerEquipmentRepository ?? new FakePlayerEquipmentRepository(),
            equipmentRepository ?? new FakeEquipmentRepository(),
            enemyDefinitionRepository,
            new FakePlayerPetRepository(),
            new QuestNpcAssignmentService(npcTemplateRepository),
            new QuestSnapshotFactory(new EquipmentStatusResolver()),
            new QuestRunFactory(enemyDefinitionRepository, new FakeQuestEndlessEnemyTemplateRepository(), new QuestEndlessFloorGenerator()),
            mirrorEnemyPlanService,
            mirrorAllyService);
    }

    private static Player CreatePlayer(string name, int level = 5)
    {
        var moveSet = new MoveSet();
        return new Player(
            new PlayerId(Guid.NewGuid()),
            name,
            level: level,
            exp: 0,
            jobLevel: 1,
            gold: 100,
            status: new Status(30, 10, 12, 8, 6, 3, 10),
            job: Job.Warrior,
            imagePath: "/images/player.png",
            moveSet: moveSet);
    }

    private static QuestStageDefinition CreateStage(int minPartyMemberCount, int maxPartyMemberCount, bool isActive, int recommendedLevel = 1, int? requiredMapUnlockFlag = null)
    {
        return new QuestStageDefinition(
            new QuestStageId(1),
            "quest-001",
            "Test Quest",
            "image/quest/test-battlefield.svg",
            recommendedLevel,
            minimumEntryLevel: null,
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
            equipmentRewards: [],
            itemRewards: [],
            isActive: isActive,
            requiredMapUnlockFlag: requiredMapUnlockFlag);
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

        public Task<QuestRun?> GetForResolutionAsync(QuestRunId id)
        {
            runs.TryGetValue(id.Value, out var run);
            return Task.FromResult(run);
        }

        public Task<QuestRoomId?> GetRoomIdAsync(QuestRunId id)
        {
            runs.TryGetValue(id.Value, out var run);
            return Task.FromResult<QuestRoomId?>(run?.RoomId);
        }

        public Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId)
        {
            var run = runs.Values.FirstOrDefault(x => x.RoomId == roomId);
            return Task.FromResult(run);
        }

        public Task<QuestRun?> GetActiveByPlayerAsync(PlayerId playerId)
            => Task.FromResult(HasActiveRunForAnyPlayer ? runs.Values.FirstOrDefault() : null);

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

        public Task SaveChatMessagesAsync(QuestRun run)
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

        public Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel)
        {
            var p = players.GetValueOrDefault(id);
            return Task.FromResult(p is not null && p.Level <= maxLevel ? p : null);
        }

        public Task<IReadOnlyList<Player>> GetPvpOpponentsAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null)
        {
            IReadOnlyList<Player> result = players.Values.Where(p => p.Id != excludeId && p.Level <= maxLevel).ToArray();
            return Task.FromResult(result);
        }

        public async Task<(IReadOnlyList<Player> Opponents, int TotalCount)> GetPvpOpponentsPageAsync(
            PlayerId excludeId,
            int maxLevel,
            int? offset = null,
            int? limit = null)
        {
            var opponents = await GetPvpOpponentsAsync(excludeId, maxLevel, offset, limit);
            var totalCount = await CountPvpOpponentsAsync(excludeId, maxLevel);
            return (opponents, totalCount);
        }

        public Task<int> CountPvpOpponentsAsync(PlayerId excludeId, int maxLevel)
        {
            var count = players.Values.Count(p => p.Id != excludeId && p.Level <= maxLevel);
            return Task.FromResult(count);
        }

        public Task<IReadOnlyList<Player>> GetAllAsync(int? offset = null, int? limit = null)
            => Task.FromResult<IReadOnlyList<Player>>(players.Values.ToArray());

        public Task<int> CountAllAsync()
            => Task.FromResult(players.Count);

        public Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids)
        {
            var idSet = ids.ToHashSet();
            return Task.FromResult<IReadOnlyList<Player>>(players.Values.Where(p => idSet.Contains(p.Id)).ToArray());
        }

        public async Task<(IReadOnlyList<Player> Players, int TotalCount)> GetPlayersPageExcludingAsync(
            PlayerId excludeId,
            int? offset = null,
            int? limit = null)
        {
            var allPlayers = await GetAllAsync();
            var filtered = allPlayers.Where(x => x.Id != excludeId).ToArray();
            var paged = filtered.AsEnumerable();

            if (offset is > 0)
            {
                paged = paged.Skip(offset.Value);
            }

            if (limit is > 0)
            {
                paged = paged.Take(limit.Value);
            }

            return (paged.ToArray(), filtered.Length);
        }

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

    private sealed class FakeMirrorCandidateRepository(IPlayerRepository playerRepository, params PlayerId[] ids)
        : IMirrorCandidateRepository
    {
        public IReadOnlyList<PlayerId> AllyIds { get; init; } = [];

        public Task<IReadOnlyList<PlayerId>> GetTopPlayerIdsWithTiesAsync(int count)
            => Task.FromResult<IReadOnlyList<PlayerId>>(ids.Take(count).ToArray());

        public Task<IReadOnlyList<Player>> GetPlayersWithMovesAsync(IEnumerable<PlayerId> playerIds)
            => playerRepository.GetPlayersAsync(playerIds);

        public Task<IReadOnlyList<PlayerId>> GetAllyPlayerIdsAsync(PlayerId excludedPlayerId, int level, int levelRange, int count)
            => Task.FromResult<IReadOnlyList<PlayerId>>(AllyIds.Where(x => x != excludedPlayerId).Take(count).ToArray());
    }

    private sealed class FakeMoveRepository : IMoveRepository
    {
        public Task<Move?> GetMoveAsync(MoveId moveId)
            => Task.FromResult<Move?>(null);

        public Task<IReadOnlyList<Move>> GetAllMovesAsync()
            => Task.FromResult<IReadOnlyList<Move>>([]);
    }

    private sealed class FakeQuestEndlessEnemyTemplateRepository : IQuestEndlessEnemyTemplateRepository
    {
        public Task<QuestEndlessEnemyTemplate?> GetAsync(QuestEnemyDefinitionId id)
            => Task.FromResult<QuestEndlessEnemyTemplate?>(null);

        public Task<IReadOnlyList<QuestEndlessEnemyTemplate>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<QuestEndlessEnemyTemplate>>([]);

        public Task<IReadOnlyList<QuestEndlessEnemyTemplate>> GetByThemeAsync(int themeNo)
            => Task.FromResult<IReadOnlyList<QuestEndlessEnemyTemplate>>([]);
    }
}
