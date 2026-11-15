using FluentAssertions;
using server.application.quest;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;
using Xunit;

namespace server.tests.quest;

public class QuestPetActionServiceTests
{
    private readonly QuestPetActionService service = new();

    [Fact]
    public void CreateDeployedPetInputs_WithMoves_AlwaysUsesMove()
    {
        var pet = CreateDeployedPet();
        var moves = new[] { CreateMove(1, AttackRange.Single) };

        var (actor, action, selectedMove) = service.CreateDeployedPetInputs(
            pet,
            "Ownerのスライム",
            moves,
            rollProvider: _ => 1);

        action.Kind.Should().Be(BattleActionKind.UseMove);
        action.MoveId.Should().Be(1);
        selectedMove.Should().Be(moves[0]);
        actor.ActorId.Should().Be(pet.ActorId);
        actor.Side.Should().Be(BattleSide.Ally);
        actor.BaseStatus.Should().Be(pet.Status);
    }

    [Fact]
    public void CreateDeployedPetInputs_WhenRollSelectsSecondMove_PicksByRoll()
    {
        var pet = CreateDeployedPet();
        var moves = new[] { CreateMove(1, AttackRange.Single), CreateMove(2, AttackRange.All) };

        var (_, action, selectedMove) = service.CreateDeployedPetInputs(pet, "Ownerのスライム", moves, _ => 2);

        action.MoveId.Should().Be(2);
        action.AttackRange.Should().Be(AttackRange.All);
        selectedMove.Should().Be(moves[1]);
    }

    [Fact]
    public void CreateDeployedPetInputs_WithNoMoves_UsesSingleNormalAttack()
    {
        var pet = CreateDeployedPet();

        var (_, action, selectedMove) = service.CreateDeployedPetInputs(pet, "Ownerのスライム", [], _ => 1);

        action.Kind.Should().Be(BattleActionKind.NormalAttack);
        action.TargetType.Should().Be(TargetType.Enemy);
        action.AttackRange.Should().Be(AttackRange.Single);
        selectedMove.Should().BeNull();
    }

    [Fact]
    public void CreateDeployedPetInputs_UsesCurrentHpOfDeployedPet()
    {
        var pet = CreateDeployedPet(currentHp: 42);

        var (actor, _, _) = service.CreateDeployedPetInputs(pet, "Ownerのスライム", [], _ => 1);

        actor.CurrentHp.Should().Be(42);
    }

    [Fact]
    public void CreateDeployedPetInputs_GrantsEnoughMpToCastAnyMove()
    {
        var pet = CreateDeployedPet();

        var (actor, _, _) = service.CreateDeployedPetInputs(pet, "Ownerのスライム", [], _ => 1);

        actor.CurrentMp.Should().BeGreaterThan(pet.Status.MaxMp, "出撃中のペットは MP を管理しない");
    }

    private static QuestDeployedPetState CreateDeployedPet(int? currentHp = null)
    {
        var snapshot = CreatePetSnapshot();
        return new QuestDeployedPetState(
            Guid.NewGuid(),
            QuestParticipantId.New(),
            snapshot.EnemyDefinitionId,
            snapshot.Status,
            new server.domain.battle.BattlePosition(BattleRow.Front, BattleColumn.Left),
            currentHp ?? snapshot.Status.MaxHp,
            isDead: false);
    }

    private static QuestPetSnapshot CreatePetSnapshot()
    {
        return new QuestPetSnapshot(
            new QuestEnemyDefinitionId(1),
            new Status(maxHp: 100, maxMp: 20, strength: 30, defense: 10, intelligence: 5, luck: 5, speed: 15));
    }

    private static Move CreateMove(int id, AttackRange attackRange)
    {
        return new Move(
            new MoveId(id),
            $"テスト技{id}",
            "テスト用の技",
            TargetType.Enemy,
            attackRange,
            mpCost: 5,
            executionPriority: 0,
            MoveCategory.Attack,
            effects:
            [
                new MoveEffect(
                    new MoveEffectId(id),
                    new MoveId(id),
                    1,
                    MoveEffectType.Damage,
                    damage: new DamageEffect(1, 1m, 1, 0m, ElementType.None))
            ]);
    }
}
