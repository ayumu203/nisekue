namespace server.domain.player;

public interface IGrowthValueRepository
{
    GrowthValue GetByJob(Job job);
}
