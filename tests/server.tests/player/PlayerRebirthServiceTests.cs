using FluentAssertions;
using server.application.player;
using server.domain.player;
using Xunit;

namespace server.tests;

public class PlayerRebirthServiceTests
{
    [Fact]
    public async Task RebirthAsync_DelegatesToExecutorWithGivenPlayerId()
    {
        var executor = new CapturingRebirthExecutor();
        var service = new PlayerRebirthService(executor);
        var playerId = new PlayerId(Guid.NewGuid());

        await service.RebirthAsync(playerId);

        executor.ReceivedPlayerId.Should().Be(playerId);
        executor.CapturedBuildInheritedStatus.Should().NotBeNull();
    }

    [Fact]
    public async Task RebirthAsync_BuildsInheritedStatusWithinExpectedRange()
    {
        var executor = new CapturingRebirthExecutor();
        var service = new PlayerRebirthService(executor);

        await service.RebirthAsync(new PlayerId(Guid.NewGuid()));
        var buildInheritedStatus = executor.CapturedBuildInheritedStatus!;

        var source = new Status(maxHp: 200, maxMp: 200, strength: 200, defense: 200, intelligence: 200, luck: 200, speed: 200);

        for (var i = 0; i < 200; i++)
        {
            var inherited = buildInheritedStatus(source);

            inherited.MaxHp.Should().BeInRange(50, 70);
            inherited.MaxMp.Should().BeInRange(50, 70);
            inherited.Strength.Should().BeInRange(50, 70);
            inherited.Defense.Should().BeInRange(50, 70);
            inherited.Intelligence.Should().BeInRange(50, 70);
            inherited.Luck.Should().BeInRange(50, 70);
            inherited.Speed.Should().BeInRange(50, 70);
        }
    }

    [Fact]
    public async Task RebirthAsync_BuildsInheritedMaxHpAtLeastOne()
    {
        var executor = new CapturingRebirthExecutor();
        var service = new PlayerRebirthService(executor);

        await service.RebirthAsync(new PlayerId(Guid.NewGuid()));
        var buildInheritedStatus = executor.CapturedBuildInheritedStatus!;

        var source = new Status(maxHp: 1, maxMp: 0, strength: 0, defense: 0, intelligence: 0, luck: 0, speed: 0);
        var inherited = buildInheritedStatus(source);

        inherited.MaxHp.Should().BeGreaterThanOrEqualTo(1);
    }

    private sealed class CapturingRebirthExecutor : IPlayerRebirthExecutor
    {
        public PlayerId? ReceivedPlayerId { get; private set; }
        public Func<Status, Status>? CapturedBuildInheritedStatus { get; private set; }

        public Task<Player> ExecuteAsync(PlayerId playerId, Func<Status, Status> buildInheritedStatus)
        {
            ReceivedPlayerId = playerId;
            CapturedBuildInheritedStatus = buildInheritedStatus;

            var player = new Player(
                playerId,
                "tester",
                level: 1,
                exp: 0,
                jobLevel: 1,
                gold: 0,
                status: new Status(maxHp: 1, maxMp: 0, strength: 0, defense: 0, intelligence: 0, luck: 0, speed: 0));
            return Task.FromResult(player);
        }
    }
}