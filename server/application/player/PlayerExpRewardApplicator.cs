using server.domain.player;
using server.domain.move;

namespace server.application.player;

public static class PlayerExpRewardApplicator
{
    public static LevelUpResult Apply(Player player, int exp, IJobProfileRepository jobProfileRepository, IJobMoveLearningRuleRepository jobMoveLearningRuleRepository)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(jobProfileRepository);
        ArgumentNullException.ThrowIfNull(jobMoveLearningRuleRepository);

        player.GainExp(exp);
        var jobProfile = jobProfileRepository.GetByJob(player.Job);
        var learningRule = jobMoveLearningRuleRepository.GetByJob(player.Job);
        return player.LevelUp(jobProfile, learningRule);
    }
}
