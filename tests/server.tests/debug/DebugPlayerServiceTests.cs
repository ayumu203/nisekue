using FluentAssertions;
using server.application.debug;
using server.domain.player;
using Xunit;

namespace server.tests;

public class DebugPlayerServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_SetsBaseStatusAndKeepsOtherValues()
    {
        var player = DebugTestPlayers.Create();
        var service = CreateService(player);

        await service.UpdateStatusAsync(player.Id, new DebugStatusValues(999, 99, 50, 40, 30, 20, 10));

        player.Status.MaxHp.Should().Be(999);
        player.Status.MaxMp.Should().Be(99);
        player.Status.Strength.Should().Be(50);
        player.Status.Defense.Should().Be(40);
        player.Status.Intelligence.Should().Be(30);
        player.Status.Luck.Should().Be(20);
        player.Status.Speed.Should().Be(10);
        player.Status.Accuracy.Should().Be(120);
        player.Status.Evasion.Should().Be(7);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenMaxHpIsZero_Throws()
    {
        var player = DebugTestPlayers.Create();
        var service = CreateService(player);

        var act = () => service.UpdateStatusAsync(player.Id, new DebugStatusValues(0, 0, 0, 0, 0, 0, 0));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task LearnMoveAsync_WithoutSlot_AddsToFirstEmptySlot()
    {
        var player = DebugTestPlayers.Create(moveIds: [1, 2]);
        var service = CreateService(player, 1, 2, 3);

        await service.LearnMoveAsync(player.Id, 3, slotIndex: null);

        player.MoveSet.Slots.Take(3).Select(x => x?.Id).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task LearnMoveAsync_WithSlot_MovesExistingMoveToSlot()
    {
        var player = DebugTestPlayers.Create(moveIds: [1, 2, 3]);
        var service = CreateService(player, 1, 2, 3);

        await service.LearnMoveAsync(player.Id, 1, slotIndex: 5);

        player.MoveSet.Slots.Select(x => x?.Id).Should().Equal(null, 2, 3, null, null, 1, null, null, null, null);
    }

    [Fact]
    public async Task LearnMoveAsync_WhenMoveDoesNotExist_ThrowsArgumentException()
    {
        var player = DebugTestPlayers.Create();
        var service = CreateService(player, 1);

        var act = () => service.LearnMoveAsync(player.Id, 999, slotIndex: null);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public async Task LearnMoveAsync_WhenSlotOutOfRange_ThrowsArgumentOutOfRangeException(int slotIndex)
    {
        var player = DebugTestPlayers.Create();
        var service = CreateService(player, 1);

        var act = () => service.LearnMoveAsync(player.Id, 1, slotIndex);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ClearMoveSlotAsync_ClearsSlot()
    {
        var player = DebugTestPlayers.Create(moveIds: [1, 2]);
        var service = CreateService(player, 1, 2);

        await service.ClearMoveSlotAsync(player.Id, 0);

        player.MoveSet.Slots.Take(2).Select(x => x?.Id).Should().Equal(null, 2);
    }

    private static DebugPlayerService CreateService(Player player, params int[] moveIds)
    {
        return new DebugPlayerService(new SinglePlayerMutationService(player), new FixedMoveRepository(moveIds));
    }
}
