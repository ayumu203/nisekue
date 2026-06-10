namespace server.domain.pet_battle;

public class PetBattleTurnState(
    int currentTurnNo,
    DateTimeOffset actionDeadlineAt,
    IEnumerable<PetBattleSubmittedCommand>? pendingCommands = null,
    int? lastResolvedTurnNo = null)
{
    private readonly List<PetBattleSubmittedCommand> pendingCommands = pendingCommands?.ToList() ?? [];

    public int CurrentTurnNo { get; private set; } = currentTurnNo > 0 ? currentTurnNo : throw new ArgumentOutOfRangeException(nameof(currentTurnNo));
    public DateTimeOffset ActionDeadlineAt { get; private set; } = actionDeadlineAt;
    public int? LastResolvedTurnNo { get; private set; } = lastResolvedTurnNo;
    public IReadOnlyList<PetBattleSubmittedCommand> PendingCommands => pendingCommands;

    public void Submit(PetBattleSubmittedCommand command)
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
        LastResolvedTurnNo = CurrentTurnNo;
        CurrentTurnNo++;
        pendingCommands.Clear();
        ActionDeadlineAt = nextDeadlineAt;
    }
}
