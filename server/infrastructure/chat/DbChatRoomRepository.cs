using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.domain.chat;
using server.domain.player;
using System.Data;

namespace server.infrastructure.chat
{
    public class DbChatRoomRepository(AppDbContext dbContext) : IChatRoomRepository
    {
        public async Task<ChatRoom> GetChatRoomAsync(PlayerId ownerId)
        {
            var roomEntity = await dbContext.ChatRooms
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Owner_id == ownerId);

            if (roomEntity is null)
            {
                return new ChatRoom(ownerId, lastChatId: 0);
            }

            var messageEntities = await dbContext.ChatMessages
                .AsNoTracking()
                .Where(x => x.Owner_id == ownerId)
                .OrderByDescending(x => x.Chat_id)
                .Take(50)
                .OrderBy(x => x.Chat_id)
                .ToListAsync();

            var messages = messageEntities.Select(MapToDomain);
            return new ChatRoom(ownerId, roomEntity.Last_chat_id, messages);
        }

        public async Task SaveAsync(ChatRoom room)
        {
            ArgumentNullException.ThrowIfNull(room);

            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var ownerId = room.OwnerId.Value;
                var dbLastChatId = await LockRoomAndGetLastChatIdAsync(ownerId);
                if (dbLastChatId is null)
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO internal.chat_rooms(owner_id, last_chat_id) VALUES ({ownerId}, 0) ON CONFLICT (owner_id) DO NOTHING");

                    dbLastChatId = await LockRoomAndGetLastChatIdAsync(ownerId)
                        ?? throw new InvalidOperationException("チャットルーム行のロック取得に失敗しました。");
                }

                var existingMessages = await dbContext.ChatMessages
                    .AsNoTracking()
                    .Where(x => x.Owner_id == room.OwnerId)
                    .Select(x => new { x.Chat_id, x.Sender_id, x.Message })
                    .ToDictionaryAsync(x => x.Chat_id);

                var nextChatId = dbLastChatId.Value;
                foreach (var message in room.Messages.OrderBy(x => x.ChatId))
                {
                    if (existingMessages.TryGetValue(message.ChatId, out var existing) &&
                        existing.Sender_id == message.SenderId &&
                        existing.Message == message.Body.Text)
                    {
                        continue;
                    }

                    checked
                    {
                        nextChatId++;
                    }

                    var chatIdToPersist = nextChatId;

                    await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT INTO internal.chat_messages(owner_id, chat_id, sender_id, message)
                        VALUES ({ownerId}, {chatIdToPersist}, {message.SenderId.Value}, {message.Body.Text})
                        ON CONFLICT (owner_id, chat_id) DO UPDATE
                        SET sender_id = EXCLUDED.sender_id,
                            message = EXCLUDED.message");
                }

                var persistedLastChatId = Math.Max(nextChatId, dbLastChatId.Value);
                await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE internal.chat_rooms
                    SET last_chat_id = {persistedLastChatId}
                    WHERE owner_id = {ownerId}");

                await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                    DELETE FROM internal.chat_messages AS m
                    WHERE m.owner_id = {ownerId}
                      AND m.chat_id NOT IN (
                        SELECT chat_id
                        FROM internal.chat_messages
                        WHERE owner_id = {ownerId}
                        ORDER BY chat_id DESC
                        LIMIT 50
                    )");

                await tx.CommitAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                throw new InvalidOperationException("チャット保存時に競合が発生しました。再試行してください。", ex);
            }
            catch (PostgresException ex)
            {
                throw new InvalidOperationException("チャットルーム情報の保存に失敗しました。", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("チャットルーム情報の保存に失敗しました。", ex);
            }
        }

        private async Task<int?> LockRoomAndGetLastChatIdAsync(Guid ownerId)
        {
            var rows = await dbContext.Database
                .SqlQueryRaw<int>("SELECT last_chat_id FROM internal.chat_rooms WHERE owner_id = {0} FOR UPDATE", ownerId)
                .ToListAsync();

            return rows.Count == 0 ? null : rows[0];
        }

        private static ChatMessage MapToDomain(ChatMessageEntity entity) =>
            new(entity.Sender_id, entity.Chat_id, new ChatText(entity.Message), entity.Created_at);
    }
}
