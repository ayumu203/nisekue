using server.application.ranking;

namespace server.endpoints;

internal static class RankingEndpoints
{
    private const string MaintenanceTokenHeaderName = "X-Maintenance-Token";

    internal static WebApplication MapRankingEndpoints(this WebApplication app)
    {
        var rankingEnabled = app.Configuration.GetValue<bool>("Ranking:Enabled");
        if (!rankingEnabled)
        {
            app.MapGet("/rankings", () => Results.Ok(new
            {
                snapshotAt = (DateTimeOffset?)null,
                rows = Array.Empty<object>()
            })).RequireAuthorization();

            app.MapPost("/internal/rankings/rebuild", () => Results.NotFound()).ExcludeFromDescription();
            return app;
        }

        app.MapGet("/rankings", async (RankingReadService rankingReadService) =>
        {
            var (snapshotAt, rows) = await rankingReadService.GetLatestAsync();
            return Results.Ok(new
            {
                snapshotAt,
                rows = rows.Select(x => new
                {
                    rankingType = x.RankingType,
                    periodKind = x.PeriodKind,
                    combatIndexRank = x.CombatIndexRank,
                    rankPosition = x.RankPosition,
                    score = x.Score,
                    player = new
                    {
                        userId = x.PlayerId,
                        userName = x.PlayerName,
                        imagePath = x.PlayerImagePath,
                        level = x.Level,
                        job = new
                        {
                            code = x.Job.ToString(),
                            displayName = x.JobDisplayName
                        },
                        rebirthCount = x.RebirthCount
                    }
                })
            });
        }).RequireAuthorization();

        app.MapPost("/internal/rankings/rebuild", async (
            HttpRequest request,
            IConfiguration configuration,
            RankingAggregationService rankingAggregationService) =>
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

            var result = await rankingAggregationService.RebuildAsync(DateTimeOffset.UtcNow);
            return Results.Ok(new
            {
                message = "ランキングを再集計しました。",
                snapshotAt = result.SnapshotAt,
                createdEntries = result.CreatedEntries
            });
        }).ExcludeFromDescription();

        return app;
    }

    private static bool SecureEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
    }
}
