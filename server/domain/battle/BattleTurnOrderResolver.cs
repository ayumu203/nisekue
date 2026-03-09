using server.domain.battle.enums;
using server.domain.move;

namespace server.domain.battle;

public class BattleTurnOrderResolver(BattleStatusResolver battleStatusResolver)
{
    public BattleActorId[] Resolve(
        IEnumerable<BattleAction> actions,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        IEnumerable<Move> moves)
    {
        ArgumentNullException.ThrowIfNull(actions);
        var snapshotMap = snapshots?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(snapshots));
        var stateMap = states?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(states));
        var moveMap = moves?.ToDictionary(x => x.Id.Id) ?? throw new ArgumentNullException(nameof(moves));

        // こんなイメージ
        // SELECT ActorId
        // FROM Actions
        // WHERE ActorState EXISTS AND IsDead = false
        // ORDER BY Priority DESC, EffectiveSpeed DESC, ActorId ASC
        return actions
            .Where(x => stateMap.TryGetValue(x.ActorId, out var state) && !state.IsDead)
            .OrderByDescending(x => ResolvePriority(x, moveMap))
            .ThenByDescending(x => ResolveEffectiveSpeed(x.ActorId, snapshotMap, stateMap))
            .ThenBy(x => x.ActorId.Value)
            .Select(x => x.ActorId)
            .ToArray();
    }

    private int ResolveEffectiveSpeed(
        BattleActorId actorId,
        IReadOnlyDictionary<BattleActorId, BattleActorSnapshot> snapshotMap,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap)
    {
        var snapshot = snapshotMap[actorId];
        var state = stateMap[actorId];
        return battleStatusResolver.BuildEffectiveStatus(snapshot, state).Speed;
    }

    private static int ResolvePriority(BattleAction action, IReadOnlyDictionary<int, Move> moveMap)
    {
        return action.Kind switch
        {
            BattleActionKind.UseMove when action.MoveId is not null && moveMap.TryGetValue(action.MoveId.Id, out var move) => move.ExecutionPriority,
            BattleActionKind.Wait => -1,
            _ => 0
        };
    }
}
