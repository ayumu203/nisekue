namespace server.domain.player;

public interface ICombatIndexRankThresholdRepository
{
    IReadOnlyList<StatusRankThreshold> GetAll();
}
