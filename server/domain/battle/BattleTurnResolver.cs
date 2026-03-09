using server.domain.move;
using server.domain.player;

namespace server.domain.battle;

public class BattleTurnResolver(
    BattleTurnOrderResolver battleTurnOrderResolver,
    BattleActionResolver battleActionResolver)
{
    private readonly BattleStatusResolver _battleStatusResolver = new();

    public BattleTurnResolution Resolve(
        IEnumerable<BattleAction> actions,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        IEnumerable<Move> moves,
        BattleFieldContext? fieldContext = null)
    {
        ArgumentNullException.ThrowIfNull(actions);
        var snapshotArray = snapshots?.ToArray() ?? throw new ArgumentNullException(nameof(snapshots));
        var stateArray = states?.Select(CloneState).ToArray() ?? throw new ArgumentNullException(nameof(states));
        var moveArray = moves?.ToArray() ?? throw new ArgumentNullException(nameof(moves));
        var actionArray = actions.ToArray();

        var orderedActorIds = battleTurnOrderResolver.Resolve(actionArray, snapshotArray, stateArray, moveArray);
        var actionMap = actionArray.ToDictionary(x => x.ActorId);
        var actionResults = new List<BattleActionResult>();

        foreach (var actorId in orderedActorIds)
        {
            if (!actionMap.TryGetValue(actorId, out var action))
            {
                continue;
            }

            actionResults.Add(battleActionResolver.Resolve(action, snapshotArray, stateArray, moveArray, fieldContext));
        }

        var snapshotMap = snapshotArray.ToDictionary(x => x.Id);
        foreach (var state in stateArray)
        {
            var beforeStatus = _battleStatusResolver.BuildEffectiveStatus(snapshotMap[state.Id], state);
            state.TickTurnEnd();
            var afterStatus = _battleStatusResolver.BuildEffectiveStatus(snapshotMap[state.Id], state);
            NormalizeCurrentResources(state, beforeStatus, afterStatus);
        }

        return new BattleTurnResolution(actionResults, stateArray.Select(CloneState));
    }

    private static void NormalizeCurrentResources(BattleActorState state, Status beforeStatus, Status afterStatus)
    {
        if (beforeStatus.MaxHp != afterStatus.MaxHp)
        {
            var normalizedHp = NormalizeByRatio(state.CurrentHp, beforeStatus.MaxHp, afterStatus.MaxHp);
            var hpDelta = state.CurrentHp - normalizedHp;
            if (hpDelta > 0)
            {
                state.ReceiveDamage(hpDelta);
            }
            else if (hpDelta < 0)
            {
                state.RestoreHp(-hpDelta, afterStatus.MaxHp);
            }
        }

        if (beforeStatus.MaxMp != afterStatus.MaxMp)
        {
            var normalizedMp = NormalizeByRatio(state.CurrentMp, beforeStatus.MaxMp, afterStatus.MaxMp);
            state.ConsumeMp(Math.Max(0, state.CurrentMp - normalizedMp));
        }
    }

    private static int NormalizeByRatio(int currentValue, int beforeMaxValue, int afterMaxValue)
    {
        if (beforeMaxValue <= 0)
        {
            return Math.Min(currentValue, afterMaxValue);
        }

        var normalized = (int)Math.Floor((decimal)currentValue * afterMaxValue / beforeMaxValue);
        return Math.Clamp(normalized, 0, afterMaxValue);
    }

    private static BattleActorState CloneState(BattleActorState state)
    {
        return new BattleActorState(
            state.Id,
            state.CurrentHp,
            state.CurrentMp,
            state.Ailments.Select(x => new BattleAilmentState(x.Type, x.RemainingTurns)),
            state.Buffs.Select(x => new BattleBuffState(x.Stat, x.CalculationType, x.Value, x.RemainingTurns)));
    }
}
