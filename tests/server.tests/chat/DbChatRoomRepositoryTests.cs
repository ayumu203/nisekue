using FluentAssertions;
using Microsoft.Data.Sqlite;
using server.domain.chat;
using server.domain.player;
using server.infrastructure.chat;
using Xunit;

namespace server.tests;

public class DbChatRoomRepositoryTests
{
    [Fact]
    public async Task GetChatRoomAsync_DeletedMessage_IsExcluded()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var ownerId = new PlayerId(Guid.NewGuid());
        var senderId = new PlayerId(Guid.NewGuid());
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.ChatRooms.Add(new ChatRoomEntity(ownerId, lastChatId: 2));
            seedContext.ChatMessages.Add(new ChatMessageEntity(ownerId, 1, ChatMessageSenderType.Player, senderId, "消した発言", DateTimeOffset.UtcNow, isAlerted: false, isDeleted: true));
            seedContext.ChatMessages.Add(new ChatMessageEntity(ownerId, 2, ChatMessageSenderType.Player, senderId, "残る発言", DateTimeOffset.UtcNow, isAlerted: false, isDeleted: false));
            await seedContext.SaveChangesAsync();
        }

        var repository = new DbChatRoomRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var room = await repository.GetChatRoomAsync(ownerId);
        var deleted = await repository.FindMessageAsync(ownerId, 1);
        var remaining = await repository.FindMessageAsync(ownerId, 2);

        room.Messages.Should().ContainSingle().Which.Body.Text.Should().Be("残る発言");
        room.LastChatId.Should().Be(2);
        deleted.Should().BeNull();
        remaining.Should().NotBeNull();
        remaining!.CanBeDeletedBy(senderId).Should().BeTrue();
    }
}
