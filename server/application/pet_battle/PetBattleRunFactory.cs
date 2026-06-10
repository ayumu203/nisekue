using server.domain.battle.enums;
using server.domain.pet_battle;
using server.domain.player;
using server.domain.quest.enums;

namespace server.application.pet_battle;

public class PetBattleRunFactory
{
    private static readonly TimeSpan TurnDeadline = TimeSpan.FromSeconds(PetBattleConstants.TurnDeadlineSeconds);

    public PetBattleRun Create(
        PetBattleRoom room,
        PetBattlePartyMemberSnapshot[] ownerSnapshots,
        PetBattlePartyMemberSnapshot[] opponentSnapshots,
        DateTimeOffset now)
    {
        var turnState = new PetBattleTurnState(1, now.Add(TurnDeadline));
        var ownerMembers = PetBattleSnapshotFactory.CreateMemberStates(ownerSnapshots, ActionMode.Manual);
        var opponentMembers = PetBattleSnapshotFactory.CreateMemberStates(opponentSnapshots, ActionMode.AutoAttackOnly);

        return new PetBattleRun(
            PetBattleRunId.NewId(),
            room.Id,
            room.OwnerPlayerId,
            room.OpponentPlayerId,
            ownerSnapshots,
            opponentSnapshots,
            turnState,
            ownerMembers,
            opponentMembers,
            lastTurnResults: null,
            startedAt: now);
    }
}
