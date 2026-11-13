using FluentAssertions;
using server.domain.player;
using server.infrastructure.move;
using server.infrastructure.player;
using server.infrastructure.quest;
using server.infrastructure.training;
using server.infrastructure.treasuremap;
using Xunit;

namespace server.tests.masterdata;

/// <summary>
/// 出力先 resources 配下のマスタデータ一式が、本番と同じ CSV リポジトリで読み込めることと、
/// ファイル間の参照（ID の指し先）が切れていないことを確認する。
/// 通常はテスト用 CSV（fixtures/resources）に対して動く。
/// スプレッドシートから書き出した CSV は -p:UseMasterDataFixtures=false を付けて検証する。
/// </summary>
public class MasterDataIntegrityTests
{
    [Fact]
    public void Constructors_WithCurrentResources_LoadEveryMasterFile()
    {
        var loaders = new Dictionary<string, Func<object>>
        {
            ["move_master.csv / move_effects.csv"] = () => new CsvMoveRepository(),
            ["job_moves.csv"] = () => new CsvJobMoveLearningRuleRepository(),
            ["combat_index_rank_thresholds.csv"] = () => new CsvCombatIndexRankThresholdRepository(),
            ["combat_index_weights.csv"] = () => new CsvCombatIndexWeightRepository(),
            ["equipment_master.csv"] = () => new CsvEquipmentRepository(),
            ["item_master.csv"] = () => new CsvItemRepository(),
            ["job_levelup_growths.csv"] = () => new CsvJobProfileRepository(),
            ["job_roadmap_ranks.csv"] = () => new CsvJobRoadmapRankRepository(),
            ["status_rank_thresholds.csv"] = () => new CsvStatusRankThresholdRepository(),
            ["endless_configs.csv"] = () => new CsvQuestEndlessConfigRepository(),
            ["endless_enemy_templates.csv"] = () => new CsvQuestEndlessEnemyTemplateRepository(),
            ["enemies.csv / enemy_moves.csv"] = () => new CsvQuestEnemyDefinitionRepository(),
            ["npc_templates.csv / npc_moves.csv / stage_npc_templates.csv"] = () => new CsvQuestNpcTemplateRepository(),
            ["stages.csv / stage_floors.csv / floor_enemy_spawns.csv / stage_*_rewards.csv"] =
                () => new CsvQuestStageRepository(new CsvQuestEndlessConfigRepository()),
            ["training_enemies.csv"] = () => new CsvTrainingEnemyRepository(),
            ["treasure_maps.csv"] = () => new CsvTreasureMapRepository(),
            ["reward_pools.csv / reward_entries.csv"] = () => new CsvTreasureMapRewardPoolRepository(),
        };

        var failures = new List<string>();
        foreach (var (files, load) in loaders)
        {
            try
            {
                load();
            }
            catch (Exception ex)
            {
                failures.Add($"{files}: {ex.Message}");
            }
        }

        failures.Should().BeEmpty("全てのマスタデータが読み込めること");
    }

    [Fact]
    public async Task StageDefinitions_WithCurrentResources_ReferenceExistingEnemiesItemsAndEquipments()
    {
        var stages = await new CsvQuestStageRepository(new CsvQuestEndlessConfigRepository()).GetAllAsync();
        var enemyIds = (await new CsvQuestEnemyDefinitionRepository().GetAllAsync()).Select(x => x.Id.Value).ToHashSet();
        var itemIds = (await new CsvItemRepository().GetAllAsync()).Select(x => x.Id.Value).ToHashSet();
        var equipmentIds = (await new CsvEquipmentRepository().GetAllAsync()).Select(x => x.Id.Value).ToHashSet();

        var missingEnemies = stages
            .SelectMany(stage => stage.Floors.SelectMany(floor => floor.Placements.Select(placement =>
                (Stage: stage.Id.Value, Floor: floor.FloorNo, EnemyId: placement.EnemyDefinitionId.Value))))
            .Where(x => !enemyIds.Contains(x.EnemyId))
            .Select(x => $"stage_id={x.Stage}, floor_no={x.Floor}, enemy_definition_id={x.EnemyId}")
            .ToList();
        var missingItems = stages
            .SelectMany(stage => stage.ItemRewards
                .Where(reward => reward.ItemId is not null)
                .Select(reward => (Stage: stage.Id.Value, ItemId: reward.ItemId!.Value.Value)))
            .Where(x => !itemIds.Contains(x.ItemId))
            .Select(x => $"stage_id={x.Stage}, item_id={x.ItemId}")
            .ToList();
        var missingEquipments = stages
            .SelectMany(stage => stage.EquipmentRewards
                .Where(reward => reward.EquipmentId is not null)
                .Select(reward => (Stage: stage.Id.Value, EquipmentId: reward.EquipmentId!.Value.Value)))
            .Where(x => !equipmentIds.Contains(x.EquipmentId))
            .Select(x => $"stage_id={x.Stage}, equipment_id={x.EquipmentId}")
            .ToList();

        missingEnemies.Should().BeEmpty("floor_enemy_spawns.csv の敵は enemies.csv に存在すること");
        missingItems.Should().BeEmpty("stage_item_rewards.csv のアイテムは item_master.csv に存在すること");
        missingEquipments.Should().BeEmpty("stage_equipment_rewards.csv の装備は equipment_master.csv に存在すること");
    }

    [Fact]
    public async Task MoveReferences_WithCurrentResources_PointToExistingMoves()
    {
        var moveIds = (await new CsvMoveRepository().GetAllMovesAsync()).Select(x => x.Id.Id).ToHashSet();

        var enemyMoves = (await new CsvQuestEnemyDefinitionRepository().GetAllAsync())
            .SelectMany(enemy => enemy.MoveIds.Select(move =>
                (Source: $"enemy_moves.csv: enemy_definition_id={enemy.Id.Value}", MoveId: move.Id)));
        var npcMoves = (await new CsvQuestNpcTemplateRepository().GetAllAsync())
            .SelectMany(npc => npc.MoveIds.Select(move =>
                (Source: $"npc_moves.csv: npc_template_id={npc.Id.Value}", MoveId: move.Id)));
        var endlessMoves = (await new CsvQuestEndlessEnemyTemplateRepository().GetAllAsync())
            .SelectMany(template => template.MoveIds.Select(move =>
                (Source: $"endless_enemy_templates.csv: template_id={template.Id.Value}", MoveId: move.Id)));
        var jobRules = new CsvJobMoveLearningRuleRepository();
        var jobMoves = Enum.GetValues<Job>()
            .SelectMany(job => jobRules.GetByJob(job).MoveIds.Select(move =>
                (Source: $"job_moves.csv: job_code={job}", MoveId: move.Id)));

        var missing = enemyMoves.Concat(npcMoves).Concat(endlessMoves).Concat(jobMoves)
            .Where(x => !moveIds.Contains(x.MoveId))
            .Select(x => $"{x.Source}, move_id={x.MoveId}")
            .ToList();

        missing.Should().BeEmpty("技の参照先は move_master.csv に存在すること");
    }

    [Fact]
    public async Task TreasureMaps_WithCurrentResources_ReferenceExistingPoolsItemsAndEquipments()
    {
        var pools = await new CsvTreasureMapRewardPoolRepository().GetAllAsync();
        var poolIds = pools.Select(x => x.Id.Value).ToHashSet();
        var itemIds = (await new CsvItemRepository().GetAllAsync()).Select(x => x.Id.Value).ToHashSet();
        var equipmentIds = (await new CsvEquipmentRepository().GetAllAsync()).Select(x => x.Id.Value).ToHashSet();

        var missingPools = (await new CsvTreasureMapRepository().GetAllAsync())
            .Where(map => !poolIds.Contains(map.RewardPoolId.Value))
            .Select(map => $"treasure_maps.csv: id={map.Id.Value}, reward_pool_id={map.RewardPoolId.Value}")
            .ToList();
        var entries = pools.SelectMany(pool => pool.Entries.Select(entry => (Pool: pool.Id.Value, Entry: entry))).ToList();
        var missingItems = entries
            .Where(x => x.Entry.ItemId is not null && !itemIds.Contains(x.Entry.ItemId.Value))
            .Select(x => $"reward_entries.csv: pool_id={x.Pool}, item_id={x.Entry.ItemId}")
            .ToList();
        var missingEquipments = entries
            .Where(x => x.Entry.EquipmentId is not null && !equipmentIds.Contains(x.Entry.EquipmentId.Value))
            .Select(x => $"reward_entries.csv: pool_id={x.Pool}, equipment_id={x.Entry.EquipmentId}")
            .ToList();

        missingPools.Should().BeEmpty("treasure_maps.csv の報酬プールは reward_pools.csv に存在すること");
        missingItems.Should().BeEmpty("reward_entries.csv のアイテムは item_master.csv に存在すること");
        missingEquipments.Should().BeEmpty("reward_entries.csv の装備は equipment_master.csv に存在すること");
    }

    [Fact]
    public async Task StageUnlockFlags_WithCurrentResources_AreGrantedByAnItem()
    {
        var grantedFlags = (await new CsvItemRepository().GetAllAsync())
            .Where(item => item.MapUnlockFlag is not null)
            .Select(item => item.MapUnlockFlag!.Value)
            .ToHashSet();

        var unreachableStages = (await new CsvQuestStageRepository(new CsvQuestEndlessConfigRepository()).GetAllAsync())
            .Where(stage => stage.RequiredMapUnlockFlag is not null && !grantedFlags.Contains(stage.RequiredMapUnlockFlag.Value))
            .Select(stage => $"stages.csv: id={stage.Id.Value}, required_map_unlock_flag={stage.RequiredMapUnlockFlag}")
            .ToList();

        unreachableStages.Should().BeEmpty("解放フラグが必要なステージは、そのフラグを付与するアイテムが item_master.csv に存在すること");
    }
}
