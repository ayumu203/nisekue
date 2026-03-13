using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest;

public class CsvQuestEnemyDefinitionRepository : IQuestEnemyDefinitionRepository
{
    private readonly IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> enemiesById;
    private readonly IReadOnlyList<QuestEnemyDefinition> enemies;

    public CsvQuestEnemyDefinitionRepository()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources", "quest");
        enemiesById = LoadEnemies(
            Path.Combine(resourceDir, "enemies.csv"),
            Path.Combine(resourceDir, "enemy_moves.csv"));
        enemies = enemiesById.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public Task<QuestEnemyDefinition?> GetAsync(QuestEnemyDefinitionId id)
    {
        enemiesById.TryGetValue(id, out var enemy);
        return Task.FromResult(enemy);
    }

    public Task<IReadOnlyList<QuestEnemyDefinition>> GetAllAsync()
    {
        return Task.FromResult(enemies);
    }

    private static IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> LoadEnemies(string enemiesPath, string enemyMovesPath)
    {
        var moveIdsByEnemy = LoadEnemyMoves(enemyMovesPath);
        var lines = CsvQuestParser.ReadDataLines(enemiesPath);
        var map = new Dictionary<QuestEnemyDefinitionId, QuestEnemyDefinition>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 12)
            {
                throw new InvalidOperationException($"enemies.csv の形式が不正です。行: {i + 1}");
            }

            var id = new QuestEnemyDefinitionId(CsvQuestParser.ParseInt(columns[0], "id", i + 1));
            moveIdsByEnemy.TryGetValue(id, out var moveIds);
            moveIds ??= [];

            map.Add(id, new QuestEnemyDefinition(
                id,
                columns[1],
                CsvQuestParser.ParseInt(columns[2], "level", i + 1),
                new Status(
                    CsvQuestParser.ParseInt(columns[3], "max_hp", i + 1),
                    CsvQuestParser.ParseInt(columns[4], "max_mp", i + 1),
                    CsvQuestParser.ParseInt(columns[5], "strength", i + 1),
                    CsvQuestParser.ParseInt(columns[6], "defense", i + 1),
                    CsvQuestParser.ParseInt(columns[7], "intelligence", i + 1),
                    CsvQuestParser.ParseInt(columns[8], "luck", i + 1),
                    CsvQuestParser.ParseInt(columns[9], "speed", i + 1)),
                columns[10],
                CsvQuestParser.ParseEnum<EnemyAiType>(columns[11], "ai_type", i + 1),
                moveIds.Select(x => new MoveId(x))));
        }

        return map;
    }

    private static IReadOnlyDictionary<QuestEnemyDefinitionId, int[]> LoadEnemyMoves(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestEnemyDefinitionId, int[]>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 2)
            {
                throw new InvalidOperationException($"enemy_moves.csv の形式が不正です。行: {i + 1}");
            }

            var id = new QuestEnemyDefinitionId(CsvQuestParser.ParseInt(columns[0], "enemy_definition_id", i + 1));
            map[id] = CsvQuestParser.ParseIntList(columns[1], "move_ids", i + 1);
        }

        return map;
    }
}
