namespace server.domain.player;

public interface IStatusRankThresholdRepository
{
    IReadOnlyList<StatusRankThreshold> GetAll();
}
