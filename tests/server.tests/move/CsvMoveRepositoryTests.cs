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

        var move = await repository.GetMoveAsync(new(504));

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

        var move = await repository.GetMoveAsync(new(401));

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

        var move = await repository.GetMoveAsync(new(105));

        move.Should().NotBeNull();
        move!.Name.Should().Be("シールドバッシュ");
        move.Effects.Should().ContainSingle();
        move.Effects[0].Damage.Should().NotBeNull();
        move.Effects[0].Damage!.AttackStat.Should().Be(BuffStat.Defense);
    }

    [Fact]
    public async Task GetMoveAsync_WhenRegenerationMoveExists_ReturnsRegenerationDefinition()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(513));

        move.Should().NotBeNull();
        move!.Name.Should().Be("リジェネレイ");
        move.Effects.Should().ContainSingle();
        move.Effects[0].Ailment.Should().NotBeNull();
        move.Effects[0].Ailment!.AilmentType.Should().Be(AilmentType.Regeneration);
        move.Effects[0].Ailment!.TriggerDamage.Should().NotBeNull();
        move.Effects[0].Ailment!.TriggerDamage!.FixedValue.Should().Be(35);
    }

    [Fact]
    public async Task GetMoveAsync_WhenBossApplicableInstantDeathMoveExists_ReturnsBossFlag()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(516));

        move.Should().NotBeNull();
        move!.Name.Should().Be("急所撃ち");
        move.Effects.Should().ContainSingle();
        move.Effects[0].Ailment.Should().NotBeNull();
        move.Effects[0].Ailment!.AilmentType.Should().Be(AilmentType.InstantDeath);
        move.Effects[0].Ailment!.AllowBossInstantDeath.Should().BeTrue();
    }

    [Fact]
    public async Task GetMoveAsync_WhenCoverAllMoveExists_ReturnsCoverAllDefinition()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(518));

        move.Should().NotBeNull();
        move!.Name.Should().Be("絶対守護");
        move.TargetType.Should().Be(TargetType.Self);
        move.Effects.Should().ContainSingle();
        move.Effects[0].EffectType.Should().Be(MoveEffectType.Ailment);
        move.Effects[0].Ailment.Should().NotBeNull();
        move.Effects[0].Ailment!.AilmentType.Should().Be(AilmentType.CoverAll);
    }

    [Fact]
    public async Task GetMoveAsync_WhenHalveSelfHpMoveExists_ReturnsHalveSelfHpEffect()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(415));

        move.Should().NotBeNull();
        move!.Name.Should().Be("血戦覚悟");
        move.Effects.Should().HaveCount(2);
        move.Effects[0].EffectType.Should().Be(MoveEffectType.HalveSelfHp);
        move.Effects[1].Buff.Should().NotBeNull();
        move.Effects[1].Buff!.BuffStat.Should().Be(BuffStat.Strength);
        move.Effects[1].Buff!.BuffCalculationType.Should().Be(BuffCalculationType.Mul);
    }

    /*
    [Fact]
    public async Task GetMoveAsync_WhenEffectImagePathConfigured_ReturnsPath()
    {
        var repository = new CsvMoveRepository();

        var move = await repository.GetMoveAsync(new(18));

        move.Should().NotBeNull();
        move!.EffectImagePath.Should().Be("effects/plamia.png");
    }
    */

    public static TheoryData<int, string, TargetType, AttackRange, int, ElementType> AttackMoveDefinitions => new()
    {
        { 101, "ルーキーストライク", TargetType.Enemy, AttackRange.Single, 4, ElementType.Strike },
        { 102, "なぎはらい", TargetType.Enemy, AttackRange.Row, 3, ElementType.Slash },
        { 103, "スラッシュ", TargetType.Enemy, AttackRange.Row, 5, ElementType.Slash },
        { 104, "ギガスラッシュ", TargetType.Enemy, AttackRange.Row, 12, ElementType.Slash },
        { 105, "シールドバッシュ", TargetType.Enemy, AttackRange.Single, 3, ElementType.Strike },
        { 106, "ヴズルイフ", TargetType.Enemy, AttackRange.Row, 4, ElementType.None },
        { 107, "ヒルフェ", TargetType.Enemy, AttackRange.Single, 5, ElementType.Holy },
        { 108, "プラーミア", TargetType.Enemy, AttackRange.Single, 3, ElementType.Fire },
        { 109, "クラインプラーミヤ", TargetType.Enemy, AttackRange.Single, 6, ElementType.Fire },
        { 110, "ミッテルプラーミヤ", TargetType.Enemy, AttackRange.Single, 6, ElementType.Fire },
        { 111, "グロースプラーミヤ", TargetType.Enemy, AttackRange.Single, 25, ElementType.Fire },
        { 112, "ウアプラーミヤ", TargetType.Enemy, AttackRange.Single, 16, ElementType.Fire },
        { 113, "クラインヴォーダ", TargetType.Enemy, AttackRange.Column, 4, ElementType.Water },
        { 114, "ミッテルヴォーダ", TargetType.Enemy, AttackRange.Column, 7, ElementType.Water },
        { 115, "グロースヴォーダ", TargetType.Enemy, AttackRange.Column, 11, ElementType.Water },
        { 116, "ウアヴォーダ", TargetType.Enemy, AttackRange.Column, 17, ElementType.Water },
        { 117, "クラインヴェーチェル", TargetType.Enemy, AttackRange.Square, 5, ElementType.Wind },
        { 118, "ミッテルヴェーチェル", TargetType.Enemy, AttackRange.Square, 8, ElementType.Wind },
        { 119, "グロースヴェーチェル", TargetType.Enemy, AttackRange.Square, 12, ElementType.Wind },
        { 120, "ウアヴェーチェル", TargetType.Enemy, AttackRange.Square, 18, ElementType.Wind },
        { 121, "クラインゼムリャ", TargetType.Enemy, AttackRange.Row, 4, ElementType.Earth },
        { 122, "ミッテルゼムリャ", TargetType.Enemy, AttackRange.Row, 7, ElementType.Earth },
        { 123, "グロースゼムリャ", TargetType.Enemy, AttackRange.Row, 11, ElementType.Earth },
        { 124, "ウアゼムリャ", TargetType.Enemy, AttackRange.Row, 17, ElementType.Earth },
        { 138, "魔神斬り", TargetType.Enemy, AttackRange.Single, 23, ElementType.Slash },
        { 203, "フレアライン", TargetType.Enemy, AttackRange.Column, 37, ElementType.Fire },
        { 205, "テンペスト", TargetType.Enemy, AttackRange.All, 92, ElementType.Wind },
        { 206, "死霊召喚", TargetType.Enemy, AttackRange.Single, 26, ElementType.None }
    };

    public static TheoryData<int, string, MoveCategory, TargetType, AttackRange> FamilyRepresentativeDefinitions => new()
    {
        { 101, "ルーキーストライク", MoveCategory.Attack, TargetType.Enemy, AttackRange.Single },
        { 102, "なぎはらい", MoveCategory.Attack, TargetType.Enemy, AttackRange.Row },
        { 105, "シールドバッシュ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Single },
        { 106, "ヴズルイフ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Row },
        { 501, "ちょうはつ", MoveCategory.Support, TargetType.Self, AttackRange.Single },
        { 401, "護符の構え", MoveCategory.Support, TargetType.Self, AttackRange.Single },
        { 402, "シルト", MoveCategory.Support, TargetType.Ally, AttackRange.All },
        { 502, "パラライズ", MoveCategory.Support, TargetType.Enemy, AttackRange.Single },
        { 503, "ポイズンミスト", MoveCategory.Support, TargetType.Enemy, AttackRange.Column },
        { 504, "トラップセット", MoveCategory.Support, TargetType.Enemy, AttackRange.Single },
        { 301, "マールイテラピー", MoveCategory.Support, TargetType.Ally, AttackRange.Single },
        { 107, "ヒルフェ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Single },
        { 108, "プラーミア", MoveCategory.Attack, TargetType.Enemy, AttackRange.Single },
        { 113, "クラインヴォーダ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Column },
        { 117, "クラインヴェーチェル", MoveCategory.Attack, TargetType.Enemy, AttackRange.Square },
        { 121, "クラインゼムリャ", MoveCategory.Attack, TargetType.Enemy, AttackRange.Row },
        { 406, "幻惑の挑発", MoveCategory.Support, TargetType.Self, AttackRange.Single },
        { 513, "リジェネレイ", MoveCategory.Support, TargetType.Ally, AttackRange.Single },
        { 516, "急所撃ち", MoveCategory.Support, TargetType.Enemy, AttackRange.Single },
        { 413, "黒霧散布", MoveCategory.Support, TargetType.Enemy, AttackRange.All }
    };

    public static TheoryData<int, string, int, decimal, int> HealMoveDefinitions => new()
    {
        { 301, "マールイテラピー", 2, 1.00m, 20 },
        { 302, "スレドニーテラピー", 30, 1.25m, 45 },
        { 303, "ボリショイテラピー", 12, 1.60m, 85 },
        { 304, "ヴェリーキーテラピー", 18, 2.10m, 150 }
    };

    public static TheoryData<int, string, AttackRange, AilmentType, int> AilmentMoveDefinitions => new()
    {
        { 502, "パラライズ", AttackRange.Single, AilmentType.Paralysis, 1 },
        { 503, "ポイズンミスト", AttackRange.Column, AilmentType.Poison, 1 },
        { 504, "トラップセット", AttackRange.Single, AilmentType.DamageTrap, 2 }
    };
}
