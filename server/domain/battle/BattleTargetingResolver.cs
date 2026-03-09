using server.domain.battle.enums;
using server.domain.move.enums;

namespace server.domain.battle;

public class BattleTargetingResolver
{
    public IReadOnlyList<BattleActorId> ResolveTargets(
        BattleTargetSelector selector,
        BattleActorSnapshot actorSnapshot,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        BattleFieldContext? fieldContext = null)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var snapshotMap = snapshots?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(snapshots));
        var stateMap = states?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(states));

        var candidates = snapshotMap.Values
            .Where(x => IsTargetTypeMatch(selector.TargetType, actorSnapshot, x))
            .Where(x => stateMap.TryGetValue(x.Id, out var state) && !state.IsDead)
            .ToArray();

        if (selector.TargetActorIds.Count > 0)
        {
            var targetIdSet = selector.TargetActorIds.ToHashSet();
            candidates = candidates
                .Where(x => targetIdSet.Contains(x.Id))
                .ToArray();
        }

        if (ShouldApplyFormationRangeControl(selector, fieldContext))
        {
            candidates = FilterByFormationRange(actorSnapshot, candidates, fieldContext!).ToArray();
            return ApplyFormationAttackRange(selector.AttackRange, candidates, fieldContext!);
        }

        return ApplyDefaultAttackRange(
            selector.AttackRange,
            candidates.OrderBy(x => x.Id.Value).Select(x => x.Id).ToArray());
    }

    private static bool ShouldApplyFormationRangeControl(BattleTargetSelector selector, BattleFieldContext? fieldContext)
    {
        return fieldContext is not null &&
               fieldContext.EnableFormationRangeControl &&
               selector.TargetType == TargetType.Enemy &&
               fieldContext.Positions.Count > 0;
    }

    private static IEnumerable<BattleActorSnapshot> FilterByFormationRange(
        BattleActorSnapshot actorSnapshot,
        IEnumerable<BattleActorSnapshot> candidates,
        BattleFieldContext fieldContext)
    {
        var positionMap = fieldContext.Positions.ToDictionary(x => x.ActorId, x => x.Position);
        if (!positionMap.TryGetValue(actorSnapshot.Id, out var actorPosition))
        {
            return candidates.OrderBy(x => x.Id.Value);
        }

        var candidatesWithPosition = candidates
            .Where(x => positionMap.ContainsKey(x.Id))
            .Select(x => new { Snapshot = x, Position = positionMap[x.Id] })
            .ToArray();
        if (candidatesWithPosition.Length == 0)
        {
            return Array.Empty<BattleActorSnapshot>();
        }

        var occupiedRows = candidatesWithPosition
            .Select(x => x.Position.Row)
            .Distinct()
            .OrderBy(x => (int)x)
            .ToArray();
        var maxReachableRows = actorPosition.Row switch
        {
            BattleRow.Front => 1,
            BattleRow.Middle => 2,
            BattleRow.Back => occupiedRows.Length,
            _ => throw new ArgumentOutOfRangeException(nameof(actorPosition.Row), $"未対応の BattleRow: {actorPosition.Row}")
        };

        var reachableRows = occupiedRows.Take(maxReachableRows).ToHashSet();
        return candidatesWithPosition
            .Where(x => reachableRows.Contains(x.Position.Row))
            .OrderBy(x => (int)x.Position.Row)
            .ThenBy(x => (int)x.Position.Column)
            .ThenBy(x => x.Snapshot.Id.Value)
            .Select(x => x.Snapshot);
    }

    private static IReadOnlyList<BattleActorId> ApplyFormationAttackRange(
        AttackRange attackRange,
        IReadOnlyList<BattleActorSnapshot> orderedCandidates,
        BattleFieldContext fieldContext)
    {
        var positionMap = fieldContext.Positions.ToDictionary(x => x.ActorId, x => x.Position);
        if (orderedCandidates.Count == 0)
        {
            return [];
        }

        var anchor = positionMap[orderedCandidates[0].Id];
        return attackRange switch
        {
            AttackRange.Single => [orderedCandidates[0].Id],
            AttackRange.Column => orderedCandidates
                .Where(x => positionMap[x.Id].Column == anchor.Column)
                .Select(x => x.Id)
                .ToArray(),
            AttackRange.Row => orderedCandidates
                .Where(x => positionMap[x.Id].Row == anchor.Row)
                .Select(x => x.Id)
                .ToArray(),
            AttackRange.Square => orderedCandidates
                .Where(x => (int)positionMap[x.Id].Row <= (int)anchor.Row + 1)
                .Take(4)
                .Select(x => x.Id)
                .ToArray(),
            AttackRange.All => orderedCandidates.Select(x => x.Id).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(attackRange), $"未対応の AttackRange: {attackRange}")
        };
    }

    private static IReadOnlyList<BattleActorId> ApplyDefaultAttackRange(AttackRange attackRange, IReadOnlyList<BattleActorId> candidates)
    {
        return attackRange switch
        {
            AttackRange.Single => candidates.Take(1).ToArray(),
            AttackRange.Column => candidates.Take(2).ToArray(),
            AttackRange.Row => candidates.Take(2).ToArray(),
            AttackRange.Square => candidates.Take(4).ToArray(),
            AttackRange.All => candidates,
            _ => throw new ArgumentOutOfRangeException(nameof(attackRange), $"未対応の AttackRange: {attackRange}")
        };
    }

    private static bool IsTargetTypeMatch(TargetType targetType, BattleActorSnapshot actorSnapshot, BattleActorSnapshot targetSnapshot)
    {
        return targetType switch
        {
            TargetType.Enemy => actorSnapshot.Side != targetSnapshot.Side,
            TargetType.Ally => actorSnapshot.Side == targetSnapshot.Side && actorSnapshot.Id != targetSnapshot.Id,
            TargetType.Self => actorSnapshot.Id == targetSnapshot.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(targetType), $"未対応の TargetType: {targetType}")
        };
    }
}
