using FluentAssertions;
using server.domain.move.enums;
using server.infrastructure.move;
using Xunit;

namespace server.tests;

public class CsvMoveRepositoryTests
{
    [Theory]
    [MemberData(nameof(FamilyRepresentativeDefinitions))]
    public async Task GetMoveAsync_WhenFamilyRepresentativeMoveExists_ReturnsExpectedSummary(
        int moveId,
        string expectedName,
        MoveCategory expectedCategory,
        TargetType expectedTargetType,
        AttackRange expectedAttackRange)
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(moveId));

        move.Should().NotBeNull();
        move!.Name.Should().Be(expectedName);
        move.Category.Should().Be(expectedCategory);
        move.TargetType.Should().Be(expectedTargetType);
        move.AttackRange.Should().Be(expectedAttackRange);
        move.Effects.Should().NotBeEmpty();
    }

    [Theory]
    [MemberData(nameof(AttackMoveDefinitions))]
    public async Task GetMoveAsync_WhenAttackMoveExists_ReturnsExpectedDefinition(
        int moveId,
        string expectedName,
        TargetType expectedTargetType,
        AttackRange expectedAttackRange,
        int expectedMpCost,
        ElementType expectedElementType)
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(moveId));

        move.Should().NotBeNull();
        move!.Name.Should().Be(expectedName);
        move.TargetType.Should().Be(expectedTargetType);
        move.AttackRange.Should().Be(expectedAttackRange);
        move.MpCost.Should().Be(expectedMpCost);
        move.Category.Should().Be(MoveCategory.Attack);
        move.Effects.Should().ContainSingle();
        move.Effects[0].EffectType.Should().Be(MoveEffectType.Damage);
        move.Effects[0].Damage.Should().NotBeNull();
        move.Effects[0].Damage!.ElementType.Should().Be(expectedElementType);
    }

    [Theory]
    [MemberData(nameof(HealMoveDefinitions))]
    public async Task GetMoveAsync_WhenHealMoveExists_ReturnsExpectedDefinition(
        int moveId,
        string expectedName,
        int expectedMpCost,
        decimal expectedPowerRate,
        int expectedFixedValue)
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(moveId));

        move.Should().NotBeNull();
        move!.Name.Should().Be(expectedName);
        move.TargetType.Should().Be(TargetType.Ally);
        move.AttackRange.Should().Be(AttackRange.Single);
        move.MpCost.Should().Be(expectedMpCost);
        move.Category.Should().Be(MoveCategory.Support);
        move.Effects.Should().ContainSingle();
        move.Effects[0].EffectType.Should().Be(MoveEffectType.Heal);
        move.Effects[0].Damage.Should().NotBeNull();
        move.Effects[0].Damage!.ElementType.Should().Be(ElementType.Holy);
        move.Effects[0].Damage!.PowerRate.Should().Be(expectedPowerRate);
        move.Effects[0].Damage!.FixedValue.Should().Be(expectedFixedValue);
    }

    [Theory]
    [MemberData(nameof(AilmentMoveDefinitions))]
    public async Task GetMoveAsync_WhenAilmentMoveExists_ReturnsExpectedDefinition(
        int moveId,
        string expectedName,
        AttackRange expectedAttackRange,
        AilmentType expectedAilment,
        int expectedAilmentTurns)
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(moveId));

        move.Should().NotBeNull();
        move!.Name.Should().Be(expectedName);
        move.TargetType.Should().Be(TargetType.Enemy);
        move.AttackRange.Should().Be(expectedAttackRange);
        move.Category.Should().Be(MoveCategory.Support);
        move.Effects.Should().ContainSingle();
        move.Effects[0].EffectType.Should().Be(MoveEffectType.Ailment);
        move.Effects[0].Ailment.Should().NotBeNull();
        move.Effects[0].Ailment!.AilmentType.Should().Be(expectedAilment);
        move.Effects[0].Ailment!.AilmentTurns.Should().Be(expectedAilmentTurns);
    }

    [Fact]
    public async Task GetMoveAsync_WhenDamageTrapMoveExists_ReturnsTrapDamageDefinition()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(12));

        move.Should().NotBeNull();
        move!.Effects[0].Ailment.Should().NotBeNull();
        move.Effects[0].Ailment!.AilmentType.Should().Be(AilmentType.DamageTrap);
        move.Effects[0].Ailment!.TriggerDamage.Should().NotBeNull();
        move.Effects[0].Ailment!.TriggerDamage!.HitCount.Should().Be(2);
        move.Effects[0].Ailment!.TriggerDamage!.PowerRate.Should().Be(0.10m);
    }

    [Fact]
    public async Task GetMoveAsync_WhenGuardStanceExists_ReturnsTauntAndDefenseBuff()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(8));

        move.Should().NotBeNull();
        move!.Name.Should().Be("護符の構え");
        move.TargetType.Should().Be(TargetType.Self);
        move.AttackRange.Should().Be(AttackRange.Single);
        move.MpCost.Should().Be(6);
        move.ExecutionPriority.Should().Be(1);
        move.Category.Should().Be(MoveCategory.Support);
        move.Effects.Should().HaveCount(2);
        move.Effects[0].EffectType.Should().Be(MoveEffectType.Ailment);
        move.Effects[0].Ailment.Should().NotBeNull();
        move.Effects[0].Ailment!.AilmentType.Should().Be(AilmentType.Taunt);
        move.Effects[1].EffectType.Should().Be(MoveEffectType.Buff);
        move.Effects[1].Buff.Should().NotBeNull();
        move.Effects[1].Buff!.BuffStat.Should().Be(BuffStat.Defense);
        move.Effects[1].Buff!.BuffCalculationType.Should().Be(BuffCalculationType.Add);
        move.Effects[1].Buff!.BuffValue.Should().Be(15);
        move.Effects[1].Buff!.BuffTurns.Should().Be(3);
    }

    [Fact]
    public async Task GetMoveAsync_WhenDefenseBasedAttackExists_ReturnsDefenseAttackStat()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(5));

        move.Should().NotBeNull();
        move!.Name.Should().Be("シールドバッシュ");
        move.Effects.Should().ContainSingle();
        move.Effects[0].Damage.Should().NotBeNull();
        move.Effects[0].Damage!.AttackStat.Should().Be(BuffStat.Defense);
    }

    [Fact]
    public async Task GetMoveAsync_WhenEffectImagePathConfigured_ReturnsPath()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(18));

        move.Should().NotBeNull();
        move!.EffectImagePath.Should().Be("effects/plamia.png");
    }

    public static TheoryData<int, string, TargetType, AttackRange, int, ElementType> AttackMoveDefinitions => new()
    {
        { 1, "ルーキーストライク", TargetType.Enemy, AttackRange.Single, 4, ElementType.Strike },
        { 2, "なぎはらい", TargetType.Enemy, AttackRange.Column, 4, ElementType.Slash },
        { 3, "スラッシュ", TargetType.Enemy, AttackRange.Column, 6, ElementType.Slash },
        { 4, "ギガスラッシュ", TargetType.Enemy, AttackRange.Column, 12, ElementType.Slash },
        { 5, "シールドバッシュ", TargetType.Enemy, AttackRange.Single, 5, ElementType.Strike },
        { 6, "ヴズルイフ", TargetType.Enemy, AttackRange.Row, 6, ElementType.None },
        { 17, "ヒルフェ", TargetType.Enemy, AttackRange.Single, 5, ElementType.Holy },
        { 18, "プラーミア", TargetType.Enemy, AttackRange.Row, 6, ElementType.Fire },
        { 19, "クラインプラーミヤ", TargetType.Enemy, AttackRange.Single, 3, ElementType.Fire },
        { 20, "ミッテルプラーミヤ", TargetType.Enemy, AttackRange.Single, 6, ElementType.Fire },
        { 21, "グロースプラーミヤ", TargetType.Enemy, AttackRange.Single, 10, ElementType.Fire },
        { 22, "ウアプラーミヤ", TargetType.Enemy, AttackRange.Single, 16, ElementType.Fire },
        { 23, "クラインヴォーダ", TargetType.Enemy, AttackRange.Column, 4, ElementType.Water },
        { 24, "ミッテルヴォーダ", TargetType.Enemy, AttackRange.Column, 7, ElementType.Water },
        { 25, "グロースヴォーダ", TargetType.Enemy, AttackRange.Column, 11, ElementType.Water },
        { 26, "ウアヴォーダ", TargetType.Enemy, AttackRange.Column, 17, ElementType.Water },
        { 27, "クラインヴェーチェル", TargetType.Enemy, AttackRange.Square, 5, ElementType.Wind },
        { 28, "ミッテルヴェーチェル", TargetType.Enemy, AttackRange.Square, 8, ElementType.Wind },
        { 29, "グロースヴェーチェル", TargetType.Enemy, AttackRange.Square, 12, ElementType.Wind },
        { 30, "ウアヴェーチェル", TargetType.Enemy, AttackRange.Square, 18, ElementType.Wind },
        { 31, "クラインゼムリャ", TargetType.Enemy, AttackRange.Row, 4, ElementType.Earth },
        { 32, "ミッテルゼムリャ", TargetType.Enemy, AttackRange.Row, 7, ElementType.Earth },
        { 33, "グロースゼムリャ", TargetType.Enemy, AttackRange.Row, 11, ElementType.Earth },
        { 34, "ウアゼムリャ", TargetType.Enemy, AttackRange.Row, 17, ElementType.Earth }
    };

    public static TheoryData<int, string, MoveCategory, TargetType, AttackRange> FamilyRepresentativeDefinitions => new()
    {
        { 1, "ルーキーストライク", MoveCategory.Attack, TargetType.Enemy, AttackRange.Single },
        { 2, "なぎはらい", MoveCategory.Attack, TargetType.Enemy, AttackRange.Column },
        { 5, "シールドバッシュ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Single },
        { 6, "ヴズルイフ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Row },
        { 7, "ちょうはつ", MoveCategory.Support, TargetType.Self, AttackRange.Single },
        { 8, "護符の構え", MoveCategory.Support, TargetType.Self, AttackRange.Single },
        { 9, "シルト", MoveCategory.Support, TargetType.Ally, AttackRange.All },
        { 10, "パラライズ", MoveCategory.Support, TargetType.Enemy, AttackRange.Single },
        { 11, "ポイズンミスト", MoveCategory.Support, TargetType.Enemy, AttackRange.Column },
        { 12, "トラップセット", MoveCategory.Support, TargetType.Enemy, AttackRange.Single },
        { 13, "マールイテラピー", MoveCategory.Support, TargetType.Ally, AttackRange.Single },
        { 17, "ヒルフェ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Single },
        { 18, "プラーミア", MoveCategory.Attack, TargetType.Enemy, AttackRange.Row },
        { 23, "クラインヴォーダ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Column },
        { 27, "クラインヴェーチェル", MoveCategory.Attack, TargetType.Enemy, AttackRange.Square },
        { 31, "クラインゼムリャ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Row }
    };

    public static TheoryData<int, string, int, decimal, int> HealMoveDefinitions => new()
    {
        { 13, "マールイテラピー", 5, 1.00m, 20 },
        { 14, "スレドニーテラピー", 8, 1.25m, 45 },
        { 15, "ボリショイテラピー", 12, 1.60m, 85 },
        { 16, "ヴェリーキーテラピー", 18, 2.10m, 150 }
    };

    public static TheoryData<int, string, AttackRange, AilmentType, int> AilmentMoveDefinitions => new()
    {
        { 10, "パラライズ", AttackRange.Single, AilmentType.Paralysis, 1 },
        { 11, "ポイズンミスト", AttackRange.Column, AilmentType.Poison, 1 },
        { 12, "トラップセット", AttackRange.Single, AilmentType.DamageTrap, 2 }
    };
}
