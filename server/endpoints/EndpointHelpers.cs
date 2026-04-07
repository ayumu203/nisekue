using System.Security.Claims;
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

    internal static string GetJobDisplayName(Job job) =>
        job switch
        {
            Job.Apprentice => "見習い",
            Job.Warrior => "戦士",
            Job.Guardian => "盾使い",
            Job.Mage => "魔法使い",
            Job.Priest => "僧侶",
            Job.Ranger => "レンジャー",
            Job.OniWarrior => "鬼武者",
            Job.SwordMaster => "ソードマスター",
            Job.Trickster => "トリックスター",
            Job.Crusader => "クルセイダー",
            Job.FireMage => "火魔法使い",
            Job.WaterMage => "水魔法使い",
            Job.WindMage => "風魔法使い",
            Job.HighPriest => "神官",
            Job.Necromancer => "死霊使い",
            Job.Sniper => "スナイパー",
            Job.TrapMaster => "罠師",
            Job.GrandWarrior => "グランドウォリアー",
            Job.GrandGuard => "グランドガード",
            Job.GrandCaster => "グランドキャスター",
            Job.GrandPriest => "グランドプリースト",
            Job.GrandRanger => "グランドレンジャー",
            Job.Shogun => "大将軍",
            Job.Archmage => "大魔法使い",
            Job.GreatThief => "大盗賊",
            _ => job.ToString()
        };

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
        authorPlayerId = thread.AuthorPlayerId,
        authorName = thread.AuthorName,
        authorImagePath = thread.AuthorImagePath
    };
}
