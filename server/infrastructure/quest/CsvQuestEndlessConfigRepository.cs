using server.domain.quest;

namespace server.infrastructure.quest;

public class CsvQuestEndlessConfigRepository : IQuestEndlessConfigRepository
{
    private readonly IReadOnlyDictionary<QuestStageId, QuestEndlessConfig> configsByStage;

    public CsvQuestEndlessConfigRepository()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources", "quest");
        configsByStage = LoadConfigs(Path.Combine(resourceDir, "endless_configs.csv"));
    }

    public Task<QuestEndlessConfig?> GetByStageIdAsync(QuestStageId stageId)
    {
        configsByStage.TryGetValue(stageId, out var config);
        return Task.FromResult(config);
    }

    private static IReadOnlyDictionary<QuestStageId, QuestEndlessConfig> LoadConfigs(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestStageId, QuestEndlessConfig>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 13)
            {
                throw new InvalidOperationException($"endless_configs.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "stage_id", i + 1));
            if (map.ContainsKey(stageId))
            {
                throw new InvalidOperationException($"endless_configs.csv で stage_id が重複しています。id={stageId}, 行: {i + 1}");
            }

            map.Add(stageId, new QuestEndlessConfig(
                (double)CsvQuestParser.ParseDecimal(columns[1], "growth_coefficient_a", i + 1),
                (double)CsvQuestParser.ParseDecimal(columns[2], "growth_exponent_p", i + 1),
                CsvQuestParser.ParseInt(columns[3], "safety_cap_floor", i + 1),
                CsvQuestParser.ParseInt(columns[4], "boss_interval", i + 1),
                CsvQuestParser.ParseDecimal(columns[5], "boss_extra_multiplier", i + 1),
                CsvQuestParser.ParseDecimal(columns[6], "boss_reward_multiplier", i + 1),
                CsvQuestParser.ParseInt(columns[7], "theme_count", i + 1),
                CsvQuestParser.ParseInt(columns[8], "floors_per_theme", i + 1),
                CsvQuestParser.ParseInt(columns[9], "min_enemies_per_floor", i + 1),
                CsvQuestParser.ParseInt(columns[10], "max_enemies_per_floor", i + 1),
                CsvQuestParser.ParseDecimal(columns[11], "reward_exp_rate", i + 1),
                CsvQuestParser.ParseDecimal(columns[12], "reward_gold_rate", i + 1)));
        }

        return map;
    }
}
