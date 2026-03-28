using System.Globalization;
using server.domain.player;

namespace server.infrastructure.player;

public class CsvItemRepository : IItemRepository
{
    private readonly IReadOnlyDictionary<ItemId, Item> itemsById;
    private readonly IReadOnlyList<Item> items;

    public CsvItemRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "item_master.csv");
        itemsById = LoadItems(csvPath);
        items = itemsById.Values.OrderBy(x => x.Id.Value).ToArray();
    }

    public Task<Item?> GetAsync(ItemId id)
    {
        itemsById.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<Item>> GetAllAsync()
    {
        return Task.FromResult(items);
    }

    private static IReadOnlyDictionary<ItemId, Item> LoadItems(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            return new Dictionary<ItemId, Item>();
        }

        var map = new Dictionary<ItemId, Item>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 15)
            {
                throw new InvalidOperationException($"item_master.csv の形式が不正です。行: {i + 1}");
            }

            var itemId = new ItemId(ParseInt(columns[0], "item_id", i + 1));
            if (map.ContainsKey(itemId))
            {
                throw new InvalidOperationException($"item_id が重複しています。item_id={itemId.Value}, 行: {i + 1}");
            }

            var effectType = ParseEnum<ItemEffectType>(columns[3], "effect_type", i + 1);
            map[itemId] = new Item(
                itemId,
                columns[1],
                columns[2],
                ParseInt(columns[4], "max_stack", i + 1),
                effectType,
                statusBonus: effectType == ItemEffectType.StatBoost
                    ? new StatusBonus(
                        ParseInt(columns[7], "bonus_max_hp", i + 1),
                        ParseInt(columns[8], "bonus_max_mp", i + 1),
                        ParseInt(columns[9], "bonus_strength", i + 1),
                        ParseInt(columns[10], "bonus_defense", i + 1),
                        ParseInt(columns[11], "bonus_intelligence", i + 1),
                        ParseInt(columns[12], "bonus_luck", i + 1),
                        ParseInt(columns[13], "bonus_speed", i + 1))
                    : null,
                changeJobTo: string.IsNullOrWhiteSpace(columns[5]) ? null : ParseEnum<Job>(columns[5], "change_job_to", i + 1),
                requiredLevel: ParseNullableInt(columns[6], "required_level", i + 1),
                requiredMasterJobs: ParseJobs(columns[14], i + 1));
        }

        return map;
    }

    private static IReadOnlySet<Job> ParseJobs(string value, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new HashSet<Job>();
        }

        return value
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(jobValue =>
            {
                if (!Enum.TryParse<Job>(jobValue, false, out var job))
                {
                    throw new InvalidOperationException($"required_master_jobs の job_code が不正です。value: {jobValue}, 行: {lineNumber}");
                }

                return job;
            })
            .ToHashSet();
    }

    private static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの数値変換に失敗しました。column: {columnName}, 行: {lineNumber}");
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

    private static TEnum ParseEnum<TEnum>(string value, string columnName, int lineNumber)
        where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, false, out var parsed))
        {
            throw new InvalidOperationException($"CSVの列値が不正です。column: {columnName}, value: {value}, 行: {lineNumber}");
        }

        return parsed;
    }
}
