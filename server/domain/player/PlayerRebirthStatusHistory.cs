namespace server.domain.player;

public class PlayerRebirthStatusHistory(
    PlayerId playerId,
    int rebirthCount,
    Status status,
    DateTimeOffset rebirthedAt)
{
    public PlayerId PlayerId { get; } = playerId;
    public int RebirthCount { get; } = rebirthCount;
    public Status Status { get; } = status ?? throw new ArgumentNullException(nameof(status));
    public DateTimeOffset RebirthedAt { get; } = rebirthedAt;
}
