namespace server.domain.quest;

public class QuestTurnState(
    int currentTurnNo,
    DateTimeOffset actionDeadlineAt,
    IEnumerable<QuestSubmittedCommand>? pendingCommands = null,
    int? lastResolvedTurnNo = null)
{
    private readonly List<QuestSubmittedCommand> pendingCommands = pendingCommands?.ToList() ?? [];

    public int CurrentTurnNo { get; private set; } = ValidateTurnNo(currentTurnNo);
    public DateTimeOffset ActionDeadlineAt { get; private set; } = actionDeadlineAt;
    public int? LastResolvedTurnNo { get; private set; } = lastResolvedTurnNo;
    public IReadOnlyList<QuestSubmittedCommand> PendingCommands => pendingCommands;

    public void Submit(QuestSubmittedCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.TurnNo != CurrentTurnNo)
        {
            throw new InvalidOperationException("現在のターン以外の行動は受け付けできません。");
        }

        if (pendingCommands.Any(x => x.ParticipantId == command.ParticipantId))
        {
            throw new InvalidOperationException("同一ターンに複数の行動は登録できません。");
        }

        pendingCommands.Add(command);
    }

    public void Advance(DateTimeOffset nextDeadlineAt)
    {
        MarkResolved();
        CurrentTurnNo++;
        ActionDeadlineAt = nextDeadlineAt;
    }

    public void MarkResolved()
    {
        LastResolvedTurnNo = CurrentTurnNo;
        pendingCommands.Clear();
    }

    private static int ValidateTurnNo(int turnNo)
    {
        if (turnNo <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(turnNo), "ターン番号は1以上である必要があります。");
        }

        return turnNo;
    }
}
