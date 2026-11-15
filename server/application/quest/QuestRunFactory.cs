using server.domain.quest;
using server.domain.quest.enums;

namespace server.application.quest;

public class QuestRunFactory(
    IQuestEnemyDefinitionRepository enemyDefinitionRepository,
    IQuestEndlessEnemyTemplateRepository endlessEnemyTemplateRepository,
    QuestEndlessFloorGenerator endlessFloorGenerator)
{
    private static readonly TimeSpan InitialTurnDeadline = TimeSpan.FromSeconds(60);

    public async Task<QuestRun> Create(
        QuestRoom room,
        QuestStageDefinition stageDefinition,
        IEnumerable<QuestRunPartyMemberSnapshot> snapshots,
        DateTimeOffset now,
        MirrorEnemyPlan? mirrorEnemyPlan = null)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(stageDefinition);
        ArgumentNullException.ThrowIfNull(snapshots);

        var snapshotArray = snapshots.ToArray();
        if (snapshotArray.Length == 0)
        {
            throw new InvalidOperationException("開始時スナップショットが空です。");
        }

        if (stageDefinition.IsMirror && mirrorEnemyPlan is null)
        {
            throw new InvalidOperationException("名鑑の祠には敵の計画が必要です。");
        }

        var (firstFloorNo, isBossFloor, placements, enemyStates) = stageDefinition.IsEndless
            ? await CreateEndlessFirstFloorAsync(stageDefinition)
            : stageDefinition.IsMirror
                ? CreateMirrorFirstFloor(mirrorEnemyPlan!)
                : await CreateStaticFirstFloorAsync(stageDefinition);

        var partyStates = snapshotArray
            .Select(x => new QuestRunPartyMemberState(
                x.ParticipantId,
                x.BaseStatus.MaxHp,
                x.BaseStatus.MaxMp,
                isDead: false,
                canActFromTurn: 1,
                x.InitialActionMode))
            .ToArray();

        return new QuestRun(
            QuestRunId.New(),
            room.Id,
            stageDefinition.Id,
            snapshotArray,
            new QuestFloorState(firstFloorNo, isBossFloor, placements),
            new QuestBattleState(partyStates, enemyStates),
            new QuestTurnState(1, now.Add(InitialTurnDeadline)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            lastTurnResults: null,
            chatMessages: [],
            startedAt: now,
            mirrorEnemyPlan: stageDefinition.IsMirror ? mirrorEnemyPlan : null);
    }

    private async Task<(int FloorNo, bool IsBoss, QuestEnemyPlacement[] Placements, QuestEnemyState[] EnemyStates)>
        CreateStaticFirstFloorAsync(QuestStageDefinition stageDefinition)
    {
        var firstFloor = stageDefinition.Floors.OrderBy(x => x.FloorNo).FirstOrDefault()
            ?? throw new InvalidOperationException("ステージに階層定義がありません。");
        var enemyStates = new List<QuestEnemyState>(firstFloor.Placements.Count);
        foreach (var placement in firstFloor.Placements)
        {
            var enemy = await enemyDefinitionRepository.GetAsync(placement.EnemyDefinitionId)
                ?? throw new KeyNotFoundException($"敵定義が見つかりません。 enemyDefinitionId={placement.EnemyDefinitionId}");
            enemyStates.Add(new QuestEnemyState(
                QuestEnemyInstanceId.New(),
                enemy.Id,
                placement.Position,
                enemy.Status.MaxHp,
                enemy.Status.MaxMp,
                isDead: false));
        }

        return (
            firstFloor.FloorNo,
            firstFloor.FloorType == FloorType.Boss,
            firstFloor.Placements.ToArray(),
            enemyStates.ToArray());
    }

    private static (int FloorNo, bool IsBoss, QuestEnemyPlacement[] Placements, QuestEnemyState[] EnemyStates)
        CreateMirrorFirstFloor(MirrorEnemyPlan plan)
    {
        var firstFloor = plan.Floors.FirstOrDefault()
            ?? throw new InvalidOperationException("名鑑の祠の敵の計画にフロアがありません。");
        return (
            firstFloor.FloorNo,
            firstFloor.IsBoss,
            firstFloor.Placements.ToArray(),
            CreateMirrorEnemyStates(plan, firstFloor));
    }

    /// <summary>名鑑の祠のフロア計画から、そのフロアの敵の初期状態（HP・MP 満タン）を作る。</summary>
    public static QuestEnemyState[] CreateMirrorEnemyStates(MirrorEnemyPlan plan, MirrorFloorPlan floor)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(floor);

        return floor.Placements
            .Select(placement =>
            {
                var enemy = plan.FindEnemy(placement.EnemyDefinitionId)
                    ?? throw new InvalidOperationException($"名鑑の祠の敵が見つかりません。 definitionId={placement.EnemyDefinitionId}");
                return new QuestEnemyState(
                    QuestEnemyInstanceId.New(),
                    enemy.DefinitionId,
                    placement.Position,
                    enemy.Status.MaxHp,
                    enemy.Status.MaxMp,
                    isDead: false);
            })
            .ToArray();
    }

    private async Task<(int FloorNo, bool IsBoss, QuestEnemyPlacement[] Placements, QuestEnemyState[] EnemyStates)>
        CreateEndlessFirstFloorAsync(QuestStageDefinition stageDefinition)
    {
        var config = stageDefinition.EndlessConfig
            ?? throw new InvalidOperationException("Endless ステージに設定がありません。");
        var templates = await endlessEnemyTemplateRepository.GetAllAsync();
        var floor = endlessFloorGenerator.GenerateFloor(config, templates, 1, Random.Shared);

        var placements = floor.Enemies.Select(x => x.Placement).ToArray();
        var enemyStates = floor.Enemies
            .Select(x => new QuestEnemyState(
                QuestEnemyInstanceId.New(),
                x.ScaledDefinition.Id,
                x.Placement.Position,
                x.ScaledDefinition.Status.MaxHp,
                x.ScaledDefinition.Status.MaxMp,
                isDead: false))
            .ToArray();

        return (floor.FloorNo, floor.IsBoss, placements, enemyStates);
    }
}
