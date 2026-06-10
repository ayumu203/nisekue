using server.domain.player;

namespace server.domain.pet_battle;

public interface IMatchingCandidateRepository
{
    Task<IReadOnlyList<MatchingCandidate>> GetCandidatesAsync(PlayerId excludePlayerId);
}
