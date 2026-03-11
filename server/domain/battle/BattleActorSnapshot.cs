using server.domain.battle.enums;
using server.domain.player;

namespace server.domain.battle;

public class BattleActorSnapshot(
    BattleActorId id,
    string displayName,
    BattleSide side,
    Status baseStatus,
    MoveSet moveSet)
{
    public BattleActorId Id { get; } = id;
    public string DisplayName { get; } = ValidateDisplayName(displayName);
    public BattleSide Side { get; } = side;
    public Status BaseStatus { get; } = baseStatus ?? throw new ArgumentNullException(nameof(baseStatus));
    public MoveSet MoveSet { get; } = moveSet ?? throw new ArgumentNullException(nameof(moveSet));

    private static string ValidateDisplayName(string displayName)
    {
        var normalized = displayName?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException("displayName は必須です。", nameof(displayName));
        }

        return normalized;
    }
}
