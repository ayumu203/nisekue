using server.domain.chat;
using server.domain.player;

namespace server.application.chat;

public class GlobalChatService(IGlobalChatRoomRepository globalChatRoomRepository, IPlayerRepository playerRepository)
{
    public async Task<GlobalChatRoomView> GetAsync()
    {
        var room = await globalChatRoomRepository.GetAsync();
        return await BuildViewAsync(room);
    }

    public async Task<GlobalChatRoomView> PostMessageAsync(PlayerId senderId, string text)
    {
        var sender = await playerRepository.GetPlayerAsync(senderId);
        if (sender is null)
        {
            throw new InvalidOperationException("投稿者のプレイヤーが見つかりません。");
        }

        var room = await globalChatRoomRepository.GetAsync();
        room.PostMessage(senderId, text);
        await globalChatRoomRepository.SaveAsync(room);

        return await GetAsync();
    }

    private async Task<GlobalChatRoomView> BuildViewAsync(GlobalChatRoom room)
    {
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

                return new GlobalChatMessageView(
                    ChatId: x.ChatId,
                    SenderType: x.SenderType.ToString(),
                    SenderId: x.SenderId,
                    SenderName: senderProfile.Name,
                    ImagePath: senderProfile.ImagePath,
                    Message: x.Body.Text,
                    CreatedAt: x.CreatedAt);
            })
            .ToArray();

        return new GlobalChatRoomView(
            LastChatId: room.LastChatId,
            Messages: messageViews);
    }
}
