using FluentAssertions;
using server.domain.move;
using server.domain.move.enums;
using Xunit;

namespace server.tests;

public class MoveDomainValidationTests
{
    [Fact]
    public void MoveId_WhenZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new MoveId(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MoveEffectId_WhenZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new MoveEffectId(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DamageEffect_WhenFixedValueIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new DamageEffect(hitCount: 1, powerRate: 1.0m, fixedValue: -1, criticalRate: 0.1m, elementType: ElementType.Slash);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DamageEffect_WhenCriticalRateExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new DamageEffect(hitCount: 1, powerRate: 1.0m, fixedValue: 0, criticalRate: 1.1m, elementType: ElementType.Slash);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AilmentEffect_WhenRateExceedsMax_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new AilmentEffect(AilmentType.Taunt, 1.1m);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BuffEffect_WhenTurnsIsZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new BuffEffect(BuffStat.Defense, BuffOp.Add, 10m, 0, 1.0m, canStack: false);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void BuffEffect_WhenMulAndValueIsZero_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new BuffEffect(BuffStat.Strength, BuffOp.Mul, 0m, 3, 1.0m, canStack: false);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MoveEffect_ValidateByType_WhenDamageWithoutDamageEffect_ThrowsInvalidOperationException()
    {
        var effect = new MoveEffect(
            effectId: new MoveEffectId(1),
            moveId: new MoveId(1),
            sequence: 1,
            effectType: MoveEffectType.Damage);

        var act = () => effect.ValidateByType();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MoveEffect_ValidateByType_WhenAilmentWithDamageEffect_ThrowsInvalidOperationException()
    {
        var effect = new MoveEffect(
            effectId: new MoveEffectId(1),
            moveId: new MoveId(1),
            sequence: 1,
            effectType: MoveEffectType.Ailment,
            damage: CreateDamageEffect(),
            ailment: new AilmentEffect(AilmentType.Taunt, 1.0m));

        var act = () => effect.ValidateByType();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Move_Constructor_WhenEffectsEmpty_ThrowsInvalidOperationException()
    {
        var act = () => CreateMove([]);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Move_Constructor_WhenMoveIdMismatch_ThrowsInvalidOperationException()
    {
        var effects = new[]
        {
            new MoveEffect(new MoveEffectId(1), new MoveId(99), 1, MoveEffectType.Damage, damage: CreateDamageEffect())
        };

        var act = () => CreateMove(effects);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Move_Constructor_WhenSequenceDuplicated_ThrowsInvalidOperationException()
    {
        var effects = new[]
        {
            new MoveEffect(new MoveEffectId(1), new MoveId(1), 1, MoveEffectType.Damage, damage: CreateDamageEffect()),
            new MoveEffect(new MoveEffectId(2), new MoveId(1), 1, MoveEffectType.Heal, damage: CreateHealEffect())
        };

        var act = () => CreateMove(effects);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Move_Constructor_WhenEffectIdDuplicated_ThrowsInvalidOperationException()
    {
        var effects = new[]
        {
            new MoveEffect(new MoveEffectId(1), new MoveId(1), 1, MoveEffectType.Damage, damage: CreateDamageEffect()),
            new MoveEffect(new MoveEffectId(1), new MoveId(1), 2, MoveEffectType.Heal, damage: CreateHealEffect())
        };

        var act = () => CreateMove(effects);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Move_GetOrderedEffects_WhenOutOfOrder_ReturnsSortedBySequence()
    {
        var effects = new[]
        {
            new MoveEffect(new MoveEffectId(2), new MoveId(1), 2, MoveEffectType.Heal, damage: CreateHealEffect()),
            new MoveEffect(new MoveEffectId(1), new MoveId(1), 1, MoveEffectType.Damage, damage: CreateDamageEffect())
        };

        var move = CreateMove(effects);
        var ordered = move.GetOrderedEffects();

        ordered.Select(x => x.Sequence).Should().Equal(1, 2);
    }

    private static Move CreateMove(IEnumerable<MoveEffect> effects) =>
        new(
            id: new MoveId(1),
            name: "テスト技",
            targetType: TargetType.Enemy,
            attackRange: AttackRange.Single,
            mpCost: 3,
            executionPriority: 0,
            category: MoveCategory.Attack,
            effects: effects);

    private static DamageEffect CreateDamageEffect() =>
        new(hitCount: 1, powerRate: 1.2m, fixedValue: 2, criticalRate: 0.1m, elementType: ElementType.Slash);

    private static DamageEffect CreateHealEffect() =>
        new(hitCount: 1, powerRate: 1.0m, fixedValue: 10, criticalRate: 0m, elementType: ElementType.Holy);
}
