namespace server.domain.quest;

public class QuestRunResolutionSummary(int turn, bool isFloorCleared, bool isQuestCompleted, bool isQuestFailed)
{
    public int Turn { get; } = turn;
    public bool IsFloorCleared { get; } = isFloorCleared;
    public bool IsQuestCompleted { get; } = isQuestCompleted;
    public bool IsQuestFailed { get; } = isQuestFailed;
}
