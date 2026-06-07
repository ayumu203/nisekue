using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.domain.chat;
using server.domain.player;
using server.shared.constants.chat;
using System.Data;

namespace server.infrastructure.chat
{
    public class DbChatRoomRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IChatRoomRepository
    {
        public async Task<ChatRoom> GetChatRoomAsync(PlayerId ownerId)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var roomEntity = await dbContext.ChatRooms
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.OwnerId == ownerId);

            if (roomEntity is null)
            {
                return new ChatRoom(ownerId, lastChatId: 0);
            }

            var messageEntities = await dbContext.ChatMessages
                .AsNoTracking()
                .Where(x => x.OwnerId == ownerId)
                .OrderByDescending(x => x.ChatId)
                .Take(ChatConstants.MessageLimit)
                .OrderBy(x => x.ChatId)
                .ToListAsync();

            var chatIds = messageEntities.Select(x => x.ChatId).ToList();
            var alertEntities = await dbContext.ChatMessageAlerts
                .AsNoTracking()
                .Where(x => x.OwnerId == ownerId && chatIds.Contains(x.ChatId))
                .ToDictionaryAsync(x => x.ChatId, x => x.IsAlerted);

            var messages = messageEntities.Select(e => MapToDomain(e, alertEntities));
            return new ChatRoom(ownerId, roomEntity.LastChatId, messages);
        }

        public async Task SaveAsync(ChatRoom room)
        {
            ArgumentNullException.ThrowIfNull(room);

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var ownerId = room.OwnerId.Value;
                var dbLastChatId = await LockRoomAndGetLastChatIdAsync(dbContext, ownerId);
                if (dbLastChatId is null)
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO internal.chat_rooms(owner_id, last_chat_id) VALUES ({ownerId}, 0) ON CONFLICT (owner_id) DO NOTHING");

                    dbLastChatId = await LockRoomAndGetLastChatIdAsync(dbContext, ownerId)
                        ?? throw new InvalidOperationException("チャットルーム行のロック取得に失敗しました。");
                }

                var messagesToPersist = room.Messages
                    .Where(x => x.ChatId > dbLastChatId.Value)
                    .OrderBy(x => x.ChatId)
                    .ToList();

                foreach (var message in messagesToPersist)
                {
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT INTO internal.chat_messages(owner_id, chat_id, sender_type, sender_id, message, is_alerted)
                        VALUES ({ownerId}, {message.ChatId}, {(int)message.SenderType}, {(message.SenderId == null ? (Guid?)null : message.SenderId.Value.Value)}, {message.Body.Text}, {message.IsAlerted})
                        ON CONFLICT (owner_id, chat_id) DO NOTHING");

                    await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT INTO internal.chat_message_alerts(owner_id, chat_id, is_alerted)
                        VALUES ({ownerId}, {message.ChatId}, {message.IsAlerted})
                        ON CONFLICT (owner_id, chat_id) DO NOTHING");
                }

                var maxPersistedChatId = messagesToPersist.Count == 0
                    ? dbLastChatId.Value
                    : messagesToPersist.Max(x => x.ChatId);
                var persistedLastChatId = Math.Max(room.LastChatId, maxPersistedChatId);
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
                        LIMIT {ChatConstants.MessageLimit}
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

        public async Task<int> MarkMessagesAlertedAsync(PlayerId ownerId, IReadOnlyCollection<int> chatIds)
        {
            ArgumentNullException.ThrowIfNull(chatIds);

            if (chatIds.Count == 0)
            {
                return 0;
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var distinctIds = chatIds.Distinct().ToArray();
            return await dbContext.ChatMessageAlerts
                .Where(x => x.OwnerId == ownerId && distinctIds.Contains(x.ChatId) && !x.IsAlerted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsAlerted, true));
        }

        private static async Task<int?> LockRoomAndGetLastChatIdAsync(AppDbContext dbContext, Guid ownerId)
        {
            var rows = await dbContext.Database
                .SqlQueryRaw<int>("SELECT last_chat_id FROM internal.chat_rooms WHERE owner_id = {0} FOR UPDATE", ownerId)
                .ToListAsync();

            return rows.Count == 0 ? null : rows[0];
        }

        private static ChatMessage MapToDomain(ChatMessageEntity entity, Dictionary<int, bool> alertLookup)
        {
            var isAlerted = alertLookup.TryGetValue(entity.ChatId, out var alerted) && alerted;
            return new(entity.SenderType, entity.SenderId, entity.ChatId, new ChatText(entity.Message), entity.CreatedAt, isAlerted);
        }
    }
}
