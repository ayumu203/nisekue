using server.domain.chat;
using server.domain.player;

namespace server.application.chat;

public class ChatService(IChatRoomRepository chatRoomRepository, IPlayerRepository playerRepository)
{
    public async Task<ChatRoomView> GetRoomAsync(PlayerId ownerId)
    {
        var room = await chatRoomRepository.GetChatRoomAsync(ownerId);
        // 投稿者のIDを取得
        var senderIds = room.Messages
            .Select(x => x.SenderId)
            .Distinct()
            .ToArray();

        // 投稿者のIDからユーザ名を取得する
        var senderLookupTasks = senderIds.Select(async senderId =>
        {
            var player = await playerRepository.GetPlayerAsync(senderId);
            return (senderId, senderName: player?.Name ?? "Unknown");
        });

        var senderLookupResults = await Task.WhenAll(senderLookupTasks);
        var senderNameMap = senderLookupResults.ToDictionary(x => x.senderId, x => x.senderName);

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
