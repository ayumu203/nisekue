using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using server.application.chat;
using server.domain.chat;
using server.domain.player;

namespace server.endpoints;

internal static class ThreadEndpoints
{
    internal static WebApplication MapThreadEndpoints(this WebApplication app)
    {
        app.MapGet("/threads", async (int? page, ThreadService threadService) =>
        {
            var result = await threadService.GetPageAsync(page ?? 1);
            return Results.Ok(EndpointHelpers.MapThreadPage(result));
        }).RequireAuthorization();

        app.MapGet("/threads/{threadId:guid}", async (Guid threadId, ThreadService threadService) =>
        {
            var thread = await threadService.GetByIdAsync(new ThreadId(threadId));
            return thread is null
                ? Results.NotFound(new { message = "スレッドが見つかりません。" })
                : Results.Ok(EndpointHelpers.MapThreadDetail(thread));
        }).RequireAuthorization();

        app.MapGet("/threads/alerts", async (ClaimsPrincipal user, ThreadService threadService) =>
        {
            var currentPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (currentPlayerId is null)
            {
                return Results.Unauthorized();
            }

            var alerts = await threadService.GetAlertSummariesAsync(currentPlayerId.Value);
            return Results.Ok(new
            {
                items = alerts.Select(x => new
                {
                    threadId = x.ThreadId,
                    threadTitle = x.ThreadTitle,
                    replyIds = x.ReplyIds,
                    latestReplyAuthorName = x.LatestReplyAuthorName,
                    latestReplyCreatedAt = x.LatestReplyCreatedAt,
                    unalertedReplyCount = x.UnalertedReplyCount
                })
            });
        }).RequireAuthorization();

        app.MapPost("/threads", async (
            ClaimsPrincipal user,
            CreateThreadRequest request,
            ThreadService threadService,
            IPlayerRepository playerRepository) =>
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

            var player = await playerRepository.GetPlayerAsync(currentPlayerId.Value);
            if (player is null)
            {
                return Results.NotFound(new { message = "投稿者のプレイヤーが見つかりません。" });
            }

            try
            {
                var thread = await threadService.CreateAsync(currentPlayerId.Value, request.Title, request.Body);
                return Results.Ok(EndpointHelpers.MapThreadDetail(thread));
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

        app.MapPost("/threads/{threadId:guid}/replies", async (
            Guid threadId,
            ClaimsPrincipal user,
            CreateThreadReplyRequest request,
            ThreadService threadService,
            IPlayerRepository playerRepository) =>
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

            var player = await playerRepository.GetPlayerAsync(currentPlayerId.Value);
            if (player is null)
            {
                return Results.NotFound(new { message = "返信者のプレイヤーが見つかりません。" });
            }

            try
            {
                var thread = await threadService.AddReplyAsync(new ThreadId(threadId), currentPlayerId.Value, request.Body);
                return Results.Ok(EndpointHelpers.MapThreadDetail(thread));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        app.MapPost("/threads/alerts", async (
            ClaimsPrincipal user,
            MarkThreadRepliesAlertedRequest request,
            ThreadService threadService) =>
        {
            var currentPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (currentPlayerId is null)
            {
                return Results.Unauthorized();
            }

            var replyIds = request.ReplyIds
                .Distinct()
                .Select(x => new ThreadReplyId(x))
                .ToArray();
            var updatedCount = await threadService.MarkRepliesAlertedAsync(currentPlayerId.Value, replyIds);
            return Results.Ok(new { updatedCount });
        }).RequireAuthorization();

        app.MapDelete("/threads/{threadId:guid}", async (
            Guid threadId,
            ClaimsPrincipal user,
            ThreadService threadService) =>
        {
            var currentPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (currentPlayerId is null)
            {
                return Results.Unauthorized();
            }

            try
            {
                await threadService.DeleteAsync(new ThreadId(threadId), currentPlayerId.Value);
                return Results.Ok(new { message = "スレッドを削除しました。" });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { message = ex.Message }, options: null, contentType: null, statusCode: StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        }).RequireAuthorization();

        return app;
    }
}
