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
            senderId, "テストユーザー", 1, 0, 1, 100,
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

    [Fact]
    public async Task DeleteMessageAsync_BySender_DeletesAndExcludesFromCount()
    {
        var senderId = new PlayerId(Guid.NewGuid());
        var room = new GlobalChatRoom();
        room.PostMessage(senderId, "消したい発言");
        room.PostMessage(senderId, "残す発言");
        var repository = new FakeGlobalChatRoomRepository(room);
        var service = new GlobalChatService(repository, new FakePlayerRepository());

        var result = await service.DeleteMessageAsync(chatId: 1, senderId);

        result.Should().Be(ChatMessageDeleteResult.Deleted);
        var view = await service.GetAsync();
        view.Messages.Should().ContainSingle().Which.Message.Should().Be("残す発言");
        view.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteMessageAsync_ByOtherPlayer_ReturnsForbidden()
    {
        var senderId = new PlayerId(Guid.NewGuid());
        var otherId = new PlayerId(Guid.NewGuid());
        var room = new GlobalChatRoom();
        room.PostMessage(senderId, "他人の発言");
        var repository = new FakeGlobalChatRoomRepository(room);
        var service = new GlobalChatService(repository, new FakePlayerRepository());

        var result = await service.DeleteMessageAsync(chatId: 1, otherId);

        result.Should().Be(ChatMessageDeleteResult.Forbidden);
        var view = await service.GetAsync();
        view.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteMessageAsync_MissingMessage_ReturnsNotFound()
    {
        var repository = new FakeGlobalChatRoomRepository(new GlobalChatRoom());
        var service = new GlobalChatService(repository, new FakePlayerRepository());

        var result = await service.DeleteMessageAsync(chatId: 1, new PlayerId(Guid.NewGuid()));

        result.Should().Be(ChatMessageDeleteResult.NotFound);
    }

    private sealed class FakeGlobalChatRoomRepository(GlobalChatRoom room) : IGlobalChatRoomRepository
    {
        private GlobalChatRoom storedRoom = room;
        private readonly HashSet<int> deletedChatIds = [];

        public Task<GlobalChatRoom> GetAsync(int page = 1)
        {
            var visibleMessages = storedRoom.Messages.Where(x => !deletedChatIds.Contains(x.ChatId));
            return Task.FromResult(new GlobalChatRoom(storedRoom.LastChatId, visibleMessages));
        }

        public Task<int> GetTotalCountAsync() =>
            Task.FromResult(storedRoom.Messages.Count(x => !deletedChatIds.Contains(x.ChatId)));

        public Task SaveAsync(GlobalChatRoom room)
        {
            storedRoom = room;
            return Task.CompletedTask;
        }

        public Task<ChatMessage?> FindMessageAsync(int chatId)
        {
            var message = deletedChatIds.Contains(chatId)
                ? null
                : storedRoom.Messages.SingleOrDefault(x => x.ChatId == chatId);
            return Task.FromResult(message);
        }

        public Task<bool> DeleteMessageAsync(int chatId, PlayerId senderId)
        {
            var message = storedRoom.Messages.SingleOrDefault(x => x.ChatId == chatId);
            if (message is null || message.SenderId != senderId || deletedChatIds.Contains(chatId))
            {
                return Task.FromResult(false);
            }

            deletedChatIds.Add(chatId);
            return Task.FromResult(true);
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
        public async Task<(IReadOnlyList<Player> Players, int TotalCount)> GetPlayersPageExcludingAsync(PlayerId excludeId, int? offset = null, int? limit = null)
        {
            var allPlayers = await GetAllAsync();
            var filtered = allPlayers.Where(x => x.Id != excludeId).ToArray();
            var paged = filtered.AsEnumerable();

            if (offset is > 0)
            {
                paged = paged.Skip(offset.Value);
            }

            if (limit is > 0)
            {
                paged = paged.Take(limit.Value);
            }

            return (paged.ToArray(), filtered.Length);
        }
        public Task<bool> UpdateNameAsync(PlayerId id, string name) => Task.FromResult(false);
        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown) => Task.FromResult<DateTimeOffset?>(null);
        public Task SaveAsync(Player player) => Task.CompletedTask;
    }
}
