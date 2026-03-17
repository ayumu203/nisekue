using server.domain.player;

namespace server.domain.move;

public interface IJobMoveLearningRuleRepository
{
    JobMoveLearningRule GetByJob(Job job);
}
