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
        IEnumerable<Equipment> equipments)
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
                    var effectiveStatus = equipmentStatusResolver.BuildEffectiveStatus(player.Status, ownedEquipments, equipments);
                    var weaponEquipmentId = ownedEquipments.FirstOrDefault(x => x.Status == EquipmentStatus.Equipped && x.Type == EquipmentType.Weapon)?.Id;
                    var armorEquipmentId = ownedEquipments.FirstOrDefault(x => x.Status == EquipmentStatus.Equipped && x.Type == EquipmentType.Armor)?.Id;

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
                        ActionMode.Manual);
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
}
