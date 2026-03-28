using System.Security.Claims;
using server.application.chat;
using server.domain.player;

namespace server.endpoints;

internal static class ChatEndpoints
{
    internal static WebApplication MapChatEndpoints(this WebApplication app)
    {
        app.MapGet("/chat/room", async (Guid ownerId, ChatService chatService) =>
        {
            var room = await chatService.GetRoomAsync(new PlayerId(ownerId));

            return Results.Ok(new
            {
                ownerId = room.OwnerId.Value,
                lastChatId = room.LastChatId,
                messages = room.Messages.Select(x => new
                {
                    chatId = x.ChatId,
                    senderType = x.SenderType,
                    senderId = x.SenderId?.Value,
                    senderName = x.SenderName,
                    imagePath = x.ImagePath,
                    text = x.Message,
                    createdAt = x.CreatedAt
                })
            });
        }).RequireAuthorization();

        app.MapPost("/chat/room/messages", async (ClaimsPrincipal user, PostChatMessageRequest request, ChatService chatService, IPlayerRepository playerRepository) =>
        {
            var currentPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (currentPlayerId is null)
            {
                return Results.Unauthorized();
            }

            var ownerId = new PlayerId(request.OwnerId);
            var senderId = currentPlayerId.Value;
            var owner = await playerRepository.GetPlayerAsync(ownerId);
            if (owner is null)
            {
                return Results.NotFound(new { message = "送信先プレイヤーが見つかりません。" });
            }

            var sender = await playerRepository.GetPlayerAsync(senderId);
            if (sender is null)
            {
                return Results.NotFound(new { message = "投稿者のプレイヤーが見つかりません。" });
            }

            try
            {
                var room = await chatService.PostMessageAsync(ownerId, senderId, request.Text);
                return Results.Ok(new
                {
                    ownerId = room.OwnerId.Value,
                    lastChatId = room.LastChatId,
                    messages = room.Messages.Select(x => new
                    {
                        chatId = x.ChatId,
                        senderType = x.SenderType,
                        senderId = x.SenderId?.Value,
                        senderName = x.SenderName,
                        imagePath = x.ImagePath,
                        text = x.Message,
                        createdAt = x.CreatedAt
                    })
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        return app;
    }
}
