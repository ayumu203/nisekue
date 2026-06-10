using System.Security.Claims;
using server.application.chat;
using server.domain.player;
using server.shared.constants.chat;

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
                    createdAt = x.CreatedAt,
                    isAlerted = x.IsAlerted
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

            if (EndpointHelpers.IsAnonymousUser(user))
            {
                return EndpointHelpers.AnonymousPostingForbidden();
            }

            if (request.Text.Length > ChatConstants.MessageMaxLength)
            {
                return Results.BadRequest(new { message = $"メッセージは{ChatConstants.MessageMaxLength}文字以内で入力してください。" });
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
                        createdAt = x.CreatedAt,
                        isAlerted = x.IsAlerted
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

        app.MapPost("/chat/room/messages/alerts", async (
            ClaimsPrincipal user,
            MarkChatMessagesAlertedRequest request,
            ChatService chatService) =>
        {
            var currentPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (currentPlayerId is null)
            {
                return Results.Unauthorized();
            }

            if (currentPlayerId.Value.Value != request.OwnerId)
            {
                return Results.Json(new { message = "他プレイヤーの通知状態は更新できません。" }, statusCode: StatusCodes.Status403Forbidden);
            }

            var updatedCount = await chatService.MarkMessagesAlertedAsync(currentPlayerId.Value, request.ChatIds.Distinct().ToArray());
            return Results.Ok(new { updatedCount });
        }).RequireAuthorization();

        app.MapGet("/chat/global", async (int? page, GlobalChatService globalChatService) =>
        {
            var currentPage = Math.Max(1, page ?? 1);
            var room = await globalChatService.GetAsync(currentPage);

            return Results.Ok(new
            {
                lastChatId = room.LastChatId,
                totalCount = room.TotalCount,
                messages = room.Messages.Select(x => new
                {
                    chatId = x.ChatId,
                    senderType = x.SenderType,
                    senderId = x.SenderId?.Value,
                    senderName = x.SenderName,
                    imagePath = x.ImagePath,
                    text = x.Message,
                    createdAt = x.CreatedAt,
                })
            });
        }).RequireAuthorization();

        app.MapPost("/chat/global/messages", async (ClaimsPrincipal user, PostGlobalChatMessageRequest request, GlobalChatService globalChatService) =>
        {
            var currentPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (currentPlayerId is null)
            {
                return Results.Unauthorized();
            }

            if (EndpointHelpers.IsAnonymousUser(user))
            {
                return EndpointHelpers.AnonymousPostingForbidden();
            }

            if (request.Text.Length > ChatConstants.MessageMaxLength)
            {
                return Results.BadRequest(new { message = $"メッセージは{ChatConstants.MessageMaxLength}文字以内で入力してください。" });
            }

            try
            {
                var room = await globalChatService.PostMessageAsync(currentPlayerId.Value, request.Text);
                return Results.Ok(new
                {
                    lastChatId = room.LastChatId,
                    totalCount = room.TotalCount,
                    messages = room.Messages.Select(x => new
                    {
                        chatId = x.ChatId,
                        senderType = x.SenderType,
                        senderId = x.SenderId?.Value,
                        senderName = x.SenderName,
                        imagePath = x.ImagePath,
                        text = x.Message,
                        createdAt = x.CreatedAt,
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
