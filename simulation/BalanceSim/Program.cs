// BalanceSim: server の実戦闘コード(QuestBattleFactory / BattleService / 各行動ポリシー)を
// そのまま駆動してステージ周回を再現し、バランス指標を実測するハーネス。
// 戦闘計算・AI・ターン解決は一切再実装しない。CI 対象外。
//
// ハーネス側の仮定(戦闘計算以外):
//  - プレイヤーは ParticipantType.Npc として参加させ、QuestAllyNpcActionPolicy(職業ロール別の実AI)で行動する。
//  - 育成ルートは「証」アイテムの必要Lv・必要マスターに従い、現職マスター後に次職へ転職する。
//  - 装備は「そのステージまでのドロップ装備から役割別スコア最良の武器+防具」を強化+0で装備する。
//  - パーティはソロ想定(プレイヤー1 + NPC補充3 = min_party_member_count)。NPC選択は実リポジトリのランダム抽選。
using System.Text.Json;
using server.application.battle;
using server.application.quest;
using server.domain.battle;
using server.domain.battle.enums;
using server.domain.move;
using server.domain.player;
using server.domain.quest;
using server.domain.quest.enums;
using server.infrastructure.move;
using server.infrastructure.player;
using server.infrastructure.quest;

var debug = Environment.GetCommandLineArgs().Contains("--debug");
var trials = ArgInt("--trials", 20);
var stageFilter = ArgList("--stages");
var routeFilter = ArgList("--routes");
var jsonPath = ArgStr("--json", null);

// ---- 実CSVリポジトリ(server と同一実装・同一データ) ----
var moveRepo = new CsvMoveRepository();
var profileRepo = new CsvJobProfileRepository();
var ruleRepo = new CsvJobMoveLearningRuleRepository();
var stageRepo = new CsvQuestStageRepository(new CsvQuestEndlessConfigRepository());
var enemyRepo = new CsvQuestEnemyDefinitionRepository();
var npcRepo = new CsvQuestNpcTemplateRepository();
var equipmentRepo = new CsvEquipmentRepository();

var allStages = (await stageRepo.GetAllAsync()).Where(x => !x.IsEndless).OrderBy(x => x.Id.Value).ToArray();
var enemyDefs = (await enemyRepo.GetAllAsync()).ToDictionary(x => x.Id);
var allEquipments = await equipmentRepo.GetAllAsync();

var battleFactory = new QuestBattleFactory();
var battleService = new BattleService();
var equipmentResolver = new EquipmentStatusResolver();

// ---- 育成ルート(「証」アイテムの必要Lv・必要マスターに準拠) ----
var routes = new Dictionary<string, RouteStep[]>
{
    ["戦士系"] = [new(Job.Warrior, 5, []), new(Job.OniWarrior, 30, [Job.Warrior]), new(Job.SwordMaster, 30, [Job.Warrior]), new(Job.GrandWarrior, 40, [Job.OniWarrior, Job.SwordMaster])],
    ["盾系"] = [new(Job.Guardian, 5, []), new(Job.Crusader, 30, [Job.Guardian]), new(Job.Trickster, 30, [Job.Guardian])],
    ["魔法系"] = [new(Job.Mage, 5, []), new(Job.FireMage, 30, [Job.Mage]), new(Job.WaterMage, 30, [Job.Mage]), new(Job.WindMage, 30, [Job.Mage])],
    ["僧侶系"] = [new(Job.Priest, 5, []), new(Job.HighPriest, 30, [Job.Priest]), new(Job.Necromancer, 30, [Job.Priest])],
    ["レンジャー系"] = [new(Job.Ranger, 5, []), new(Job.Sniper, 30, [Job.Ranger]), new(Job.TrapMaster, 30, [Job.Ranger])],
};

var results = new List<TrialResult>();
Console.WriteLine($"trials={trials} / stages={allStages.Length} / routes={routes.Count}");
foreach (var stage in allStages)
{
    if (stageFilter is not null && !stageFilter.Contains(stage.Id.Value.ToString())) continue;
    var level = Math.Min(stage.RecommendedLevel, 100);

    foreach (var (routeName, plan) in routes)
    {
        if (routeFilter is not null && !routeFilter.Contains(routeName)) continue;
        for (var t = 0; t < trials; t++)
        {
            var trial = await RunTrialAsync(stage, level, routeName, plan);
            results.Add(trial);
        }
    }
}

PrintSummary(results);
if (jsonPath is not null)
{
    File.WriteAllText(jsonPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = false }));
    Console.WriteLine($"json: {jsonPath}");
}

// ================================================================

async Task<TrialResult> RunTrialAsync(QuestStageDefinition stage, int level, string routeName, RouteStep[] plan)
{
    var now = DateTimeOffset.UtcNow;

    // --- プレイヤー育成(実 Player エンティティ: 成長・技習得・転職判定は実コード) ---
    var player = BuildPlayer(plan, level);
    var gear = PickGear(stage, player.Job);
    var effective = equipmentResolver.BuildEffectiveStatus(
        player.Status,
        player.Job,
        gear.Select(e => new PlayerEquipment(
            PlayerEquipmentId.New(), player.Id, e.Id, e.Type, EquipmentStatus.Equipped,
            e.MaxDurability, 0, 0, now, now)).ToArray(),
        allEquipments);

    // --- パーティ編成(ソロ + NPC補充。実リポジトリのランダム抽選・実FormationLayout) ---
    var npcCount = Math.Max(0, stage.MinPartyMemberCount - 1);
    var npcs = await npcRepo.GetForStartAsync(stage.Id, npcCount);
    var layout = new FormationLayout();
    var snapshots = new List<QuestRunPartyMemberSnapshot>();

    var playerPid = QuestParticipantId.New();
    var playerPos = layout.FindFirstEmpty(PreferredRow(player.Job));
    layout.Assign(playerPid, playerPos);
    // Type=Npc で実AI(QuestAllyNpcActionPolicy)に行動させつつ、ActionMode=Manual で
    // 「継続可能メンバー」(IsContinuable) として扱わせる。プレイヤー死亡=敗北(ソロ実挙動)。
    snapshots.Add(new QuestRunPartyMemberSnapshot(
        playerPid, ParticipantType.Npc, $"SIM-{routeName}", null, player.Job, effective,
        null, null, player.MoveSet, playerPos, ActionMode.Manual));

    foreach (var npc in npcs)
    {
        var pid = QuestParticipantId.New();
        var pos = layout.FindFirstEmpty(npc.PreferredRow);
        layout.Assign(pid, pos);
        var moveSet = new MoveSet();
        for (var i = 0; i < npc.MoveIds.Count && i < MoveSet.MaxSlots; i++)
        {
            moveSet.SetSlot(i, npc.MoveIds[i]);
        }
        snapshots.Add(new QuestRunPartyMemberSnapshot(
            pid, ParticipantType.Npc, npc.Name, null, npc.Job, npc.BaseStatus,
            null, null, moveSet, pos, ActionMode.AutoAttackOnly));
    }

    // --- QuestRun 構築(QuestRunFactory.CreateStaticFirstFloorAsync と同一手順) ---
    var floors = stage.Floors.OrderBy(x => x.FloorNo).ToArray();
    var firstFloor = floors[0];
    var finalFloorNo = floors[^1].FloorNo;
    var partyStates = snapshots
        .Select(x => new QuestRunPartyMemberState(x.ParticipantId, x.BaseStatus.MaxHp, x.BaseStatus.MaxMp, isDead: false, canActFromTurn: 1, x.InitialActionMode))
        .ToArray();
    var run = new QuestRun(
        QuestRunId.New(),
        QuestRoomId.New(),
        stage.Id,
        snapshots,
        new QuestFloorState(firstFloor.FloorNo, firstFloor.FloorType == FloorType.Boss, firstFloor.Placements.ToArray()),
        new QuestBattleState(partyStates, MakeEnemyStates(firstFloor)),
        new QuestTurnState(1, now.AddMinutes(1)),
        new QuestTrapCollection(),
        new QuestRewardAccumulator(),
        lastTurnResults: null,
        chatMessages: [],
        startedAt: now);

    // --- ターンループ(TryResolveIfReadyAsync の戦闘部分と同一手順) ---
    var floorTypeByNo = floors.ToDictionary(x => x.FloorNo, x => x.FloorType);
    var floorTurns = new Dictionary<int, int>();
    var floorDamageDealt = new Dictionary<int, long>();   // 味方→敵 総与ダメージ(フロア別)
    var floorCleared = new HashSet<int>();
    var allyStatus = snapshots.ToDictionary(x => x.ParticipantId.Value, x => x.BaseStatus);
    var allyMaxHp = snapshots.ToDictionary(x => x.ParticipantId.Value, x => x.BaseStatus.MaxHp);
    var playerMaxHp = effective.MaxHp;
    var bossHitsOnAllies = new List<double>();   // ボス階での 被弾1行動あたり (maxHp / damage) = 何発耐えるか
    var enemyHits = new List<EnemyHitSample>();  // 敵定義ごとの味方への1行動ダメージ標本
    var guard = 600;

    while (run.Status == QuestRunStatus.InProgress && guard-- > 0)
    {
        var currentFloorNo = run.FloorState.CurrentFloorNo;
        var fieldContext = battleFactory.CreateBattleFieldContext(run);
        var actors = battleFactory.CreateActorInputs(run, enemyDefs);
        var (actions, moves) = await battleFactory.CreateTurnInputsAsync(run, moveRepo, enemyDefs);
        var resolution = battleService.ResolveTurn(new BattleTurnRequest(actors, actions, moves, fieldContext));

        var isBossFloor = floorTypeByNo[currentFloorNo] == FloorType.Boss;
        var enemyDefByActorId = run.BattleState.Enemies.ToDictionary(x => x.Id.Value, x => x.EnemyDefinitionId.Value);
        foreach (var action in resolution.ActionResults.Where(x => !x.IsTurnEndEffect))
        {
            if (enemyDefByActorId.TryGetValue(action.ActorId.Value, out var defId))
            {
                // 敵の行動 → 味方への被弾標本
                foreach (var target in action.TargetResults.Where(x => x.Damage > 0 && allyMaxHp.ContainsKey(x.TargetActorId.Value)))
                {
                    var st = allyStatus[target.TargetActorId.Value];
                    enemyHits.Add(new EnemyHitSample(defId, target.Damage, st.MaxHp, st.Defense, st.Intelligence, isBossFloor));
                    if (isBossFloor)
                    {
                        bossHitsOnAllies.Add((double)st.MaxHp / target.Damage);
                    }
                }
            }
            else if (allyMaxHp.ContainsKey(action.ActorId.Value))
            {
                // 味方の行動 → フロアDPS計測
                foreach (var target in action.TargetResults.Where(x => x.Damage > 0 && enemyDefByActorId.ContainsKey(x.TargetActorId.Value)))
                {
                    floorDamageDealt[currentFloorNo] = floorDamageDealt.GetValueOrDefault(currentFloorNo) + target.Damage;
                }
            }
        }

        var summary = run.ApplyBattleResolution(
            resolution,
            battleFactory.CreatePartyActorMap(run),
            battleFactory.CreateEnemyActorMap(run),
            finalFloorNo,
            now.AddMinutes(1));

        if (debug)
        {
            var names = snapshots.ToDictionary(x => x.ParticipantId.Value, x => x.DisplayName);
            foreach (var e in run.BattleState.Enemies) names[e.Id.Value] = enemyDefs[e.EnemyDefinitionId].Name;
            Console.WriteLine($"--- {currentFloorNo}F turn{floorTurns.GetValueOrDefault(currentFloorNo) + 1} ---");
            foreach (var action in resolution.ActionResults.Where(x => !x.IsTurnEndEffect))
            {
                var targets = string.Join(", ", action.TargetResults.Select(x =>
                    $"{names.GetValueOrDefault(x.TargetActorId.Value, "?")}{(x.Damage > 0 ? $" -{x.Damage}" : x.HpChange != 0 ? $" +{x.HpChange}" : "")}{(x.IsDefeated ? "☠" : "")}"));
                Console.WriteLine($"  {names.GetValueOrDefault(action.ActorId.Value, "?"),-14} {action.ActionKind}{(action.MoveId is not null ? $"({action.MoveId.Id})" : "")} → {targets}");
            }
            var party = string.Join(" / ", run.BattleState.PartyMembers.Select(m =>
                $"{names[m.ParticipantId.Value]}:{m.CurrentHp}{(m.IsDead ? "☠" : "")}"));
            Console.WriteLine($"  [味方] {party}");
        }

        floorTurns[currentFloorNo] = floorTurns.GetValueOrDefault(currentFloorNo) + 1;

        if (summary.IsFloorCleared)
        {
            floorCleared.Add(currentFloorNo);
            if (run.Status == QuestRunStatus.InProgress && !summary.IsQuestCompleted)
            {
                var next = floors.First(x => x.FloorNo == currentFloorNo + 1);
                run.StartNextFloor(MakeEnemyStates(next), next.FloorType == FloorType.Boss, next.Placements.ToArray(), now.AddMinutes(1));
            }
        }
    }

    var deadCount = run.BattleState.PartyMembers.Count(x => x.IsDead);
    var floorTotalHp = floors.ToDictionary(
        x => x.FloorNo,
        x => x.Placements.Sum(p => (long)enemyDefs[p.EnemyDefinitionId].Status.MaxHp));
    var floorStats = floorTurns
        .Select(x => new FloorStat(
            x.Key,
            floorTypeByNo[x.Key].ToString(),
            x.Value,
            floorDamageDealt.GetValueOrDefault(x.Key),
            floorTotalHp.GetValueOrDefault(x.Key),
            floorCleared.Contains(x.Key)))
        .OrderBy(x => x.FloorNo)
        .ToArray();
    return new TrialResult(
        stage.Id.Value,
        stage.Name,
        level,
        routeName,
        player.Job.ToString(),
        run.Status.ToString(),
        floorTurns.Where(x => floorTypeByNo[x.Key] == FloorType.Normal).Select(x => x.Value).ToArray(),
        floorTurns.Where(x => floorTypeByNo[x.Key] == FloorType.Boss).Select(x => x.Value).ToArray(),
        run.FloorState.CurrentFloorNo,
        deadCount,
        bossHitsOnAllies.ToArray(),
        playerMaxHp,
        floorStats,
        enemyHits.ToArray());
}

QuestEnemyState[] MakeEnemyStates(QuestFloorDefinition floor)
{
    return floor.Placements
        .Select(p =>
        {
            var def = enemyDefs[p.EnemyDefinitionId];
            return new QuestEnemyState(QuestEnemyInstanceId.New(), def.Id, p.Position, def.Status.MaxHp, def.Status.MaxMp, isDead: false);
        })
        .ToArray();
}

Player BuildPlayer(RouteStep[] plan, int targetLevel)
{
    var moveSet = new MoveSet();
    moveSet.SetSlot(0, new MoveId(101));
    var player = new Player(
        new PlayerId(Guid.NewGuid()), "SIM", level: 1, exp: 0, jobLevel: 1, gold: 0,
        status: new Status(maxHp: 24, maxMp: 8, strength: 7, defense: 5, intelligence: 5, luck: 3, speed: 4),
        job: Job.Apprentice,
        moveSet: moveSet);
    player.LevelUp(profileRepo.GetByJob(player.Job), ruleRepo.GetByJob(player.Job));

    var stepIndex = 0;
    while (player.Level < targetLevel)
    {
        player.GainExp(player.RequiredExpForNextLevel());
        player.LevelUp(profileRepo.GetByJob(player.Job), ruleRepo.GetByJob(player.Job));

        while (stepIndex < plan.Length)
        {
            var step = plan[stepIndex];
            var canByItem = player.Level >= step.MinLevel && step.NeedMasters.All(player.MasteredJobs.Contains);
            var currentMastered = player.MasteredJobs.Contains(player.Job);
            if (!canByItem || !currentMastered)
            {
                break;
            }

            player.ChangeJob(step.Job, profileRepo.GetByJob(step.Job), ruleRepo.GetByJob(step.Job));
            player.LevelUp(profileRepo.GetByJob(player.Job), ruleRepo.GetByJob(player.Job)); // 転職直後の職業Lv1技を同期
            stepIndex++;
        }
    }

    return player;
}

IReadOnlyList<Equipment> PickGear(QuestStageDefinition stage, Job job)
{
    var (wStr, wDef, wInt, wHp, wSpd) = RoleWeights(job);
    var candidates = allStages
        .Where(x => x.Id.Value <= stage.Id.Value)
        .SelectMany(x => x.EquipmentRewards)
        .Where(x => !x.IsMiss && x.EquipmentId is not null)
        .Select(x => allEquipments.First(e => e.Id == x.EquipmentId!.Value))
        .Where(e => e.CanEquip(job))
        .Distinct()
        .ToArray();

    return candidates
        .GroupBy(e => e.Type)
        .Select(g => g
            .OrderByDescending(e =>
                e.BonusValues.Strength * wStr +
                e.BonusValues.Defense * wDef +
                e.BonusValues.Intelligence * wInt +
                e.BonusValues.MaxHp * wHp +
                e.BonusValues.Speed * wSpd)
            .First())
        .ToArray();
}

static (double Str, double Def, double Int, double Hp, double Spd) RoleWeights(Job job) => job switch
{
    Job.Guardian or Job.Crusader or Job.Trickster or Job.GrandGuard or Job.GreatKnight or Job.Shugoshin
        => (1, 3, 0, 0.35, 0),
    Job.Mage or Job.FireMage or Job.WaterMage or Job.WindMage or Job.GrandCaster or Job.Archmage or Job.Seikaiou
        => (0, 1, 3, 0.25, 0),
    Job.Priest or Job.HighPriest or Job.Necromancer or Job.GrandPriest
        => (0, 1, 3, 0.3, 0),
    Job.Ranger or Job.Sniper or Job.TrapMaster or Job.GrandRanger or Job.GreatThief or Job.Matouou
        => (3, 0, 0, 0.25, 1),
    _ => (3, 2, 0, 0.25, 0),
};

static BattleRow PreferredRow(Job job) => job switch
{
    Job.Ranger or Job.Sniper or Job.TrapMaster or Job.GrandRanger or Job.GreatThief or Job.Matouou => BattleRow.Middle,
    Job.Mage or Job.FireMage or Job.WaterMage or Job.WindMage or Job.GrandCaster or Job.Archmage or Job.Seikaiou => BattleRow.Back,
    Job.Priest or Job.HighPriest or Job.Necromancer or Job.GrandPriest => BattleRow.Back,
    _ => BattleRow.Front,
};

static void PrintSummary(List<TrialResult> results)
{
    Console.WriteLine();
    Console.WriteLine($"{"stage",-5} {"ステージ",-14} {"Lv",4} {"ルート",-8} {"クリア率",6} {"通常平均T",8} {"ボス平均T",8} {"死亡平均",6} {"被弾中央(発)",8} {"被弾最悪(発)",8}");
    foreach (var group in results.GroupBy(x => (x.StageId, x.Route)).OrderBy(x => x.Key.StageId).ThenBy(x => x.Key.Route))
    {
        var list = group.ToList();
        var clearRate = 100.0 * list.Count(x => x.Outcome == "Succeeded") / list.Count;
        var normal = list.SelectMany(x => x.NormalFloorTurns).DefaultIfEmpty(0).Average();
        var boss = list.SelectMany(x => x.BossFloorTurns).DefaultIfEmpty(0).Average();
        var deaths = list.Average(x => x.DeadCount);
        var hits = list.SelectMany(x => x.BossHitTolerances).OrderBy(x => x).ToArray();
        var median = hits.Length > 0 ? hits[hits.Length / 2] : double.NaN;
        var worst = hits.Length > 0 ? hits[0] : double.NaN;
        Console.WriteLine($"{group.Key.StageId,-5} {list[0].StageName,-14} {list[0].Level,4} {group.Key.Route,-8} {clearRate,5:F0}% {normal,8:F1} {boss,8:F1} {deaths,6:F1} {median,8:F1} {worst,8:F1}");
    }
}

static int ArgInt(string name, int fallback)
{
    var args = Environment.GetCommandLineArgs();
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? int.Parse(args[i + 1]) : fallback;
}

static string? ArgStr(string name, string? fallback)
{
    var args = Environment.GetCommandLineArgs();
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
}

static HashSet<string>? ArgList(string name)
{
    var value = ArgStr(name, null);
    return value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
}

internal record RouteStep(Job Job, int MinLevel, Job[] NeedMasters);

internal record FloorStat(int FloorNo, string FloorType, int Turns, long DamageDealt, long TotalEnemyHp, bool Cleared);

internal record EnemyHitSample(int EnemyDefId, int Damage, int TargetMaxHp, int TargetDefense, int TargetIntelligence, bool IsBossFloor);

internal record TrialResult(
    int StageId,
    string StageName,
    int Level,
    string Route,
    string Job,
    string Outcome,
    int[] NormalFloorTurns,
    int[] BossFloorTurns,
    int ReachedFloorNo,
    int DeadCount,
    double[] BossHitTolerances,
    int PlayerMaxHp,
    FloorStat[] Floors,
    EnemyHitSample[] EnemyHits);
