namespace server.shared.constants.player;

public static class PlayerCacheConstants
{
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public const string AllPlayersKey = "players:all";

    public static string PlayerKey(Guid id) => $"player:{id}";
}
