using FluentAssertions;
using server.domain.pet;
using server.domain.player;
using server.domain.quest;
using Xunit;

namespace server.tests.pet;

public class PlayerPetTests
{
    [Fact]
    public void Capture_CreatesNonStandbyPetWithZeroBonus()
    {
        var playerId = new PlayerId(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        var pet = PlayerPet.Capture(playerId, new QuestEnemyDefinitionId(7), now);

        pet.PlayerId.Should().Be(playerId);
        pet.EnemyDefinitionId.Should().Be(new QuestEnemyDefinitionId(7));
        pet.IsStandby.Should().BeFalse();
        pet.BonusStatus.MaxHp.Should().Be(0);
        pet.BonusStatus.Speed.Should().Be(0);
        pet.CapturedAt.Should().Be(now);
        pet.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void Train_AddsOnePercentOfPlayerBaseStatus()
    {
        var pet = CreatePet();
        var playerStatus = new Status(maxHp: 500, maxMp: 300, strength: 250, defense: 200, intelligence: 150, luck: 120, speed: 100);

        pet.Train(playerStatus, DateTimeOffset.UtcNow);

        pet.BonusStatus.MaxHp.Should().Be(5);
        pet.BonusStatus.MaxMp.Should().Be(3);
        pet.BonusStatus.Strength.Should().Be(2);
        pet.BonusStatus.Defense.Should().Be(2);
        pet.BonusStatus.Intelligence.Should().Be(1);
        pet.BonusStatus.Luck.Should().Be(1);
        pet.BonusStatus.Speed.Should().Be(1);
    }

    [Fact]
    public void Train_WithStatusBelowOneHundred_FloorsGainToZero()
    {
        var pet = CreatePet();
        var playerStatus = new Status(maxHp: 199, maxMp: 99, strength: 99, defense: 99, intelligence: 99, luck: 99, speed: 99);

        pet.Train(playerStatus, DateTimeOffset.UtcNow);

        pet.BonusStatus.MaxHp.Should().Be(1);
        pet.BonusStatus.Strength.Should().Be(0, "1%未満は切り捨てられる");
    }

    [Fact]
    public void Train_CalledTwice_AccumulatesBonus()
    {
        var pet = CreatePet();
        var playerStatus = new Status(maxHp: 500, maxMp: 300, strength: 250, defense: 200, intelligence: 150, luck: 120, speed: 100);

        pet.Train(playerStatus, DateTimeOffset.UtcNow);
        pet.Train(playerStatus, DateTimeOffset.UtcNow);

        pet.BonusStatus.MaxHp.Should().Be(10);
        pet.BonusStatus.Strength.Should().Be(4);
    }

    [Fact]
    public void Train_UpdatesUpdatedAt()
    {
        var pet = CreatePet();
        var trainedAt = DateTimeOffset.UtcNow.AddMinutes(5);

        pet.Train(new Status(100, 0, 1, 1, 1, 1, 1), trainedAt);

        pet.UpdatedAt.Should().Be(trainedAt);
    }

    [Fact]
    public void Standby_SetsIsStandby()
    {
        var pet = CreatePet();
        var standbiedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        pet.Standby(standbiedAt);

        pet.IsStandby.Should().BeTrue();
        pet.UpdatedAt.Should().Be(standbiedAt);
    }

    [Fact]
    public void ClearStandby_ClearsIsStandby()
    {
        var pet = CreatePet();
        pet.Standby(DateTimeOffset.UtcNow);

        pet.ClearStandby(DateTimeOffset.UtcNow.AddMinutes(1));

        pet.IsStandby.Should().BeFalse();
    }

    private static PlayerPet CreatePet()
    {
        return PlayerPet.Capture(new PlayerId(Guid.NewGuid()), new QuestEnemyDefinitionId(1), DateTimeOffset.UtcNow);
    }
}
