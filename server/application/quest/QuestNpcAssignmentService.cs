using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestNpcAssignmentService(IQuestNpcTemplateRepository npcTemplateRepository)
{
    public async Task<IReadOnlyList<QuestNpcTemplate>> AssignForStart(QuestRoom room, int minPartyMemberCount)
    {
        ArgumentNullException.ThrowIfNull(room);
        if (minPartyMemberCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minPartyMemberCount), "最低出撃人数は1以上である必要があります。");
        }

        var activeParticipants = room.Participants.Count(x => x.Status != ParticipantStatus.Left);
        var requiredNpcCount = Math.Max(0, minPartyMemberCount - activeParticipants);
        if (requiredNpcCount == 0)
        {
            return [];
        }

        var templates = await npcTemplateRepository.GetForStartAsync(room.StageId, requiredNpcCount);
        if (templates.Count < requiredNpcCount)
        {
            throw new InvalidOperationException($"NPC テンプレートが不足しています。required={requiredNpcCount}, actual={templates.Count}");
        }

        return templates.Take(requiredNpcCount).ToArray();
    }
}
