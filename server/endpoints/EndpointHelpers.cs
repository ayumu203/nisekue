using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using server.application.chat;
using server.domain.player;
using server.domain.quest;

namespace server.endpoints;

internal static class EndpointHelpers
{
    internal const string AnonymousPostingForbiddenMessage = "匿名ログイン中は投稿できません。アカウント連携後に利用してください。";

    internal static PlayerId? TryGetPlayerId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(subject, out var guid) ? new PlayerId(guid) : null;
    }

    internal static bool IsAnonymousUser(ClaimsPrincipal user)
    {
        if (TryGetBooleanClaim(user, "is_anonymous") is true)
        {
            return true;
        }

        return ClaimContainsAnonymousProvider(user, "providers")
            || JsonClaimContainsAnonymousProvider(user, "app_metadata")
            || JsonClaimContainsAnonymousProvider(user, "user_metadata");
    }

    internal const string MaintenanceTokenHeaderName = "X-Maintenance-Token";

    /// <summary>
    /// /internal/* 用。X-Maintenance-Token が Maintenance:MarketCleanupToken と一致しなければエラーの結果を返す。
    /// </summary>
    internal static IResult? ValidateMaintenanceToken(HttpRequest request, IConfiguration configuration)
    {
        var expectedToken = configuration["Maintenance:MarketCleanupToken"];
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            return Results.Problem(
                detail: "Maintenance:MarketCleanupToken が設定されていません。",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var providedToken = request.Headers[MaintenanceTokenHeaderName].ToString();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedToken),
            Encoding.UTF8.GetBytes(expectedToken))
            ? null
            : Results.Unauthorized();
    }

    internal static IResult Forbidden(string message) =>
        Results.Json(new { message }, options: null, contentType: null, statusCode: StatusCodes.Status403Forbidden);

    internal static IResult AnonymousPostingForbidden() =>
        Results.Json(
            new { message = AnonymousPostingForbiddenMessage },
            options: null,
            contentType: null,
            statusCode: StatusCodes.Status403Forbidden);

    private static bool? TryGetBooleanClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirstValue(claimType);
        return bool.TryParse(value, out var result) ? result : null;
    }

    private static bool ClaimContainsAnonymousProvider(ClaimsPrincipal user, string claimType)
    {
        foreach (var claim in user.FindAll(claimType))
        {
            if (claim.Value.Contains("anonymous", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool JsonClaimContainsAnonymousProvider(ClaimsPrincipal user, string claimType)
    {
        var raw = user.FindFirstValue(claimType);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (!document.RootElement.TryGetProperty("providers", out var providers)
                || providers.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var provider in providers.EnumerateArray())
            {
                if (provider.ValueKind == JsonValueKind.String
                    && string.Equals(provider.GetString(), "anonymous", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    internal static string GetJobDisplayName(Job job) => JobDisplayNames.GetDisplayName(job);

    internal static object MapQuestStage(QuestStageDefinition stage) => new
    {
        stageId = stage.Id.Value,
        stageCode = stage.StageCode,
        name = stage.Name,
        recommendedLevel = stage.RecommendedLevel,
        minimumEntryLevel = stage.MinimumEntryLevel ?? Math.Max(1, (int)Math.Ceiling(stage.RecommendedLevel * 0.7m)),
        minPartyMemberCount = stage.MinPartyMemberCount,
        maxPartyMemberCount = stage.MaxPartyMemberCount,
        isActive = stage.IsActive,
        floors = stage.Floors.Select(floor => new
        {
            floorNo = floor.FloorNo,
            floorType = floor.FloorType.ToString(),
            enemyCount = floor.Placements.Count
        })
    };

    internal static object MapQuestRoom(QuestRoom room) => new
    {
        roomId = room.Id.Value,
        ownerPlayerId = room.OwnerId.Value,
        stageId = room.StageId.Value,
        mode = room.Mode.ToString(),
        status = room.Status.ToString(),
        version = room.Version,
        closeReason = room.CloseReason?.ToString(),
        createdAt = room.CreatedAt,
        closedAt = room.ClosedAt,
        canStart = room.CanStart(),
        formation = new
        {
            occupiedPositions = room.Formation.OccupiedPositions.Select(position => new
            {
                row = position.Row.ToString(),
                column = position.Column.ToString()
            })
        },
        participants = room.Participants.Select(participant => new
        {
            participantId = participant.Id.Value,
            type = participant.Type.ToString(),
            playerId = participant.PlayerId?.Value,
            npcTemplateId = participant.NpcTemplateId?.Value,
            displayName = participant.DisplayName,
            status = participant.Status.ToString(),
            isOwner = participant.IsOwner,
            position = new
            {
                row = participant.Position.Row.ToString(),
                column = participant.Position.Column.ToString()
            },
            joinedAt = participant.JoinedAt,
            lastSeenAt = participant.LastSeenAt,
            leftAt = participant.LeftAt
        })
    };

    internal static object MapThreadPage(ThreadListPageView page) => new
    {
        items = page.Items.Select(MapThreadSummary),
        page = page.Page,
        pageSize = page.PageSize,
        totalCount = page.TotalCount,
        hasNextPage = page.HasNextPage
    };

    internal static object MapThreadDetail(ThreadDetailView thread) => new
    {
        id = thread.Id,
        title = thread.Title,
        body = thread.Body,
        createdAt = thread.CreatedAt,
        updatedAt = thread.UpdatedAt,
        lastRepliedAt = thread.LastRepliedAt,
        authorPlayerId = thread.AuthorPlayerId,
        authorName = thread.AuthorName,
        authorImagePath = thread.AuthorImagePath,
        imageUrl = thread.ImageUrl,
        imageIsSensitive = thread.ImageIsSensitive,
        replies = thread.Replies.Select(reply => new
        {
            id = reply.Id,
            body = reply.Body,
            createdAt = reply.CreatedAt,
            authorPlayerId = reply.AuthorPlayerId,
            authorName = reply.AuthorName,
            authorImagePath = reply.AuthorImagePath
        })
    };

    private static object MapThreadSummary(ThreadSummaryView thread) => new
    {
        id = thread.Id,
        title = thread.Title,
        previewBody = thread.PreviewBody,
        createdAt = thread.CreatedAt,
        lastRepliedAt = thread.LastRepliedAt,
        replyCount = thread.ReplyCount,
        hasImage = thread.HasImage,
        authorPlayerId = thread.AuthorPlayerId,
        authorName = thread.AuthorName,
        authorImagePath = thread.AuthorImagePath
    };
}
