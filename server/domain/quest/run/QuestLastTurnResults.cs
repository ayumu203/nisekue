namespace server.domain.quest;

public class QuestLastTurnResults(
    int turnNo,
    DateTimeOffset resolvedAt,
    IEnumerable<QuestChatMessage>? chatMessages = null,
    IEnumerable<QuestResolvedAction>? actions = null,
    QuestFloorTransition? floorTransition = null,
    QuestRunTransition? runTransition = null)
{
    private readonly QuestChatMessage[] chatMessages = chatMessages?.ToArray() ?? [];
    private readonly QuestResolvedAction[] actions = actions?.ToArray() ?? [];

    public int TurnNo { get; } = turnNo;
    public DateTimeOffset ResolvedAt { get; } = resolvedAt;
    public IReadOnlyList<QuestChatMessage> ChatMessages => chatMessages;
    public IReadOnlyList<QuestResolvedAction> Actions => actions;
    public QuestFloorTransition? FloorTransition { get; } = floorTransition;
    public QuestRunTransition? RunTransition { get; } = runTransition;
}

public class QuestResolvedAction(
    Guid? actorParticipantId,
    Guid? actorEnemyInstanceId,
    string actorDisplayName,
    string actionKind,
    int? moveId,
    string? moveName,
    bool succeeded,
    IEnumerable<QuestResolvedTargetSummary>? targetSummaries = null,
    IEnumerable<string>? logs = null,
    IEnumerable<QuestBattleLogEntry>? logEntries = null)
{
    private readonly QuestResolvedTargetSummary[] targetSummaries = targetSummaries?.ToArray() ?? [];
    private readonly string[] logs = logs?.ToArray() ?? [];
    private readonly QuestBattleLogEntry[] logEntries = logEntries?.ToArray() ?? [];

    public Guid? ActorParticipantId { get; } = actorParticipantId;
    public Guid? ActorEnemyInstanceId { get; } = actorEnemyInstanceId;
    public string ActorDisplayName { get; } = actorDisplayName;
    public string ActionKind { get; } = actionKind;
    public int? MoveId { get; } = moveId;
    public string? MoveName { get; } = moveName;
    public bool Succeeded { get; } = succeeded;
    public IReadOnlyList<QuestResolvedTargetSummary> TargetSummaries => targetSummaries;
    public IReadOnlyList<string> Logs => logs;
    public IReadOnlyList<QuestBattleLogEntry> LogEntries => logEntries;
}

public class QuestBattleLogEntry(string text, IEnumerable<QuestBattleLogSegment>? segments = null)
{
    private readonly QuestBattleLogSegment[] segments = segments?.ToArray() ?? [];

    public string Text { get; } = text;
    public IReadOnlyList<QuestBattleLogSegment> Segments => segments;
}

public class QuestBattleLogSegment(string text, string tone)
{
    public string Text { get; } = text;
    public string Tone { get; } = tone;
}

public class QuestResolvedTargetSummary(
    Guid? targetParticipantId,
    Guid? targetEnemyInstanceId,
    string targetDisplayName,
    string resultType,
    int hpChange,
    int mpChange,
    IEnumerable<string>? appliedEffects = null,
    IEnumerable<string>? removedEffects = null,
    bool isDeadAfterAction = false)
{
    private readonly string[] appliedEffects = appliedEffects?.ToArray() ?? [];
    private readonly string[] removedEffects = removedEffects?.ToArray() ?? [];

    public Guid? TargetParticipantId { get; } = targetParticipantId;
    public Guid? TargetEnemyInstanceId { get; } = targetEnemyInstanceId;
    public string TargetDisplayName { get; } = targetDisplayName;
    public string ResultType { get; } = resultType;
    public int HpChange { get; } = hpChange;
    public int MpChange { get; } = mpChange;
    public IReadOnlyList<string> AppliedEffects => appliedEffects;
    public IReadOnlyList<string> RemovedEffects => removedEffects;
    public bool IsDeadAfterAction { get; } = isDeadAfterAction;
}

public class QuestFloorTransition(int previousFloorNo, int currentFloorNo, bool floorCleared, bool bossFloorReached)
{
    public int PreviousFloorNo { get; } = previousFloorNo;
    public int CurrentFloorNo { get; } = currentFloorNo;
    public bool FloorCleared { get; } = floorCleared;
    public bool BossFloorReached { get; } = bossFloorReached;
}

public class QuestRunTransition(string previousStatus, string currentStatus, bool questEnded)
{
    public string PreviousStatus { get; } = previousStatus;
    public string CurrentStatus { get; } = currentStatus;
    public bool QuestEnded { get; } = questEnded;
}
