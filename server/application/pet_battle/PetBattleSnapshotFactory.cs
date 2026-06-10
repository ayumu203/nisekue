using server.domain.battle;
using server.domain.battle.enums;
using server.domain.pet;
using server.domain.pet_battle;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.pet_battle;

public class PetBattleSnapshotFactory
{
    private static readonly BattlePosition[] AutoPlacementOrder =
    [
        new(BattleRow.Front, BattleColumn.Left),
        new(BattleRow.Front, BattleColumn.Right),
        new(BattleRow.Middle, BattleColumn.Left),
        new(BattleRow.Middle, BattleColumn.Right),
        new(BattleRow.Back, BattleColumn.Left),
        new(BattleRow.Back, BattleColumn.Right),
    ];

    public PetBattlePartyMemberSnapshot[] CreateOwnerSnapshots(
        IReadOnlyList<PetBattleRoomSlot> slots,
        IReadOnlyList<PlayerPet> ownerPets,
        IReadOnlyDictionary<int, QuestEnemyDefinition> enemyDefinitions)
    {
        var snapshots = new List<PetBattlePartyMemberSnapshot>();
        foreach (var slot in slots)
        {
            var pet = ownerPets.FirstOrDefault(p => p.Id == slot.PetId);
            if (pet is null) continue;
            if (!enemyDefinitions.TryGetValue(pet.EnemyDefinitionId.Value, out var definition)) continue;

            var status = PetStatusResolver.Resolve(pet, definition);
            snapshots.Add(new PetBattlePartyMemberSnapshot(
                PetBattleParticipantId.NewId(),
                definition.Id.Value,
                definition.Name,
                definition.ImagePath,
                status,
                definition.MoveIds.Select(m => m.Id),
                slot.Row,
                slot.Column));
        }

        return snapshots.ToArray();
    }

    public PetBattlePartyMemberSnapshot[] CreateOpponentSnapshots(
        IReadOnlyList<PlayerPet> opponentPets,
        IReadOnlyDictionary<int, QuestEnemyDefinition> enemyDefinitions)
    {
        var snapshots = new List<PetBattlePartyMemberSnapshot>();
        var placementIndex = 0;

        foreach (var pet in opponentPets)
        {
            if (placementIndex >= AutoPlacementOrder.Length) break;
            if (!enemyDefinitions.TryGetValue(pet.EnemyDefinitionId.Value, out var definition)) continue;

            var pos = AutoPlacementOrder[placementIndex++];
            var status = PetStatusResolver.Resolve(pet, definition);
            snapshots.Add(new PetBattlePartyMemberSnapshot(
                PetBattleParticipantId.NewId(),
                definition.Id.Value,
                definition.Name,
                definition.ImagePath,
                status,
                definition.MoveIds.Select(m => m.Id),
                pos.Row,
                pos.Column));
        }

        return snapshots.ToArray();
    }

    public static PetBattlePartyMemberState[] CreateMemberStates(
        IEnumerable<PetBattlePartyMemberSnapshot> snapshots,
        ActionMode actionMode) =>
        snapshots.Select(s => new PetBattlePartyMemberState(
            s.ParticipantId,
            s.BaseStatus.MaxHp,
            s.BaseStatus.MaxMp,
            isDead: false,
            canActFromTurn: 1,
            actionMode)).ToArray();
}
