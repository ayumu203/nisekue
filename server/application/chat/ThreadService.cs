using server.domain.chat;
using server.domain.player;
using server.shared.constants.chat;
using DomainThread = server.domain.chat.Thread;

namespace server.application.chat;

public class ThreadService(IThreadRepository threadRepository, IPlayerRepository playerRepository)
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
                    profile.ImagePath);
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

    public async Task<ThreadDetailView> CreateAsync(PlayerId authorPlayerId, string title, string body)
    {
        var author = await playerRepository.GetPlayerAsync(authorPlayerId);
        if (author is null)
        {
            throw new InvalidOperationException("投稿者のプレイヤーが見つかりません。");
        }

        var thread = DomainThread.Create(authorPlayerId, title, body);
        await threadRepository.CreateAsync(thread, ThreadConstants.MaxThreadsPerPlayer);

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

        await threadRepository.DeleteAsync(threadId);
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
            replies);
    }

    private async Task<Dictionary<PlayerId, (string Name, string? ImagePath)>> LoadProfilesAsync(IEnumerable<PlayerId> playerIds)
    {
        var ids = playerIds.Distinct().ToArray();
        var profiles = new Dictionary<PlayerId, (string Name, string? ImagePath)>();
        foreach (var playerId in ids)
        {
            var player = await playerRepository.GetPlayerAsync(playerId);
            profiles[playerId] = (player?.Name ?? "Unknown", player?.ImagePath);
        }

        return profiles;
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
