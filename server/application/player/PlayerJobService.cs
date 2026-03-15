using server.domain.move;
using server.domain.player;

namespace server.application.player;

public class PlayerJobService(
    IPlayerRepository playerRepository,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository,
    IMoveRepository moveRepository)
{
    public async Task<PlayerJobChangeResult> ChangeJobAsync(PlayerId playerId, Job nextJob)
    {
        var player = await playerRepository.GetPlayerAsync(playerId)
            ?? throw new KeyNotFoundException("プレイヤーが見つかりません。");

        var nextProfile = jobProfileRepository.GetByJob(nextJob);
        var learningRule = jobMoveLearningRuleRepository.GetByJob(nextJob);
        var learnedMoveIds = player.ChangeJob(nextJob, nextProfile, learningRule);

        await playerRepository.SaveAsync(player);

        return new PlayerJobChangeResult(
            player,
            await BuildLearnedMoveViewsAsync(learnedMoveIds));
    }

    public async Task<IReadOnlyList<LearnedMoveView>> BuildLearnedMoveViewsAsync(IReadOnlyList<MoveId> moveIds)
    {
        ArgumentNullException.ThrowIfNull(moveIds);

        if (moveIds.Count == 0)
        {
            return [];
        }

        var views = new List<LearnedMoveView>(moveIds.Count);
        foreach (var moveId in moveIds)
        {
            var move = await moveRepository.GetMoveAsync(moveId)
                ?? throw new InvalidOperationException($"技が見つかりません。 moveId={moveId.Id}");
            views.Add(new LearnedMoveView(move.Id.Id, move.Name));
        }

        return views;
    }
}
