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

    private sealed class FakeChatRoomRepository(ChatRoom room) : IChatRoomRepository
    {
        private ChatRoom storedRoom = room;

        public Task<ChatRoom> GetChatRoomAsync(PlayerId ownerId)
            => Task.FromResult(storedRoom.OwnerId == ownerId ? storedRoom : new ChatRoom(ownerId));

        public Task SaveAsync(ChatRoom room)
        {
            storedRoom = room;
            return Task.CompletedTask;
        }

        public Task<int> MarkMessagesAlertedAsync(PlayerId ownerId, IReadOnlyCollection<int> chatIds)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class FakePlayerRepository : IPlayerRepository
    {
        public Task<Player?> GetPlayerAsync(PlayerId id) => Task.FromResult<Player?>(null);
        public Task<IReadOnlyList<Player>> GetAllAsync() => Task.FromResult<IReadOnlyList<Player>>([]);
        public Task<IReadOnlyList<Player>> GetPlayersAsync(IEnumerable<PlayerId> ids) => Task.FromResult<IReadOnlyList<Player>>([]);
        public Task<bool> UpdateNameAsync(PlayerId id, string name) => Task.FromResult(false);
        public Task<DateTimeOffset?> TryStartTrainingCooldownAsync(PlayerId id, DateTimeOffset nowUtc, TimeSpan cooldown) => Task.FromResult<DateTimeOffset?>(null);
        public Task SaveAsync(Player player) => Task.CompletedTask;
    }
}
