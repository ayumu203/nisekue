using FluentAssertions;
using server.application.battle;
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
        result.IsLevelUp.Should().BeFalse();
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
    public async Task ExecuteTraining_WhenBothSurviveAfterThreeTurns_ReturnsDraw()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 5,
            exp: 0,
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

    private static TrainingService CreateService(IPlayerRepository playerRepository, ITrainingEnemyRepository enemyRepository)
    {
        return new TrainingService(
            playerRepository,
            enemyRepository,
            new FakeMoveRepository(),
            new FakeGrowthValueRepository(),
            new BattleService(),
            new TrainingBattleFactory(),
            new TrainingOutcomeJudge(),
            new TrainingExpCalculator());
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

    private sealed class FakePlayerRepository(Player player) : IPlayerRepository
    {
        private Player playerState = player;

        public bool SaveCalled { get; private set; }

        public Task<Player?> GetPlayerAsync(PlayerId id)
        {
            return Task.FromResult(playerState.Id == id ? playerState : null);
        }

        public Task<IReadOnlyList<Player>> GetAllAsync()
        {
            IReadOnlyList<Player> players = [playerState];
            return Task.FromResult(players);
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

    private sealed class FakeGrowthValueRepository : IGrowthValueRepository
    {
        public GrowthValue GetByJob(Job job)
        {
            return new GrowthValue(
                MaxHp: 1,
                MaxMp: 0,
                Strength: 1,
                Defense: 1,
                Intelligence: 1,
                Luck: 1,
                Speed: 1);
        }
    }

    private static Move CreateAttackMove(int moveId, string name, int mpCost, BuffStat? attackStat = null)
    {
        return new Move(
            new MoveId(moveId),
            name,
            $"{name}のテスト技",
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
