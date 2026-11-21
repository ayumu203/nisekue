using server.application.chat;
using server.domain.chat;
using server.domain.player;

namespace server.endpoints;

/// <summary>
/// 管理者用ワークフロー（GitHub Actions）から呼ぶ /internal/* のエンドポイント。
/// - set-player-role-*.yml: Poster 権限の付け外し
/// - delete-thread-*.yml: スレッドの削除（画像も消す）
/// - set-thread-image-sensitive-*.yml: スレッド画像のセンシティブ設定
/// いずれも X-Maintenance-Token で認証する。
/// </summary>
internal static class ModerationEndpoints
{
    private const string PosterRoleValue = "poster";
    private const string NoneRoleValue = "none";

    internal static WebApplication MapModerationEndpoints(this WebApplication app)
    {
        app.MapPut("/internal/players/{playerId:guid}/role", async (
            HttpRequest request,
            IConfiguration configuration,
            Guid playerId,
            SetPlayerRoleRequest body,
            IPlayerRepository playerRepository,
            IPlayerRoleRepository playerRoleRepository) =>
        {
            var tokenError = EndpointHelpers.ValidateMaintenanceToken(request, configuration);
            if (tokenError is not null)
            {
                return tokenError;
            }

            var role = body.Role?.Trim().ToLowerInvariant();
            if (role is not (PosterRoleValue or NoneRoleValue))
            {
                return Results.Problem(
                    detail: $"role は '{PosterRoleValue}' か '{NoneRoleValue}' を指定してください。",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var id = new PlayerId(playerId);
            var player = await playerRepository.GetPlayerAsync(id);
            if (player is null)
            {
                return Results.Problem(
                    detail: $"プレイヤー '{playerId}' は見つかりません。",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var currentRole = await playerRoleRepository.FindRoleAsync(id);
            if (currentRole == PlayerRole.Admin)
            {
                return Results.Problem(
                    detail: "管理者（admin）の権限はワークフローからは変更できません。Supabase から変更してください。",
                    statusCode: StatusCodes.Status409Conflict);
            }

            PlayerRole? newRole = null;
            if (role == PosterRoleValue)
            {
                if (!PlayerRolePolicy.IsEligibleForPoster(player))
                {
                    return Results.Problem(
                        detail: $"{player.Name} は条件（レベル{PlayerRolePolicy.PosterRequiredLevel}以上、または転生{PlayerRolePolicy.PosterRequiredRebirthCount}回以上）を満たしていません。"
                            + $"（レベル{player.Level}、転生{player.RebirthCount}回）",
                        statusCode: StatusCodes.Status409Conflict);
                }

                newRole = PlayerRole.Poster;
            }

            await playerRoleRepository.SetRoleAsync(id, newRole);
            return Results.Ok(new
            {
                message = newRole is null ? "権限を解除しました。" : "画像投稿の権限（poster）を付与しました。",
                playerId,
                name = player.Name,
                level = player.Level,
                rebirthCount = player.RebirthCount,
                role = newRole is null ? NoneRoleValue : PosterRoleValue
            });
        }).ExcludeFromDescription();

        app.MapDelete("/internal/threads/{threadId:guid}", async (
            HttpRequest request,
            IConfiguration configuration,
            Guid threadId,
            ThreadService threadService) =>
        {
            var tokenError = EndpointHelpers.ValidateMaintenanceToken(request, configuration);
            if (tokenError is not null)
            {
                return tokenError;
            }

            try
            {
                await threadService.DeleteByAdminAsync(new ThreadId(threadId));
                return Results.Ok(new { message = "スレッドを削除しました。", threadId });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        }).ExcludeFromDescription();

        app.MapPut("/internal/threads/{threadId:guid}/image-sensitive", async (
            HttpRequest request,
            IConfiguration configuration,
            Guid threadId,
            SetThreadImageSensitiveRequest body,
            ThreadService threadService) =>
        {
            var tokenError = EndpointHelpers.ValidateMaintenanceToken(request, configuration);
            if (tokenError is not null)
            {
                return tokenError;
            }

            try
            {
                await threadService.SetImageSensitiveAsync(new ThreadId(threadId), body.IsSensitive);
                return Results.Ok(new
                {
                    message = body.IsSensitive ? "画像をセンシティブに設定しました。" : "画像のセンシティブ設定を解除しました。",
                    threadId,
                    isSensitive = body.IsSensitive
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        }).ExcludeFromDescription();

        return app;
    }
}
