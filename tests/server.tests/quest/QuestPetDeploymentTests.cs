using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.player;
using server.domain.quest;
using Xunit;

namespace server.tests.quest;

public class QuestPetDeploymentTests
{
    [Fact]
    public void Create_WithThreePets_FillsFromFrontRow()
    {
        var deployment = QuestPetDeployment.Create(
            QuestParticipantId.New(),
            [CreatePetSnapshot(1), CreatePetSnapshot(2), CreatePetSnapshot(3)],
            Guid.NewGuid);

        deployment.Pets.Select(x => x.Position).Should().Equal(
            new BattlePosition(BattleRow.Front, BattleColumn.Left),
            new BattlePosition(BattleRow.Front, BattleColumn.Right),
            new BattlePosition(BattleRow.Middle, BattleColumn.Left));
        deployment.Pets.Select(x => x.EnemyDefinitionId.Value).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Create_StartsWithFullHpAndThreeTurns()
    {
        var pet = CreatePetSnapshot(1);

        var deployment = QuestPetDeployment.Create(QuestParticipantId.New(), [pet], Guid.NewGuid);

        deployment.RemainingTurns.Should().Be(3);
        deployment.IsActive.Should().BeTrue();
        deployment.Pets.Should().ContainSingle().Which.CurrentHp.Should().Be(pet.Status.MaxHp);
    }

    [Fact]
    public void Create_WithNoPets_ThrowsInvalidOperationException()
    {
        var act = () => QuestPetDeployment.Create(QuestParticipantId.New(), [], Guid.NewGuid);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConsumeTurn_AfterThreeTurns_IsNotActive()
    {
        var deployment = QuestPetDeployment.Create(QuestParticipantId.New(), [CreatePetSnapshot(1)], Guid.NewGuid);

        deployment.ConsumeTurn();
        deployment.ConsumeTurn();
        deployment.IsActive.Should().BeTrue();

        deployment.ConsumeTurn();
        deployment.IsActive.Should().BeFalse();
        deployment.RemainingTurns.Should().Be(0);
    }

    [Fact]
    public void ApplyResolution_WhenOnePetDefeated_KeepsOthersActive()
    {
        var deployment = QuestPetDeployment.Create(
            QuestParticipantId.New(),
            [CreatePetSnapshot(1), CreatePetSnapshot(2)],
            Guid.NewGuid);
        var defeated = deployment.Pets[0];

        deployment.ApplyResolution(new BattleTurnResolution(
            actionResults: [],
            updatedStates: [new BattleActorState(new BattleActorId(defeated.ActorId), 0, 0)]));

        defeated.IsDead.Should().BeTrue();
        deployment.LivingPets.Should().ContainSingle().Which.EnemyDefinitionId.Value.Should().Be(2);
        deployment.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ApplyResolution_WhenAllPetsDefeated_IsNotActive()
    {
        var deployment = QuestPetDeployment.Create(QuestParticipantId.New(), [CreatePetSnapshot(1)], Guid.NewGuid);
        var pet = deployment.Pets[0];

        deployment.ApplyResolution(new BattleTurnResolution(
            actionResults: [],
            updatedStates: [new BattleActorState(new BattleActorId(pet.ActorId), 0, 0)]));

        deployment.IsActive.Should().BeFalse();
    }

    private static QuestPetSnapshot CreatePetSnapshot(int enemyDefinitionId)
    {
        return new QuestPetSnapshot(
            new QuestEnemyDefinitionId(enemyDefinitionId),
            new Status(maxHp: 50, maxMp: 10, strength: 10, defense: 5, intelligence: 1, luck: 1, speed: 5));
    }
}
