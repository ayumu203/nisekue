using FluentAssertions;
using server.application.battle;
using server.application.player;
using server.application.training;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.training;
using Xunit;

namespace server.tests;

public class TrainingServiceTests
{
    private const int PhysicalAttackMoveId = 1001;
    private const int MagicAttackMoveId = 1002;
    private const int HighCostAttackMoveId = 1003;

    [Fact]
    public async Task ExecuteTraining_WithValidPlayerAndEnemy_ReturnsWinResultAndSavesPlayer()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 10, strength: 12, defense: 8, intelligence: 5, luck: 2, speed: 10),
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 5,
            status: new Status(maxHp: 20, maxMp: 0, strength: 8, defense: 3, intelligence: 2, luck: 1, speed: 5));

        var playerRepository = new FakePlayerRepository(player);
        var service = CreateService(playerRepository, new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.TrainingResult.Should().Be("Win");
        result.Turn.Should().Be(3);
        result.CurrentPlayerHp.Should().Be(28);
        result.CurrentEnemyHp.Should().Be(0);
        result.Exp.Should().Be(12);
        result.IsPlayerLevelUp.Should().BeFalse();
        result.IsJobLevelUp.Should().BeTrue();
        playerRepository.SaveCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTraining_WhenSpeedIsEqual_PlayerActsFirst()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 1,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 50, maxMp: 0, strength: 30, defense: 5, intelligence: 0, luck: 0, speed: 10),
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 10, maxMp: 0, strength: 20, defense: 0, intelligence: 0, luck: 0, speed: 10));

        var service = CreateService(new FakePlayerRepository(player), new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.TrainingResult.Should().Be("Win");
        result.Turn.Should().Be(1);
        result.CurrentPlayerHp.Should().Be(player.Status.MaxHp);
        result.CurrentEnemyHp.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteTraining_WhenPlayerActsFirstAndDefeatsEnemy_EnemyDoesNotCounterattack()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 1,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 50, maxMp: 0, strength: 30, defense: 5, intelligence: 0, luck: 0, speed: 10),
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 10, maxMp: 0, strength: 20, defense: 0, intelligence: 0, luck: 0, speed: 1));

        var service = CreateService(new FakePlayerRepository(player), new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.Turn.Should().Be(1);
        result.CurrentEnemyHp.Should().Be(0);
        result.CurrentPlayerHp.Should().Be(player.Status.MaxHp);
    }

    [Fact]
    public async Task ExecuteTraining_WhenEnemyActsFirstAndDefeatsPlayer_PlayerDoesNotCounterattack()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 1,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 10, maxMp: 0, strength: 25, defense: 0, intelligence: 0, luck: 0, speed: 1),
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 30, maxMp: 0, strength: 30, defense: 0, intelligence: 0, luck: 0, speed: 10));

        var service = CreateService(new FakePlayerRepository(player), new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.TrainingResult.Should().Be("Lose");
        result.Turn.Should().Be(1);
        result.CurrentPlayerHp.Should().Be(0);
        result.CurrentEnemyHp.Should().Be(enemy.Status.MaxHp);
    }

    [Fact]
    public async Task ExecuteTraining_WhenPlayerUsesIntelligenceBuild_UsesIntelligenceScaledDamage()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Mage",
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 10, strength: 2, defense: 5, intelligence: 10, luck: 0, speed: 10),
            moveSet: CreateMoveSet(MagicAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Sage Dummy",
            imagePath: "/image/training/02_mage.png",
            level: 5,
            status: new Status(maxHp: 14, maxMp: 0, strength: 1, defense: 9, intelligence: 9, luck: 0, speed: 5));

        var service = CreateService(new FakePlayerRepository(player), new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [MagicAttackMoveId, MagicAttackMoveId, MagicAttackMoveId]);

        result.TrainingResult.Should().Be("Win");
        result.Turn.Should().Be(3);
        result.CurrentEnemyHp.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteTraining_WithEquippedWeapon_AppliesEffectiveStatusAndConsumesDurability()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 1,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 20, maxMp: 0, strength: 7, defense: 5, intelligence: 0, luck: 0, speed: 10),
            job: Job.Apprentice,
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 10, maxMp: 0, strength: 1, defense: 0, intelligence: 0, luck: 0, speed: 1));
        var equipment = new Equipment(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            20,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            equipment.Id,
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: 10,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var equipmentRepository = new FakeEquipmentRepository(equipment);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);

        var service = CreateService(
            new FakePlayerRepository(player),
            new FakeTrainingEnemyRepository(enemy),
            playerEquipmentRepository,
            equipmentRepository);

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.TrainingResult.Should().Be("Win");
        result.Turn.Should().Be(1);
        playerEquipmentRepository.StoredEquipments.Should().ContainSingle();
        playerEquipmentRepository.StoredEquipments[0].Durability.Should().Be(9);
    }

    [Fact]
    public async Task ExecuteTraining_WithInventoryEquipment_DoesNotConsumeDurability()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 1,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 20, maxMp: 0, strength: 7, defense: 5, intelligence: 0, luck: 0, speed: 10),
            job: Job.Apprentice,
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 10, maxMp: 0, strength: 1, defense: 0, intelligence: 0, luck: 0, speed: 1));
        var equipment = new Equipment(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            20,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            equipment.Id,
            EquipmentType.Weapon,
            EquipmentStatus.Inventory,
            durability: 10,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);

        var service = CreateService(
            new FakePlayerRepository(player),
            new FakeTrainingEnemyRepository(enemy),
            playerEquipmentRepository,
            new FakeEquipmentRepository(equipment));

        await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        playerEquipmentRepository.StoredEquipments.Should().ContainSingle();
        playerEquipmentRepository.StoredEquipments[0].Durability.Should().Be(10);
        playerEquipmentRepository.StoredEquipments[0].Status.Should().Be(EquipmentStatus.Inventory);
    }

    [Fact]
    public async Task ExecuteTraining_WithLastDurabilityEquipment_MarksBroken()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 1,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 20, maxMp: 0, strength: 7, defense: 5, intelligence: 0, luck: 0, speed: 10),
            job: Job.Apprentice,
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 10, maxMp: 0, strength: 1, defense: 0, intelligence: 0, luck: 0, speed: 1));
        var equipment = new Equipment(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            20,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            equipment.Id,
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: 1,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);

        var service = CreateService(
            new FakePlayerRepository(player),
            new FakeTrainingEnemyRepository(enemy),
            playerEquipmentRepository,
            new FakeEquipmentRepository(equipment));

        await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        playerEquipmentRepository.StoredEquipments.Should().ContainSingle();
        playerEquipmentRepository.StoredEquipments[0].Durability.Should().Be(0);
        playerEquipmentRepository.StoredEquipments[0].Status.Should().Be(EquipmentStatus.Broken);
    }

    [Fact]
    public async Task ExecuteTraining_WhenEquippedWeaponLevelGapIsWithinThreshold_IncreasesWeaponMastery()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 10,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 20, maxMp: 0, strength: 12, defense: 5, intelligence: 0, luck: 0, speed: 10),
            job: Job.Apprentice,
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 17,
            status: new Status(maxHp: 10, maxMp: 0, strength: 1, defense: 0, intelligence: 0, luck: 0, speed: 1));
        var equipment = new Equipment(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            20,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            equipment.Id,
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: 10,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);

        var service = CreateService(
            new FakePlayerRepository(player),
            new FakeTrainingEnemyRepository(enemy),
            playerEquipmentRepository,
            new FakeEquipmentRepository(equipment));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.WeaponMasteryDelta.Should().Be(1);
        playerEquipmentRepository.StoredEquipments[0].Mastery.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTraining_WhenEquippedWeaponLevelGapExceedsThreshold_DoesNotIncreaseWeaponMastery()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 10,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 20, maxMp: 0, strength: 12, defense: 5, intelligence: 0, luck: 0, speed: 10),
            job: Job.Apprentice,
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 18,
            status: new Status(maxHp: 10, maxMp: 0, strength: 1, defense: 0, intelligence: 0, luck: 0, speed: 1));
        var equipment = new Equipment(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            20,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            equipment.Id,
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: 10,
            mastery: 0,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);

        var service = CreateService(
            new FakePlayerRepository(player),
            new FakeTrainingEnemyRepository(enemy),
            playerEquipmentRepository,
            new FakeEquipmentRepository(equipment));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.WeaponMasteryDelta.Should().Be(0);
        playerEquipmentRepository.StoredEquipments[0].Mastery.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteTraining_WhenEquippedWeaponAlreadyAtMasteryCap_DoesNotIncreaseWeaponMastery()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 10,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 20, maxMp: 0, strength: 12, defense: 5, intelligence: 0, luck: 0, speed: 10),
            job: Job.Apprentice,
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 10,
            status: new Status(maxHp: 10, maxMp: 0, strength: 1, defense: 0, intelligence: 0, luck: 0, speed: 1));
        var equipment = new Equipment(
            new EquipmentId(1001),
            "旅立ちの剣",
            "旅路の始まりを告げる剣。",
            EquipmentType.Weapon,
            10,
            5,
            5,
            new EquipmentStatusBonus(0, 0, 3, 0, 0, 0, 0),
            new HashSet<Job> { Job.Apprentice });
        var playerEquipment = new PlayerEquipment(
            PlayerEquipmentId.New(),
            playerId,
            equipment.Id,
            EquipmentType.Weapon,
            EquipmentStatus.Equipped,
            durability: 10,
            mastery: 5,
            acquiredAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);
        var playerEquipmentRepository = new FakePlayerEquipmentRepository(playerEquipment);

        var service = CreateService(
            new FakePlayerRepository(player),
            new FakeTrainingEnemyRepository(enemy),
            playerEquipmentRepository,
            new FakeEquipmentRepository(equipment));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.WeaponMasteryDelta.Should().Be(0);
        playerEquipmentRepository.StoredEquipments[0].Mastery.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteTraining_WhenBothSurviveAfterThreeTurns_ReturnsDraw()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 0, strength: 8, defense: 8, intelligence: 0, luck: 0, speed: 10),
            moveSet: CreateMoveSet(PhysicalAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 5,
            status: new Status(maxHp: 30, maxMp: 0, strength: 8, defense: 8, intelligence: 0, luck: 0, speed: 5));

        var service = CreateService(new FakePlayerRepository(player), new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [PhysicalAttackMoveId, PhysicalAttackMoveId, PhysicalAttackMoveId]);

        result.TrainingResult.Should().Be("Draw");
        result.Turn.Should().Be(3);
        result.CurrentPlayerHp.Should().Be(27);
        result.CurrentEnemyHp.Should().Be(27);
        result.Exp.Should().Be(4);
    }

    [Fact]
    public async Task ExecuteTraining_WhenPlayerDoesNotHaveEnoughMp_WaitsForThatTurn()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Mage",
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 5, strength: 2, defense: 5, intelligence: 10, luck: 0, speed: 10),
            moveSet: CreateMoveSet(MagicAttackMoveId, HighCostAttackMoveId));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 5,
            status: new Status(maxHp: 20, maxMp: 0, strength: 8, defense: 3, intelligence: 2, luck: 1, speed: 5));

        var service = CreateService(new FakePlayerRepository(player), new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [MagicAttackMoveId, HighCostAttackMoveId, MagicAttackMoveId]);

        result.TrainingResult.Should().Be("Draw");
        result.Turn.Should().Be(3);
        result.CurrentPlayerHp.Should().Be(21);
        result.CurrentEnemyHp.Should().Be(12);
        result.Exp.Should().Be(6);
    }

    [Fact]
    public async Task ExecuteTraining_WhenPlayerSelectsNormalAttack_UsesTrainingNormalAttack()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Battler",
            level: 5,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 0, strength: 10, defense: 8, intelligence: 2, luck: 0, speed: 10));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 5,
            status: new Status(maxHp: 20, maxMp: 0, strength: 8, defense: 3, intelligence: 2, luck: 1, speed: 5));

        var service = CreateService(new FakePlayerRepository(player), new FakeTrainingEnemyRepository(enemy));

        var result = await service.ExecuteTraining(playerId, enemy.Id, [null, null, null]);

        result.TrainingResult.Should().Be("Win");
        result.Turn.Should().Be(3);
        result.CurrentPlayerHp.Should().Be(28);
        result.CurrentEnemyHp.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenWinWithSmallLevelDifference_ReturnsExpectedExperience()
    {
        var calculator = new TrainingExpCalculator();
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 10,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 0, strength: 10, defense: 5, intelligence: 8, luck: 0, speed: 5));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 12,
            status: new Status(maxHp: 55, maxMp: 0, strength: 8, defense: 5, intelligence: 3, luck: 0, speed: 4));

        var metrics = new TrainingContributionMetrics(
            PlayerDealtTotalDamage: 40,
            PlayerEffectiveHealTotal: 0,
            CurrentPlayerHp: player.Status.MaxHp);
        var exp = calculator.Calculate(player, enemy, metrics, TrainingOutcome.Win);

        exp.Should().Be(25);
    }

    [Fact]
    public void Calculate_WhenLoseWithLargeLevelDifference_ReturnsContributionBasedExperience()
    {
        var calculator = new TrainingExpCalculator();
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 1,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 0, strength: 5, defense: 2, intelligence: 3, luck: 0, speed: 3));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 30,
            status: new Status(maxHp: 100, maxMp: 0, strength: 20, defense: 10, intelligence: 10, luck: 0, speed: 10));

        var metrics = new TrainingContributionMetrics(
            PlayerDealtTotalDamage: 1,
            PlayerEffectiveHealTotal: 0,
            CurrentPlayerHp: 0);
        var exp = calculator.Calculate(player, enemy, metrics, TrainingOutcome.Lose);

        exp.Should().Be(19);
    }

    [Fact]
    public void Calculate_WhenHealContributionIsPresent_IncreasesExperience()
    {
        var calculator = new TrainingExpCalculator();
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Priest",
            level: 10,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 40, maxMp: 20, strength: 4, defense: 6, intelligence: 10, luck: 0, speed: 6));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 10,
            status: new Status(maxHp: 40, maxMp: 0, strength: 8, defense: 5, intelligence: 3, luck: 0, speed: 4));

        var metrics = new TrainingContributionMetrics(
            PlayerDealtTotalDamage: 20,
            PlayerEffectiveHealTotal: 20,
            CurrentPlayerHp: 10);
        var exp = calculator.Calculate(player, enemy, metrics, TrainingOutcome.Draw);

        exp.Should().Be(14);
    }

    [Fact]
    public void Calculate_WhenComputedExpIsZero_ReturnsMinimumExperience()
    {
        var calculator = new TrainingExpCalculator();
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 50,
            exp: 0,
            jobLevel: 1,
            jobExp: 0,
            gold: 100,
            status: new Status(maxHp: 30, maxMp: 0, strength: 10, defense: 10, intelligence: 10, luck: 0, speed: 10));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Slime",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 10, maxMp: 0, strength: 1, defense: 1, intelligence: 1, luck: 0, speed: 1));

        var metrics = new TrainingContributionMetrics(
            PlayerDealtTotalDamage: 0,
            PlayerEffectiveHealTotal: 0,
            CurrentPlayerHp: 0);
        var exp = calculator.Calculate(player, enemy, metrics, TrainingOutcome.Lose);

        exp.Should().Be(1);
    }

    private static TrainingService CreateService(
        IPlayerRepository playerRepository,
        ITrainingEnemyRepository enemyRepository,
        IPlayerEquipmentRepository? playerEquipmentRepository = null,
        IEquipmentRepository? equipmentRepository = null)
    {
        return new TrainingService(
            playerRepository,
            playerEquipmentRepository ?? new FakePlayerEquipmentRepository(),
            equipmentRepository ?? new FakeEquipmentRepository(),
            enemyRepository,
            new FakeMoveRepository(),
            new FakeJobProfileRepository(),
            new FakeJobMoveLearningRuleRepository(),
            new PlayerJobService(
                playerRepository,
                new FakeJobProfileRepository(),
                new FakeJobMoveLearningRuleRepository(),
                new FakeMoveRepository()),
            new BattleService(),
            new TrainingBattleFactory(),
            new TrainingOutcomeJudge(),
            new TrainingExpCalculator(),
            new TrainingWeaponMasteryPolicy(),
            new EquipmentStatusResolver());
    }

    private static MoveSet CreateMoveSet(params int[] moveIds)
    {
        var slots = new MoveId?[MoveSet.MaxSlots];
        for (var i = 0; i < moveIds.Length; i++)
        {
            slots[i] = new MoveId(moveIds[i]);
        }

        return new MoveSet(slots);
    }

    private sealed class FakeMoveRepository : IMoveRepository
    {
        private readonly IReadOnlyDictionary<int, Move> moveById = new Dictionary<int, Move>
        {
            [PhysicalAttackMoveId] = CreateAttackMove(PhysicalAttackMoveId, "斬る", mpCost: 0),
            [MagicAttackMoveId] = CreateAttackMove(MagicAttackMoveId, "火球", mpCost: 3, attackStat: BuffStat.Intelligence),
            [HighCostAttackMoveId] = CreateAttackMove(HighCostAttackMoveId, "大火球", mpCost: 6, attackStat: BuffStat.Intelligence)
        };

        public Task<Move?> GetMoveAsync(MoveId moveId)
        {
            moveById.TryGetValue(moveId.Id, out var move);
            return Task.FromResult(move);
        }

        public Task<IReadOnlyList<Move>> GetAllMovesAsync()
        {
            return Task.FromResult((IReadOnlyList<Move>)moveById.Values.ToArray());
        }
    }

    private sealed class FakePlayerEquipmentRepository(params PlayerEquipment[] equipments) : IPlayerEquipmentRepository
    {
        private readonly Dictionary<PlayerId, List<PlayerEquipment>> equipmentsByPlayerId = equipments
            .GroupBy(x => x.PlayerId)
            .ToDictionary(x => x.Key, x => x.ToList());

        public IReadOnlyList<PlayerEquipment> StoredEquipments => equipmentsByPlayerId.Values.SelectMany(x => x).ToArray();

        public Task<IReadOnlyList<PlayerEquipment>> GetByPlayerAsync(PlayerId playerId)
            => Task.FromResult((IReadOnlyList<PlayerEquipment>)(equipmentsByPlayerId.TryGetValue(playerId, out var playerEquipments)
                ? playerEquipments.ToArray()
                : []));

        public Task<IReadOnlyList<PlayerEquipment>> GetEquippedByPlayerAsync(PlayerId playerId)
        {
            var all = equipmentsByPlayerId.TryGetValue(playerId, out var playerEquipments)
                ? playerEquipments.ToArray()
                : [];
            return Task.FromResult((IReadOnlyList<PlayerEquipment>)all.Where(x => x.Status == EquipmentStatus.Equipped).ToArray());
        }

        public Task<PlayerEquipment?> GetAsync(PlayerEquipmentId playerEquipmentId)
            => Task.FromResult(StoredEquipments.FirstOrDefault(x => x.Id == playerEquipmentId));

        public Task SaveAsync(IReadOnlyList<PlayerEquipment> playerEquipments)
        {
            foreach (var group in playerEquipments.GroupBy(x => x.PlayerId))
            {
                equipmentsByPlayerId[group.Key] = group.ToList();
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(PlayerEquipmentId playerEquipmentId)
        {
            foreach (var group in equipmentsByPlayerId.ToArray())
            {
                var filtered = group.Value.Where(x => x.Id != playerEquipmentId).ToList();
                if (filtered.Count != group.Value.Count)
                {
                    equipmentsByPlayerId[group.Key] = filtered;
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeEquipmentRepository(params Equipment[] equipments) : IEquipmentRepository
    {
        private readonly IReadOnlyList<Equipment> storedEquipments = equipments;

        public Task<Equipment?> GetAsync(EquipmentId id)
            => Task.FromResult(storedEquipments.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Equipment>> GetAllAsync()
            => Task.FromResult(storedEquipments);
    }

    private sealed class FakePlayerRepository(Player player) : IPlayerRepository
    {
        private Player playerState = player;

        public bool SaveCalled { get; private set; }

        public Task<Player?> GetPlayerAsync(PlayerId id)
        {
            return Task.FromResult(playerState.Id == id ? playerState : null);
        }

        public Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel)
        {
            return Task.FromResult(playerState.Id == id && playerState.Level <= maxLevel ? playerState : null);
        }

        public Task<IReadOnlyList<Player>> GetAllAsync()
        {
            IReadOnlyList<Player> players = [playerState];
            return Task.FromResult(players);
        }

        public Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids)
        {
            var idSet = ids.ToHashSet();
            IReadOnlyList<Player> result = idSet.Contains(playerState.Id) ? [playerState] : [];
            return Task.FromResult(result);
        }

        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown)
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }

        public Task<bool> UpdateNameAsync(PlayerId id, string name)
        {
            if (playerState.Id != id)
            {
                return Task.FromResult(false);
            }

            playerState.UpdateName(name);
            return Task.FromResult(true);
        }

        public Task SaveAsync(Player player)
        {
            SaveCalled = true;
            playerState = player;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTrainingEnemyRepository(TrainingEnemy enemy) : ITrainingEnemyRepository
    {
        public Task<TrainingEnemy?> GetTrainingEnemyAsync(TrainingEnemyId id)
        {
            return Task.FromResult(enemy.Id == id ? enemy : null);
        }

        public Task<IReadOnlyList<TrainingEnemy>> GetTrainingEnemiesAsync()
        {
            IReadOnlyList<TrainingEnemy> list = [enemy];
            return Task.FromResult(list);
        }
    }

    private sealed class FakeJobProfileRepository : IJobProfileRepository
    {
        public JobProfile GetByJob(Job job)
        {
            return new JobProfile(
                job,
                description: $"{job} profile",
                masterLevel: 5,
                requiredMasterJobs: [],
                growthValue: new GrowthValue(
                    MaxHp: 1,
                    MaxMp: 0,
                    Strength: 1,
                    Defense: 1,
                    Intelligence: 1,
                    Luck: 1,
                    Speed: 1));
        }

        public IReadOnlyList<JobProfile> GetAll() => [GetByJob(Job.Apprentice)];
    }

    private sealed class FakeJobMoveLearningRuleRepository : IJobMoveLearningRuleRepository
    {
        public JobMoveLearningRule GetByJob(Job job)
        {
            return new JobMoveLearningRule(job, []);
        }
    }

    private static Move CreateAttackMove(int moveId, string name, int mpCost, BuffStat? attackStat = null)
    {
        return new Move(
            new MoveId(moveId),
            name,
            $"{name}のテストスキル",
            TargetType.Enemy,
            AttackRange.Single,
            mpCost,
            0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(moveId),
                    new MoveId(moveId),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1m, 0, 0m, ElementType.None, attackStat))
            ]);
    }
}
