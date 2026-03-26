using server.domain.quest;
using server.shared.constants.player;

namespace server.application.quest;

internal static class QuestNpcImageAssignmentPolicy
{
    public static string Resolve(QuestParticipantId participantId, QuestNpcTemplateId? npcTemplateId)
    {
        var templateSeed = npcTemplateId?.Value ?? 0;
        var participantSeed = BitConverter.ToInt32(participantId.Value.ToByteArray(), 0);
        var seed = unchecked(participantSeed ^ (templateSeed * 397));
        var index = (int)((uint)seed % PlayerImageCatalog.FileNames.Count);
        return PlayerImageCatalog.FileNames[index];
    }
}
