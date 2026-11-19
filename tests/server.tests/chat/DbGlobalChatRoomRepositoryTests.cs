using FluentAssertions;
using Microsoft.Data.Sqlite;
using server.domain.chat;
using server.domain.player;
using server.infrastructure.chat;
using server.shared.constants.chat;
using Xunit;

namespace server.tests;

public class DbGlobalChatRoomRepositoryTests
{
    [Fact]
    public async Task GetAsync_DeletedMessage_IsExcludedFromMessagesAndCount()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var senderId = new PlayerId(Guid.NewGuid());
        await using (var seedContext = TestHelpers.CreateSqliteDbContext(connection))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.GlobalChatRooms.Add(new GlobalChatRoomEntity(ChatConstants.GlobalChatRoomId, lastChatId: 2));
            seedContext.GlobalChatMessages.Add(new GlobalChatMessageEntity(1, ChatMessageSenderType.Player, senderId, "消した発言", DateTimeOffset.UtcNow, isDeleted: true));
            seedContext.GlobalChatMessages.Add(new GlobalChatMessageEntity(2, ChatMessageSenderType.Player, senderId, "残る発言", DateTimeOffset.UtcNow, isDeleted: false));
            await seedContext.SaveChangesAsync();
        }

        var repository = new DbGlobalChatRoomRepository(new TestHelpers.SqliteDbContextFactory(connection));

        var room = await repository.GetAsync();
        var totalCount = await repository.GetTotalCountAsync();
        var deleted = await repository.FindMessageAsync(1);
        var remaining = await repository.FindMessageAsync(2);

        room.Messages.Should().ContainSingle().Which.Body.Text.Should().Be("残る発言");
        room.LastChatId.Should().Be(2);
        totalCount.Should().Be(1);
        deleted.Should().BeNull();
        remaining.Should().NotBeNull();
    }
}
