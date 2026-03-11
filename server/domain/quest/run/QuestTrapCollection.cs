namespace server.domain.quest;

public class QuestTrapCollection(IEnumerable<QuestTrapState>? traps = null)
{
    private readonly List<QuestTrapState> traps = traps?.ToList() ?? [];

    public IReadOnlyList<QuestTrapState> Traps => traps;

    public void Add(QuestTrapState trap)
    {
        ArgumentNullException.ThrowIfNull(trap);
        traps.Add(trap);
    }

    public void RemoveExpired(int floorNo)
    {
        traps.RemoveAll(x => x.ExpiresAfterFloorNo < floorNo || x.IsTriggered);
    }
}
