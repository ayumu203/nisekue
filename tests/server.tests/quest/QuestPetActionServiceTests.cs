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
    public void CreateSummonInputs_WhenRollSelectsAttack_ReturnsNormalAttackOnAllEnemies()
    {
        var pet = CreatePetSnapshot();
        var moves = new[] { CreateMove(1, AttackRange.Single) };

        var (actor, action, selectedMove) = service.CreateSummonInputs(
            Guid.NewGuid(),
            "Ownerのスライム",
            pet,
            moves,
            rollProvider: _ => 1);

        action.Kind.Should().Be(BattleActionKind.NormalAttack);
        action.TargetType.Should().Be(TargetType.Enemy);
        action.AttackRange.Should().Be(AttackRange.All);
        selectedMove.Should().BeNull();
        actor.Side.Should().Be(BattleSide.Ally);
        actor.BaseStatus.Should().Be(pet.Status);
        actor.CurrentHp.Should().Be(pet.Status.MaxHp);
    }

    [Fact]
    public void CreateSummonInputs_WhenRollSelectsMove_UsesMoveTargeting()
    {
        var pet = CreatePetSnapshot();
        var move = CreateMove(10, AttackRange.Single);
        var rolls = new Queue<int>([2, 1]); // 1回目: 技を選択 / 2回目: 1番目の技

        var (_, action, selectedMove) = service.CreateSummonInputs(
            Guid.NewGuid(),
            "Ownerのスライム",
            pet,
            [move],
            rollProvider: _ => rolls.Dequeue());

        action.Kind.Should().Be(BattleActionKind.UseMove);
        action.MoveId.Should().Be(10);
        action.TargetType.Should().Be(move.TargetType);
        action.AttackRange.Should().Be(AttackRange.Single, "技の対象仕様に従う");
        selectedMove.Should().Be(move);
    }

    [Fact]
    public void CreateSummonInputs_WhenRollSelectsSecondMove_PicksByRoll()
    {
        var pet = CreatePetSnapshot();
        var moves = new[] { CreateMove(10, AttackRange.Single), CreateMove(20, AttackRange.All) };
        var rolls = new Queue<int>([2, 2]);

        var (_, action, _) = service.CreateSummonInputs(Guid.NewGuid(), "Ownerのスライム", pet, moves, _ => rolls.Dequeue());

        action.MoveId.Should().Be(20);
    }

    [Fact]
    public void CreateSummonInputs_WithNoMoves_AlwaysNormalAttack()
    {
        var pet = CreatePetSnapshot();

        var (_, action, selectedMove) = service.CreateSummonInputs(
            Guid.NewGuid(),
            "Ownerのスライム",
            pet,
            availableMoves: [],
            rollProvider: _ => 2);

        action.Kind.Should().Be(BattleActionKind.NormalAttack);
        action.AttackRange.Should().Be(AttackRange.All);
        selectedMove.Should().BeNull();
    }

    [Fact]
    public void CreateSummonInputs_GrantsEnoughMpToCastAnyMove()
    {
        var pet = CreatePetSnapshot();

        var (actor, _, _) = service.CreateSummonInputs(Guid.NewGuid(), "Ownerのスライム", pet, [], _ => 1);

        actor.CurrentMp.Should().BeGreaterThan(pet.Status.MaxMp, "単発登場のためMP切れで技が失敗しないようにする");
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
