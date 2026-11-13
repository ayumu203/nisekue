using FluentAssertions;
using server.application.debug;
using server.domain.player;
using Xunit;

namespace server.tests;

public class DebugAccessServiceTests
{
    [Fact]
    public async Task CanUseAsync_WhenAdmin_ReturnsTrue()
    {
        var service = CreateService(enabled: true, alwaysEnabled: false, role: PlayerRole.Admin);

        (await service.CanUseAsync(new PlayerId(Guid.NewGuid()))).Should().BeTrue();
    }

    [Fact]
    public async Task CanUseAsync_WhenRoleIsNull_ReturnsFalse()
    {
        var service = CreateService(enabled: true, alwaysEnabled: false, role: null);

        (await service.CanUseAsync(new PlayerId(Guid.NewGuid()))).Should().BeFalse();
    }

    [Fact]
    public async Task CanUseAsync_WhenAlwaysEnabled_ReturnsTrueWithoutRole()
    {
        var service = CreateService(enabled: true, alwaysEnabled: true, role: null);

        (await service.CanUseAsync(new PlayerId(Guid.NewGuid()))).Should().BeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanUseAsync_WhenDisabled_ReturnsFalseEvenForAdmin(bool alwaysEnabled)
    {
        var service = CreateService(enabled: false, alwaysEnabled: alwaysEnabled, role: PlayerRole.Admin);

        (await service.CanUseAsync(new PlayerId(Guid.NewGuid()))).Should().BeFalse();
    }

    private static DebugAccessService CreateService(bool enabled, bool alwaysEnabled, PlayerRole? role)
    {
        return new DebugAccessService(new DebugSettings(enabled, alwaysEnabled), new FixedRoleRepository(role));
    }

    private sealed class FixedRoleRepository(PlayerRole? role) : IPlayerRoleRepository
    {
        public Task<PlayerRole?> FindRoleAsync(PlayerId id) => Task.FromResult(role);
    }
}
