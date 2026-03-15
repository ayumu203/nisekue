using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestAllyNpcActionPolicy
{
    public QuestSubmittedCommand SelectAction(
        QuestRun run,
        QuestParticipantId participantId,
        IReadOnlyList<Move> availableMoves,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(availableMoves);

        var snapshot = run.PartySnapshots.FirstOrDefault(x => x.ParticipantId == participantId)
            ?? throw new KeyNotFoundException($"参加者スナップショットが見つかりません。 participantId={participantId.Value}");
        var state = run.BattleState.FindPartyMember(participantId);
        var orderedMoves = OrderMovesByMoveSet(snapshot.MoveSet, availableMoves);

        return snapshot.Job switch
        {
            Job.Warrior => SelectWarriorAction(run, snapshot, state, orderedMoves, now),
            Job.Guardian => SelectGuardianAction(run, snapshot, state, orderedMoves, now),
            Job.Mage => SelectMageAction(run, snapshot, state, orderedMoves, now),
            Job.Priest => SelectPriestAction(run, snapshot, state, orderedMoves, now),
            Job.Ranger => SelectRangerAction(run, snapshot, state, orderedMoves, now),
            _ => CreateNormalAttack(run, participantId, snapshot.StartPosition, now, EnemyFrontFirstOrder)
        };
    }

    private static QuestSubmittedCommand SelectWarriorAction(
        QuestRun run,
        QuestRunPartyMemberSnapshot snapshot,
        QuestRunPartyMemberState state,
        IReadOnlyList<Move> orderedMoves,
        DateTimeOffset now)
    {
        var target = FindEnemy(run, EnemyFrontFirstOrder);
        if (target is null)
        {
            return CreateWait(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, now);
        }

        var usableSingleAttack = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsSingleAttackMove)
            .ToArray();
        if (usableSingleAttack.Length > 0 && run.FloorState.IsBossFloor)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableSingleAttack[0], target.Value.Position, now);
        }

        return CreateNormalAttack(run, snapshot.ParticipantId, snapshot.StartPosition, now, EnemyFrontFirstOrder);
    }

    private static QuestSubmittedCommand SelectGuardianAction(
        QuestRun run,
        QuestRunPartyMemberSnapshot snapshot,
        QuestRunPartyMemberState state,
        IReadOnlyList<Move> orderedMoves,
        DateTimeOffset now)
    {
        var target = FindEnemy(run, EnemyFrontFirstOrder);
        if (target is null)
        {
            return CreateWait(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, now);
        }

        var hasTaunt = state.Ailments.Any(x => x.Type == AilmentType.Taunt);
        var tauntMove = orderedMoves
            .Where(move => CanUseMove(state, move))
            .FirstOrDefault(IsTauntMove);
        if (!hasTaunt && tauntMove is not null)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, tauntMove, null, now);
        }

        var isMpConserving = state.CurrentMp <= (int)Math.Floor(snapshot.BaseStatus.MaxMp * 0.3m);
        if (!isMpConserving)
        {
            var defenseAttackMove = orderedMoves
                .Where(move => CanUseMove(state, move))
                .FirstOrDefault(IsDefenseAttackMove);
            if (run.FloorState.IsBossFloor && defenseAttackMove is not null)
            {
                return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, defenseAttackMove, target.Value.Position, now);
            }
        }

        return CreateNormalAttack(run, snapshot.ParticipantId, snapshot.StartPosition, now, EnemyFrontFirstOrder);
    }

    private static QuestSubmittedCommand SelectMageAction(
        QuestRun run,
        QuestRunPartyMemberSnapshot snapshot,
        QuestRunPartyMemberState state,
        IReadOnlyList<Move> orderedMoves,
        DateTimeOffset now)
    {
        var target = FindEnemy(run, EnemyBackFirstOrder);
        if (target is null)
        {
            return CreateWait(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, now);
        }

        var usableSingleAttacks = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsSingleAttackMove)
            .OrderBy(move => move.MpCost)
            .ThenBy(move => GetMoveSlotIndex(snapshot.MoveSet, move.Id))
            .ToArray();
        var usableAreaAttacks = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsAreaAttackMove)
            .OrderByDescending(GetAttackRangePriority)
            .ThenBy(move => GetMoveSlotIndex(snapshot.MoveSet, move.Id))
            .ToArray();

        if (run.FloorState.IsBossFloor && usableSingleAttacks.Length > 0)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableSingleAttacks[0], target.Value.Position, now);
        }

        var hasHalfMp = state.CurrentMp >= (int)Math.Ceiling(snapshot.BaseStatus.MaxMp * 0.5m);
        if (!run.FloorState.IsBossFloor && hasHalfMp && usableAreaAttacks.Length > 0)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableAreaAttacks[0], target.Value.Position, now);
        }

        if (usableSingleAttacks.Length > 0)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableSingleAttacks[0], target.Value.Position, now);
        }

        var restoreMpMove = orderedMoves
            .Where(move => CanUseMove(state, move))
            .FirstOrDefault(IsRestoreMpMove);
        if (restoreMpMove is not null)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, restoreMpMove, snapshot.StartPosition, now);
        }

        return CreateNormalAttack(run, snapshot.ParticipantId, snapshot.StartPosition, now, EnemyBackFirstOrder);
    }

    private static QuestSubmittedCommand SelectPriestAction(
        QuestRun run,
        QuestRunPartyMemberSnapshot snapshot,
        QuestRunPartyMemberState state,
        IReadOnlyList<Move> orderedMoves,
        DateTimeOffset now)
    {
        var healTarget = FindLowHpAlly(run, snapshot.ParticipantId);
        var usableHealMoves = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsHealMove)
            .ToArray();
        if (healTarget is not null && usableHealMoves.Length > 0)
        {
            var selectedHeal = SelectBestHealMove(snapshot, healTarget.Value.Snapshot, healTarget.Value.State, usableHealMoves);
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, selectedHeal, healTarget.Value.Snapshot.StartPosition, now);
        }

        var usableDefenseBuffMoves = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsDefenseBuffMove)
            .OrderByDescending(GetAttackRangePriority)
            .ThenByDescending(GetDefenseBuffValue)
            .ThenBy(move => GetMoveSlotIndex(snapshot.MoveSet, move.Id))
            .ToArray();
        if (usableDefenseBuffMoves.Length > 0)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableDefenseBuffMoves[0], null, now);
        }

        var prayerTarget = FindPrayerTarget(run, snapshot.ParticipantId);
        if (prayerTarget is not null)
        {
            return new QuestSubmittedCommand(
                snapshot.ParticipantId,
                run.TurnState.CurrentTurnNo,
                ActionKind.Prayer,
                now,
                selectedTargetPosition: prayerTarget.StartPosition,
                isAutoSubmitted: true);
        }

        return CreateNormalAttack(run, snapshot.ParticipantId, snapshot.StartPosition, now, EnemyFrontFirstOrder);
    }

    private static QuestSubmittedCommand SelectRangerAction(
        QuestRun run,
        QuestRunPartyMemberSnapshot snapshot,
        QuestRunPartyMemberState state,
        IReadOnlyList<Move> orderedMoves,
        DateTimeOffset now)
    {
        var usableTrapMoves = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsTrapMove)
            .ToArray();
        if (run.Traps.Traps.Count == 0 && usableTrapMoves.Length > 0)
        {
            var trapTarget = FindEnemy(run, EnemyBackFirstOrder);
            if (trapTarget is not null)
            {
                return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableTrapMoves[0], trapTarget.Value.Position, now);
            }
        }

        var statusTarget = FindEnemy(run, EnemyBackMidThenFrontOrder);
        var usableStatusMoves = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsStatusAilmentMove)
            .ToArray();
        if (statusTarget is not null && usableStatusMoves.Length > 0)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableStatusMoves[0], statusTarget.Value.Position, now);
        }

        var usableSingleAttack = orderedMoves
            .Where(move => CanUseMove(state, move))
            .Where(IsSingleAttackMove)
            .OrderBy(move => move.MpCost)
            .ThenBy(move => GetMoveSlotIndex(snapshot.MoveSet, move.Id))
            .ToArray();
        if (statusTarget is not null && usableSingleAttack.Length > 0)
        {
            return CreateUseMove(snapshot.ParticipantId, run.TurnState.CurrentTurnNo, usableSingleAttack[0], statusTarget.Value.Position, now);
        }

        return CreateNormalAttack(run, snapshot.ParticipantId, snapshot.StartPosition, now, EnemyBackFirstOrder);
    }

    private static IReadOnlyList<Move> OrderMovesByMoveSet(MoveSet moveSet, IReadOnlyList<Move> availableMoves)
    {
        var moveById = availableMoves.ToDictionary(x => x.Id.Id);
        var ordered = new List<Move>();
        foreach (var moveId in moveSet.Slots)
        {
            if (moveId is not null && moveById.TryGetValue(moveId.Id, out var move))
            {
                ordered.Add(move);
            }
        }

        return ordered;
    }

    private static bool CanUseMove(QuestRunPartyMemberState state, Move move)
    {
        return state.CurrentMp >= move.MpCost;
    }

    private static bool IsSingleAttackMove(Move move)
    {
        return move.TargetType == TargetType.Enemy &&
               move.AttackRange == AttackRange.Single &&
               move.Effects.Any(effect => effect.EffectType == MoveEffectType.Damage);
    }

    private static bool IsHealMove(Move move)
    {
        return move.Effects.Any(effect => effect.EffectType == MoveEffectType.Heal);
    }

    private static bool IsTauntMove(Move move)
    {
        return move.Effects.Any(effect =>
            effect.EffectType == MoveEffectType.Ailment &&
            effect.Ailment?.AilmentType == AilmentType.Taunt);
    }

    private static bool IsDefenseAttackMove(Move move)
    {
        return move.Effects.Any(effect =>
            effect.EffectType == MoveEffectType.Damage &&
            effect.Damage?.AttackStat == BuffStat.Defense);
    }

    private static bool IsAreaAttackMove(Move move)
    {
        return move.TargetType == TargetType.Enemy &&
               move.AttackRange != AttackRange.Single &&
               move.Effects.Any(effect => effect.EffectType == MoveEffectType.Damage);
    }

    private static bool IsDefenseBuffMove(Move move)
    {
        return move.Effects.Any(effect =>
            effect.EffectType == MoveEffectType.Buff &&
            effect.Buff?.BuffStat == BuffStat.Defense);
    }

    private static bool IsTrapMove(Move move)
    {
        return move.Effects.Any(effect =>
            effect.EffectType == MoveEffectType.Ailment &&
            effect.Ailment is not null &&
            (effect.Ailment.AilmentType == AilmentType.DamageTrap || effect.Ailment.AilmentType == AilmentType.PoisonTrap));
    }

    private static bool IsStatusAilmentMove(Move move)
    {
        return move.TargetType == TargetType.Enemy &&
               move.Effects.Any(effect =>
                   effect.EffectType == MoveEffectType.Ailment &&
                   effect.Ailment is not null &&
                   effect.Ailment.AilmentType is not (AilmentType.DamageTrap or AilmentType.PoisonTrap));
    }

    private static bool IsRestoreMpMove(Move move)
    {
        return move.Effects.Any(effect => effect.EffectType == MoveEffectType.RestoreMp);
    }

    private static Move SelectBestHealMove(
        QuestRunPartyMemberSnapshot healer,
        QuestRunPartyMemberSnapshot targetSnapshot,
        QuestRunPartyMemberState targetState,
        IReadOnlyList<Move> healMoves)
    {
        var bestInRange = new List<(Move Move, int HealedHp)>();
        var bestUnder = new List<(Move Move, int HealedHp)>();
        var bestOver = new List<(Move Move, int HealedHp)>();

        foreach (var move in healMoves)
        {
            var healedHp = EstimateHealedHp(healer, targetState, move);
            var ratio = healedHp / (decimal)targetSnapshot.BaseStatus.MaxHp;
            if (ratio >= 0.9m && ratio <= 1m)
            {
                bestInRange.Add((move, healedHp));
                continue;
            }

            if (healedHp <= targetSnapshot.BaseStatus.MaxHp)
            {
                bestUnder.Add((move, healedHp));
                continue;
            }

            bestOver.Add((move, healedHp));
        }

        if (bestInRange.Count > 0)
        {
            return bestInRange
                .OrderByDescending(x => x.HealedHp)
                .ThenBy(x => x.Move.MpCost)
                .First().Move;
        }

        if (bestUnder.Count > 0)
        {
            return bestUnder
                .OrderByDescending(x => x.HealedHp)
                .ThenBy(x => x.Move.MpCost)
                .First().Move;
        }

        return bestOver
            .OrderBy(x => x.HealedHp - targetSnapshot.BaseStatus.MaxHp)
            .ThenBy(x => x.Move.MpCost)
            .First().Move;
    }

    private static int EstimateHealedHp(QuestRunPartyMemberSnapshot healer, QuestRunPartyMemberState targetState, Move move)
    {
        var effect = move.Effects.First(x => x.EffectType == MoveEffectType.Heal);
        var damage = effect.Damage ?? throw new InvalidOperationException("Heal 効果に Damage 定義がありません。");
        var attackStat = damage.AttackStat ?? (move.Category != MoveCategory.Attack ? BuffStat.Intelligence : BuffStat.Strength);
        var attackPower = attackStat switch
        {
            BuffStat.Intelligence => healer.BaseStatus.Intelligence,
            BuffStat.Defense => healer.BaseStatus.Defense,
            _ => healer.BaseStatus.Strength
        };
        var healValue = damage.FixedValue + (int)Math.Round(attackPower * damage.PowerRate, MidpointRounding.AwayFromZero);
        return targetState.CurrentHp + Math.Max(1, healValue);
    }

    private static int GetAttackRangePriority(Move move)
    {
        return move.AttackRange switch
        {
            AttackRange.All => 5,
            AttackRange.Square => 4,
            AttackRange.Row => 3,
            AttackRange.Column => 3,
            AttackRange.Single => 2,
            _ => 0
        };
    }

    private static decimal GetDefenseBuffValue(Move move)
    {
        return move.Effects
            .Where(effect => effect.EffectType == MoveEffectType.Buff && effect.Buff?.BuffStat == BuffStat.Defense)
            .Select(effect => effect.Buff!.BuffValue)
            .DefaultIfEmpty(0m)
            .Max();
    }

    private static int GetMoveSlotIndex(MoveSet moveSet, MoveId moveId)
    {
        for (var i = 0; i < moveSet.Slots.Count; i++)
        {
            if (moveSet.Slots[i]?.Id == moveId.Id)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static QuestSubmittedCommand CreateNormalAttack(
        QuestRun run,
        QuestParticipantId participantId,
        BattlePosition actorPosition,
        DateTimeOffset now,
        IReadOnlyList<BattlePosition> preferredPositions)
    {
        var target = FindReachableEnemy(run, actorPosition, preferredPositions);
        if (target is null)
        {
            return CreateWait(participantId, run.TurnState.CurrentTurnNo, now);
        }

        return new QuestSubmittedCommand(
            participantId,
            run.TurnState.CurrentTurnNo,
            ActionKind.NormalAttack,
            now,
            selectedTargetPosition: target?.Position,
            isAutoSubmitted: true);
    }

    private static (QuestEnemyState Enemy, BattlePosition Position)? FindReachableEnemy(
        QuestRun run,
        BattlePosition actorPosition,
        IReadOnlyList<BattlePosition> preferredPositions)
    {
        var reachableRows = GetReachableRows(actorPosition.Row);
        var reachableEnemies = run.BattleState.Enemies
            .Where(x => !x.IsDead && reachableRows.Contains(x.Position.Row))
            .ToArray();
        if (reachableEnemies.Length == 0)
        {
            return null;
        }

        foreach (var preferredPosition in preferredPositions)
        {
            var enemy = reachableEnemies.FirstOrDefault(x => x.Position == preferredPosition);
            if (enemy is not null)
            {
                return (enemy, enemy.Position);
            }
        }

        var fallback = reachableEnemies
            .OrderBy(x => (int)x.Position.Row)
            .ThenBy(x => (int)x.Position.Column)
            .FirstOrDefault();
        return fallback is null ? null : (fallback, fallback.Position);
    }

    private static IReadOnlySet<BattleRow> GetReachableRows(BattleRow actorRow)
    {
        return actorRow switch
        {
            BattleRow.Front => new HashSet<BattleRow> { BattleRow.Front },
            BattleRow.Middle => new HashSet<BattleRow> { BattleRow.Front, BattleRow.Middle },
            BattleRow.Back => new HashSet<BattleRow> { BattleRow.Front, BattleRow.Middle, BattleRow.Back },
            _ => throw new ArgumentOutOfRangeException(nameof(actorRow), $"未対応の BattleRow: {actorRow}")
        };
    }

    private static QuestSubmittedCommand CreateUseMove(
        QuestParticipantId participantId,
        int turnNo,
        Move move,
        BattlePosition? targetPosition,
        DateTimeOffset now)
    {
        return new QuestSubmittedCommand(
            participantId,
            turnNo,
            ActionKind.UseMove,
            now,
            move.Id,
            targetPosition,
            isAutoSubmitted: true);
    }

    private static QuestSubmittedCommand CreateWait(QuestParticipantId participantId, int turnNo, DateTimeOffset now)
    {
        return new QuestSubmittedCommand(
            participantId,
            turnNo,
            ActionKind.Wait,
            now,
            isAutoSubmitted: true);
    }

    private static (QuestEnemyState Enemy, BattlePosition Position)? FindEnemy(QuestRun run, IReadOnlyList<BattlePosition> preferredPositions)
    {
        foreach (var preferredPosition in preferredPositions)
        {
            var enemy = run.BattleState.Enemies.FirstOrDefault(x => !x.IsDead && x.Position == preferredPosition);
            if (enemy is not null)
            {
                return (enemy, enemy.Position);
            }
        }

        var fallback = run.BattleState.Enemies
            .Where(x => !x.IsDead)
            .OrderBy(x => (int)x.Position.Row)
            .ThenBy(x => (int)x.Position.Column)
            .FirstOrDefault();
        return fallback is null ? null : (fallback, fallback.Position);
    }

    private static (QuestRunPartyMemberSnapshot Snapshot, QuestRunPartyMemberState State)? FindLowHpAlly(
        QuestRun run,
        QuestParticipantId actorParticipantId)
    {
        foreach (var position in AllyFrontFirstOrder)
        {
            var ally = run.PartySnapshots
                .Join(run.BattleState.PartyMembers, x => x.ParticipantId, x => x.ParticipantId, (snapshot, state) => new { snapshot, state })
                .FirstOrDefault(x =>
                    x.snapshot.ParticipantId != actorParticipantId &&
                    x.snapshot.StartPosition == position &&
                    !x.state.IsDead &&
                    !x.state.HasLeftQuest &&
                    x.state.CurrentHp < (int)Math.Floor(x.snapshot.BaseStatus.MaxHp * 0.5m));
            if (ally is not null)
            {
                return (ally.snapshot, ally.state);
            }
        }

        return null;
    }

    private static QuestRunPartyMemberSnapshot? FindPrayerTarget(QuestRun run, QuestParticipantId actorParticipantId)
    {
        foreach (var position in AllyFrontFirstOrder)
        {
            var ally = run.PartySnapshots
                .Join(run.BattleState.PartyMembers, x => x.ParticipantId, x => x.ParticipantId, (snapshot, state) => new { snapshot, state })
                .FirstOrDefault(x =>
                    x.snapshot.ParticipantId != actorParticipantId &&
                    x.snapshot.StartPosition == position &&
                    !x.state.IsDead &&
                    !x.state.HasLeftQuest);
            if (ally is not null)
            {
                return ally.snapshot;
            }
        }

        return null;
    }

    private static readonly BattlePosition[] EnemyFrontFirstOrder =
    [
        new(BattleRow.Front, BattleColumn.Left),
        new(BattleRow.Front, BattleColumn.Right),
        new(BattleRow.Middle, BattleColumn.Left),
        new(BattleRow.Middle, BattleColumn.Right),
        new(BattleRow.Back, BattleColumn.Left),
        new(BattleRow.Back, BattleColumn.Right)
    ];

    private static readonly BattlePosition[] EnemyBackFirstOrder =
    [
        new(BattleRow.Back, BattleColumn.Left),
        new(BattleRow.Back, BattleColumn.Right),
        new(BattleRow.Middle, BattleColumn.Left),
        new(BattleRow.Middle, BattleColumn.Right),
        new(BattleRow.Front, BattleColumn.Left),
        new(BattleRow.Front, BattleColumn.Right)
    ];

    private static readonly BattlePosition[] EnemyBackMidThenFrontOrder =
    [
        new(BattleRow.Back, BattleColumn.Left),
        new(BattleRow.Back, BattleColumn.Right),
        new(BattleRow.Middle, BattleColumn.Left),
        new(BattleRow.Middle, BattleColumn.Right),
        new(BattleRow.Front, BattleColumn.Left),
        new(BattleRow.Front, BattleColumn.Right)
    ];

    private static readonly BattlePosition[] AllyFrontFirstOrder =
    [
        new(BattleRow.Front, BattleColumn.Left),
        new(BattleRow.Front, BattleColumn.Right),
        new(BattleRow.Middle, BattleColumn.Left),
        new(BattleRow.Middle, BattleColumn.Right),
        new(BattleRow.Back, BattleColumn.Left),
        new(BattleRow.Back, BattleColumn.Right)
    ];
}
