using server.domain.chat;
using server.domain.player;

namespace server.application.chat;

public class ChatService(IChatRoomRepository chatRoomRepository, IPlayerRepository playerRepository)
{
    public async Task EnsureRoomAsync(PlayerId ownerId)
    {
        var room = await chatRoomRepository.GetChatRoomAsync(ownerId);
        await chatRoomRepository.SaveAsync(room);
    }

    public async Task<ChatRoomView> GetRoomAsync(PlayerId ownerId)
    {
        var room = await chatRoomRepository.GetChatRoomAsync(ownerId);
        // 投稿者のIDを取得
        var senderIds = room.Messages
            .Select(x => x.SenderId)
            .Distinct()
            .ToArray();

        // 同一スコープのDbContextを並列利用しないよう順次取得する
        var senderNameMap = new Dictionary<PlayerId, string>();
        foreach (var senderId in senderIds)
        {
            var player = await playerRepository.GetPlayerAsync(senderId);
            senderNameMap[senderId] = player?.Name ?? "Unknown";
        }

        var messageViews = room.Messages
            .OrderBy(x => x.ChatId)
            .Select(x => new ChatMessageView(
                ChatId: x.ChatId,
                SenderName: senderNameMap.TryGetValue(x.SenderId, out var senderName) ? senderName : "Unknown",
                Message: x.Body.Text,
                CreatedAt: x.CreatedAt))
            .ToArray();

        return new ChatRoomView(
            OwnerId: room.OwnerId,
            LastChatId: room.LastChatId,
            Messages: messageViews);
    }

    public async Task<ChatRoomView> PostMessageAsync(PlayerId ownerId, PlayerId senderId, string text)
    {
        var sender = await playerRepository.GetPlayerAsync(senderId);
        if (sender is null)
        {
            throw new InvalidOperationException("投稿者のプレイヤーが見つかりません。");
        }

        var room = await chatRoomRepository.GetChatRoomAsync(ownerId);
        room.PostMessage(senderId, text);
        await chatRoomRepository.SaveAsync(room);

        return await GetRoomAsync(ownerId);
    }
}
