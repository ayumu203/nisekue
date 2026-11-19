using server.domain.player;

namespace server.domain.chat;

public interface IChatRoomRepository
{
    Task<ChatRoom> GetChatRoomAsync(PlayerId ownerId);
    Task SaveAsync(ChatRoom room);
    Task<int> MarkMessagesAlertedAsync(PlayerId ownerId, IReadOnlyCollection<int> chatIds);

    /// <summary>削除されていないメッセージを1件取得する。存在しない、または削除済みなら null。</summary>
    Task<ChatMessage?> FindMessageAsync(PlayerId ownerId, int chatId);

    /// <summary>投稿者が senderId と一致し、未削除のメッセージを論理削除する。更新できたら true。</summary>
    Task<bool> DeleteMessageAsync(PlayerId ownerId, int chatId, PlayerId senderId);
}
