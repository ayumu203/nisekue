using server.application.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.move.enums;
using server.domain.quest;

namespace server.application.quest;

public class QuestPetActionService
{
    // 出撃中のペットは MP を管理しない（技の消費 MP で失敗しない値を与える）
    private const int DeployedPetMp = 999999;

    // 出撃中のペット1体の戦闘入力を作る。技を持っていれば毎ターンランダムに1つ使い、なければ通常攻撃。
    public (BattleActorInput Actor, BattleActionInput Action, Move? SelectedMove) CreateDeployedPetInputs(
        QuestDeployedPetState pet,
        string displayName,
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
            ActorId: pet.ActorId,
            DisplayName: displayName,
            Side: BattleSide.Ally,
            BaseStatus: pet.Status,
            MoveSet: moveSet,
            CurrentHp: pet.CurrentHp,
            CurrentMp: DeployedPetMp,
            Ailments: pet.Ailments.ToArray(),
            Buffs: pet.Buffs.ToArray());

        if (learnableMoves.Length == 0)
        {
            var attack = new BattleActionInput(
                ActorId: pet.ActorId,
                Kind: BattleActionKind.NormalAttack,
                MoveId: null,
                TargetType: TargetType.Enemy,
                AttackRange: AttackRange.Single);
            return (actor, attack, null);
        }

        var selectedMove = learnableMoves[rollProvider(learnableMoves.Length) - 1];
        var action = new BattleActionInput(
            ActorId: pet.ActorId,
            Kind: BattleActionKind.UseMove,
            MoveId: selectedMove.Id.Id,
            TargetType: selectedMove.TargetType,
            AttackRange: selectedMove.AttackRange);
        return (actor, action, selectedMove);
    }
}
