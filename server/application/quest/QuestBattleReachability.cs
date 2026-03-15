using server.domain.battle.enums;

namespace server.application.quest;

internal static class QuestBattleReachability
{
    public static IReadOnlySet<BattleRow> GetReachableRows(BattleRow actorRow)
    {
        return actorRow switch
        {
            BattleRow.Front => new HashSet<BattleRow> { BattleRow.Front },
            BattleRow.Middle => new HashSet<BattleRow> { BattleRow.Front, BattleRow.Middle },
            BattleRow.Back => new HashSet<BattleRow> { BattleRow.Front, BattleRow.Middle, BattleRow.Back },
            _ => throw new ArgumentOutOfRangeException(nameof(actorRow), $"未対応の BattleRow: {actorRow}")
        };
    }
}
