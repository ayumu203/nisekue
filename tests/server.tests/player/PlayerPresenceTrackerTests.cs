using FluentAssertions;
using server.application.player;
using Xunit;

namespace server.tests.player;

public class PlayerPresenceTrackerTests
{
    [Fact]
    public void CountOnlinePlayers_WhenNoConnections_ReturnsZero()
    {
        var tracker = new PlayerPresenceTracker();

        tracker.CountOnlinePlayers().Should().Be(0);
    }

    [Fact]
    public void CountOnlinePlayers_WhenSamePlayerHasMultipleConnections_CountsOnce()
    {
        var tracker = new PlayerPresenceTracker();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        tracker.Connect("a-1", playerA);
        tracker.Connect("a-2", playerA);
        tracker.Connect("b-1", playerB);

        tracker.CountOnlinePlayers().Should().Be(2);
    }

    [Fact]
    public void Disconnect_WhenOneOfMultipleConnectionsCloses_KeepsPlayerOnline()
    {
        var tracker = new PlayerPresenceTracker();
        var player = Guid.NewGuid();
        tracker.Connect("a-1", player);
        tracker.Connect("a-2", player);

        tracker.Disconnect("a-1");

        tracker.CountOnlinePlayers().Should().Be(1);
    }

    [Fact]
    public void Disconnect_WhenLastConnectionCloses_RemovesPlayer()
    {
        var tracker = new PlayerPresenceTracker();
        tracker.Connect("a-1", Guid.NewGuid());

        tracker.Disconnect("a-1");

        tracker.CountOnlinePlayers().Should().Be(0);
    }

    [Fact]
    public void Disconnect_WhenConnectionIsUnknown_DoesNothing()
    {
        var tracker = new PlayerPresenceTracker();
        tracker.Connect("a-1", Guid.NewGuid());

        tracker.Disconnect("unknown");

        tracker.CountOnlinePlayers().Should().Be(1);
    }
}
