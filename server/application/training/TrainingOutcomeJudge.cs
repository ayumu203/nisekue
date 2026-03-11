using server.domain.battle;

namespace server.application.training;

public class TrainingOutcomeJudge
{
    public TrainingOutcome Judge(
        IEnumerable<BattleActorState> states,
        BattleActorId playerActorId,
        BattleActorId enemyActorId)
    {
        ArgumentNullException.ThrowIfNull(states);

        var stateMap = states.ToDictionary(x => x.Id);
        var playerState = stateMap[playerActorId];
        var enemyState = stateMap[enemyActorId];

        if (playerState.CurrentHp > 0 && enemyState.CurrentHp <= 0)
        {
            return TrainingOutcome.Win;
        }

        if (playerState.CurrentHp <= 0 && enemyState.CurrentHp > 0)
        {
            return TrainingOutcome.Lose;
        }

        return TrainingOutcome.Draw;
    }
}
