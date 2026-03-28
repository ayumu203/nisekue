using System.Globalization;
using server.domain.player;

namespace server.infrastructure.player;

public class CsvEquipmentRepository : IEquipmentRepository
{
    private readonly IReadOnlyDictionary<EquipmentId, Equipment> equipmentById;
    private readonly IReadOnlyList<Equipment> equipments;

    public CsvEquipmentRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "equipment_master.csv");
        equipmentById = LoadEquipments(csvPath);
        equipments = equipmentById.Values.OrderBy(x => x.Id.Value).ToArray();
    }

    public Task<Equipment?> GetAsync(EquipmentId id)
    {
        equipmentById.TryGetValue(id, out var equipment);
        return Task.FromResult(equipment);
    }

    public Task<IReadOnlyList<Equipment>> GetAllAsync()
    {
        return Task.FromResult(equipments);
    }

    private static IReadOnlyDictionary<EquipmentId, Equipment> LoadEquipments(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルに装備データがありません: {csvPath}");
        }

        var map = new Dictionary<EquipmentId, Equipment>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 12)
            {
                throw new InvalidOperationException($"equipment_master.csv の形式が不正です。行: {i + 1}");
            }

            var equipmentId = new EquipmentId(ParseInt(columns[0], "equipment_id", i + 1));
            if (map.ContainsKey(equipmentId))
            {
                throw new InvalidOperationException($"equipment_id が重複しています。equipment_id={equipmentId.Value}, 行: {i + 1}");
            }

            map[equipmentId] = new Equipment(
                equipmentId,
                columns[1],
                ParseEnum<EquipmentType>(columns[2], "equipment_type", i + 1),
                ParseInt(columns[3], "max_durability", i + 1),
                new EquipmentStatusBonus(
                    ParseInt(columns[4], "bonus_max_hp", i + 1),
                    ParseInt(columns[5], "bonus_max_mp", i + 1),
                    ParseInt(columns[6], "bonus_strength", i + 1),
                    ParseInt(columns[7], "bonus_defense", i + 1),
                    ParseInt(columns[8], "bonus_intelligence", i + 1),
                    ParseInt(columns[9], "bonus_luck", i + 1),
                    ParseInt(columns[10], "bonus_speed", i + 1)),
                ParseJobs(columns[11], i + 1));
        }

        return map;
    }

    private static IReadOnlySet<Job> ParseJobs(string value, int lineNumber)
    {
        var jobs = value
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(jobValue =>
            {
                if (!Enum.TryParse<Job>(jobValue, false, out var job))
                {
                    throw new InvalidOperationException($"equippable_jobs の job_code が不正です。value: {jobValue}, 行: {lineNumber}");
                }

                return job;
            })
            .ToHashSet();

        if (jobs.Count == 0)
        {
            throw new InvalidOperationException($"equippable_jobs が空です。行: {lineNumber}");
        }

        return jobs;
    }

    private static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの数値変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
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
