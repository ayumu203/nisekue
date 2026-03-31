using System.Globalization;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;

namespace server.infrastructure.treasuremap;

public sealed class CsvTreasureMapRewardPoolRepository : ITreasureMapRewardPoolRepository
{
    private readonly IReadOnlyDictionary<TreasureMapRewardPoolId, TreasureMapRewardPool> poolsById;
    private readonly IReadOnlyList<TreasureMapRewardPool> pools;

    public CsvTreasureMapRewardPoolRepository()
    {
        var poolPath = Path.Combine(AppContext.BaseDirectory, "resources", "treasuremap", "reward_pools.csv");
        var entryPath = Path.Combine(AppContext.BaseDirectory, "resources", "treasuremap", "reward_entries.csv");
        pools = Load(poolPath, entryPath);
        poolsById = pools.ToDictionary(x => x.Id);
    }

    public Task<TreasureMapRewardPool?> GetAsync(TreasureMapRewardPoolId id)
    {
        poolsById.TryGetValue(id, out var pool);
        return Task.FromResult(pool);
    }

    public Task<IReadOnlyList<TreasureMapRewardPool>> GetAllAsync()
    {
        return Task.FromResult(pools);
    }

    private static IReadOnlyList<TreasureMapRewardPool> Load(string poolsPath, string entriesPath)
    {
        if (!File.Exists(poolsPath) || !File.Exists(entriesPath))
        {
            throw new InvalidOperationException("Treasure map reward CSV files were not found.");
        }

        var entriesByPoolId = LoadEntries(entriesPath)
            .GroupBy(x => x.poolId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TreasureMapRewardEntry>)g.Select(x => x.entry).ToArray());

        var poolLines = File.ReadAllLines(poolsPath);
        if (poolLines.Length <= 1)
        {
            return Array.Empty<TreasureMapRewardPool>();
        }

        var result = new List<TreasureMapRewardPool>(poolLines.Length - 1);
        for (var i = 1; i < poolLines.Length; i++)
        {
            var line = poolLines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 3)
            {
                throw new InvalidOperationException($"reward_pools.csv has invalid column count at line {i + 1}.");
            }

            var id = new TreasureMapRewardPoolId(ParseInt(columns[0], "id", i + 1));
            var name = columns[1];
            var isEnabled = ParseBool(columns[2], "is_enabled", i + 1);
            if (!isEnabled)
            {
                continue;
            }

            if (!entriesByPoolId.TryGetValue(id, out var entries) || entries.Count == 0)
            {
                throw new InvalidOperationException($"No reward entries found for pool {id.Value}.");
            }

            result.Add(new TreasureMapRewardPool(id, name, entries));
        }

        return result;
    }

    private static IReadOnlyList<(TreasureMapRewardPoolId poolId, TreasureMapRewardEntry entry)> LoadEntries(string entriesPath)
    {
        var lines = File.ReadAllLines(entriesPath);
        if (lines.Length <= 1)
        {
            return Array.Empty<(TreasureMapRewardPoolId, TreasureMapRewardEntry)>();
        }

        var result = new List<(TreasureMapRewardPoolId, TreasureMapRewardEntry)>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 11)
            {
                throw new InvalidOperationException($"reward_entries.csv has invalid column count at line {i + 1}.");
            }

            var poolId = new TreasureMapRewardPoolId(ParseInt(columns[1], "pool_id", i + 1));
            var entry = new TreasureMapRewardEntry(
                ParseEnum<TreasureMapRewardType>(columns[2], "reward_type", i + 1),
                ParseInt(columns[5], "weight", i + 1),
                itemId: ParseNullableInt(columns[3], "item_id", i + 1),
                equipmentId: ParseNullableInt(columns[4], "equipment_id", i + 1),
                quantityMin: ParseNullableInt(columns[6], "quantity_min", i + 1),
                quantityMax: ParseNullableInt(columns[7], "quantity_max", i + 1),
                goldAmount: ParseNullableInt(columns[8], "gold_amount", i + 1),
                experienceAmount: ParseNullableInt(columns[9], "experience_amount", i + 1),
                isFallback: ParseBool(columns[10], "is_fallback", i + 1));

            result.Add((poolId, entry));
        }

        return result;
    }

    private static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"Failed to parse integer column '{columnName}' at line {lineNumber}.");
        }

        return parsed;
    }

    private static int? ParseNullableInt(string value, string columnName, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return ParseInt(value, columnName, lineNumber);
    }

    private static bool ParseBool(string value, string columnName, int lineNumber)
    {
        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidOperationException($"Failed to parse bool column '{columnName}' at line {lineNumber}.");
        }

        return parsed;
    }

    private static TEnum ParseEnum<TEnum>(string value, string columnName, int lineNumber)
        where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, false, out var parsed))
        {
            throw new InvalidOperationException($"Failed to parse enum column '{columnName}' with value '{value}' at line {lineNumber}.");
        }

        return parsed;
    }
}
