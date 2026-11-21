using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using server.application.chat;
using server.domain.chat;
using server.domain.player;
using server.infrastructure.chat.images;
using server.shared.constants.chat;

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

        // 画像を付けられるか（投稿フォームで画像の選択欄を出すかどうかに使う）
        app.MapGet("/threads/image-permission", async (
            ClaimsPrincipal user,
            IPlayerRoleRepository playerRoleRepository,
            IThreadImageStorage imageStorage) =>
        {
            var currentPlayerId = EndpointHelpers.TryGetPlayerId(user);
            if (currentPlayerId is null)
            {
                return Results.Unauthorized();
            }

            var canPostImage = !EndpointHelpers.IsAnonymousUser(user)
                && PlayerRolePolicy.CanPostThreadImage(await playerRoleRepository.FindRoleAsync(currentPlayerId.Value));

            return Results.Ok(new
            {
                canPostImage,
                isImageUploadAvailable = imageStorage.IsAvailable,
                maxImageBytes = ThreadConstants.ImageMaxUploadBytes
            });
        }).RequireAuthorization();

        // 画像なしは JSON（{ title, body }）、画像ありは multipart/form-data（title, body, image, imageIsSensitive）
        app.MapPost("/threads", async (
            HttpRequest httpRequest,
            ClaimsPrincipal user,
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

            var (request, error) = await ReadCreateThreadRequestAsync(httpRequest);
            if (error is not null)
            {
                return error;
            }

            try
            {
                var thread = await threadService.CreateAsync(
                    currentPlayerId.Value,
                    request!.Title,
                    request.Body,
                    request.Image);
                return Results.Ok(EndpointHelpers.MapThreadDetail(thread));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return EndpointHelpers.Forbidden(ex.Message);
            }
            catch (ThreadImageUnavailableException ex)
            {
                return Results.Json(new { message = ex.Message }, options: null, contentType: null, statusCode: StatusCodes.Status503ServiceUnavailable);
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

    /// <summary>ローカル開発用。LocalThreadImageStorage に保存した画像を配信する（R2 を使う環境では登録しない）。</summary>
    internal static WebApplication MapLocalThreadImageEndpoint(this WebApplication app)
    {
        app.MapGet($"{LocalThreadImageStorage.RoutePrefix}/{{**key}}", (string key, LocalThreadImageStorage storage, HttpResponse response) =>
        {
            var path = storage.FindFile(key);
            if (path is null)
            {
                return Results.NotFound();
            }

            response.Headers["X-Content-Type-Options"] = "nosniff";
            var contentType = key.EndsWith(".png", StringComparison.Ordinal) ? "image/png" : "image/jpeg";
            return Results.File(path, contentType);
        }).ExcludeFromDescription();

        return app;
    }

    // 画像を含めても、本文などを足してこの大きさに収まる
    private const long MaxCreateThreadRequestBytes = ThreadConstants.ImageMaxUploadBytes + (1024 * 1024);

    private sealed record CreateThreadInput(string Title, string Body, ThreadImageUpload? Image);

    private static async Task<(CreateThreadInput? Request, IResult? Error)> ReadCreateThreadRequestAsync(HttpRequest httpRequest)
    {
        var sizeFeature = httpRequest.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = MaxCreateThreadRequestBytes;
        }

        if (httpRequest.ContentLength > MaxCreateThreadRequestBytes)
        {
            return (null, ImageTooLarge());
        }

        try
        {
            if (!httpRequest.HasFormContentType)
            {
                var json = await httpRequest.ReadFromJsonAsync<CreateThreadRequest>();
                return json is null
                    ? (null, Results.BadRequest(new { message = "リクエストの形式が正しくありません。" }))
                    : (new CreateThreadInput(json.Title ?? string.Empty, json.Body ?? string.Empty, null), null);
            }

            var form = await httpRequest.ReadFormAsync();
            var title = form["title"].ToString();
            var body = form["body"].ToString();
            var file = form.Files.GetFile("image");
            if (file is null || file.Length == 0)
            {
                return (new CreateThreadInput(title, body, null), null);
            }

            if (file.Length > ThreadConstants.ImageMaxUploadBytes)
            {
                return (null, ImageTooLarge());
            }

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream((int)file.Length);
            await stream.CopyToAsync(buffer);

            var isSensitive = bool.TryParse(form["imageIsSensitive"].ToString(), out var parsed) && parsed;
            return (new CreateThreadInput(title, body, new ThreadImageUpload(buffer.ToArray(), isSensitive)), null);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return (null, ImageTooLarge());
        }
        catch (InvalidDataException)
        {
            return (null, ImageTooLarge());
        }
        catch (System.Text.Json.JsonException)
        {
            return (null, Results.BadRequest(new { message = "リクエストの形式が正しくありません。" }));
        }
    }

    private static IResult ImageTooLarge() =>
        Results.Json(
            new { message = $"画像は{ThreadConstants.ImageMaxUploadBytes / 1024 / 1024}MBまでです。" },
            options: null,
            contentType: null,
            statusCode: StatusCodes.Status413PayloadTooLarge);
}
