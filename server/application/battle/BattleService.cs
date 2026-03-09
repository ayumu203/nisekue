using server.domain.battle;
using server.domain.move;

namespace server.application.battle;

public class BattleService
{
    private readonly BattleTurnResolver _battleTurnResolver;

    public BattleService()
    {
        var statusResolver = new BattleStatusResolver();
        var damageCalculator = new BattleDamageCalculator();
        var actionResolver = new BattleActionResolver(damageCalculator, statusResolver);
        var turnOrderResolver = new BattleTurnOrderResolver(statusResolver);
        _battleTurnResolver = new BattleTurnResolver(turnOrderResolver, actionResolver);
    }

    public BattleTurnResolution ResolveTurn(BattleTurnRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshots = request.Actors
            .Select(x => new BattleActorSnapshot(
                new BattleActorId(x.ActorId),
                x.DisplayName,
                x.Side,
                x.BaseStatus,
                x.MoveSet))
            .ToArray();

        var states = request.Actors
            .Select(x => new BattleActorState(
                new BattleActorId(x.ActorId),
                x.CurrentHp ?? x.BaseStatus.MaxHp,
                x.CurrentMp ?? x.BaseStatus.MaxMp,
                x.Ailments,
                x.Buffs))
            .ToArray();

        var actions = request.Actions
            .Select(x => new BattleAction(
                new BattleActorId(x.ActorId),
                x.Kind,
                new BattleTargetSelector(
                    x.TargetType,
                    x.AttackRange,
                    x.TargetActorIds?.Select(id => new BattleActorId(id))),
                x.MoveId is null ? null : new MoveId(x.MoveId.Value)))
            .ToArray();

        return _battleTurnResolver.Resolve(actions, snapshots, states, request.Moves);
    }
}
