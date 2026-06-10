using server.application.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.pet_battle;

namespace server.application.pet_battle;

public class PetBattleEnemyActionPolicy
{
    public BattleActionInput SelectAction(
        PetBattlePartyMemberSnapshot opponentSnapshot,
        IReadOnlyList<PetBattlePartyMemberState> ownerMemberStates,
        IReadOnlyList<PetBattlePartyMemberSnapshot> ownerSnapshots,
        IReadOnlyList<Move> availableMoves)
    {
        var actorId = opponentSnapshot.ParticipantId.Value;

        var usableMoves = availableMoves.ToArray();

        var livingOwnerIds = ownerMemberStates
            .Where(m => !m.IsDead)
            .Select(m => m.ParticipantId.Value)
            .ToArray();

        if (livingOwnerIds.Length == 0)
        {
            return NormalAttack(actorId, AttackRange.All, TargetType.Enemy);
        }

        if (usableMoves.Length > 0)
        {
            var move = usableMoves[Random.Shared.Next(usableMoves.Length)];
            return new BattleActionInput(
                actorId,
                BattleActionKind.UseMove,
                move.Id.Id,
                move.TargetType,
                move.AttackRange,
                livingOwnerIds.Length > 0 ? [livingOwnerIds[Random.Shared.Next(livingOwnerIds.Length)]] : null);
        }

        return NormalAttack(actorId, AttackRange.All, TargetType.Enemy);
    }

    private static BattleActionInput NormalAttack(Guid actorId, AttackRange range, TargetType targetType) =>
        new(actorId, BattleActionKind.NormalAttack, null, targetType, range);
}
