using server.domain.player;

namespace server.domain.quest;

public class QuestRoomJoinPolicy(int? minRequiredLevel = null, IEnumerable<PlayerId>? allowedPlayerIds = null)
{
    private readonly PlayerId[] allowedPlayerIds = (allowedPlayerIds ?? [])
        .Distinct()
        .OrderBy(x => x.Value)
        .ToArray();

    public int? MinRequiredLevel { get; } = ValidateMinRequiredLevel(minRequiredLevel);
    public IReadOnlyCollection<PlayerId> AllowedPlayerIds => allowedPlayerIds.ToArray();
    public bool HasAllowedPlayerRestriction => allowedPlayerIds.Length > 0;

    public bool CanJoin(PlayerId playerId, int level) => GetJoinDeniedReason(playerId, level) is null;

    public string? GetJoinDeniedReason(PlayerId playerId, int level)
    {
        if (MinRequiredLevel is not null && level < MinRequiredLevel.Value)
        {
            return "LevelRequirementNotMet";
        }

        if (HasAllowedPlayerRestriction && !allowedPlayerIds.Contains(playerId))
        {
            return "NotAllowedPlayer";
        }

        return null;
    }

    private static int? ValidateMinRequiredLevel(int? minRequiredLevel)
    {
        if (minRequiredLevel is not null && minRequiredLevel.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minRequiredLevel), "参加可能レベルは1以上である必要があります。");
        }

        return minRequiredLevel;
    }
}
