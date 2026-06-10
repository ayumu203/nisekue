using FluentAssertions;
using server.domain.pet_battle;
using server.domain.player;
using Xunit;

namespace server.tests.pet_battle;

public class PlayerPetBattleStatsTests
{
    private static readonly PlayerId PlayerId = new(Guid.NewGuid());
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void CreateInitial_SetsDefaultValues()
    {
        var stats = PlayerPetBattleStats.CreateInitial(PlayerId, Now);

        stats.PlayerId.Should().Be(PlayerId);
        stats.Rating.Should().Be(PetBattleConstants.InitialRating);
        stats.Wins.Should().Be(0);
        stats.Losses.Should().Be(0);
        stats.TotalBattles.Should().Be(0);
        stats.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void ApplyWin_IncrementsRatingAndWins()
    {
        var stats = PlayerPetBattleStats.CreateInitial(PlayerId, Now);

        stats.ApplyWin(Now);

        stats.Rating.Should().Be(PetBattleConstants.InitialRating + PetBattleConstants.WinPoints);
        stats.Wins.Should().Be(1);
        stats.TotalBattles.Should().Be(1);
        stats.Losses.Should().Be(0);
    }

    [Fact]
    public void ApplyLoss_DecrementsRatingAndIncreasesLosses()
    {
        var stats = PlayerPetBattleStats.CreateInitial(PlayerId, Now);

        stats.ApplyLoss(Now);

        stats.Rating.Should().Be(PetBattleConstants.InitialRating - PetBattleConstants.LossPoints);
        stats.Losses.Should().Be(1);
        stats.TotalBattles.Should().Be(1);
        stats.Wins.Should().Be(0);
    }

    [Fact]
    public void ApplyLoss_WhenRatingWouldGoBelowFloor_ClampedToFloor()
    {
        var stats = new PlayerPetBattleStats(PlayerId, PetBattleConstants.RatingFloor, 0, 0, 0, Now);

        stats.ApplyLoss(Now);

        stats.Rating.Should().Be(PetBattleConstants.RatingFloor);
    }

    [Theory]
    [InlineData(4, 0)]   // 4 - 5 = -1 → クランプして 0
    [InlineData(5, 0)]   // 5 - 5 = 0  → ちょうど下限
    [InlineData(6, 1)]   // 6 - 5 = 1  → クランプ非発動
    public void ApplyLoss_ClampsBoundary_ToExpectedRating(int initialRating, int expectedRating)
    {
        var stats = new PlayerPetBattleStats(PlayerId, initialRating, 0, 0, 0, Now);

        stats.ApplyLoss(Now);

        stats.Rating.Should().Be(expectedRating);
    }

    [Fact]
    public void ApplyWin_ThenApplyLoss_ResultsInNetPositiveRating()
    {
        var stats = PlayerPetBattleStats.CreateInitial(PlayerId, Now);

        stats.ApplyWin(Now);
        stats.ApplyLoss(Now);

        stats.Rating.Should().Be(
            PetBattleConstants.InitialRating + PetBattleConstants.WinPoints - PetBattleConstants.LossPoints);
        stats.Wins.Should().Be(1);
        stats.Losses.Should().Be(1);
        stats.TotalBattles.Should().Be(2);
    }
}
