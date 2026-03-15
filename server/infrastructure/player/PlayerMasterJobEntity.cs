using server.domain.player;

namespace server.infrastructure.player;

public class PlayerMasterJobEntity
{
    public Guid PlayerId { get; set; }
    public Job Job { get; set; }
    public DateTimeOffset MasteredAt { get; set; }
}
