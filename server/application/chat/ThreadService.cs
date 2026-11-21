using Microsoft.Extensions.Logging;
using server.domain.chat;
using server.domain.player;
using server.shared.constants.chat;
using DomainThread = server.domain.chat.Thread;

namespace server.application.chat;

public class ThreadService(
    IThreadRepository threadRepository,
    IPlayerRepository playerRepository,
    IPlayerRoleRepository playerRoleRepository,
    IThreadImageProcessor imageProcessor,
    IThreadImageStorage imageStorage,
    ILogger<ThreadService> logger)
{
    public async Task<ThreadListPageView> GetPageAsync(int page)
    {
        var result = await threadRepository.GetPageAsync(new ThreadQuery(page, ThreadConstants.ListPageSize));
        var profileMap = await LoadProfilesAsync(result.Items.Select(x => x.AuthorPlayerId));

        var items = result.Items
            .Select(item =>
            {
                var profile = ResolveProfile(profileMap, item.AuthorPlayerId);
                return new ThreadSummaryView(
                    item.Id.Value,
                    item.Title,
                    BuildPreview(item.Body),
                    item.CreatedAt,
                    item.LastRepliedAt,
                    item.ReplyCount,
                    item.AuthorPlayerId.Value,
                    profile.Name,
                    profile.ImagePath,
                    item.HasImage);
            })
            .ToArray();

        return new ThreadListPageView(items, result.Page, result.PageSize, result.TotalCount, result.HasNextPage);
    }

    public async Task<ThreadDetailView?> GetByIdAsync(ThreadId threadId)
    {
        var thread = await threadRepository.FindByIdAsync(threadId);
        if (thread is null)
        {
            return null;
        }

        return await BuildDetailAsync(thread);
    }

    public async Task<ThreadDetailView> CreateAsync(
        PlayerId authorPlayerId,
        string title,
        string body,
        ThreadImageUpload? imageUpload = null)
    {
        var author = await playerRepository.GetPlayerAsync(authorPlayerId);
        if (author is null)
        {
            throw new InvalidOperationException("投稿者のプレイヤーが見つかりません。");
        }

        // 画像を保存してから本文の検証で弾かれないよう、先に本文を検証する
        _ = new ThreadTitle(title);
        _ = new ThreadBody(body);

        var image = imageUpload is null
            ? null
            : await StoreImageAsync(authorPlayerId, imageUpload);

        var thread = DomainThread.Create(authorPlayerId, title, body, image);
        IReadOnlyList<string> overflowImageKeys;
        try
        {
            overflowImageKeys = await threadRepository.CreateAsync(thread, ThreadConstants.MaxThreadsPerPlayer);
        }
        catch
        {
            if (image is not null)
            {
                await DeleteImageQuietlyAsync(image.Key);
            }

            throw;
        }

        foreach (var key in overflowImageKeys)
        {
            await DeleteImageQuietlyAsync(key);
        }

        var persisted = await threadRepository.FindByIdAsync(thread.Id);
        if (persisted is null)
        {
            throw new InvalidOperationException("スレッドの保存に失敗しました。");
        }

        return await BuildDetailAsync(persisted);
    }

    public async Task<ThreadDetailView> AddReplyAsync(ThreadId threadId, PlayerId authorPlayerId, string body)
    {
        var author = await playerRepository.GetPlayerAsync(authorPlayerId);
        if (author is null)
        {
            throw new InvalidOperationException("返信者のプレイヤーが見つかりません。");
        }

        var thread = await threadRepository.FindByIdAsync(threadId)
            ?? throw new KeyNotFoundException("対象スレッドが見つかりません。");

        thread.AddReply(ThreadReplyId.New(), authorPlayerId, new ThreadReplyBody(body));
        await threadRepository.SaveAsync(thread, ThreadConstants.MaxRepliesPerThread);

        return await BuildDetailAsync(thread);
    }

    public async Task DeleteAsync(ThreadId threadId, PlayerId requestorId)
    {
        var thread = await threadRepository.FindByIdAsync(threadId)
            ?? throw new KeyNotFoundException("対象スレッドが見つかりません。");

        if (thread.AuthorPlayerId != requestorId)
        {
            throw new UnauthorizedAccessException("スレッドの削除権限がありません。");
        }

        var imageKey = await threadRepository.DeleteAsync(threadId);
        if (imageKey is not null)
        {
            await DeleteImageQuietlyAsync(imageKey);
        }
    }

    /// <summary>管理者用ワークフローからの削除。投稿者に関係なく削除する。</summary>
    public async Task DeleteByAdminAsync(ThreadId threadId)
    {
        var imageKey = await threadRepository.DeleteAsync(threadId);
        if (imageKey is not null)
        {
            await DeleteImageQuietlyAsync(imageKey);
        }
    }

    /// <summary>管理者用ワークフローから、画像のセンシティブ設定を付け外しする。</summary>
    public async Task SetImageSensitiveAsync(ThreadId threadId, bool isSensitive)
    {
        if (await threadRepository.SetImageSensitiveAsync(threadId, isSensitive))
        {
            return;
        }

        var thread = await threadRepository.FindByIdAsync(threadId)
            ?? throw new KeyNotFoundException("対象スレッドが見つかりません。");

        if (thread.Image is null)
        {
            throw new InvalidOperationException("このスレッドには画像が付いていません。");
        }

        throw new InvalidOperationException("センシティブ設定の更新に失敗しました。");
    }

    public async Task<IReadOnlyList<ThreadAlertSummaryView>> GetAlertSummariesAsync(PlayerId authorPlayerId)
    {
        var alerts = await threadRepository.GetAlertSummariesAsync(authorPlayerId);
        if (alerts.Count == 0)
        {
            return [];
        }

        var profileMap = await LoadProfilesAsync(alerts.Select(x => x.LatestReplyAuthorPlayerId));
        return alerts
            .Select(alert =>
            {
                var latestAuthor = ResolveProfile(profileMap, alert.LatestReplyAuthorPlayerId);
                return new ThreadAlertSummaryView(
                    alert.ThreadId.Value,
                    alert.ThreadTitle,
                    alert.ReplyIds.Select(x => x.Value).ToArray(),
                    latestAuthor.Name,
                    alert.LatestReplyCreatedAt,
                    alert.UnalertedReplyCount);
            })
            .ToArray();
    }

    public Task<int> MarkRepliesAlertedAsync(PlayerId authorPlayerId, IReadOnlyCollection<ThreadReplyId> replyIds)
    {
        return replyIds.Count == 0
            ? Task.FromResult(0)
            : threadRepository.MarkRepliesAlertedAsync(authorPlayerId, replyIds);
    }

    private async Task<ThreadDetailView> BuildDetailAsync(DomainThread thread)
    {
        var profileMap = await LoadProfilesAsync([thread.AuthorPlayerId, .. thread.Replies.Select(x => x.AuthorPlayerId)]);
        var authorProfile = ResolveProfile(profileMap, thread.AuthorPlayerId);

        var replies = thread.Replies
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id.Value)
            .Select(reply =>
            {
                var profile = ResolveProfile(profileMap, reply.AuthorPlayerId);
                return new ThreadReplyView(
                    reply.Id.Value,
                    reply.Body.Value,
                    reply.CreatedAt,
                    reply.AuthorPlayerId.Value,
                    profile.Name,
                    profile.ImagePath);
            })
            .ToArray();

        return new ThreadDetailView(
            thread.Id.Value,
            thread.Title.Value,
            thread.Body.Value,
            thread.CreatedAt,
            thread.UpdatedAt,
            thread.LastRepliedAt,
            thread.AuthorPlayerId.Value,
            authorProfile.Name,
            authorProfile.ImagePath,
            replies,
            thread.Image is null ? null : imageStorage.GetPublicUrl(thread.Image.Key),
            thread.Image?.IsSensitive ?? false);
    }

    private async Task<ThreadImage> StoreImageAsync(PlayerId authorPlayerId, ThreadImageUpload upload)
    {
        var role = await playerRoleRepository.FindRoleAsync(authorPlayerId);
        if (!PlayerRolePolicy.CanPostThreadImage(role))
        {
            throw new UnauthorizedAccessException("画像を投稿する権限がありません。");
        }

        if (!imageStorage.IsAvailable)
        {
            throw new ThreadImageUnavailableException();
        }

        var processed = imageProcessor.Process(upload.Content);
        var key = ThreadImage.CreateKey(authorPlayerId, processed.Format);
        await imageStorage.SaveAsync(key, processed.Content, processed.Format.ToContentType());
        return new ThreadImage(key, upload.IsSensitive);
    }

    /// <summary>画像の削除に失敗してもスレッドの操作自体は成功させる（ログだけ残す）。</summary>
    private async Task DeleteImageQuietlyAsync(string key)
    {
        try
        {
            await imageStorage.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "スレッド画像の削除に失敗しました: {ImageKey}", key);
        }
    }

    private async Task<Dictionary<PlayerId, (string Name, string? ImagePath)>> LoadProfilesAsync(IEnumerable<PlayerId> playerIds)
    {
        var ids = playerIds.Distinct().ToArray();
        var fetchedPlayers = await playerRepository.GetPlayersAsync(ids);
        var playerById = fetchedPlayers.ToDictionary(x => x.Id);
        return ids.ToDictionary(
            id => id,
            id => playerById.TryGetValue(id, out var player)
                ? (player.Name, player.ImagePath)
                : ("Unknown", (string?)null));
    }

    private static (string Name, string? ImagePath) ResolveProfile(
        IReadOnlyDictionary<PlayerId, (string Name, string? ImagePath)> profileMap,
        PlayerId playerId) =>
        profileMap.TryGetValue(playerId, out var profile) ? profile : ("Unknown", null);

    private static string BuildPreview(string body) =>
        body.Length <= ThreadConstants.PreviewMaxLength
            ? body
            : body[..ThreadConstants.PreviewMaxLength];
}
