namespace server.domain.player;

public static class MapUnlockFlag
{
    public const int Map1 = 0x1; // 天界の楽団
    public const int Map2 = 0x2; // 黄金の里
    public const int Map3 = 0x4; // エンドレス（はてなき旅路）

    private static readonly IReadOnlySet<int> ValidFlags = new HashSet<int> { Map1, Map2, Map3 };

    public static bool IsValidFlag(int flag) => ValidFlags.Contains(flag);
}
