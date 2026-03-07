using System.Globalization;
using server.domain.player;
using server.domain.training;

namespace server.infrastructure.training;

public class CsvTrainingEnemyRepository : ITrainingEnemyRepository
{
    private readonly IReadOnlyDictionary<TrainingEnemyId, TrainingEnemy> enemies;

    public CsvTrainingEnemyRepository()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "resources", "training_enemies.csv");
        enemies = LoadEnemies(csvPath);
    }

    public Task<TrainingEnemy?> GetTrainingEnemyAsync(TrainingEnemyId id)
    {
        enemies.TryGetValue(id, out var enemy);
        return Task.FromResult(enemy);
    }

    private static IReadOnlyDictionary<TrainingEnemyId, TrainingEnemy> LoadEnemies(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException($"CSVファイルが見つかりません: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException($"CSVファイルに敵データがありません: {csvPath}");
        }

        var map = new Dictionary<TrainingEnemyId, TrainingEnemy>();
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = line.Split(',', StringSplitOptions.TrimEntries);
            if (columns.Length != 11)
            {
                throw new InvalidOperationException($"CSV形式が不正です。行: {i + 1}");
            }

            var id = ParseInt(columns[0], "id", i + 1);
            var enemyId = new TrainingEnemyId(id);
            if (map.ContainsKey(enemyId))
            {
                throw new InvalidOperationException($"CSV内で敵IDが重複しています。id: {id}, 行: {i + 1}");
            }

            var enemy = new TrainingEnemy(
                enemyId,
                columns[1],
                columns[2],
                ParseInt(columns[3], "level", i + 1),
                new BaseStatus(
                    ParseInt(columns[4], "max_hp", i + 1),
                    ParseInt(columns[5], "max_mp", i + 1),
                    ParseInt(columns[6], "strength", i + 1),
                    ParseInt(columns[7], "defense", i + 1),
                    ParseInt(columns[8], "intelligence", i + 1),
                    ParseInt(columns[9], "luck", i + 1),
                    ParseInt(columns[10], "speed", i + 1)));

            map.Add(enemyId, enemy);
        }

        if (map.Count == 0)
        {
            throw new InvalidOperationException($"CSVファイルに有効な敵データがありません: {csvPath}");
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
