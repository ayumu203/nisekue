using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest;

public class CsvQuestEndlessEnemyTemplateRepository : IQuestEndlessEnemyTemplateRepository
{
    private readonly IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEndlessEnemyTemplate> templatesById;
    private readonly IReadOnlyList<QuestEndlessEnemyTemplate> templates;
    private readonly IReadOnlyDictionary<int, List<QuestEndlessEnemyTemplate>> templatesByTheme;

    public CsvQuestEndlessEnemyTemplateRepository()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources", "quest");
        templatesById = LoadTemplates(Path.Combine(resourceDir, "endless_enemy_templates.csv"));
        templates = templatesById.Values.OrderBy(x => x.Id.Value).ToArray();
        templatesByTheme = templates
            .GroupBy(x => x.ThemeNo)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public Task<QuestEndlessEnemyTemplate?> GetAsync(QuestEnemyDefinitionId id)
    {
        templatesById.TryGetValue(id, out var template);
        return Task.FromResult(template);
    }

    public Task<IReadOnlyList<QuestEndlessEnemyTemplate>> GetAllAsync()
    {
        return Task.FromResult(templates);
    }

    public Task<IReadOnlyList<QuestEndlessEnemyTemplate>> GetByThemeAsync(int themeNo)
    {
        templatesByTheme.TryGetValue(themeNo, out var themeTemplates);
        return Task.FromResult<IReadOnlyList<QuestEndlessEnemyTemplate>>(themeTemplates ?? []);
    }

    private static IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEndlessEnemyTemplate> LoadTemplates(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestEnemyDefinitionId, QuestEndlessEnemyTemplate>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 14)
            {
                throw new InvalidOperationException($"endless_enemy_templates.csv の形式が不正です。行: {i + 1}");
            }

            var id = new QuestEnemyDefinitionId(CsvQuestParser.ParseInt(columns[0], "template_id", i + 1));
            if (map.ContainsKey(id))
            {
                throw new InvalidOperationException($"endless_enemy_templates.csv で template_id が重複しています。id={id}, 行: {i + 1}");
            }

            var moveIds = CsvQuestParser.ParseIntList(columns[13], "move_ids", i + 1);

            map.Add(id, new QuestEndlessEnemyTemplate(
                id,
                CsvQuestParser.ParseInt(columns[1], "theme_no", i + 1),
                CsvQuestParser.ParseEnum<EnemyAiType>(columns[2], "archetype", i + 1),
                CsvQuestParser.ParseBool(columns[3], "is_boss", i + 1),
                columns[4],
                new Status(
                    CsvQuestParser.ParseInt(columns[5], "base_max_hp", i + 1),
                    CsvQuestParser.ParseInt(columns[6], "base_max_mp", i + 1),
                    CsvQuestParser.ParseInt(columns[7], "base_strength", i + 1),
                    CsvQuestParser.ParseInt(columns[8], "base_defense", i + 1),
                    CsvQuestParser.ParseInt(columns[9], "base_intelligence", i + 1),
                    CsvQuestParser.ParseInt(columns[10], "base_luck", i + 1),
                    CsvQuestParser.ParseInt(columns[11], "base_speed", i + 1)),
                columns[12],
                moveIds.Select(x => new MoveId(x))));
        }

        return map;
    }
}
