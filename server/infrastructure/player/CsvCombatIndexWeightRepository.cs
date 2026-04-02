using System.Globalization;
using server.domain.player;

namespace server.infrastructure.player;

public sealed class CsvCombatIndexWeightRepository : ICombatIndexWeightRepository
{
    private readonly IReadOnlyList<CombatIndexWeight> weights;

    public CsvCombatIndexWeightRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "player", "combat_index_weights.csv");
        weights = Load(csvPath);
    }

    public IReadOnlyList<CombatIndexWeight> GetAll() => weights;

    private static IReadOnlyList<CombatIndexWeight> Load(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルに戦闘指数重みデータがありません: {csvPath}");
        }

        var result = new List<CombatIndexWeight>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 2)
            {
                throw new InvalidOperationException($"combat_index_weights.csv の形式が不正です。行: {i + 1}");
            }

            if (!double.TryParse(columns[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
            {
                throw new InvalidOperationException($"weight の数値変換に失敗しました。行: {i + 1}");
            }

            result.Add(new CombatIndexWeight(columns[0], weight));
        }

        return result;
    }
}
