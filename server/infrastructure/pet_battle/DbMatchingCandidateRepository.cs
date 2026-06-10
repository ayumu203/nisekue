using Microsoft.EntityFrameworkCore;
using server.domain.pet_battle;
using server.domain.player;

namespace server.infrastructure.pet_battle;

public class DbMatchingCandidateRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IMatchingCandidateRepository
{
    public async Task<IReadOnlyList<MatchingCandidate>> GetCandidatesAsync(PlayerId excludePlayerId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var petCounts = await dbContext.PlayerPets
            .AsNoTracking()
            .Where(p => p.PlayerId != excludePlayerId.Value)
            .GroupBy(p => p.PlayerId)
            .Select(g => new { PlayerId = g.Key, Count = g.Count() })
            .ToListAsync();

        var playerIds = petCounts.Select(x => x.PlayerId).ToList();

        var statsMap = await dbContext.PlayerPetBattleStats
            .AsNoTracking()
            .Where(s => playerIds.Contains(s.PlayerId))
            .ToDictionaryAsync(s => s.PlayerId, s => s.Rating);

        return petCounts
            .Select(x => new MatchingCandidate(
                new PlayerId(x.PlayerId),
                statsMap.TryGetValue(x.PlayerId, out var rating) ? rating : PetBattleConstants.InitialRating,
                x.Count))
            .ToArray();
    }
}
