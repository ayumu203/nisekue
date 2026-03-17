using server.domain.move;

namespace server.domain.player;

public sealed class LevelUpResult(
    bool hasPlayerLeveledUp,
    bool hasJobLeveledUp,
    bool hasMasteredCurrentJob,
    IReadOnlyList<MoveId> newlyLearnedMoveIds)
{
    public bool HasPlayerLeveledUp { get; } = hasPlayerLeveledUp;
    public bool HasJobLeveledUp { get; } = hasJobLeveledUp;
    public bool HasLeveledUp { get; } = hasPlayerLeveledUp || hasJobLeveledUp;
    public bool HasMasteredCurrentJob { get; } = hasMasteredCurrentJob;
    public IReadOnlyList<MoveId> NewlyLearnedMoveIds { get; } = newlyLearnedMoveIds ?? throw new ArgumentNullException(nameof(newlyLearnedMoveIds));
}
