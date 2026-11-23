using FluentAssertions;
using server.application.quest;
using server.domain.player;
using Xunit;

namespace server.tests.quest;

public class QuestInProgressGuardTests
{
    [Fact]
    public async Task EnsureNotInQuestAsync_WhenInQuest_ThrowsQuestInProgressExceptionWithMessage()
    {
        var guard = new FakeQuestInProgressGuard(isInQuest: true);

        var act = () => guard.EnsureNotInQuestAsync(new PlayerId(Guid.NewGuid()), QuestInProgressMessages.Rebirth);

        // 既存の 409 処理（InvalidOperationException）に乗ることも確認する。
        (await act.Should().ThrowAsync<QuestInProgressException>())
            .Which.Should().BeAssignableTo<InvalidOperationException>()
            .Which.Message.Should().Be(QuestInProgressMessages.Rebirth);
    }

    [Fact]
    public async Task EnsureNotInQuestAsync_WhenNotInQuest_DoesNotThrow()
    {
        var guard = new FakeQuestInProgressGuard(isInQuest: false);

        var act = () => guard.EnsureNotInQuestAsync(new PlayerId(Guid.NewGuid()), QuestInProgressMessages.Rebirth);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsInQuestAsync_ReturnsWhetherPlayerHasActiveRun(bool hasActiveRun)
    {
        var guard = new FakeQuestInProgressGuard(isInQuest: hasActiveRun);

        var result = await guard.IsInQuestAsync(new PlayerId(Guid.NewGuid()));

        result.Should().Be(hasActiveRun);
    }
}
