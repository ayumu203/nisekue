using System.Globalization;
using server.application.announcement;
using server.domain.announcement;

namespace server.endpoints;

internal static class AnnouncementEndpoints
{
    private const string MaintenanceTokenHeaderName = "X-Maintenance-Token";

    internal static WebApplication MapAnnouncementEndpoints(this WebApplication app)
    {
        // ログイン前でも読めるよう認証なしで公開する（個人情報を含まない）
        app.MapGet("/announcements", async (AnnouncementService announcementService) =>
        {
            var announcements = await announcementService.ListAsync();
            return Results.Ok(new
            {
                items = announcements.Select(ToResponse)
            });
        });

        app.MapPost("/internal/announcements", async (
            HttpRequest request,
            IConfiguration configuration,
            PostAnnouncementRequest body,
            AnnouncementService announcementService) =>
        {
            var tokenError = ValidateMaintenanceToken(request, configuration);
            if (tokenError is not null)
            {
                return tokenError;
            }

            DateOnly? date = null;
            if (!string.IsNullOrWhiteSpace(body.Date))
            {
                if (!DateOnly.TryParseExact(body.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return Results.Problem(
                        detail: "date は YYYY-MM-DD 形式で指定してください。",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                date = parsed;
            }

            try
            {
                var announcement = await announcementService.PostAsync(
                    new PostAnnouncementCommand(body.Id, date, body.Title ?? string.Empty, body.Body ?? string.Empty),
                    DateTimeOffset.UtcNow);

                return Results.Created($"/announcements/{announcement.Id.Value}", ToResponse(announcement));
            }
            catch (AnnouncementIdConflictException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
            }
        }).ExcludeFromDescription();

        app.MapDelete("/internal/announcements/{id}", async (
            HttpRequest request,
            IConfiguration configuration,
            string id,
            AnnouncementService announcementService) =>
        {
            var tokenError = ValidateMaintenanceToken(request, configuration);
            if (tokenError is not null)
            {
                return tokenError;
            }

            var deleted = await announcementService.DeleteAsync(id);
            return deleted
                ? Results.Ok(new { message = "お知らせを削除しました。", id })
                : Results.Problem(detail: $"お知らせID '{id}' は見つかりません。", statusCode: StatusCodes.Status404NotFound);
        }).ExcludeFromDescription();

        return app;
    }

    private static object ToResponse(Announcement announcement)
    {
        return new
        {
            id = announcement.Id.Value,
            date = announcement.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            title = announcement.Title,
            body = announcement.Body
        };
    }

    private static IResult? ValidateMaintenanceToken(HttpRequest request, IConfiguration configuration)
    {
        var expectedToken = configuration["Maintenance:MarketCleanupToken"];
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            return Results.Problem(
                detail: "Maintenance:MarketCleanupToken が設定されていません。",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var providedToken = request.Headers[MaintenanceTokenHeaderName].ToString();
        return SecureEquals(providedToken, expectedToken) ? null : Results.Unauthorized();
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

internal sealed record PostAnnouncementRequest(string? Id, string? Date, string? Title, string? Body);
