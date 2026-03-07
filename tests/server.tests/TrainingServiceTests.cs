using FluentAssertions;
using server.application.training;
using server.domain.player;
using server.domain.training;
using Xunit;

namespace server.tests;

public class TrainingServiceTests
{
    // 有効なプレイヤーと敵で訓練を実行すると、勝利結果を返してプレイヤー保存が呼ばれることを確認する。
    [Fact]
    public async Task ExecuteTraining_WithValidPlayerAndEnemy_ReturnsWinResultAndSavesPlayer()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var player = new Player(
            playerId,
            name: "Tester",
            level: 5,
            exp: 0,
            status: new Status(maxHp: 30, maxMp: 10, strength: 12, defense: 8, intelligence: 5, luck: 2, speed: 10));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 5,
            status: new Status(maxHp: 20, maxMp: 0, strength: 8, defense: 3, intelligence: 2, luck: 1, speed: 5));

        var playerRepository = new FakePlayerRepository(player);
        var enemyRepository = new FakeTrainingEnemyRepository(enemy);
        var service = new TrainingService(playerRepository, enemyRepository);

        var result = await service.ExecuteTraining(playerId, enemy.Id);

        result.TrainingResult.Should().Be("Win");
        result.Turn.Should().Be(3);
        result.Exp.Should().BeGreaterThan(0);
        result.IsLevelUp.Should().BeFalse();
        playerRepository.SaveCalled.Should().BeTrue();
    }

    // プレイヤーが先行して敵を撃破したターンでは、敵の反撃が発生しないことを確認する。
    [Fact]
    public void ExecuteTurn_WhenPlayerActsFirstAndDefeatsEnemy_EnemyDoesNotCounterattack()
    {
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 1,
            exp: 0,
            status: new Status(maxHp: 50, maxMp: 0, strength: 30, defense: 5, intelligence: 0, luck: 0, speed: 10));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 10, maxMp: 0, strength: 20, defense: 0, intelligence: 0, luck: 0, speed: 1));

        var before = new TurnResult(Turn: 0, CurrentPlayerHp: player.Status.MaxHp, CurrentEnemyHp: enemy.Status.MaxHp);

        var after = TrainingService.ExecuteTurn(player, enemy, before);

        after.Turn.Should().Be(1);
        after.CurrentEnemyHp.Should().Be(0);
        after.CurrentPlayerHp.Should().Be(player.Status.MaxHp);
    }

    // 敵が先行してプレイヤーを撃破したターンでは、プレイヤーの反撃が発生しないことを確認する。
    [Fact]
    public void ExecuteTurn_WhenEnemyActsFirstAndDefeatsPlayer_PlayerDoesNotCounterattack()
    {
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 1,
            exp: 0,
            status: new Status(maxHp: 10, maxMp: 0, strength: 25, defense: 0, intelligence: 0, luck: 0, speed: 1));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 30, maxMp: 0, strength: 30, defense: 0, intelligence: 0, luck: 0, speed: 10));

        var before = new TurnResult(Turn: 0, CurrentPlayerHp: player.Status.MaxHp, CurrentEnemyHp: enemy.Status.MaxHp);

        var after = TrainingService.ExecuteTurn(player, enemy, before);

        after.Turn.Should().Be(1);
        after.CurrentPlayerHp.Should().Be(0);
        after.CurrentEnemyHp.Should().Be(enemy.Status.MaxHp);
    }

    // 1ターンで双方が生存する場合、双方のHPが減少してターン数が進むことを確認する。
    [Fact]
    public void ExecuteTurn_WhenBothSurvive_BothHpDecreaseAndTurnAdvances()
    {
        var player = new Player(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: 1,
            exp: 0,
            status: new Status(maxHp: 50, maxMp: 0, strength: 10, defense: 8, intelligence: 0, luck: 0, speed: 10));

        var enemy = new TrainingEnemy(
            new TrainingEnemyId(1),
            name: "Enemy",
            imagePath: "/image/training/01_heishi.png",
            level: 1,
            status: new Status(maxHp: 40, maxMp: 0, strength: 9, defense: 7, intelligence: 0, luck: 0, speed: 5));

        var before = new TurnResult(Turn: 0, CurrentPlayerHp: player.Status.MaxHp, CurrentEnemyHp: enemy.Status.MaxHp);

        var after = TrainingService.ExecuteTurn(player, enemy, before);

        after.Turn.Should().Be(1);
        after.CurrentEnemyHp.Should().Be(37);
        after.CurrentPlayerHp.Should().Be(49);
    }

    // 勝利かつレベル差が小さい条件で、経験値が期待値どおりに計算されることを確認する。
    [Fact]
    public void CalcExp_WhenWinWithSmallLevelDifference_ReturnsExpectedExperience()
    {
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

        var turnResult = new TurnResult(Turn: 2, CurrentPlayerHp: 10, CurrentEnemyHp: 15);

        var exp = TrainingService.CalcExp(player, enemy, turnResult);

        exp.Should().Be(6);
    }

    // 敗北かつレベル差が大きい条件で、経験値が最低値で計算されることを確認する。
    [Fact]
    public void CalcExp_WhenLoseWithLargeLevelDifference_ReturnsMinimumExperience()
    {
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

        var turnResult = new TurnResult(Turn: 3, CurrentPlayerHp: 0, CurrentEnemyHp: 99);

        var exp = TrainingService.CalcExp(player, enemy, turnResult);

        exp.Should().Be(1);
    }

    private sealed class FakePlayerRepository(Player player) : IPlayerRepository
    {
        private Player playerState = player;

        public bool SaveCalled { get; private set; }

        public Task<Player?> GetPlayerAsync(PlayerId id)
        {
            return Task.FromResult(playerState.Id == id ? playerState : null);
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
            IReadOnlyList<TrainingEnemy> list = new[] { enemy };
            return Task.FromResult(list);
        }
    }
}
