using FluentAssertions;
using server.application.player;
using server.application.quest;
using server.domain.player;
using server.tests.quest;
using Xunit;

namespace server.tests;

public class PlayerDataResetServiceTests
{
    [Fact]
    public async Task ResetDataAsync_WhenNotInQuest_ResetsPlayer()
    {
        var player = CreatePlayer();
        var mutationService = new RecordingPlayerMutationService(player);
        var service = new PlayerDataResetService(mutationService, new FakeQuestInProgressGuard());

        var result = await service.ResetDataAsync(player.Id);

        mutationService.MutateCount.Should().Be(1);
        result.Level.Should().Be(1);
    }

    [Fact]
    public async Task ResetDataAsync_WhenInQuest_ThrowsAndDoesNotResetPlayer()
    {
        var player = CreatePlayer();
        var mutationService = new RecordingPlayerMutationService(player);
        var service = new PlayerDataResetService(mutationService, new FakeQuestInProgressGuard(isInQuest: true));

        var act = () => service.ResetDataAsync(player.Id);

        await act.Should().ThrowAsync<QuestInProgressException>()
            .WithMessage(QuestInProgressMessages.DataReset);
        mutationService.MutateCount.Should().Be(0);
        player.Level.Should().Be(50);
    }

    private static Player CreatePlayer()
    {
        return new Player(
            new PlayerId(Guid.NewGuid()),
            "Tester",
            level: 50,
            exp: 0,
            jobLevel: 1,
            gold: 1_000_000,
            status: new Status(300, 100, 80, 60, 40, 30, 50));
    }

    private sealed class RecordingPlayerMutationService(Player player) : IPlayerMutationService
    {
        public int MutateCount { get; private set; }

        public async Task<TResult> MutateAsync<TResult>(PlayerId playerId, Func<Player, Task<TResult>> mutation)
        {
            MutateCount++;
            return await mutation(player);
        }
    }
}
