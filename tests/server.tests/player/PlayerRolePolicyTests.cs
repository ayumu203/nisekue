using FluentAssertions;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerRolePolicyTests
{
    [Theory]
    [InlineData(PlayerRole.Admin, true)]
    [InlineData(PlayerRole.Poster, true)]
    [InlineData(null, false)]
    public void CanPostThreadImage_ByRole_ReturnsExpected(PlayerRole? role, bool expected)
    {
        PlayerRolePolicy.CanPostThreadImage(role).Should().Be(expected);
    }

    [Theory]
    [InlineData(100, 0, true)]
    [InlineData(99, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(50, 2, true)]
    [InlineData(1, 0, false)]
    public void IsEligibleForPoster_ByLevelAndRebirth_ReturnsExpected(int level, int rebirthCount, bool expected)
    {
        var player = CreatePlayer(level, rebirthCount);

        PlayerRolePolicy.IsEligibleForPoster(player).Should().Be(expected);
    }

    private static Player CreatePlayer(int level, int rebirthCount) =>
        new(
            new PlayerId(Guid.NewGuid()),
            "Tester",
            level: level,
            exp: 0,
            jobLevel: 1,
            gold: 100,
            status: new Status(30, 10, 10, 5, 3, 3, 5, accuracy: 120, evasion: 7),
            rebirthCount: rebirthCount);
}
