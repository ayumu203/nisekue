namespace server.domain.player;

public static class ExpMultiplierFlags
{
    private static readonly IReadOnlyDictionary<int, decimal> FlagToMultiplier = new Dictionary<int, decimal>
    {
        [0x8] = 1.1m,
        [0x4] = 1.5m,
        [0x2] = 2.0m,
        [0x1] = 3.0m
    };

    public static int ToFlag(decimal multiplier)
    {
        foreach (var (flag, m) in FlagToMultiplier)
        {
            if (m == multiplier)
            {
                return flag;
            }
        }

        throw new ArgumentException($"未対応の経験値倍率です: {multiplier}", nameof(multiplier));
    }

    public static decimal ToMultiplier(int flags)
    {
        if (flags == 0)
        {
            return 1.0m;
        }

        foreach (var (flag, multiplier) in FlagToMultiplier)
        {
            if ((flags & flag) != 0)
            {
                return multiplier;
            }
        }

        return 1.0m;
    }
}
