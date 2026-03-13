using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestSnapshotFactory
{
    public QuestRunPartyMemberSnapshot[] Create(
        IEnumerable<QuestParticipant> participants,
        IEnumerable<Player> players,
        IEnumerable<QuestNpcTemplate> npcTemplates)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(npcTemplates);

        var playerById = players.ToDictionary(x => x.Id);
        var npcById = npcTemplates.ToDictionary(x => x.Id);

        return participants
            .Where(x => x.Status != ParticipantStatus.Left)
            .Select(participant =>
            {
                if (participant.Type == ParticipantType.Player)
                {
                    var player = participant.PlayerId is not null && playerById.TryGetValue(participant.PlayerId.Value, out var foundPlayer)
                        ? foundPlayer
                        : throw new KeyNotFoundException($"プレイヤー情報が見つかりません。 participantId={participant.Id}");

                    return new QuestRunPartyMemberSnapshot(
                        participant.Id,
                        participant.Type,
                        participant.DisplayName,
                        player.ImagePath,
                        player.Job,
                        player.Status,
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
                    imagePath: null,
                    npcTemplate.Job,
                    npcTemplate.BaseStatus,
                    moveSet,
                    participant.Position,
                    ActionMode.AutoAttackOnly);
            })
            .ToArray();
    }
}
