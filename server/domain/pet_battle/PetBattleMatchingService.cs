using server.domain.player;

namespace server.domain.pet_battle;

public static class PetBattleMatchingService
{
    public static PlayerId? SelectOpponent(
        PlayerId ownerId,
        int ownerRating,
        IReadOnlyList<MatchingCandidate> candidates)
    {
        var eligible = candidates
            .Where(c => c.PlayerId != ownerId && c.PetCount > 0)
            .ToArray();

        if (eligible.Length == 0)
        {
            return null;
        }

        var narrow = eligible.Where(c => Math.Abs(c.Rating - ownerRating) <= PetBattleConstants.MatchingRangeNarrow).ToArray();
        if (narrow.Length > 0)
        {
            return narrow[Random.Shared.Next(narrow.Length)].PlayerId;
        }

        var wide = eligible.Where(c => Math.Abs(c.Rating - ownerRating) <= PetBattleConstants.MatchingRangeWide).ToArray();
        if (wide.Length > 0)
        {
            return wide[Random.Shared.Next(wide.Length)].PlayerId;
        }

        return eligible[Random.Shared.Next(eligible.Length)].PlayerId;
    }
}
