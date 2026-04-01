using System.Globalization;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;

namespace server.infrastructure.treasuremap;

public sealed class CsvTreasureMapRepository : ITreasureMapRepository
{
    private readonly IReadOnlyDictionary<TreasureMapId, TreasureMap> mapsById;
    private readonly IReadOnlyDictionary<string, TreasureMap> mapsByCode;
    private readonly IReadOnlyList<TreasureMap> maps;

    public CsvTreasureMapRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "treasuremap", "treasure_maps.csv");
        maps = Load(csvPath);
        mapsById = maps.ToDictionary(x => x.Id);
        mapsByCode = maps.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
    }

    public Task<TreasureMap?> GetAsync(TreasureMapId id)
    {
        mapsById.TryGetValue(id, out var map);
        return Task.FromResult(map);
    }

    public Task<TreasureMap?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult<TreasureMap?>(null);
        }

        mapsByCode.TryGetValue(code.Trim(), out var map);
        return Task.FromResult(map);
    }

    public Task<IReadOnlyList<TreasureMap>> GetAllAsync()
    {
        return Task.FromResult(maps);
    }

    private static IReadOnlyList<TreasureMap> Load(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSV file not found: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            return Array.Empty<TreasureMap>();
        }

        var results = new List<TreasureMap>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 9)
            {
                throw new InvalidOperationException($"treasure_maps.csv has invalid column count at line {i + 1}.");
            }

            var id = ParseInt(columns[0], "id", i + 1);
            var code = columns[1];
            var name = columns[2];
            var description = columns[3];
            var grade = ParseEnum<TreasureMapGrade>(columns[4], "grade", i + 1);
            var durationSeconds = ParseInt(columns[5], "duration_seconds", i + 1);
            var rewardPoolId = ParseInt(columns[6], "reward_pool_id", i + 1);
            var isMarketable = ParseBool(columns[7], "is_marketable", i + 1);
            var isHiddenFromInventory = ParseBool(columns[8], "is_hidden_from_inventory", i + 1);

            results.Add(new TreasureMap(
                new TreasureMapId(id),
                code,
                name,
                description,
                grade,
                durationSeconds,
                new TreasureMapRewardPoolId(rewardPoolId),
                isMarketable,
                isHiddenFromInventory));
        }

        return results;
    }

    private static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"Failed to parse integer column '{columnName}' at line {lineNumber}.");
        }

        return parsed;
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
