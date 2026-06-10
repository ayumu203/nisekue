using server.domain.battle;
using server.domain.quest.enums;

namespace server.domain.pet_battle;

public class PetBattlePartyMemberState(
    PetBattleParticipantId participantId,
    int currentHp,
    int currentMp,
    bool isDead,
    int canActFromTurn,
    ActionMode actionMode,
    IEnumerable<BattleAilmentState>? ailments = null,
    IEnumerable<BattleBuffState>? buffs = null)
{
    private BattleAilmentState[] ailments = ailments?.ToArray() ?? [];
    private BattleBuffState[] buffs = buffs?.ToArray() ?? [];

    public PetBattleParticipantId ParticipantId { get; } = participantId;
    public int CurrentHp { get; private set; } = ValidateNonNegative(currentHp, nameof(currentHp));
    public int CurrentMp { get; private set; } = ValidateNonNegative(currentMp, nameof(currentMp));
    public bool IsDead { get; private set; } = isDead;
    public int CanActFromTurn { get; private set; } = ValidatePositive(canActFromTurn, nameof(canActFromTurn));
    public ActionMode ActionMode { get; private set; } = actionMode;
    public IReadOnlyList<BattleAilmentState> Ailments => ailments;
    public IReadOnlyList<BattleBuffState> Buffs => buffs;

    public bool CanAcceptManualCommand(int currentTurnNo) =>
        !IsDead && ActionMode == ActionMode.Manual && CanActFromTurn <= currentTurnNo;

    public void SwitchToAutoAttackOnly()
    {
        ActionMode = ActionMode.AutoAttackOnly;
    }

    public void ApplyBattleState(BattleActorState state, int currentTurnNo)
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
