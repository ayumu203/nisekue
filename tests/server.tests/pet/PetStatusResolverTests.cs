using FluentAssertions;
using server.domain.pet;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.pet;

public class PetStatusResolverTests
{
    [Fact]
    public void Resolve_SumsEnemyBaseStatusAndTrainedBonus()
    {
        var definition = CreateDefinition(new QuestEnemyDefinitionId(1));
        var pet = new PlayerPet(
            PlayerPetId.New(),
            new PlayerId(Guid.NewGuid()),
            definition.Id,
            new PetBonusStatus(maxHp: 10, maxMp: 9, strength: 8, defense: 7, intelligence: 6, luck: 5, speed: 4),
            isStandby: false,
            capturedAt: DateTimeOffset.UtcNow,
            updatedAt: DateTimeOffset.UtcNow);

        var status = PetStatusResolver.Resolve(pet, definition);

        status.MaxHp.Should().Be(110);
        status.MaxMp.Should().Be(59);
        status.Strength.Should().Be(28);
        status.Defense.Should().Be(22);
        status.Intelligence.Should().Be(16);
        status.Luck.Should().Be(10);
        status.Speed.Should().Be(14);
    }

    [Fact]
    public void Resolve_WithMismatchedDefinition_ThrowsInvalidOperationException()
    {
        var pet = PlayerPet.Capture(new PlayerId(Guid.NewGuid()), new QuestEnemyDefinitionId(1), DateTimeOffset.UtcNow);
        var otherDefinition = CreateDefinition(new QuestEnemyDefinitionId(2));

        var act = () => PetStatusResolver.Resolve(pet, otherDefinition);

        act.Should().Throw<InvalidOperationException>();
    }

    private static QuestEnemyDefinition CreateDefinition(QuestEnemyDefinitionId id)
    {
        return new QuestEnemyDefinition(
            id,
            "Slime",
            level: 5,
            new Status(maxHp: 100, maxMp: 50, strength: 20, defense: 15, intelligence: 10, luck: 5, speed: 10),
            "/images/slime.png",
            EnemyAiType.Aggressive,
            moveIds: []);
    }
}
