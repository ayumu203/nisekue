using FluentAssertions;
using server.domain.pet_battle;
using server.domain.player;
using Xunit;

namespace server.tests.pet_battle;

public class PetBattleMatchingServiceTests
{
    private static readonly PlayerId OwnerId = new(Guid.NewGuid());

    [Fact]
    public void SelectOpponent_WhenNoCandidates_ReturnsNull()
    {
        var result = PetBattleMatchingService.SelectOpponent(OwnerId, 1000, []);

        result.Should().BeNull();
    }

    [Fact]
    public void SelectOpponent_WhenAllCandidatesHaveNoPets_ReturnsNull()
    {
        var candidates = new[]
        {
            new MatchingCandidate(new PlayerId(Guid.NewGuid()), 1000, PetCount: 0),
            new MatchingCandidate(new PlayerId(Guid.NewGuid()), 1000, PetCount: 0),
        };

        var result = PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates);

        result.Should().BeNull();
    }

    [Fact]
    public void SelectOpponent_WhenOnlyOwnerInList_ReturnsNull()
    {
        var candidates = new[]
        {
            new MatchingCandidate(OwnerId, 1000, PetCount: 3),
        };

        var result = PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates);

        result.Should().BeNull();
    }

    [Fact]
    public void SelectOpponent_WhenNarrowRangeMatch_ReturnsNarrowRangeCandidate()
    {
        var nearId = new PlayerId(Guid.NewGuid());
        var farId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(nearId, 1050, PetCount: 2),   // 差50: narrow範囲内
            new MatchingCandidate(farId, 1300, PetCount: 2),    // 差300: wide範囲のみ
        };

        // narrow候補のみになるはずなので nearId が返る
        var results = Enumerable.Range(0, 20)
            .Select(_ => PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates))
            .ToArray();

        results.Should().OnlyContain(id => id == nearId);
    }

    [Fact]
    public void SelectOpponent_WhenNoNarrowButWideRangeMatch_ReturnsWideRangeCandidate()
    {
        var wideId = new PlayerId(Guid.NewGuid());
        var outOfRangeId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(wideId, 1300, PetCount: 2),      // 差300: wide範囲内
            new MatchingCandidate(outOfRangeId, 1600, PetCount: 2), // 差600: 全候補のみ
        };

        var results = Enumerable.Range(0, 20)
            .Select(_ => PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates))
            .ToArray();

        results.Should().OnlyContain(id => id == wideId);
    }

    [Fact]
    public void SelectOpponent_WhenNoRangeMatch_ReturnsAnyEligibleCandidate()
    {
        var id1 = new PlayerId(Guid.NewGuid());
        var id2 = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(id1, 200, PetCount: 1),
            new MatchingCandidate(id2, 1800, PetCount: 1),
        };

        var results = Enumerable.Range(0, 50)
            .Select(_ => PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates))
            .ToArray();

        results.Should().NotContainNulls();
        results.Should().OnlyContain(id => id == id1 || id == id2);
    }

    [Fact]
    public void SelectOpponent_PrefersNarrowOverWideRange()
    {
        var narrowId = new PlayerId(Guid.NewGuid());
        var wideId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(narrowId, 1100, PetCount: 1),  // 差100
            new MatchingCandidate(wideId, 1400, PetCount: 1),    // 差400
        };

        var results = Enumerable.Range(0, 30)
            .Select(_ => PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates))
            .ToArray();

        results.Should().OnlyContain(id => id == narrowId);
    }

    // --- 境界値: narrow ±200 ---

    [Fact]
    public void SelectOpponent_WhenDiffExactlyNarrowBound_IsIncludedInNarrow()
    {
        var narrowId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(narrowId, 1200, PetCount: 1),  // 差200: narrow境界上
        };

        var result = PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates);

        result.Should().Be(narrowId);
    }

    [Fact]
    public void SelectOpponent_WhenDiffExactlyNarrowBoundLower_IsIncludedInNarrow()
    {
        var narrowId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(narrowId, 800, PetCount: 1),   // 差-200: 負方向のnarrow境界上
        };

        var result = PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates);

        result.Should().Be(narrowId);
    }

    [Fact]
    public void SelectOpponent_WhenDiffOneOverNarrowBound_FallsToWideTier()
    {
        var overId = new PlayerId(Guid.NewGuid());
        var withinId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(overId, 1201, PetCount: 1),    // 差201: narrow外・wide内
            new MatchingCandidate(withinId, 1100, PetCount: 1),  // 差100: narrow内
        };

        // narrow候補がいるので overId は選ばれない
        var results = Enumerable.Range(0, 20)
            .Select(_ => PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates))
            .ToArray();

        results.Should().OnlyContain(id => id == withinId);
    }

    // --- 境界値: wide ±500 ---

    [Fact]
    public void SelectOpponent_WhenDiffExactlyWideBound_IsIncludedInWide()
    {
        var wideId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(wideId, 1500, PetCount: 1),   // 差500: wide境界上
        };

        var result = PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates);

        result.Should().Be(wideId);
    }

    [Fact]
    public void SelectOpponent_WhenDiffOneOverWideBound_FallsToAllTier()
    {
        var overId = new PlayerId(Guid.NewGuid());
        var withinId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(overId, 1501, PetCount: 1),    // 差501: wide外
            new MatchingCandidate(withinId, 1400, PetCount: 1),  // 差400: wide内
        };

        // wide候補がいるので overId は選ばれない
        var results = Enumerable.Range(0, 20)
            .Select(_ => PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates))
            .ToArray();

        results.Should().OnlyContain(id => id == withinId);
    }

    [Fact]
    public void SelectOpponent_WhenLowerRatedOpponent_IsSelectedByNarrowRange()
    {
        var lowerId = new PlayerId(Guid.NewGuid());
        var candidates = new[]
        {
            new MatchingCandidate(lowerId, 850, PetCount: 2),   // 差-150: 低レート側、narrow内
        };

        var result = PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates);

        result.Should().Be(lowerId);
    }
}
