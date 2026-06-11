using FluentAssertions;
using server.application.chat;
using server.domain.chat;
using server.domain.player;
using Xunit;

namespace server.tests;

public class GlobalChatServiceTests
{
    [Fact]
    public async Task GetAsync_EmptyRoom_ReturnsEmptyMessages()
    {
        var repository = new FakeGlobalChatRoomRepository(new GlobalChatRoom());
        var playerRepository = new FakePlayerRepository();
        var service = new GlobalChatService(repository, playerRepository);

        var view = await service.GetAsync();

        view.Messages.Should().BeEmpty();
        view.LastChatId.Should().Be(0);
    }

    [Fact]
    public async Task PostMessageAsync_ValidSender_AppendsMessage()
    {
        var senderId = new PlayerId(Guid.NewGuid());
        var sender = new Player(
            senderId, "テストユーザー", 1, 0, 1, 0, 100,
            new Status(10, 0, 1, 1, 1, 1, 1));
        var repository = new FakeGlobalChatRoomRepository(new GlobalChatRoom());
        var playerRepository = new FakePlayerRepository(sender);
        var service = new GlobalChatService(repository, playerRepository);

        var view = await service.PostMessageAsync(senderId, "こんにちは全体");

        view.Messages.Should().ContainSingle();
        view.Messages[0].Message.Should().Be("こんにちは全体");
        view.Messages[0].SenderName.Should().Be("テストユーザー");
        view.Messages[0].SenderType.Should().Be(ChatMessageSenderType.Player.ToString());
    }

    [Fact]
    public async Task PostMessageAsync_SenderNotFound_ThrowsInvalidOperationException()
    {
        var senderId = new PlayerId(Guid.NewGuid());
        var repository = new FakeGlobalChatRoomRepository(new GlobalChatRoom());
        var playerRepository = new FakePlayerRepository();
        var service = new GlobalChatService(repository, playerRepository);

        var act = () => service.PostMessageAsync(senderId, "メッセージ");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*投稿者のプレイヤーが見つかりません*");
    }

    private sealed class FakeGlobalChatRoomRepository(GlobalChatRoom room) : IGlobalChatRoomRepository
    {
        private GlobalChatRoom storedRoom = room;

        public Task<GlobalChatRoom> GetAsync(int page = 1) => Task.FromResult(storedRoom);

        public Task<int> GetTotalCountAsync() => Task.FromResult(storedRoom.Messages.Count);

        public Task SaveAsync(GlobalChatRoom room)
        {
            storedRoom = room;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePlayerRepository(Player? player = null) : IPlayerRepository
    {
        public Task<Player?> GetPlayerAsync(PlayerId id) => Task.FromResult(player);
        public Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel) => Task.FromResult<Player?>(null);
        public Task<IReadOnlyList<Player>> GetPvpOpponentsAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null) => Task.FromResult<IReadOnlyList<Player>>([]);
        public async Task<(IReadOnlyList<Player> Opponents, int TotalCount)> GetPvpOpponentsPageAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null)
        {
            var opponents = await GetPvpOpponentsAsync(excludeId, maxLevel, offset, limit);
            return (opponents, opponents.Count);
        }
        public Task<int> CountPvpOpponentsAsync(PlayerId excludeId, int maxLevel) => Task.FromResult(0);
        public Task<IReadOnlyList<Player>> GetAllAsync(int? offset = null, int? limit = null) => Task.FromResult<IReadOnlyList<Player>>([]);
        public Task<int> CountAllAsync() => Task.FromResult(player is null ? 0 : 1);
        public Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids) =>
            Task.FromResult<IReadOnlyList<Player>>(player is not null ? [player] : []);
        public Task<bool> UpdateNameAsync(PlayerId id, string name) => Task.FromResult(false);
        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown) => Task.FromResult<DateTimeOffset?>(null);
        public Task SaveAsync(Player player) => Task.CompletedTask;
    }
}
