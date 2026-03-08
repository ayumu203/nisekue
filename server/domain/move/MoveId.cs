using server.shared.constants.move;

namespace server.domain.move;

public class MoveId(int id)
{
    public int Id { get; } = Validate(id);

    private static int Validate(int id)
    {
        if (id < MoveConstants.Constraints.MinId)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "moveId は1以上である必要があります。");
        }

        return id;
    }
}
