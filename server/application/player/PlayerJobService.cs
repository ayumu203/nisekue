using System.Collections.Generic;
using server.domain.move;
using server.domain.player;

namespace server.application.player;

public class PlayerJobService(
    IPlayerRepository playerRepository,
    IJobProfileRepository jobProfileRepository,
    IJobMoveLearningRuleRepository jobMoveLearningRuleRepository,
    IMoveRepository moveRepository)
{
    private static readonly IReadOnlySet<Job> BaseJobs = new HashSet<Job>
    {
        Job.Apprentice,
        Job.Warrior,
        Job.Guardian,
        Job.Mage,
        Job.Priest,
        Job.Ranger
    };

    public async Task<PlayerJobChangeResult> ChangeJobAsync(PlayerId playerId, Job nextJob)
    {
        if (!BaseJobs.Contains(nextJob))
        {
            throw new InvalidOperationException("そのジョブへの転職は転職アイテム使用時にのみ可能です。");
        }

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
                ?? throw new InvalidOperationException($"スキルが見つかりません。 moveId={moveId.Id}");
            views.Add(new LearnedMoveView(move.Id.Id, move.Name));
        }

        return views;
    }
}
