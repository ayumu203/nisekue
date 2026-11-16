using Microsoft.EntityFrameworkCore;
using server.domain.player;
using server.domain.quest;
using server.infrastructure.player;

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

    public async Task<IReadOnlyList<Player>> GetPlayersWithMovesAsync(IEnumerable<PlayerId> ids)
    {
        var idValues = ids.Select(x => x.Value).Distinct().ToList();
        if (idValues.Count == 0)
        {
            return [];
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var players = await dbContext.Players
            .AsNoTracking()
            .Where(x => idValues.Contains(x.Id))
            .ToListAsync();
        var moves = await dbContext.PlayerMoves
            .AsNoTracking()
            .Where(x => idValues.Contains(x.PlayerId))
            .ToDictionaryAsync(x => x.PlayerId);

        return players
            .Select(entity => PlayerEntityMapper.MapToDomain(entity, moves.GetValueOrDefault(entity.Id)))
            .ToArray();
    }
}
