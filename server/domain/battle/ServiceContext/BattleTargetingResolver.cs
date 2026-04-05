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
            .Where(x => stateMap.TryGetValue(x.Id, out var state) && IsLifeStateMatch(selector.TargetLifeState, state))
            .ToArray();

        if (selector.TargetActorIds.Count > 0)
        {
            var targetIdSet = selector.TargetActorIds.ToHashSet();
            candidates = candidates
                .Where(x => targetIdSet.Contains(x.Id))
                .ToArray();
        }

        candidates = ReorderCandidatesBySelectedPosition(selector, candidates, fieldContext).ToArray();

        if (ShouldApplyFormationRangeControl(selector, fieldContext))
        {
            candidates = FilterByFormationRange(actorSnapshot, candidates, fieldContext!).ToArray();
            return ApplyFormationAttackRange(selector, candidates, fieldContext!);
        }

        return ApplyDefaultAttackRange(
            selector.AttackRange,
            selector.SelectedPosition is null || fieldContext is null || fieldContext.Positions.Count == 0
                ? candidates.OrderBy(x => x.Id.Value).Select(x => x.Id).ToArray()
                : candidates.Select(x => x.Id).ToArray());
    }

    public BattleActorId ResolveDamageReceiver(
        BattleActorId targetId,
        IEnumerable<BattleActorSnapshot> snapshots,
        IEnumerable<BattleActorState> states,
        BattleFieldContext? fieldContext = null)
    {
        var snapshotMap = snapshots?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(snapshots));
        var stateMap = states?.ToDictionary(x => x.Id) ?? throw new ArgumentNullException(nameof(states));
        if (!snapshotMap.TryGetValue(targetId, out var targetSnapshot) || !stateMap.TryGetValue(targetId, out var targetState))
        {
            return targetId;
        }

        if (targetState.IsDead)
        {
            return targetId;
        }

        var tauntTarget = FindHighestPriorityActorWithAilment(targetSnapshot.Side, AilmentType.Taunt, snapshotMap.Values, stateMap, fieldContext);
        var preferredTargetId = tauntTarget?.Id ?? targetId;

        var coverActor = FindHighestPriorityActorWithAilment(targetSnapshot.Side, AilmentType.CoverAll, snapshotMap.Values, stateMap, fieldContext);
        return coverActor?.Id ?? preferredTargetId;
    }

    private static BattleActorSnapshot? FindHighestPriorityActorWithAilment(
        BattleSide side,
        AilmentType ailmentType,
        IEnumerable<BattleActorSnapshot> snapshots,
        IReadOnlyDictionary<BattleActorId, BattleActorState> stateMap,
        BattleFieldContext? fieldContext)
    {
        var candidates = snapshots
            .Where(snapshot => snapshot.Side == side)
            .Where(snapshot => stateMap.TryGetValue(snapshot.Id, out var state) &&
                               !state.IsDead &&
                               state.Ailments.Any(ailment => ailment.Type == ailmentType));

        var positionMap = fieldContext?.Positions.ToDictionary(x => x.ActorId, x => x.Position);
        if (positionMap is not null)
        {
            return candidates
                .OrderBy(snapshot => GetTauntPriority(positionMap, snapshot.Id))
                .ThenBy(snapshot => snapshot.Id.Value)
                .FirstOrDefault();
        }

        return candidates
            .OrderBy(snapshot => snapshot.Id.Value)
            .FirstOrDefault();
    }

    private static int GetTauntPriority(IReadOnlyDictionary<BattleActorId, BattlePosition> positionMap, BattleActorId actorId)
    {
        if (!positionMap.TryGetValue(actorId, out var position))
        {
            return int.MaxValue;
        }

        return position.Row switch
        {
            BattleRow.Front when position.Column == BattleColumn.Left => 0,
            BattleRow.Front when position.Column == BattleColumn.Right => 1,
            BattleRow.Middle when position.Column == BattleColumn.Left => 2,
            BattleRow.Middle when position.Column == BattleColumn.Right => 3,
            BattleRow.Back when position.Column == BattleColumn.Left => 4,
            BattleRow.Back when position.Column == BattleColumn.Right => 5,
            _ => int.MaxValue
        };
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
            return candidates
                .Where(x => positionMap.ContainsKey(x.Id))
                .OrderBy(x => x.Id.Value);
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
        BattleTargetSelector selector,
        IReadOnlyList<BattleActorSnapshot> orderedCandidates,
        BattleFieldContext fieldContext)
    {
        var positionMap = fieldContext.Positions.ToDictionary(x => x.ActorId, x => x.Position);
        if (orderedCandidates.Count == 0)
        {
            return [];
        }

        var anchor = ResolveAnchorPosition(selector, orderedCandidates, positionMap);
        return selector.AttackRange switch
        {
            AttackRange.Single => orderedCandidates
                .Where(x => positionMap[x.Id] == anchor)
                .Select(x => x.Id)
                .Take(1)
                .ToArray(),
            AttackRange.AcrossRows => orderedCandidates
                .Where(x => positionMap[x.Id].Column == anchor.Column)
                .Select(x => x.Id)
                .ToArray(),
            AttackRange.AcrossColumns => orderedCandidates
                .Where(x => positionMap[x.Id].Row == anchor.Row)
                .Select(x => x.Id)
                .ToArray(),
            AttackRange.Square => orderedCandidates
                .Where(x => (int)positionMap[x.Id].Row <= (int)anchor.Row + 1)
                .Take(4)
                .Select(x => x.Id)
                .ToArray(),
            AttackRange.All => orderedCandidates.Select(x => x.Id).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(selector.AttackRange), $"未対応の AttackRange: {selector.AttackRange}")
        };
    }

    private static IReadOnlyList<BattleActorSnapshot> ReorderCandidatesBySelectedPosition(
        BattleTargetSelector selector,
        IReadOnlyList<BattleActorSnapshot> candidates,
        BattleFieldContext? fieldContext)
    {
        if (selector.SelectedPosition is null || fieldContext is null || fieldContext.Positions.Count == 0 || candidates.Count == 0)
        {
            return candidates;
        }

        var positionMap = fieldContext.Positions.ToDictionary(x => x.ActorId, x => x.Position);
        var anchor = candidates.FirstOrDefault(x =>
            positionMap.TryGetValue(x.Id, out var position) &&
            position == selector.SelectedPosition.Value);
        if (anchor is null)
        {
            return candidates;
        }

        return [anchor, .. candidates.Where(x => x.Id != anchor.Id)];
    }

    private static BattlePosition ResolveAnchorPosition(
        BattleTargetSelector selector,
        IReadOnlyList<BattleActorSnapshot> orderedCandidates,
        IReadOnlyDictionary<BattleActorId, BattlePosition> positionMap)
    {
        if (selector.SelectedPosition is not null)
        {
            var selectedCandidate = orderedCandidates.FirstOrDefault(x =>
                positionMap.TryGetValue(x.Id, out var position) &&
                position == selector.SelectedPosition.Value);
            if (selectedCandidate is not null)
            {
                return selector.SelectedPosition.Value;
            }
        }

        return positionMap[orderedCandidates[0].Id];
    }

    private static IReadOnlyList<BattleActorId> ApplyDefaultAttackRange(AttackRange attackRange, IReadOnlyList<BattleActorId> candidates)
    {
        return attackRange switch
        {
            AttackRange.Single => candidates.Take(1).ToArray(),
            AttackRange.AcrossRows => candidates.Take(2).ToArray(),
            AttackRange.AcrossColumns => candidates.Take(2).ToArray(),
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

    private static bool IsLifeStateMatch(TargetLifeState targetLifeState, BattleActorState state)
    {
        return targetLifeState switch
        {
            TargetLifeState.Alive => !state.IsDead,
            TargetLifeState.Dead => state.IsDead,
            TargetLifeState.Any => true,
            _ => throw new ArgumentOutOfRangeException(nameof(targetLifeState), $"未対応の TargetLifeState: {targetLifeState}")
        };
    }
}
