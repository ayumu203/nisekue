using server.application.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;

namespace server.application.quest;

public class QuestPetActionService
{
    // 単発登場のため MP は管理しない（技の消費 MP で失敗しない値を与える）
    private const int SummonedPetMp = 999999;

    public (BattleActorInput Actor, BattleActionInput Action, Move? SelectedMove) CreateSummonInputs(
        Guid petActorId,
        string displayName,
        QuestPetSnapshot pet,
        IReadOnlyList<Move> availableMoves,
        Func<int, int> rollProvider)
    {
        ArgumentNullException.ThrowIfNull(pet);
        ArgumentNullException.ThrowIfNull(availableMoves);
        ArgumentNullException.ThrowIfNull(rollProvider);

        var moveSet = new MoveSet();
        for (var i = 0; i < availableMoves.Count && i < MoveSet.MaxSlots; i++)
        {
            moveSet.SetSlot(i, availableMoves[i].Id);
        }

        var learnableMoves = availableMoves.Take(MoveSet.MaxSlots).ToArray();

        var actor = new BattleActorInput(
            ActorId: petActorId,
            DisplayName: displayName,
            Side: BattleSide.Ally,
            BaseStatus: pet.Status,
            MoveSet: moveSet,
            CurrentHp: pet.Status.MaxHp,
            CurrentMp: SummonedPetMp);

        var usesMove = learnableMoves.Length > 0 && rollProvider(2) == 2;
        if (!usesMove)
        {
            var attack = new BattleActionInput(
                ActorId: petActorId,
                Kind: BattleActionKind.NormalAttack,
                MoveId: null,
                TargetType: TargetType.Enemy,
                AttackRange: AttackRange.All);
            return (actor, attack, null);
        }

        var selectedMove = learnableMoves[rollProvider(learnableMoves.Length) - 1];
        var action = new BattleActionInput(
            ActorId: petActorId,
            Kind: BattleActionKind.UseMove,
            MoveId: selectedMove.Id.Id,
            TargetType: selectedMove.TargetType,
            AttackRange: selectedMove.AttackRange);
        return (actor, action, selectedMove);
    }
}
