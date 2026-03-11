using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerTests
{
    // レベルが0ではプレイヤーの初期化でエラーが出る.
    [Fact]
    public void Constructor_WhenLevelIsZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => CreatePlayer(level: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    // レベルが0未満ではプレイヤーの初期化でエラーが出る.
    [Fact]
    public void Constructor_WhenLevelIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var act = () => CreatePlayer(level: -5);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
    // レベルが1以上ならok
    [Fact]
    public void Constructor_WhenLevelIsPositive_KeepsOriginalValue()
    {
        var player = CreatePlayer(level: 1);

        player.Level.Should().Be(1);
    }
    // レベルが1以上ならok2
    [Fact]
    public void Constructor_WhenLevelIsPositive_KeepsOriginalValue2()
    {
        var player = CreatePlayer(level: 5);

        player.Level.Should().Be(5);
    }

    // 経験値が閾値に達していない場合は、レベルもステータスも変化しない.
    [Fact]
    public void LevelUp_WhenExpIsInsufficient_ReturnsFalseAndKeepsValues()
    {
        var player = CreatePlayer(level: 1, exp: 9, job: Job.Warrior);
        var beforeStatus = player.Status;
        var growthValueRepository = new FakeGrowthValueRepository();

        var isLevelUp = player.LevelUp(growthValueRepository);

        isLevelUp.Should().BeFalse();
        player.Level.Should().Be(1);
        player.Exp.Should().Be(9);
        player.Status.Should().Be(beforeStatus);
    }

    // 1回レベルアップ時に経験値が消費され、ジョブ成長値が反映される.
    [Theory]
    [InlineData(Job.Warrior, 3, 0, 2, 1, 0, 0, 1)]
    [InlineData(Job.Mage, 0, 4, 0, 0, 3, 1, 0)]
    public void LevelUp_WhenExpReachesThreshold_ConsumesExpAndAppliesGrowth(
        Job job,
        int addHp,
        int addMp,
        int addStrength,
        int addDefense,
        int addIntelligence,
        int addLuck,
        int addSpeed)
    {
        var player = CreatePlayer(level: 1, exp: 10, job: job);
        var beforeStatus = player.Status;
        var growthValueRepository = new FakeGrowthValueRepository();

        var isLevelUp = player.LevelUp(growthValueRepository);

        isLevelUp.Should().BeTrue();
        player.Level.Should().Be(2);
        player.Exp.Should().Be(0);
        player.Status.MaxHp.Should().Be(beforeStatus.MaxHp + addHp);
        player.Status.MaxMp.Should().Be(beforeStatus.MaxMp + addMp);
        player.Status.Strength.Should().Be(beforeStatus.Strength + addStrength);
        player.Status.Defense.Should().Be(beforeStatus.Defense + addDefense);
        player.Status.Intelligence.Should().Be(beforeStatus.Intelligence + addIntelligence);
        player.Status.Luck.Should().Be(beforeStatus.Luck + addLuck);
        player.Status.Speed.Should().Be(beforeStatus.Speed + addSpeed);
    }

    // 複数回レベルアップ時は、成長値がレベルアップ回数分だけ加算される.
    [Fact]
    public void LevelUp_WhenExpAllowsMultipleLevelUps_AppliesGrowthPerLevel()
    {
        var player = CreatePlayer(level: 1, exp: 30, job: Job.Warrior);
        var beforeStatus = player.Status;
        var growthValueRepository = new FakeGrowthValueRepository();

        var isLevelUp = player.LevelUp(growthValueRepository);

        isLevelUp.Should().BeTrue();
        player.Level.Should().Be(3);
        player.Exp.Should().Be(0);
        player.Status.MaxHp.Should().Be(beforeStatus.MaxHp + (3 * 2));
        player.Status.MaxMp.Should().Be(beforeStatus.MaxMp + (0 * 2));
        player.Status.Strength.Should().Be(beforeStatus.Strength + (2 * 2));
        player.Status.Defense.Should().Be(beforeStatus.Defense + (1 * 2));
        player.Status.Intelligence.Should().Be(beforeStatus.Intelligence + (0 * 2));
        player.Status.Luck.Should().Be(beforeStatus.Luck + (0 * 2));
        player.Status.Speed.Should().Be(beforeStatus.Speed + (1 * 2));
    }

    private static Player CreatePlayer(int level, int exp = 0, Job job = Job.Apprentice) =>
        new(
            new PlayerId(Guid.NewGuid()),
            name: "Tester",
            level: level,
            exp: exp,
            status: new Status(
                maxHp: 10,
                maxMp: 0,
                strength: 1,
                defense: 1,
                intelligence: 1,
                luck: 1,
                speed: 1),
            job: job);

    private sealed class FakeGrowthValueRepository : IGrowthValueRepository
    {
        public GrowthValue GetByJob(Job job)
        {
            return job switch
            {
                Job.Warrior => new GrowthValue(MaxHp: 3, MaxMp: 0, Strength: 2, Defense: 1, Intelligence: 0, Luck: 0, Speed: 1),
                Job.Mage => new GrowthValue(MaxHp: 0, MaxMp: 4, Strength: 0, Defense: 0, Intelligence: 3, Luck: 1, Speed: 0),
                _ => new GrowthValue(MaxHp: 1, MaxMp: 1, Strength: 1, Defense: 1, Intelligence: 1, Luck: 1, Speed: 1),
            };
        }
    }
}
