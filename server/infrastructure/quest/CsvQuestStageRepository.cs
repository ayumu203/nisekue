using server.domain.quest;
using server.domain.quest.enums;

namespace server.infrastructure.quest;

public class CsvQuestStageRepository : IQuestStageRepository
{
    private readonly IReadOnlyDictionary<QuestStageId, QuestStageDefinition> stagesById;
    private readonly IReadOnlyDictionary<string, QuestStageDefinition> stagesByCode;
    private readonly IReadOnlyList<QuestStageDefinition> stages;

    public CsvQuestStageRepository()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources", "quest");
        stagesById = LoadStages(
            Path.Combine(resourceDir, "stages.csv"),
            Path.Combine(resourceDir, "stage_floors.csv"),
            Path.Combine(resourceDir, "floor_enemy_spawns.csv"),
            Path.Combine(resourceDir, "stage_equipment_rewards.csv"),
            Path.Combine(resourceDir, "stage_item_rewards.csv"));
        stagesByCode = stagesById.Values.ToDictionary(x => x.StageCode, StringComparer.OrdinalIgnoreCase);
        stages = stagesById.Values.OrderBy(x => x.StageCode, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public Task<QuestStageDefinition?> GetAsync(QuestStageId id)
    {
        stagesById.TryGetValue(id, out var stage);
        return Task.FromResult(stage);
    }

    public Task<QuestStageDefinition?> GetByStageCodeAsync(string stageCode)
    {
        if (string.IsNullOrWhiteSpace(stageCode))
        {
            return Task.FromResult<QuestStageDefinition?>(null);
        }

        stagesByCode.TryGetValue(stageCode.Trim(), out var stage);
        return Task.FromResult(stage);
    }

    public Task<IReadOnlyList<QuestStageDefinition>> GetAllAsync()
    {
        return Task.FromResult(stages);
    }

    private static IReadOnlyDictionary<QuestStageId, QuestStageDefinition> LoadStages(
        string stagesPath,
        string floorsPath,
        string spawnsPath,
        string equipmentRewardsPath,
        string itemRewardsPath)
    {
        var floorRowsByStage = LoadFloorRows(floorsPath);
        var spawnRowsByFloor = LoadSpawnRows(spawnsPath);
        var rewardRowsByStage = LoadRewardRows(equipmentRewardsPath);
        var itemRewardRowsByStage = LoadItemRewardRows(itemRewardsPath);
        var lines = CsvQuestParser.ReadDataLines(stagesPath);
        var map = new Dictionary<QuestStageId, QuestStageDefinition>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 9)
            {
                throw new InvalidOperationException($"stages.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "id", i + 1));
            if (map.ContainsKey(stageId))
            {
                throw new InvalidOperationException($"stages.csv で stage_id が重複しています。id={stageId}, 行: {i + 1}");
            }

            floorRowsByStage.TryGetValue(stageId, out var floorRows);
            floorRows ??= [];
            rewardRowsByStage.TryGetValue(stageId, out var rewardRows);
            rewardRows ??= [];
            itemRewardRowsByStage.TryGetValue(stageId, out var itemRewardRows);
            itemRewardRows ??= [];
            var floors = floorRows
                .OrderBy(x => x.FloorNo)
                .Select(row =>
                {
                    spawnRowsByFloor.TryGetValue((stageId, row.FloorNo), out var spawnRows);
                    spawnRows ??= [];
                    return new QuestFloorDefinition(
                        row.FloorNo,
                        row.FloorType,
                        spawnRows.OrderBy(x => x.PlacementNo).Select(x => x.ToPlacement()),
                        new QuestFloorRewardRule(row.ExpRate, row.GoldRate));
                })
                .ToArray();

            map.Add(stageId, new QuestStageDefinition(
                stageId,
                columns[1],
                columns[2],
                columns[3],
                CsvQuestParser.ParseInt(columns[4], "recommended_level", i + 1),
                string.IsNullOrWhiteSpace(columns[5]) ? null : CsvQuestParser.ParseInt(columns[5], "minimum_entry_level", i + 1),
                CsvQuestParser.ParseInt(columns[6], "min_party_member_count", i + 1),
                CsvQuestParser.ParseInt(columns[7], "max_party_member_count", i + 1),
                floors,
                rewardRows.OrderByDescending(x => x.Weight).Select(x => x.ToRewardEntry()).ToArray(),
                itemRewardRows.OrderByDescending(x => x.Weight).Select(x => x.ToRewardEntry()).ToArray(),
                CsvQuestParser.ParseBool(columns[8], "is_active", i + 1)));
        }

        return map;
    }

    private static IReadOnlyDictionary<QuestStageId, List<FloorRow>> LoadFloorRows(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestStageId, List<FloorRow>>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 5)
            {
                throw new InvalidOperationException($"stage_floors.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "stage_id", i + 1));
            var row = new FloorRow(
                CsvQuestParser.ParseInt(columns[1], "floor_no", i + 1),
                CsvQuestParser.ParseEnum<FloorType>(columns[2], "floor_type", i + 1),
                CsvQuestParser.ParseDecimal(columns[3], "exp_rate", i + 1),
                CsvQuestParser.ParseDecimal(columns[4], "gold_rate", i + 1));

            if (!map.TryGetValue(stageId, out var list))
            {
                list = [];
                map[stageId] = list;
            }

            list.Add(row);
        }

        return map;
    }

    private static IReadOnlyDictionary<(QuestStageId StageId, int FloorNo), List<SpawnRow>> LoadSpawnRows(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<(QuestStageId StageId, int FloorNo), List<SpawnRow>>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 6)
            {
                throw new InvalidOperationException($"floor_enemy_spawns.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "stage_id", i + 1));
            var floorNo = CsvQuestParser.ParseInt(columns[1], "floor_no", i + 1);
            var key = (stageId, floorNo);
            var row = new SpawnRow(
                CsvQuestParser.ParseInt(columns[2], "placement_no", i + 1),
                new QuestEnemyDefinitionId(CsvQuestParser.ParseInt(columns[3], "enemy_definition_id", i + 1)),
                CsvQuestParser.ParseBattlePosition(columns[4], columns[5], i + 1));

            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }

            list.Add(row);
        }

        return map;
    }

    private static IReadOnlyDictionary<QuestStageId, List<RewardRow>> LoadRewardRows(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestStageId, List<RewardRow>>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 4)
            {
                throw new InvalidOperationException($"stage_equipment_rewards.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "stage_id", i + 1));
            var row = new RewardRow(
                string.IsNullOrWhiteSpace(columns[1]) ? null : new server.domain.player.EquipmentId(CsvQuestParser.ParseInt(columns[1], "equipment_id", i + 1)),
                CsvQuestParser.ParseInt(columns[2], "weight", i + 1),
                CsvQuestParser.ParseBool(columns[3], "is_miss", i + 1));

            if (!map.TryGetValue(stageId, out var list))
            {
                list = [];
                map[stageId] = list;
            }

            list.Add(row);
        }

        return map;
    }

    private static IReadOnlyDictionary<QuestStageId, List<ItemRewardRow>> LoadItemRewardRows(string csvPath)
    {
        var lines = CsvQuestParser.ReadDataLines(csvPath);
        var map = new Dictionary<QuestStageId, List<ItemRewardRow>>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = CsvQuestParser.SplitColumns(line);
            if (columns.Length != 4)
            {
                throw new InvalidOperationException($"stage_item_rewards.csv の形式が不正です。行: {i + 1}");
            }

            var stageId = new QuestStageId(CsvQuestParser.ParseInt(columns[0], "stage_id", i + 1));
            var row = new ItemRewardRow(
                string.IsNullOrWhiteSpace(columns[1]) ? null : new server.domain.player.ItemId(CsvQuestParser.ParseInt(columns[1], "item_id", i + 1)),
                CsvQuestParser.ParseInt(columns[2], "weight", i + 1),
                CsvQuestParser.ParseBool(columns[3], "is_miss", i + 1));

            if (!map.TryGetValue(stageId, out var list))
            {
                list = [];
                map[stageId] = list;
            }

            list.Add(row);
        }

        return map;
    }

    private sealed record FloorRow(int FloorNo, FloorType FloorType, decimal ExpRate, decimal GoldRate);

    private sealed record SpawnRow(int PlacementNo, QuestEnemyDefinitionId EnemyDefinitionId, server.domain.battle.BattlePosition Position)
    {
        public QuestEnemyPlacement ToPlacement() => new(PlacementNo, EnemyDefinitionId, Position);
    }

    private sealed record RewardRow(server.domain.player.EquipmentId? EquipmentId, int Weight, bool IsMiss)
    {
        public QuestStageEquipmentRewardEntry ToRewardEntry() => new(EquipmentId, Weight, IsMiss);
    }

    private sealed record ItemRewardRow(server.domain.player.ItemId? ItemId, int Weight, bool IsMiss)
    {
        public QuestStageItemRewardEntry ToRewardEntry() => new(ItemId, Weight, IsMiss);
    }
}
