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

        // narrow候補がいるので wideId は選ばれない
        var results = Enumerable.Range(0, 30)
            .Select(_ => PetBattleMatchingService.SelectOpponent(OwnerId, 1000, candidates))
            .ToArray();

        results.Should().OnlyContain(id => id == narrowId);
    }
}
