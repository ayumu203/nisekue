using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.domain.chat;
using server.domain.player;
using server.shared.constants.chat;
using System.Data;

namespace server.infrastructure.chat;

public class DbGlobalChatRoomRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IGlobalChatRoomRepository
{
    public async Task<GlobalChatRoom> GetAsync(int page = 1)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var roomEntity = await dbContext.GlobalChatRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ChatConstants.GlobalChatRoomId);

        if (roomEntity is null)
        {
            return new GlobalChatRoom(lastChatId: 0);
        }

        var skip = (page - 1) * ChatConstants.PageSize;
        var messageEntities = await dbContext.GlobalChatMessages
            .AsNoTracking()
            .OrderByDescending(x => x.ChatId)
            .Skip(skip)
            .Take(ChatConstants.PageSize)
            .OrderBy(x => x.ChatId)
            .ToListAsync();

        var messages = messageEntities.Select(MapToDomain);
        return new GlobalChatRoom(roomEntity.LastChatId, messages);
    }

    public async Task<int> GetTotalCountAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GlobalChatMessages.AsNoTracking().CountAsync();
    }

    public async Task SaveAsync(GlobalChatRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var roomId = ChatConstants.GlobalChatRoomId;
            var dbLastChatId = await LockRoomAndGetLastChatIdAsync(dbContext, roomId);
            if (dbLastChatId is null)
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO internal.global_chat_rooms(id, last_chat_id) VALUES ({roomId}, 0) ON CONFLICT (id) DO NOTHING");

                dbLastChatId = await LockRoomAndGetLastChatIdAsync(dbContext, roomId)
                    ?? throw new InvalidOperationException("全体チャットルーム行のロック取得に失敗しました。");
            }

            var newMessages = room.Messages
                .Where(x => x.ChatId > room.PersistedLastChatId)
                .OrderBy(x => x.ChatId)
                .ToList();

            for (var i = 0; i < newMessages.Count; i++)
            {
                var message = newMessages[i];
                var assignedChatId = dbLastChatId.Value + 1 + i;
                await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO internal.global_chat_messages(chat_id, sender_type, sender_id, message)
                    VALUES ({assignedChatId}, {(int)message.SenderType}, {(message.SenderId == null ? (Guid?)null : message.SenderId.Value.Value)}, {message.Body.Text})
                    ON CONFLICT (chat_id) DO NOTHING");
            }

            var persistedLastChatId = newMessages.Count == 0
                ? dbLastChatId.Value
                : dbLastChatId.Value + newMessages.Count;
            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE internal.global_chat_rooms
                SET last_chat_id = {persistedLastChatId}
                WHERE id = {roomId}");

            await tx.CommitAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InvalidOperationException("全体チャット保存時に競合が発生しました。再試行してください。", ex);
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException("全体チャットルーム情報の保存に失敗しました。", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("全体チャットルーム情報の保存に失敗しました。", ex);
        }
    }

    private static async Task<int?> LockRoomAndGetLastChatIdAsync(AppDbContext dbContext, Guid roomId)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<int>("SELECT last_chat_id FROM internal.global_chat_rooms WHERE id = {0} FOR UPDATE", roomId)
            .ToListAsync();

        return rows.Count == 0 ? null : rows[0];
    }

    private static ChatMessage MapToDomain(GlobalChatMessageEntity entity) =>
        new(entity.SenderType, entity.SenderId, entity.ChatId, new ChatText(entity.Message), entity.CreatedAt, isAlerted: false);
}
