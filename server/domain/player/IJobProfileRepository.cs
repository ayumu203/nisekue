namespace server.domain.player;

public interface IJobProfileRepository
{
    JobProfile GetByJob(Job job);
    IReadOnlyList<JobProfile> GetAll();
}
