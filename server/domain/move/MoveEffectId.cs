using server.shared.constants.move;

namespace server.domain.move;

public class MoveEffectId(int id)
{
    public int Id { get; } = Validate(id);

    private static int Validate(int id)
    {
        if (id < MoveConstants.Constraints.MinId)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "effectId は1以上である必要があります。");
        }
        return id;
    }
}
