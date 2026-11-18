using server.domain.player;

namespace server.domain.chat;

public interface IGlobalChatRoomRepository
{
    Task<GlobalChatRoom> GetAsync(int page = 1);
    Task<int> GetTotalCountAsync();
    Task SaveAsync(GlobalChatRoom room);

    /// <summary>削除されていないメッセージを1件取得する。存在しない、または削除済みなら null。</summary>
    Task<ChatMessage?> FindMessageAsync(int chatId);

    /// <summary>投稿者が senderId と一致し、未削除のメッセージを論理削除する。更新できたら true。</summary>
    Task<bool> DeleteMessageAsync(int chatId, PlayerId senderId);
}
