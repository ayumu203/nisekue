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
        PetBattlePartyMemberState opponentMemberState,
        IReadOnlyList<PetBattlePartyMemberState> ownerMemberStates,
        IReadOnlyList<Move> availableMoves)
    {
        var actorId = opponentSnapshot.ParticipantId.Value;

        var livingOwnerIds = ownerMemberStates
            .Where(m => !m.IsDead)
            .Select(m => m.ParticipantId.Value)
            .ToArray();

        if (livingOwnerIds.Length == 0)
        {
            return NormalAttack(actorId);
        }

        // MP が足りてダメージを与えられる攻撃系スキルのみ選択
        var offensiveMoves = availableMoves
            .Where(m => m.MpCost <= opponentMemberState.CurrentMp
                && m.Effects.Any(e =>
                    e.EffectType == MoveEffectType.Damage
                    || e.EffectType == MoveEffectType.Knockout
                    || e.EffectType == MoveEffectType.HalveSelfHp))
            .ToArray();

        if (offensiveMoves.Length > 0)
        {
            var move = offensiveMoves[Random.Shared.Next(offensiveMoves.Length)];
            return new BattleActionInput(
                actorId,
                BattleActionKind.UseMove,
                move.Id.Id,
                move.TargetType,
                move.AttackRange,
                move.TargetType == TargetType.Enemy
                    ? [livingOwnerIds[Random.Shared.Next(livingOwnerIds.Length)]]
                    : null);
        }

        return NormalAttack(actorId);
    }

    private static BattleActionInput NormalAttack(Guid actorId) =>
        new(actorId, BattleActionKind.NormalAttack, null, TargetType.Enemy, AttackRange.Single);
}
