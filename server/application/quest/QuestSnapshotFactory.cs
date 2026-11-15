using server.domain.pet;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestSnapshotFactory(EquipmentStatusResolver equipmentStatusResolver)
{
    public QuestRunPartyMemberSnapshot[] Create(
        IEnumerable<QuestParticipant> participants,
        IEnumerable<Player> players,
        IEnumerable<QuestNpcTemplate> npcTemplates,
        IEnumerable<PlayerEquipment> playerEquipments,
        IEnumerable<Equipment> equipments,
        // 所持ペット（スタンバイを先頭、残りは捕獲順に並べ替えて保存する）
        IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerPet>>? petsByPlayerId = null,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions = null)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(npcTemplates);
        ArgumentNullException.ThrowIfNull(playerEquipments);
        ArgumentNullException.ThrowIfNull(equipments);

        var playerById = players.ToDictionary(x => x.Id);
        var npcById = npcTemplates.ToDictionary(x => x.Id);
        var equipmentsByPlayerId = playerEquipments.GroupBy(x => x.PlayerId).ToDictionary(x => x.Key, x => (IReadOnlyList<PlayerEquipment>)x.ToArray());

        return participants
            .Where(x => x.Status != ParticipantStatus.Left)
            .Select(participant =>
            {
                if (participant.Type == ParticipantType.Player)
                {
                    var player = participant.PlayerId is not null && playerById.TryGetValue(participant.PlayerId.Value, out var foundPlayer)
                        ? foundPlayer
                        : throw new KeyNotFoundException($"プレイヤー情報が見つかりません。 participantId={participant.Id}");
                    var ownedEquipments = participant.PlayerId is not null && equipmentsByPlayerId.TryGetValue(participant.PlayerId.Value, out var foundEquipments)
                        ? foundEquipments
                        : Array.Empty<PlayerEquipment>();
                    var effectiveStatus = equipmentStatusResolver.BuildEffectiveStatus(player.Status, player.Job, ownedEquipments, equipments);
                    var weaponEquipmentId = ownedEquipments.FirstOrDefault(x => x.Status == EquipmentStatus.Equipped && x.Type == EquipmentType.Weapon)?.Id;
                    var armorEquipmentId = ownedEquipments.FirstOrDefault(x => x.Status == EquipmentStatus.Equipped && x.Type == EquipmentType.Armor)?.Id;

                    var petSnapshots = CreatePetSnapshots(player.Id, petsByPlayerId, enemyDefinitions);

                    return new QuestRunPartyMemberSnapshot(
                        participant.Id,
                        participant.Type,
                        participant.DisplayName,
                        player.ImagePath,
                        player.Job,
                        effectiveStatus,
                        weaponEquipmentId,
                        armorEquipmentId,
                        player.MoveSet,
                        participant.Position,
                        ActionMode.Manual,
                        petSnapshots);
                }

                var npcTemplate = participant.NpcTemplateId is not null && npcById.TryGetValue(participant.NpcTemplateId.Value, out var foundTemplate)
                    ? foundTemplate
                    : throw new KeyNotFoundException($"NPC テンプレートが見つかりません。 participantId={participant.Id}");

                var moveSet = new MoveSet();
                foreach (var moveIdWithIndex in npcTemplate.MoveIds.Take(MoveSet.MaxSlots).Select((moveId, index) => new { moveId, index }))
                {
                    moveSet.SetSlot(moveIdWithIndex.index, moveIdWithIndex.moveId);
                }

                return new QuestRunPartyMemberSnapshot(
                    participant.Id,
                    participant.Type,
                    participant.DisplayName,
                    imagePath: QuestNpcImageAssignmentPolicy.Resolve(participant.Id, participant.NpcTemplateId),
                    npcTemplate.Job,
                    npcTemplate.BaseStatus,
                    weaponEquipmentId: null,
                    armorEquipmentId: null,
                    moveSet,
                    participant.Position,
                    ActionMode.AutoAttackOnly);
            })
            .ToArray();
    }

    private static QuestPetSnapshot[] CreatePetSnapshots(
        PlayerId playerId,
        IReadOnlyDictionary<PlayerId, IReadOnlyList<PlayerPet>>? petsByPlayerId,
        IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>? enemyDefinitions)
    {
        if (petsByPlayerId is null ||
            enemyDefinitions is null ||
            !petsByPlayerId.TryGetValue(playerId, out var pets))
        {
            return [];
        }

        // OrderBy は安定ソートなので、スタンバイ以外は渡された順（捕獲順）のまま並ぶ。
        return pets
            .OrderBy(pet => pet.IsStandby ? 0 : 1)
            .Take(PetConstants.MaxPetCount)
            .Where(pet => enemyDefinitions.ContainsKey(pet.EnemyDefinitionId))
            .Select(pet => new QuestPetSnapshot(pet.EnemyDefinitionId, PetStatusResolver.Resolve(pet, enemyDefinitions[pet.EnemyDefinitionId])))
            .ToArray();
    }
}
