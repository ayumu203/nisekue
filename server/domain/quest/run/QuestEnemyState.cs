using server.domain.battle;

namespace server.domain.quest;

public class QuestEnemyState(
    QuestEnemyInstanceId id,
    QuestEnemyDefinitionId enemyDefinitionId,
    BattlePosition position,
    int currentHp,
    int currentMp,
    bool isDead,
    IEnumerable<BattleAilmentState>? ailments = null,
    IEnumerable<BattleBuffState>? buffs = null,
    bool isCaptured = false,
    int reviveUsedCount = 0)
{
    private BattleAilmentState[] ailments = ailments?.ToArray() ?? [];
    private BattleBuffState[] buffs = buffs?.ToArray() ?? [];

    public QuestEnemyInstanceId Id { get; } = id;
    public QuestEnemyDefinitionId EnemyDefinitionId { get; } = enemyDefinitionId;
    public BattlePosition Position { get; private set; } = position;
    public int CurrentHp { get; private set; } = ValidateNonNegative(currentHp, nameof(currentHp));
    public int CurrentMp { get; private set; } = ValidateNonNegative(currentMp, nameof(currentMp));
    public bool IsDead { get; private set; } = isDead;
    public bool IsCaptured { get; private set; } = isCaptured;

    // このフロアで蘇生技を使った回数（名鑑の祠の上限判定用）。敵はフロアごとに作り直すので次のフロアには残らない。
    public int ReviveUsedCount { get; private set; } = ValidateNonNegative(reviveUsedCount, nameof(reviveUsedCount));
    public IReadOnlyList<BattleAilmentState> Ailments => ailments;
    public IReadOnlyList<BattleBuffState> Buffs => buffs;

    public bool IsAlive => !IsDead;

    public void MarkDead()
    {
        IsDead = true;
        CurrentHp = 0;
    }

    public void MarkCaptured()
    {
        if (IsDead)
        {
            throw new InvalidOperationException("倒れた敵は捕獲できません。");
        }

        IsCaptured = true;
        MarkDead();
    }

    public void RecordReviveUsed()
    {
        ReviveUsedCount++;
    }

    public void ApplyBattleState(BattleActorState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        CurrentHp = state.CurrentHp;
        CurrentMp = state.CurrentMp;
        IsDead = state.IsDead;

        ailments = state.Ailments.ToArray();
        buffs = state.Buffs.ToArray();
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
    }
}
