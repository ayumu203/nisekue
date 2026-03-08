using System.Globalization;
using server.domain.player;

namespace server.infrastructure.player;

public class CsvGrowthValueRepository : IGrowthValueRepository
{
    private readonly IReadOnlyDictionary<Job, GrowthValue> growthByJob;

    public CsvGrowthValueRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "job_levelup_growths.csv");
        growthByJob = LoadGrowthByJob(csvPath);
    }

    public GrowthValue GetByJob(Job job)
    {
        if (growthByJob.TryGetValue(job, out var growth))
        {
            return growth;
        }

        throw new InvalidOperationException($"ジョブ {job} のステータス上昇値が定義されていません。");
    }

    private static IReadOnlyDictionary<Job, GrowthValue> LoadGrowthByJob(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルにジョブデータがありません: {csvPath}");
        }

        var map = new Dictionary<Job, GrowthValue>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 10)
            {
                throw new InvalidOperationException($"CSV形式が不正です。行: {i + 1}");
            }

            var jobCode = columns[1];
            if (!Enum.TryParse<Job>(jobCode, ignoreCase: false, out var job))
            {
                throw new InvalidOperationException($"CSVのjob_codeが不正です。value: {jobCode}, 行: {i + 1}");
            }

            if (map.ContainsKey(job))
            {
                throw new InvalidOperationException($"CSV内でジョブコードが重複しています。job_code: {jobCode}, 行: {i + 1}");
            }

            map.Add(job, new GrowthValue(
                MaxHp: ParseInt(columns[3], "max_hp", i + 1),
                MaxMp: ParseInt(columns[4], "max_mp", i + 1),
                Strength: ParseInt(columns[5], "strength", i + 1),
                Defense: ParseInt(columns[6], "defense", i + 1),
                Intelligence: ParseInt(columns[7], "intelligence", i + 1),
                Luck: ParseInt(columns[8], "luck", i + 1),
                Speed: ParseInt(columns[9], "speed", i + 1)));
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException($"CSVファイルに有効なジョブデータがありません: {csvPath}");
        }

        return map;
    }

    private static int ParseInt(string value, string columnName, int lineNumber)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"CSVの数値変換に失敗しました。column: {columnName}, 行: {lineNumber}");
        }

        return parsed;
    }
}
