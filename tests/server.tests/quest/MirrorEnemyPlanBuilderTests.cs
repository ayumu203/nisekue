using FluentAssertions;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using Xunit;

namespace server.tests.quest;

public class MirrorEnemyPlanBuilderTests
{
    private static readonly IReadOnlySet<int> NoExcludedMoves = new HashSet<int>();

    [Fact]
    public void Build_With20Candidates_PlacesWeakerBandsOnEarlierFloors()
    {
        // rank 1 が最強（転生回数が多い）。
        var candidates = Enumerable.Range(1, 20).Select(rank => CreateCandidate($"P{rank}", rebirthCount: 100 - rank)).ToArray();
        var self = CreateCandidate("Self", rebirthCount: 0);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build(candidates, self, CreateFloors(), NoExcludedMoves);

        NamesOnFloor(plan, 1).Should().BeEquivalentTo(new[] { "P16", "P17", "P18", "P19", "P20" });
        NamesOnFloor(plan, 2).Should().BeEquivalentTo(new[] { "P11", "P12", "P13", "P14", "P15" });
        NamesOnFloor(plan, 3).Should().BeEquivalentTo(new[] { "P6", "P7", "P8", "P9", "P10" });
        NamesOnFloor(plan, 4).Should().BeEquivalentTo(new[] { "P1", "P2", "P3", "P4", "P5" });
        NamesOnFloor(plan, 5).Should().BeEquivalentTo(new[] { "Self", "P1", "P2", "P3" });
        plan.GetFloor(5).IsBoss.Should().BeTrue();
        plan.GetFloor(1).IsBoss.Should().BeFalse();
    }

    [Fact]
    public void Build_WithCustomConfig_UsesConfiguredCounts()
    {
        var candidates = Enumerable.Range(1, 12).Select(rank => CreateCandidate($"P{rank}", rebirthCount: 100 - rank)).ToArray();
        var self = CreateCandidate("Self", rebirthCount: 0);
        var config = MirrorTestConfigs.Default(topPlayerCount: 10, enemiesPerNormalFloor: 2, bossCompanionCount: 1);

        var plan = new MirrorEnemyPlanBuilder(config).Build(candidates, self, CreateFloors(), NoExcludedMoves);

        NamesOnFloor(plan, 1).Should().BeEquivalentTo(new[] { "P9", "P10" });
        NamesOnFloor(plan, 2).Should().BeEquivalentTo(new[] { "P7", "P8" });
        NamesOnFloor(plan, 3).Should().BeEquivalentTo(new[] { "P5", "P6" });
        NamesOnFloor(plan, 4).Should().BeEquivalentTo(new[] { "P3", "P4" });
        NamesOnFloor(plan, 5).Should().BeEquivalentTo(new[] { "Self", "P1" });
        plan.Enemies.Select(x => x.Name).Should().NotContain(new[] { "P11", "P12" });
    }

    [Fact]
    public void Build_WhenBossAppearanceWouldExceedTotalLimit_ExcludesCompanionFromBossFloor()
    {
        var p1 = CreateCandidate("P1", rebirthCount: 10);
        var p2 = CreateCandidate("P2", rebirthCount: 9);
        var self = CreateCandidate("Self", rebirthCount: 0);
        IReadOnlyList<QuestFloorDefinition> floors =
            [CreateFloor(1, FloorType.Normal), CreateFloor(2, FloorType.Normal), CreateFloor(3, FloorType.Boss)];

        var limited = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default(maxNormalAppearances: 2, maxTotalAppearances: 2))
            .Build([p1, p2], self, floors, NoExcludedMoves);
        var relaxed = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default(maxNormalAppearances: 2, maxTotalAppearances: 3))
            .Build([p1, p2], self, floors, NoExcludedMoves);

        NamesOnFloor(limited, 3).Should().BeEquivalentTo(new[] { "Self" });
        NamesOnFloor(relaxed, 3).Should().BeEquivalentTo(new[] { "Self", "P1", "P2" });
    }

    [Fact]
    public void Build_WhenSelfIsCandidate_KeepsOneAppearanceForBossFloor()
    {
        var self = CreateCandidate("Self", rebirthCount: 99);
        var p1 = CreateCandidate("P1", rebirthCount: 1);
        IReadOnlyList<QuestFloorDefinition> floors =
            [CreateFloor(1, FloorType.Normal), CreateFloor(2, FloorType.Normal), CreateFloor(3, FloorType.Boss)];

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default(maxNormalAppearances: 2, maxTotalAppearances: 2))
            .Build([self, p1], self, floors, NoExcludedMoves);

        NamesOnFloor(plan, 1).Should().BeEquivalentTo(new[] { "P1", "Self" });
        NamesOnFloor(plan, 2).Should().BeEquivalentTo(new[] { "P1" });
        NamesOnFloor(plan, 3).Should().BeEquivalentTo(new[] { "Self" });
    }

    [Fact]
    public void Build_WithMoreThan20Candidates_UsesOnlyTop20()
    {
        var candidates = Enumerable.Range(1, 25).Select(rank => CreateCandidate($"P{rank}", rebirthCount: 100 - rank)).ToArray();
        var self = CreateCandidate("Self", rebirthCount: 0);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build(candidates, self, CreateFloors(), NoExcludedMoves);

        plan.Enemies.Select(x => x.Name).Should().NotContain(new[] { "P21", "P22", "P23", "P24", "P25" });
    }

    [Fact]
    public void RankTop_WhenRebirthAndLevelAreTied_OrdersByCombatIndex()
    {
        var weak = CreateCandidate("Weak", rebirthCount: 3, level: 100, combatIndex: 10);
        var strong = CreateCandidate("Strong", rebirthCount: 3, level: 100, combatIndex: 99);
        var higherLevel = CreateCandidate("HigherLevel", rebirthCount: 3, level: 101, combatIndex: 1);
        var moreRebirths = CreateCandidate("MoreRebirths", rebirthCount: 4, level: 1, combatIndex: 1);

        var ranked = MirrorEnemyPlanBuilder.RankTop([weak, strong, higherLevel, moreRebirths], topCount: 20);

        ranked.Select(x => x.Name).Should().ContainInOrder("MoreRebirths", "HigherLevel", "Strong", "Weak");
    }

    [Fact]
    public void Build_WithFewCandidates_NeverDuplicatesWithinFloorAndLimitsAppearances()
    {
        var candidates = Enumerable.Range(1, 8).Select(rank => CreateCandidate($"P{rank}", rebirthCount: 100 - rank)).ToArray();
        var self = CreateCandidate("Self", rebirthCount: 0);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build(candidates, self, CreateFloors(), NoExcludedMoves);

        foreach (var floor in plan.Floors)
        {
            floor.Placements.Select(x => x.EnemyDefinitionId).Should().OnlyHaveUniqueItems();
        }

        var normalAppearances = plan.Floors
            .Where(x => !x.IsBoss)
            .SelectMany(x => x.Placements)
            .GroupBy(x => x.EnemyDefinitionId)
            .Select(x => x.Count());
        normalAppearances.Should().OnlyContain(count => count <= MirrorTestConfigs.Default().MaxNormalAppearances);

        var totalAppearances = plan.Floors
            .SelectMany(x => x.Placements)
            .GroupBy(x => x.EnemyDefinitionId)
            .Select(x => x.Count());
        totalAppearances.Should().OnlyContain(count => count <= 3);

        // 8人×2回＝16枠しか使えないので、埋まりきらないフロアは敵が減る。
        plan.Floors.Where(x => !x.IsBoss).Sum(x => x.Placements.Count).Should().Be(16);
        plan.Floors.Where(x => !x.IsBoss).Should().OnlyContain(x => x.Placements.Count >= 1);
    }

    [Fact]
    public void Build_WithFewerThanFiveCandidates_PlacesAtMostCandidateCountPerFloor()
    {
        var candidates = Enumerable.Range(1, 3).Select(rank => CreateCandidate($"P{rank}", rebirthCount: 100 - rank)).ToArray();
        var self = CreateCandidate("Self", rebirthCount: 0);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build(candidates, self, CreateFloors(), NoExcludedMoves);

        plan.Floors.Where(x => !x.IsBoss).Should().OnlyContain(x => x.Placements.Count >= 1 && x.Placements.Count <= 3);
        NamesOnFloor(plan, 5).Should().BeEquivalentTo(new[] { "Self", "P1", "P2", "P3" });
    }

    [Fact]
    public void Build_WhenOnlySelfExists_PutsAnEnemyOnEveryFloor()
    {
        var self = CreateCandidate("Self", rebirthCount: 0);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build([self], self, CreateFloors(), NoExcludedMoves);

        plan.Floors.Should().OnlyContain(x => x.Placements.Count == 1);
        plan.Enemies.Should().ContainSingle();
    }

    [Fact]
    public void Build_WhenSelfIsTopRanked_BossCompanionsExcludeSelf()
    {
        var self = CreateCandidate("Self", rebirthCount: 999);
        var others = Enumerable.Range(1, 19).Select(rank => CreateCandidate($"P{rank}", rebirthCount: 100 - rank));
        var candidates = others.Append(self).ToArray();

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build(candidates, self, CreateFloors(), NoExcludedMoves);

        NamesOnFloor(plan, 5).Should().BeEquivalentTo(new[] { "Self", "P1", "P2", "P3" });
        // 自分は上位1位として4階にも出る（通常フロアから外さない）。
        NamesOnFloor(plan, 4).Should().Contain("Self");
    }

    [Fact]
    public void Build_AssignsUniqueDefinitionIdsFromMirrorRange()
    {
        var candidates = Enumerable.Range(1, 20).Select(rank => CreateCandidate($"P{rank}", rebirthCount: 100 - rank)).ToArray();
        var self = CreateCandidate("Self", rebirthCount: 0);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build(candidates, self, CreateFloors(), NoExcludedMoves);

        plan.Enemies.Select(x => x.DefinitionId.Value).Should().OnlyHaveUniqueItems();
        plan.Enemies.Should().OnlyContain(x => x.DefinitionId.Value >= MirrorEnemyPlanBuilder.FirstDefinitionId);
        plan.Enemies.Should().HaveCount(21);
    }

    [Fact]
    public void Build_RemovesExcludedMovesFromSnapshots()
    {
        var self = CreateCandidate("Self", rebirthCount: 0, moveIds: [101, 102, 103]);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build([self], self, CreateFloors(), new HashSet<int> { 102 });

        plan.Enemies.Single().MoveIds.Select(x => x.Id).Should().Equal(101, 103);
    }

    [Fact]
    public void Build_UsesEffectiveStatusForSnapshot()
    {
        var status = new Status(1234, 56, 78, 90, 12, 34, 56);
        var self = CreateCandidate("Self", rebirthCount: 0, status: status);

        var plan = new MirrorEnemyPlanBuilder(MirrorTestConfigs.Default()).Build([self], self, CreateFloors(), NoExcludedMoves);

        var definition = plan.BuildEnemyDefinitions().Values.Single();
        definition.Status.MaxHp.Should().Be(1234);
        definition.Name.Should().Be("Self");
    }

    [Fact]
    public void AssignPositions_WhenThreeFrontJobs_OverflowGoesToMiddleRow()
    {
        var members = new[]
        {
            CreateCandidate("W1", rebirthCount: 0, job: Job.Warrior),
            CreateCandidate("W2", rebirthCount: 0, job: Job.Guardian),
            CreateCandidate("W3", rebirthCount: 0, job: Job.Shogun),
        };

        var positions = MirrorEnemyPlanBuilder.AssignPositions(members);

        positions.Should().Equal(
            new BattlePosition(BattleRow.Front, BattleColumn.Left),
            new BattlePosition(BattleRow.Front, BattleColumn.Right),
            new BattlePosition(BattleRow.Middle, BattleColumn.Left));
    }

    [Fact]
    public void AssignPositions_WhenBackRowIsFull_UsesFrontRowsFromMiddle()
    {
        var members = new[]
        {
            CreateCandidate("M1", rebirthCount: 0, job: Job.Mage),
            CreateCandidate("M2", rebirthCount: 0, job: Job.Priest),
            CreateCandidate("M3", rebirthCount: 0, job: Job.Archmage),
        };

        var positions = MirrorEnemyPlanBuilder.AssignPositions(members);

        positions.Should().Equal(
            new BattlePosition(BattleRow.Back, BattleColumn.Left),
            new BattlePosition(BattleRow.Back, BattleColumn.Right),
            new BattlePosition(BattleRow.Middle, BattleColumn.Left));
    }

    [Theory]
    [InlineData(Job.Warrior, BattleRow.Front)]
    [InlineData(Job.Guardian, BattleRow.Front)]
    [InlineData(Job.Trickster, BattleRow.Middle)]
    [InlineData(Job.Ranger, BattleRow.Middle)]
    [InlineData(Job.GreatThief, BattleRow.Middle)]
    [InlineData(Job.Apprentice, BattleRow.Middle)]
    [InlineData(Job.Necromancer, BattleRow.Middle)]
    [InlineData(Job.Mage, BattleRow.Back)]
    [InlineData(Job.HighPriest, BattleRow.Back)]
    public void ResolvePreferredRow_ReturnsRowByJob(Job job, BattleRow expected)
    {
        MirrorJobRowPolicy.ResolvePreferredRow(job).Should().Be(expected);
    }

    private static IReadOnlyList<string> NamesOnFloor(MirrorEnemyPlan plan, int floorNo)
    {
        return plan.GetFloor(floorNo).Placements
            .Select(x => plan.FindEnemy(x.EnemyDefinitionId)!.Name)
            .ToArray();
    }

    private static IReadOnlyList<QuestFloorDefinition> CreateFloors()
    {
        return
        [
            CreateFloor(1, FloorType.Normal),
            CreateFloor(2, FloorType.Normal),
            CreateFloor(3, FloorType.Normal),
            CreateFloor(4, FloorType.Normal),
            CreateFloor(5, FloorType.Boss),
        ];
    }

    private static QuestFloorDefinition CreateFloor(int floorNo, FloorType floorType)
    {
        return new QuestFloorDefinition(floorNo, floorType, [], new QuestFloorRewardRule(0, 0));
    }

    private static MirrorCandidate CreateCandidate(
        string name,
        int rebirthCount,
        int level = 100,
        int combatIndex = 0,
        Job job = Job.Warrior,
        Status? status = null,
        int[]? moveIds = null)
    {
        return new MirrorCandidate(
            new PlayerId(Guid.NewGuid()),
            name,
            "image/character/ch001_bmnpc.png",
            job,
            level,
            rebirthCount,
            status ?? new Status(100, 10, 10, 10, 10, 10, 10),
            combatIndex,
            (moveIds ?? [101]).Select(x => new MoveId(x)));
    }
}
