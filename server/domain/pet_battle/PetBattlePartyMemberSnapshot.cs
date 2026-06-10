using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;

namespace server.domain.pet_battle;

public class PetBattlePartyMemberSnapshot(
    PetBattleParticipantId participantId,
    int enemyDefinitionId,
    string displayName,
    string? imagePath,
    Status baseStatus,
    IEnumerable<int> moveIds,
    BattleRow startRow,
    BattleColumn startColumn)
{
    public PetBattleParticipantId ParticipantId { get; } = participantId;
    public int EnemyDefinitionId { get; } = enemyDefinitionId;
    public string DisplayName { get; } = string.IsNullOrWhiteSpace(displayName)
        ? throw new ArgumentException("表示名は必須です。", nameof(displayName))
        : displayName.Trim();
    public string? ImagePath { get; } = string.IsNullOrWhiteSpace(imagePath) ? null : imagePath.Trim();
    public Status BaseStatus { get; } = baseStatus ?? throw new ArgumentNullException(nameof(baseStatus));
    public IReadOnlyList<int> MoveIds { get; } = moveIds?.ToArray() ?? throw new ArgumentNullException(nameof(moveIds));
    public BattleRow StartRow { get; } = startRow;
    public BattleColumn StartColumn { get; } = startColumn;

    public BattlePosition StartPosition => new(StartRow, StartColumn);

    public MoveSet BuildMoveSet()
    {
        var ids = MoveIds.Take(MoveSet.MaxSlots).ToArray();
        var slots = new MoveId?[MoveSet.MaxSlots];
        for (var i = 0; i < ids.Length; i++)
        {
            slots[i] = new MoveId(ids[i]);
        }

        return new MoveSet(slots);
    }
}
