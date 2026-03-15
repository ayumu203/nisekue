using server.domain.move;

namespace server.domain.player;

public sealed class LevelUpResult(
    bool hasLeveledUp,
    bool hasMasteredCurrentJob,
    IReadOnlyList<MoveId> newlyLearnedMoveIds)
{
    public bool HasLeveledUp { get; } = hasLeveledUp;
    public bool HasMasteredCurrentJob { get; } = hasMasteredCurrentJob;
    public IReadOnlyList<MoveId> NewlyLearnedMoveIds { get; } = newlyLearnedMoveIds ?? throw new ArgumentNullException(nameof(newlyLearnedMoveIds));
}
