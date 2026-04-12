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
            .OfType<PlayerId>()
            .Distinct()
            .ToArray();

        var senderProfileMap = new Dictionary<PlayerId, (string Name, string? ImagePath)>();
        foreach (var senderId in senderIds)
        {
            var player = await playerRepository.GetPlayerAsync(senderId);
            senderProfileMap[senderId] = (player?.Name ?? "Unknown", player?.ImagePath);
        }

        var messageViews = room.Messages
            .OrderBy(x => x.ChatId)
            .Select(x =>
            {
                var senderProfile = x.SenderId is not null && senderProfileMap.TryGetValue(x.SenderId.Value, out var profile)
                    ? profile
                    : ("System", null);

                return new ChatMessageView(
                    ChatId: x.ChatId,
                    SenderType: x.SenderType.ToString(),
                    SenderId: x.SenderId,
                    SenderName: senderProfile.Name,
                    ImagePath: senderProfile.ImagePath,
                    Message: x.Body.Text,
                    CreatedAt: x.CreatedAt,
                    IsAlerted: x.IsAlerted);
            })
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

    public async Task PostSystemMessageAsync(PlayerId ownerId, string text)
    {
        var room = await chatRoomRepository.GetChatRoomAsync(ownerId);
        room.PostSystemMessage(text);
        await chatRoomRepository.SaveAsync(room);
    }

    public Task<int> MarkMessagesAlertedAsync(PlayerId ownerId, IReadOnlyCollection<int> chatIds)
    {
        return chatIds.Count == 0
            ? Task.FromResult(0)
            : chatRoomRepository.MarkMessagesAlertedAsync(ownerId, chatIds);
    }
}
