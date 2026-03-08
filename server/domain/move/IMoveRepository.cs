namespace server.domain.move;

public interface IMoveRepository
{
    Task<Move?> GetMoveAsync(MoveId moveId);
    Task<IReadOnlyList<Move>> GetAllMovesAsync();
}
