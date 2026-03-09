namespace server.domain.battle;

public class BattleTurnResolution(
    IEnumerable<BattleActionResult>? actionResults,
    IEnumerable<BattleActorState>? updatedStates)
{
    private readonly BattleActionResult[] _actionResults = actionResults?.ToArray() ?? [];
    private readonly BattleActorState[] _updatedStates = updatedStates?.ToArray() ?? [];

    public IReadOnlyList<BattleActionResult> ActionResults => _actionResults;
    public IReadOnlyList<BattleActorState> UpdatedStates => _updatedStates;
}
