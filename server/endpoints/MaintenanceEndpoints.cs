using System.Net;
using System.Security.Cryptography;
using System.Text;
using server.application.maintenance;
using server.application.player;

namespace server.endpoints;

internal static class MaintenanceEndpoints
{
    private const string MaintenanceTokenHeaderName = "X-Maintenance-Token";

    internal static WebApplication MapMaintenanceEndpoints(this WebApplication app)
    {
        app.MapPost("/internal/market/listings/cleanup-expired", async (
            HttpRequest request,
            IConfiguration configuration,
            MarketListingCleanupService marketListingCleanupService) =>
        {
            var expectedToken = configuration["Maintenance:MarketCleanupToken"];
            if (string.IsNullOrWhiteSpace(expectedToken))
            {
                return Results.Problem(
                    detail: "Maintenance:MarketCleanupToken が設定されていません。",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var providedToken = request.Headers[MaintenanceTokenHeaderName].ToString();
            if (!SecureEquals(providedToken, expectedToken))
            {
                return Results.Unauthorized();
            }

            var result = await marketListingCleanupService.DeleteExpiredAsync(DateTimeOffset.UtcNow);
            return Results.Ok(new
            {
                message = "期限切れ出品を削除しました。",
                deletedListings = result.DeletedListings,
                deletedEquipments = result.DeletedEquipments,
                deletedItemQuantity = result.DeletedItemQuantity
            });
        }).ExcludeFromDescription();

        app.MapPost("/internal/development/cleanup-game-data", async (
            HttpRequest request,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            DevelopmentDataCleanupService developmentDataCleanupService) =>
        {
            if (!IsLocalDevelopmentRequest(request))
            {
                var expectedToken = configuration["Maintenance:MarketCleanupToken"];
                if (string.IsNullOrWhiteSpace(expectedToken))
                {
                    return Results.Problem(
                        detail: "Maintenance:MarketCleanupToken が設定されていません。",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                var providedToken = request.Headers[MaintenanceTokenHeaderName].ToString();
                if (!SecureEquals(providedToken, expectedToken))
                {
                    return Results.Unauthorized();
                }
            }

            try
            {
                var result = await developmentDataCleanupService.CleanupAsync();
                return Results.Ok(new
                {
                    message = "開発用クエストデータを削除しました。",
                    deletedQuestRooms = result.DeletedQuestRooms,
                    deletedQuestRoomAllowedPlayers = result.DeletedQuestRoomAllowedPlayers,
                    deletedQuestRoomParticipants = result.DeletedQuestRoomParticipants,
                    deletedQuestRuns = result.DeletedQuestRuns,
                    deletedQuestRunPartySnapshots = result.DeletedQuestRunPartySnapshots,
                    deletedQuestRunPartyMembers = result.DeletedQuestRunPartyMembers,
                    deletedQuestRunEnemies = result.DeletedQuestRunEnemies,
                    deletedQuestTurnCommands = result.DeletedQuestTurnCommands,
                    deletedQuestFloorTraps = result.DeletedQuestFloorTraps,
                    deletedQuestRewardSummaries = result.DeletedQuestRewardSummaries
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"ゲームデータの削除中にエラーが発生しました: {ex.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }).ExcludeFromDescription();

        app.MapPost("/internal/maintenance/cleanup-old-quest-data", async (
            HttpRequest request,
            IConfiguration configuration,
            int? retentionDays,
            QuestDataCleanupService questDataCleanupService) =>
        {
            var expectedToken = configuration["Maintenance:MarketCleanupToken"];
            if (string.IsNullOrWhiteSpace(expectedToken))
            {
                return Results.Problem(
                    detail: "Maintenance:MarketCleanupToken が設定されていません。",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var providedToken = request.Headers[MaintenanceTokenHeaderName].ToString();
            if (!SecureEquals(providedToken, expectedToken))
            {
                return Results.Unauthorized();
            }

            var days = retentionDays ?? 7;
            if (days < 1)
            {
                return Results.BadRequest(new { message = "retentionDays は1以上で指定してください。" });
            }

            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days);
            var result = await questDataCleanupService.DeleteOldDataAsync(cutoffDate);
            return Results.Ok(new
            {
                message = "古いクエストデータを削除しました。",
                cutoffDate,
                deletedQuestTurnCommands = result.DeletedQuestTurnCommands,
                deletedQuestFloorTraps = result.DeletedQuestFloorTraps,
                deletedQuestRunEnemies = result.DeletedQuestRunEnemies,
                deletedQuestRunPartyMembers = result.DeletedQuestRunPartyMembers,
                deletedQuestRunPartySnapshots = result.DeletedQuestRunPartySnapshots,
                deletedQuestRewardSummaries = result.DeletedQuestRewardSummaries,
                deletedQuestRuns = result.DeletedQuestRuns,
                deletedQuestRoomParticipants = result.DeletedQuestRoomParticipants,
                deletedQuestRoomAllowedPlayers = result.DeletedQuestRoomAllowedPlayers,
                deletedQuestRooms = result.DeletedQuestRooms
            });
        }).ExcludeFromDescription();

        return app;
    }

    private static bool SecureEquals(string actual, string expected)
    {
        if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        if (actualBytes.Length != expectedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }

    private static bool IsLocalDevelopmentRequest(HttpRequest request)
    {
        var remoteIp = request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return false;
        }

        return IPAddress.IsLoopback(remoteIp);
    }
}
