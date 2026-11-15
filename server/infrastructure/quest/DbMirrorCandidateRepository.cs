using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.domain.quest;

namespace server.infrastructure.quest;

public class DbMirrorCandidateRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IMirrorCandidateRepository
{
    public async Task<IReadOnlyList<PlayerId>> GetTopPlayerIdsWithTiesAsync(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "件数は1以上である必要があります。");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // count 人目の（転生回数, レベル）を取り、それ以上の人を同点分も含めて全員返す。
        var boundary = await dbContext.Players
            .AsNoTracking()
            .OrderByDescending(x => x.RebirthCount)
            .ThenByDescending(x => x.Level)
            .Skip(count - 1)
            .Select(x => new { x.RebirthCount, x.Level })
            .FirstOrDefaultAsync();

        var query = dbContext.Players.AsNoTracking();
        if (boundary is not null)
        {
            var boundaryRebirthCount = boundary.RebirthCount;
            var boundaryLevel = boundary.Level;
            query = query.Where(x =>
                x.RebirthCount > boundaryRebirthCount ||
                (x.RebirthCount == boundaryRebirthCount && x.Level >= boundaryLevel));
        }

        var ids = await query
            .Select(x => x.Id)
            .ToListAsync();

        return ids.Select(x => new PlayerId(x)).ToArray();
    }

    public async Task<IReadOnlyList<PlayerId>> GetAllyPlayerIdsAsync(PlayerId excludedPlayerId, int level, int levelRange, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var excludedId = excludedPlayerId.Value;
        var minLevel = level - levelRange;
        var maxLevel = level + levelRange;

        var inRange = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.Id != excludedId && x.Level >= minLevel && x.Level <= maxLevel)
            .OrderBy(_ => EF.Functions.Random())
            .Select(x => x.Id)
            .Take(count)
            .ToListAsync();
        if (inRange.Count >= count)
        {
            return inRange.Select(x => new PlayerId(x)).ToArray();
        }

        // レベル差の範囲内で足りないときは、レベルが近い順に足す。
        var nearest = await dbContext.Players
            .AsNoTracking()
            .Where(x => x.Id != excludedId && !inRange.Contains(x.Id))
            .OrderBy(x => Math.Abs(x.Level - level))
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(count - inRange.Count)
            .ToListAsync();

        return inRange.Concat(nearest).Select(x => new PlayerId(x)).ToArray();
    }
}
