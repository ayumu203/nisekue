using server.domain.battle;
using server.domain.battle.enums;
using server.domain.pet;

namespace server.domain.quest;

// 呼出で出撃したペットのチーム。出撃中はプレイヤー側の代わりに戦闘へ参加する。
// 呼出ターンを 1 ターン目として PetConstants.DeploymentTurns ターン戦い、残りターン 0 またはペット全滅で終了する。
public class QuestPetDeployment(int remainingTurns, IEnumerable<QuestDeployedPetState> pets)
{
    // 出撃したペットは前列左 → 前列右 → 中列左 → … の順に詰める。
    private static readonly BattlePosition[] FormationOrder =
    [
        new(BattleRow.Front, BattleColumn.Left),
        new(BattleRow.Front, BattleColumn.Right),
        new(BattleRow.Middle, BattleColumn.Left),
        new(BattleRow.Middle, BattleColumn.Right),
        new(BattleRow.Back, BattleColumn.Left),
        new(BattleRow.Back, BattleColumn.Right)
    ];

    private readonly QuestDeployedPetState[] pets = pets?.ToArray()
        ?? throw new ArgumentNullException(nameof(pets));

    public int RemainingTurns { get; private set; } = remainingTurns < 0
        ? throw new ArgumentOutOfRangeException(nameof(remainingTurns), "0以上である必要があります。")
        : remainingTurns;
    public IReadOnlyList<QuestDeployedPetState> Pets => pets;
    public IReadOnlyList<QuestDeployedPetState> LivingPets => pets.Where(x => !x.IsDead).ToArray();
    public bool IsActive => RemainingTurns > 0 && pets.Any(x => !x.IsDead);

    public static QuestPetDeployment Create(
        QuestParticipantId ownerParticipantId,
        IReadOnlyList<QuestPetSnapshot> petSnapshots,
        Func<Guid> actorIdFactory)
    {
        ArgumentNullException.ThrowIfNull(petSnapshots);
        ArgumentNullException.ThrowIfNull(actorIdFactory);
        if (petSnapshots.Count == 0)
        {
            throw new InvalidOperationException("ペットがいないため呼出できません。");
        }

        var deployedPets = petSnapshots
            .Take(Math.Min(PetConstants.MaxPetCount, FormationOrder.Length))
            .Select((pet, index) => new QuestDeployedPetState(
                actorIdFactory(),
                ownerParticipantId,
                pet.EnemyDefinitionId,
                pet.Status,
                FormationOrder[index],
                pet.Status.MaxHp,
                isDead: false))
            .ToArray();

        return new QuestPetDeployment(PetConstants.DeploymentTurns, deployedPets);
    }

    public QuestDeployedPetState? FindByActorId(Guid actorId)
    {
        return pets.FirstOrDefault(x => x.ActorId == actorId);
    }

    public void ApplyResolution(BattleTurnResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        foreach (var state in resolution.UpdatedStates)
        {
            FindByActorId(state.Id.Value)?.ApplyBattleState(state);
        }
    }

    public void ConsumeTurn()
    {
        if (RemainingTurns > 0)
        {
            RemainingTurns--;
        }
    }
}
