namespace server.domain.player;

public interface IJobRoadmapRankRepository
{
    int? GetRank(Job job);
    IReadOnlyList<Job> GetJobs();
}
