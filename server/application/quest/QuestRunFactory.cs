using server.domain.quest;

namespace server.application.quest;

public class QuestRunFactory(IQuestEnemyDefinitionRepository enemyDefinitionRepository)
{
    private static readonly TimeSpan InitialTurnDeadline = TimeSpan.FromSeconds(60);

    public async Task<QuestRun> Create(
        QuestRoom room,
        QuestStageDefinition stageDefinition,
        IEnumerable<QuestRunPartyMemberSnapshot> snapshots,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(stageDefinition);
        ArgumentNullException.ThrowIfNull(snapshots);

        var snapshotArray = snapshots.ToArray();
        if (snapshotArray.Length == 0)
        {
            throw new InvalidOperationException("開始時スナップショットが空です。");
        }

        var firstFloor = stageDefinition.Floors.OrderBy(x => x.FloorNo).FirstOrDefault()
            ?? throw new InvalidOperationException("ステージに階層定義がありません。");
        var enemyDefinitions = new List<QuestEnemyDefinition>(firstFloor.Placements.Count);
        foreach (var placement in firstFloor.Placements)
        {
            var enemy = await enemyDefinitionRepository.GetAsync(placement.EnemyDefinitionId)
                ?? throw new KeyNotFoundException($"敵定義が見つかりません。 enemyDefinitionId={placement.EnemyDefinitionId}");
            enemyDefinitions.Add(enemy);
        }

        var partyStates = snapshotArray
            .Select(x => new QuestRunPartyMemberState(
                x.ParticipantId,
                x.BaseStatus.MaxHp,
                x.BaseStatus.MaxMp,
                isDead: false,
                canActFromTurn: 1,
                x.InitialActionMode))
            .ToArray();

        var enemyStates = firstFloor.Placements
            .Zip(enemyDefinitions, (placement, enemy) => new QuestEnemyState(
                QuestEnemyInstanceId.New(),
                enemy.Id,
                placement.Position,
                enemy.Status.MaxHp,
                enemy.Status.MaxMp,
                isDead: false))
            .ToArray();

        return new QuestRun(
            QuestRunId.New(),
            room.Id,
            stageDefinition.Id,
            snapshotArray,
            new QuestFloorState(firstFloor.FloorNo, firstFloor.FloorType == server.domain.quest.enums.FloorType.Boss, firstFloor.Placements),
            new QuestBattleState(partyStates, enemyStates),
            new QuestTurnState(1, now.Add(InitialTurnDeadline)),
            new QuestTrapCollection(),
            new QuestRewardAccumulator(),
            [],
            now);
    }
}
