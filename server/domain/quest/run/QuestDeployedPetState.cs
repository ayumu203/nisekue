using server.domain.battle;
using server.domain.player;

namespace server.domain.quest;

// 呼出で出撃中のペット1体の状態。出撃中の 3 ターンの間だけ HP・状態異常を持ち越す。
public class QuestDeployedPetState(
    Guid actorId,
    QuestParticipantId ownerParticipantId,
    QuestEnemyDefinitionId enemyDefinitionId,
    Status status,
    BattlePosition position,
    int currentHp,
    bool isDead,
    IEnumerable<BattleAilmentState>? ailments = null,
    IEnumerable<BattleBuffState>? buffs = null)
{
    private BattleAilmentState[] ailments = ailments?.ToArray() ?? [];
    private BattleBuffState[] buffs = buffs?.ToArray() ?? [];

    public Guid ActorId { get; } = actorId == Guid.Empty
        ? throw new ArgumentException("ペットのアクター ID は必須です。", nameof(actorId))
        : actorId;
    public QuestParticipantId OwnerParticipantId { get; } = ownerParticipantId;
    public QuestEnemyDefinitionId EnemyDefinitionId { get; } = enemyDefinitionId;
    public Status Status { get; } = status ?? throw new ArgumentNullException(nameof(status));
    public BattlePosition Position { get; } = position;
    public int CurrentHp { get; private set; } = currentHp < 0
        ? throw new ArgumentOutOfRangeException(nameof(currentHp), "0以上である必要があります。")
        : currentHp;
    public bool IsDead { get; private set; } = isDead;
    public IReadOnlyList<BattleAilmentState> Ailments => ailments;
    public IReadOnlyList<BattleBuffState> Buffs => buffs;

    public void ApplyBattleState(BattleActorState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        CurrentHp = state.CurrentHp;
        IsDead = state.IsDead;
        ailments = state.Ailments.ToArray();
        buffs = state.Buffs.ToArray();
    }
}
