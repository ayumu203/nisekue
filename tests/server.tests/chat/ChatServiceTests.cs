using FluentAssertions;
using server.application.chat;
using server.domain.chat;
using server.domain.move;
using server.domain.player;
using Xunit;

namespace server.tests;

public class ChatServiceTests
{
    [Fact]
    public async Task PostSystemMessageAsync_AppendsSystemMessageToExistingRoom()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var roomRepository = new FakeChatRoomRepository(new ChatRoom(ownerId));
        var playerRepository = new FakePlayerRepository();
        var service = new ChatService(roomRepository, playerRepository);

        await service.PostSystemMessageAsync(ownerId, "銅の剣 x1 が 120 Gold で売れました。");

        var room = await roomRepository.GetChatRoomAsync(ownerId);
        room.Messages.Should().ContainSingle();
        room.Messages[0].SenderType.Should().Be(ChatMessageSenderType.System);
        room.Messages[0].SenderId.Should().BeNull();
        room.Messages[0].Body.Text.Should().Be("銅の剣 x1 が 120 Gold で売れました。");
    }

    [Fact]
    public async Task DeleteMessageAsync_BySender_DeletesAndHidesMessage()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var senderId = new PlayerId(Guid.NewGuid());
        var room = new ChatRoom(ownerId);
        room.PostMessage(senderId, "消したい発言");
        var roomRepository = new FakeChatRoomRepository(room);
        var service = new ChatService(roomRepository, new FakePlayerRepository());

        var result = await service.DeleteMessageAsync(ownerId, chatId: 1, senderId);

        result.Should().Be(ChatMessageDeleteResult.Deleted);
        var view = await service.GetRoomAsync(ownerId);
        view.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMessageAsync_ByRoomOwnerForOthersMessage_ReturnsForbidden()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var senderId = new PlayerId(Guid.NewGuid());
        var room = new ChatRoom(ownerId);
        room.PostMessage(senderId, "他人の発言");
        var roomRepository = new FakeChatRoomRepository(room);
        var service = new ChatService(roomRepository, new FakePlayerRepository());

        var result = await service.DeleteMessageAsync(ownerId, chatId: 1, ownerId);

        result.Should().Be(ChatMessageDeleteResult.Forbidden);
        var view = await service.GetRoomAsync(ownerId);
        view.Messages.Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteMessageAsync_SystemMessage_ReturnsForbidden()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var room = new ChatRoom(ownerId);
        room.PostSystemMessage("システム通知");
        var roomRepository = new FakeChatRoomRepository(room);
        var service = new ChatService(roomRepository, new FakePlayerRepository());

        var result = await service.DeleteMessageAsync(ownerId, chatId: 1, ownerId);

        result.Should().Be(ChatMessageDeleteResult.Forbidden);
    }

    [Fact]
    public async Task DeleteMessageAsync_AlreadyDeleted_ReturnsNotFound()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var senderId = new PlayerId(Guid.NewGuid());
        var room = new ChatRoom(ownerId);
        room.PostMessage(senderId, "消したい発言");
        var roomRepository = new FakeChatRoomRepository(room);
        var service = new ChatService(roomRepository, new FakePlayerRepository());
        await service.DeleteMessageAsync(ownerId, chatId: 1, senderId);

        var result = await service.DeleteMessageAsync(ownerId, chatId: 1, senderId);

        result.Should().Be(ChatMessageDeleteResult.NotFound);
    }

    [Fact]
    public async Task DeleteMessageAsync_MissingMessage_ReturnsNotFound()
    {
        var ownerId = new PlayerId(Guid.NewGuid());
        var roomRepository = new FakeChatRoomRepository(new ChatRoom(ownerId));
        var service = new ChatService(roomRepository, new FakePlayerRepository());

        var result = await service.DeleteMessageAsync(ownerId, chatId: 99, ownerId);

        result.Should().Be(ChatMessageDeleteResult.NotFound);
    }

    private sealed class FakeChatRoomRepository(ChatRoom room) : IChatRoomRepository
    {
        private ChatRoom storedRoom = room;
        private readonly HashSet<int> deletedChatIds = [];

        public Task<ChatRoom> GetChatRoomAsync(PlayerId ownerId)
        {
            if (storedRoom.OwnerId != ownerId)
            {
                return Task.FromResult(new ChatRoom(ownerId));
            }

            var visibleMessages = storedRoom.Messages.Where(x => !deletedChatIds.Contains(x.ChatId));
            return Task.FromResult(new ChatRoom(ownerId, storedRoom.LastChatId, visibleMessages));
        }

        public Task SaveAsync(ChatRoom room)
        {
            storedRoom = room;
            return Task.CompletedTask;
        }

        public Task<int> MarkMessagesAlertedAsync(PlayerId ownerId, IReadOnlyCollection<int> chatIds)
        {
            return Task.FromResult(0);
        }

        public Task<ChatMessage?> FindMessageAsync(PlayerId ownerId, int chatId)
        {
            var message = storedRoom.OwnerId == ownerId && !deletedChatIds.Contains(chatId)
                ? storedRoom.Messages.SingleOrDefault(x => x.ChatId == chatId)
                : null;
            return Task.FromResult(message);
        }

        public Task<bool> DeleteMessageAsync(PlayerId ownerId, int chatId, PlayerId senderId)
        {
            var message = storedRoom.OwnerId == ownerId
                ? storedRoom.Messages.SingleOrDefault(x => x.ChatId == chatId)
                : null;
            if (message is null || message.SenderId != senderId || deletedChatIds.Contains(chatId))
            {
                return Task.FromResult(false);
            }

            deletedChatIds.Add(chatId);
            return Task.FromResult(true);
        }
    }

    private sealed class FakePlayerRepository : IPlayerRepository
    {
        public Task<Player?> GetPlayerAsync(PlayerId id) => Task.FromResult<Player?>(null);
        public Task<Player?> GetPlayerWithinLevelCapAsync(PlayerId id, int maxLevel) => Task.FromResult<Player?>(null);
        public Task<IReadOnlyList<Player>> GetPvpOpponentsAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null) => Task.FromResult<IReadOnlyList<Player>>([]);
        public async Task<(IReadOnlyList<Player> Opponents, int TotalCount)> GetPvpOpponentsPageAsync(PlayerId excludeId, int maxLevel, int? offset = null, int? limit = null)
        {
            var opponents = await GetPvpOpponentsAsync(excludeId, maxLevel, offset, limit);
            return (opponents, opponents.Count);
        }
        public Task<int> CountPvpOpponentsAsync(PlayerId excludeId, int maxLevel) => Task.FromResult(0);
        public Task<IReadOnlyList<Player>> GetAllAsync(int? offset = null, int? limit = null) => Task.FromResult<IReadOnlyList<Player>>([]);
        public Task<int> CountAllAsync() => Task.FromResult(0);
        public Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids) => Task.FromResult<IReadOnlyList<Player>>([]);
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
