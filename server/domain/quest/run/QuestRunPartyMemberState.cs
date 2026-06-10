using server.domain.battle;
using server.domain.pet;
using server.domain.quest.enums;

namespace server.domain.quest;

public class QuestRunPartyMemberState(
    QuestParticipantId participantId,
    int currentHp,
    int currentMp,
    bool isDead,
    int canActFromTurn,
    ActionMode actionMode,
    bool hasLeftQuest = false,
    bool isManualControlRequested = false,
    IEnumerable<BattleAilmentState>? ailments = null,
    IEnumerable<BattleBuffState>? buffs = null,
    int petSummonsUsed = 0)
{
    private BattleAilmentState[] ailments = ailments?.ToArray() ?? [];
    private BattleBuffState[] buffs = buffs?.ToArray() ?? [];

    public QuestParticipantId ParticipantId { get; } = participantId;
    public int CurrentHp { get; private set; } = ValidateNonNegative(currentHp, nameof(currentHp));
    public int CurrentMp { get; private set; } = ValidateNonNegative(currentMp, nameof(currentMp));
    public bool IsDead { get; private set; } = isDead;
    public int CanActFromTurn { get; private set; } = ValidatePositive(canActFromTurn, nameof(canActFromTurn));
    public ActionMode ActionMode { get; private set; } = actionMode;
    public bool HasLeftQuest { get; private set; } = hasLeftQuest;
    public bool IsManualControlRequested { get; private set; } = isManualControlRequested;
    public int PetSummonsUsed { get; private set; } = ValidateNonNegative(petSummonsUsed, nameof(petSummonsUsed));
    public IReadOnlyList<BattleAilmentState> Ailments => ailments;
    public IReadOnlyList<BattleBuffState> Buffs => buffs;

    public bool HasRemainingPetSummons => PetSummonsUsed < PetConstants.MaxSummonsPerQuestRun;

    public void ConsumePetSummon()
    {
        if (!HasRemainingPetSummons)
        {
            throw new InvalidOperationException("このクエストでの呼出回数の上限に達しています。");
        }

        PetSummonsUsed++;
    }

    public void SwitchToAutoAttackOnly()
    {
        ActionMode = ActionMode.AutoAttackOnly;
        IsManualControlRequested = false;
    }

    public void SwitchToManual()
    {
        ActionMode = ActionMode.Manual;
        IsManualControlRequested = false;
    }

    public void RequestManualControl()
    {
        if (ActionMode != ActionMode.AutoAttackOnly)
        {
            throw new InvalidOperationException("自動操作状態の参加者のみ手動復帰を申請できます。");
        }

        IsManualControlRequested = true;
    }

    public void LeaveQuest()
    {
        HasLeftQuest = true;
        IsManualControlRequested = false;
    }

    public void ApplyBattleState(server.domain.battle.BattleActorState state, int currentTurnNo)
    {
        ArgumentNullException.ThrowIfNull(state);

        var wasDead = IsDead;
        CurrentHp = state.CurrentHp;
        CurrentMp = state.CurrentMp;
        IsDead = state.IsDead;

        Array.Resize(ref ailments, state.Ailments.Count);
        for (var i = 0; i < state.Ailments.Count; i++)
        {
            ailments[i] = state.Ailments[i];
        }

        Array.Resize(ref buffs, state.Buffs.Count);
        for (var i = 0; i < state.Buffs.Count; i++)
        {
            buffs[i] = state.Buffs[i];
        }

        if (wasDead && !IsDead)
        {
            CanActFromTurn = currentTurnNo + 1;
        }
    }

    public bool IsContinuable()
    {
        return !IsDead && !HasLeftQuest && ActionMode != ActionMode.AutoAttackOnly;
    }

    public bool CanAcceptManualCommand(int currentTurnNo)
    {
        return !IsDead &&
               !HasLeftQuest &&
               ActionMode == ActionMode.Manual &&
               CanActFromTurn <= currentTurnNo;
    }

    private static int ValidateNonNegative(int value, string paramName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "0以上である必要があります。");
        }

        return value;
    }

    private static int ValidatePositive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "1以上である必要があります。");
        }

        return value;
    }
}
