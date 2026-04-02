using System.Globalization;
using server.domain.player;

namespace server.infrastructure.player;

public sealed class CsvStatusRankThresholdRepository : IStatusRankThresholdRepository
{
    private readonly IReadOnlyList<StatusRankThreshold> thresholds;

    public CsvStatusRankThresholdRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "player", "status_rank_thresholds.csv");
        thresholds = Load(csvPath);
    }

    public IReadOnlyList<StatusRankThreshold> GetAll() => thresholds;

    private static IReadOnlyList<StatusRankThreshold> Load(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルにランクしきい値データがありません: {csvPath}");
        }

        var result = new List<StatusRankThreshold>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 3)
            {
                throw new InvalidOperationException($"status_rank_thresholds.csv の形式が不正です。行: {i + 1}");
            }

            if (!Enum.TryParse<StatusRank>(columns[1], ignoreCase: false, out var rank))
            {
                throw new InvalidOperationException($"rank が不正です。value={columns[1]}, 行: {i + 1}");
            }

            if (!int.TryParse(columns[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxValue))
            {
                throw new InvalidOperationException($"max_value の数値変換に失敗しました。行: {i + 1}");
            }

            result.Add(new StatusRankThreshold(columns[0], rank, maxValue));
        }

        return result;
    }
}
