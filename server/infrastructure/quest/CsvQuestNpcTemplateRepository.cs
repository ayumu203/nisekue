using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest;

public class CsvQuestNpcTemplateRepository : IQuestNpcTemplateRepository
{
    private readonly IReadOnlyDictionary<QuestNpcTemplateId, QuestNpcTemplate> templatesById;
    private readonly IReadOnlyList<QuestNpcTemplate> templates;
    private readonly IReadOnlyDictionary<QuestStageId, QuestNpcTemplateId[]> templateIdsByStageId;

    public CsvQuestNpcTemplateRepository()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources", "quest");
        templatesById = LoadTemplates(
            Path.Combine(resourceDir, "npc_templates.csv"),
            Path.Combine(resourceDir, "npc_moves.csv"));
        templateIdsByStageId = LoadStageNpcTemplates(Path.Combine(resourceDir, "stage_npc_templates.csv"));
        templates = templatesById.Values.OrderBy(x => x.Id.Value).ToArray();
    }

    public Task<IReadOnlyList<QuestNpcTemplate>> GetForStartAsync(QuestStageId stageId, int count)
    {
        var requiredCount = Math.Max(count, 0);
        if (requiredCount == 0)
        {
            return Task.FromResult<IReadOnlyList<QuestNpcTemplate>>([]);
        }

        if (!templateIdsByStageId.TryGetValue(stageId, out var candidateIds) || candidateIds.Length == 0)
        {
            throw new InvalidOperationException($"stage_npc_templates.csv に stage_id={stageId.Value} の候補定義がありません。");
        }

        var pool = candidateIds
            .Distinct()
            .Select(id => templatesById.TryGetValue(id, out var template)
                ? template
                : throw new InvalidOperationException($"npc_templates.csv に存在しない npc_template_id={id.Value} が stage_id={stageId.Value} に指定されています。"))
            .ToArray();
        if (pool.Length < requiredCount)
        {
            throw new InvalidOperationException($"stage_id={stageId.Value} の NPC 候補数が不足しています。required={requiredCount}, actual={pool.Length}");
        }

        var selected = pool
            .OrderBy(_ => Random.Shared.Next())
            .Take(requiredCount)
            .ToArray();
        return Task.FromResult<IReadOnlyList<QuestNpcTemplate>>(selected);
    }

    public Task<IReadOnlyList<QuestNpcTemplate>> GetAllAsync()
    {
        return Task.FromResult(templates);
    }

    private static IReadOnlyDictionary<QuestNpcTemplateId, QuestNpcTemplate> LoadTemplates(string templatesPath, string npcMovesPath)
    {
        var moveIdsByTemplate = LoadNpcMoves(npcMovesPath);
        var lines = CsvQuestParser.ReadDataLines(templatesPath);
        var map = new Dictionary<QuestNpcTemplateId, QuestNpcTemplate>();

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
                throw new InvalidOperationException($"npc_templates.csv の形式が不正です。行: {i + 1}");
            }

            var id = new QuestNpcTemplateId(CsvQuestParser.ParseInt(columns[0], "id", i + 1));
            moveIdsByTemplate.TryGetValue(id, out var moveIds);
            moveIds ??= [];

            map.Add(id, new QuestNpcTemplate(
                id,
                columns[1],
                CsvQuestParser.ParseEnum<Job>(columns[2], "job", i + 1),
                CsvQuestParser.ParseEnum<BattleRow>(columns[3], "preferred_row", i + 1),
                CsvQuestParser.ParseInt(columns[4], "level", i + 1),
                new Status(
                    CsvQuestParser.ParseInt(columns[5], "max_hp", i + 1),
                    CsvQuestParser.ParseInt(columns[6], "max_mp", i + 1),
                    CsvQuestParser.ParseInt(columns[7], "strength", i + 1),
                    CsvQuestParser.ParseInt(columns[8], "defense", i + 1),
                    CsvQuestParser.ParseInt(columns[9], "intelligence", i + 1),
                    CsvQuestParser.ParseInt(columns[10], "luck", i + 1),
                    CsvQuestParser.ParseInt(columns[11], "speed", i + 1)),
                moveIds.Select(x => new MoveId(x)),
                CsvQuestParser.ParseEnum<NpcRole>(columns[12], "role", i + 1)));
        }

        return map;
    }

    private static IReadOnlyDictionary<QuestNpcTemplateId, int[]> LoadNpcMoves(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestNpcTemplateId, int[]>();

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
                throw new InvalidOperationException($"npc_moves.csv の形式が不正です。行: {i + 1}");
            }

            var id = new QuestNpcTemplateId(CsvQuestParser.ParseInt(columns[0], "npc_template_id", i + 1));
            map[id] = CsvQuestParser.ParseIntList(columns[1], "move_ids", i + 1);
        }

        return map;
    }

    private static IReadOnlyDictionary<QuestStageId, QuestNpcTemplateId[]> LoadStageNpcTemplates(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestStageId, QuestNpcTemplateId[]>();

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
                throw new InvalidOperationException($"stage_npc_templates.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "stage_id", i + 1));
            if (map.ContainsKey(stageId))
            {
                throw new InvalidOperationException($"stage_npc_templates.csv で stage_id が重複しています。stage_id={stageId.Value}, 行: {i + 1}");
            }

            map[stageId] = CsvQuestParser.ParseIntList(columns[1], "npc_template_ids", i + 1)
                .Select(id => new QuestNpcTemplateId(id))
                .ToArray();
        }

        return map;
    }
}
