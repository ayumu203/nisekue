using server.domain.quest;

namespace server.infrastructure.quest;

/// <summary>名鑑の祠の調整値（mirror_configs.csv）を stage_id ごとに読む。</summary>
public class CsvQuestMirrorConfigRepository : IQuestMirrorConfigRepository
{
    private const int ColumnCount = 12;

    private readonly IReadOnlyDictionary<QuestStageId, QuestMirrorConfig> configsByStage;

    public CsvQuestMirrorConfigRepository()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources", "quest");
        configsByStage = LoadConfigs(Path.Combine(resourceDir, "mirror_configs.csv"));
    }

    public Task<QuestMirrorConfig?> GetByStageIdAsync(QuestStageId stageId)
    {
        configsByStage.TryGetValue(stageId, out var config);
        return Task.FromResult(config);
    }

    private static IReadOnlyDictionary<QuestStageId, QuestMirrorConfig> LoadConfigs(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestStageId, QuestMirrorConfig>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != ColumnCount)
            {
                throw new InvalidOperationException($"mirror_configs.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "stage_id", i + 1));
            if (map.ContainsKey(stageId))
            {
                throw new InvalidOperationException($"mirror_configs.csv で stage_id が重複しています。id={stageId}, 行: {i + 1}");
            }

            try
            {
                map.Add(stageId, new QuestMirrorConfig(
                    CsvQuestParser.ParseInt(columns[1], "top_player_count", i + 1),
                    CsvQuestParser.ParseInt(columns[2], "enemies_per_normal_floor", i + 1),
                    CsvQuestParser.ParseInt(columns[3], "boss_companion_count", i + 1),
                    CsvQuestParser.ParseInt(columns[4], "max_normal_appearances", i + 1),
                    CsvQuestParser.ParseInt(columns[5], "max_total_appearances", i + 1),
                    (double)CsvQuestParser.ParseDecimal(columns[6], "second_choice_rate", i + 1),
                    (double)CsvQuestParser.ParseDecimal(columns[7], "self_buff_rate", i + 1),
                    CsvQuestParser.ParseInt(columns[8], "self_harm_min_hp_percent", i + 1),
                    CsvQuestParser.ParseInt(columns[9], "heal_hp_percent", i + 1),
                    CsvQuestParser.ParseInt(columns[10], "restore_mp_percent", i + 1),
                    CsvQuestParser.ParseInt(columns[11], "max_revives_per_floor", i + 1)));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidOperationException($"mirror_configs.csv の値が範囲外です。項目: {ex.ParamName}, 行: {i + 1}", ex);
            }
        }

        return map;
    }
}
