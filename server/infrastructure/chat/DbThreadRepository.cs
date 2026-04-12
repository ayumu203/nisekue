using Microsoft.EntityFrameworkCore;
using Npgsql;
using server.domain.chat;
using server.domain.player;
using System.Data;
using server.shared.constants.chat;
using DomainThread = server.domain.chat.Thread;

namespace server.infrastructure.chat;

public class DbThreadRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IThreadRepository
{
    public async Task CreateAsync(DomainThread thread, int maxThreadCount)
    {
        ArgumentNullException.ThrowIfNull(thread);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await LockPlayerAsync(dbContext, thread.AuthorPlayerId.Value);

            dbContext.Threads.Add(MapToEntity(thread));
            await dbContext.SaveChangesAsync();

            var threadIds = await dbContext.Threads
                .Where(x => x.AuthorPlayerId == thread.AuthorPlayerId.Value)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => x.Id)
                .ToListAsync();

            var overflowCount = threadIds.Count - maxThreadCount;
            if (overflowCount > 0)
            {
                var deleteIds = threadIds.Take(overflowCount).ToHashSet();
                var threadsToDelete = await dbContext.Threads
                    .Where(x => deleteIds.Contains(x.Id))
                    .ToListAsync();

                dbContext.Threads.RemoveRange(threadsToDelete);
                await dbContext.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InvalidOperationException("スレッド作成時に競合が発生しました。再試行してください。", ex);
        }
    }

    public async Task<DomainThread?> FindByIdAsync(ThreadId threadId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var threadEntity = await dbContext.Threads
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == threadId);

        if (threadEntity is null)
        {
            return null;
        }

        var replyEntities = await dbContext.ThreadReplies
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

        return MapToDomain(threadEntity, replyEntities);
    }

    public async Task SaveAsync(DomainThread thread, int maxReplyCount)
    {
        ArgumentNullException.ThrowIfNull(thread);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var exists = await LockThreadAsync(dbContext, thread.Id.Value);
            if (!exists)
            {
                throw new InvalidOperationException("対象スレッドが見つかりません。");
            }

            var existingReplyIds = await dbContext.ThreadReplies
                .AsNoTracking()
                .Where(x => x.ThreadId == thread.Id)
                .Select(x => x.Id)
                .ToHashSetAsync();

            var repliesToAdd = thread.Replies
                .Where(x => !existingReplyIds.Contains(x.Id))
                .Select(x => MapToEntity(thread.Id, x))
                .ToArray();

            var currentReplyCount = existingReplyIds.Count;
            if (currentReplyCount + repliesToAdd.Length > maxReplyCount)
            {
                throw new InvalidOperationException($"返信は{maxReplyCount}件までです。");
            }

            if (repliesToAdd.Length > 0)
            {
                dbContext.ThreadReplies.AddRange(repliesToAdd);
            }

            var affected = await dbContext.Threads
                .Where(x => x.Id == thread.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.UpdatedAt, thread.UpdatedAt)
                    .SetProperty(x => x.LastRepliedAt, thread.LastRepliedAt));

            if (affected == 0)
            {
                throw new InvalidOperationException("対象スレッドが見つかりません。");
            }

            await dbContext.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InvalidOperationException("返信保存時に競合が発生しました。再試行してください。", ex);
        }
    }

    public async Task DeleteAsync(ThreadId threadId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var exists = await LockThreadAsync(dbContext, threadId.Value);
            if (!exists)
            {
                throw new KeyNotFoundException("対象スレッドが見つかりません。");
            }

            var threadEntity = await dbContext.Threads
                .SingleOrDefaultAsync(x => x.Id == threadId);

            if (threadEntity is null)
            {
                throw new KeyNotFoundException("対象スレッドが見つかりません。");
            }

            dbContext.Threads.Remove(threadEntity);
            await dbContext.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InvalidOperationException("スレッド削除時に競合が発生しました。再試行してください。", ex);
        }
    }

    public async Task<IReadOnlyList<ThreadAlertSummary>> GetAlertSummariesAsync(PlayerId authorPlayerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var rows = await dbContext.ThreadReplies
            .AsNoTracking()
            .Where(reply => !reply.IsAuthorAlerted)
            .Join(
                dbContext.Threads.AsNoTracking().Where(thread => thread.AuthorPlayerId == authorPlayerId.Value),
                reply => reply.ThreadId,
                thread => thread.Id,
                (reply, thread) => new
                {
                    thread.Id,
                    thread.Title,
                    ReplyId = reply.Id,
                    reply.AuthorPlayerId,
                    reply.CreatedAt
                })
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.ReplyId)
            .ToListAsync();

        return rows
            .GroupBy(x => new { x.Id, x.Title })
            .Select(group =>
            {
                var latest = group
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.ReplyId)
                    .First();
                return new ThreadAlertSummary(
                    group.Key.Id,
                    group.Key.Title,
                    group.Select(x => x.ReplyId).ToArray(),
                    new PlayerId(latest.AuthorPlayerId),
                    latest.CreatedAt,
                    group.Count());
            })
            .ToArray();
    }

    public async Task<int> MarkRepliesAlertedAsync(PlayerId authorPlayerId, IReadOnlyCollection<ThreadReplyId> replyIds)
    {
        ArgumentNullException.ThrowIfNull(replyIds);

        if (replyIds.Count == 0)
        {
            return 0;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var distinctIds = replyIds.Distinct().ToArray();
        return await dbContext.ThreadReplies
            .Where(x => distinctIds.Contains(x.Id) && !x.IsAuthorAlerted)
            .Join(
                dbContext.Threads.Where(thread => thread.AuthorPlayerId == authorPlayerId.Value),
                reply => reply.ThreadId,
                thread => thread.Id,
                (reply, _) => reply)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsAuthorAlerted, true));
    }

    public async Task<ThreadPageResult> GetPageAsync(ThreadQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var totalCount = await dbContext.Threads.CountAsync();
        var skip = (query.Page - 1) * query.PageSize;

        var items = await dbContext.Threads
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(x => new ThreadListItem(
                x.Id,
                new PlayerId(x.AuthorPlayerId),
                x.Title,
                x.Body,
                x.CreatedAt,
                x.UpdatedAt,
                x.LastRepliedAt,
                dbContext.ThreadReplies.Count(reply => reply.ThreadId == x.Id)))
            .ToListAsync();

        return new ThreadPageResult(items, query.Page, query.PageSize, totalCount);
    }

    private static ThreadEntity MapToEntity(DomainThread thread) =>
        new(
            thread.Id,
            thread.AuthorPlayerId.Value,
            thread.Title.Value,
            thread.Body.Value,
            thread.CreatedAt,
            thread.UpdatedAt,
            thread.LastRepliedAt);

    private static ThreadReplyEntity MapToEntity(ThreadId threadId, ThreadReply reply) =>
        new(
            reply.Id,
            threadId,
            reply.AuthorPlayerId.Value,
            reply.Body.Value,
            reply.CreatedAt,
            reply.IsAuthorAlerted);

    private static DomainThread MapToDomain(ThreadEntity entity, IReadOnlyList<ThreadReplyEntity> replyEntities) =>
        new(
            entity.Id,
            new PlayerId(entity.AuthorPlayerId),
            new ThreadTitle(entity.Title),
            new ThreadBody(entity.Body),
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.LastRepliedAt,
            replyEntities.Select(reply => new ThreadReply(
                reply.Id,
                new PlayerId(reply.AuthorPlayerId),
                new ThreadReplyBody(reply.Body),
                reply.CreatedAt,
                reply.IsAuthorAlerted)));

    private static async Task LockPlayerAsync(AppDbContext dbContext, Guid playerId)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<Guid>("SELECT id FROM internal.players WHERE id = {0} FOR UPDATE", playerId)
            .ToListAsync();

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("投稿者のプレイヤーが見つかりません。");
        }
    }

    private static async Task<bool> LockThreadAsync(AppDbContext dbContext, Guid threadId)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<Guid>("SELECT id FROM internal.threads WHERE id = {0} FOR UPDATE", threadId)
            .ToListAsync();

        return rows.Count > 0;
    }
}
