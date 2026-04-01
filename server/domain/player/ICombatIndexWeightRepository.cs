namespace server.domain.player;

public interface ICombatIndexWeightRepository
{
    IReadOnlyList<CombatIndexWeight> GetAll();
}
