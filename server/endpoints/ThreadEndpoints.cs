using System.Security.Claims;
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

        return app;
    }
}
