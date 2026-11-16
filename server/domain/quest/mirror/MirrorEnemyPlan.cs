namespace server.domain.quest;

/// <summary>名鑑の祠の1フロア分の敵配置。</summary>
public sealed class MirrorFloorPlan(int floorNo, bool isBoss, IEnumerable<QuestEnemyPlacement> placements)
{
    private readonly QuestEnemyPlacement[] placements = placements?.ToArray() ?? throw new ArgumentNullException(nameof(placements));

    public int FloorNo { get; } = floorNo > 0 ? floorNo : throw new ArgumentOutOfRangeException(nameof(floorNo), "フロア番号は1以上である必要があります。");
    public bool IsBoss { get; } = isBoss;
    public IReadOnlyList<QuestEnemyPlacement> Placements => placements;
}

/// <summary>
/// 名鑑の祠でラン開始時に確定した、敵のスナップショットと全フロアの配置。
/// </summary>
public sealed class MirrorEnemyPlan
{
    private readonly MirrorEnemySnapshot[] enemies;
    private readonly MirrorFloorPlan[] floors;

    public MirrorEnemyPlan(IEnumerable<MirrorEnemySnapshot> enemies, IEnumerable<MirrorFloorPlan> floors)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(floors);

        this.enemies = enemies.ToArray();
        this.floors = floors.OrderBy(x => x.FloorNo).ToArray();

        var definitionIds = this.enemies.Select(x => x.DefinitionId).ToHashSet();
        if (definitionIds.Count != this.enemies.Length)
        {
            throw new ArgumentException("敵の仮IDが重複しています。", nameof(enemies));
        }

        var missing = this.floors
            .SelectMany(x => x.Placements)
            .FirstOrDefault(x => !definitionIds.Contains(x.EnemyDefinitionId));
        if (missing is not null)
        {
            throw new ArgumentException($"配置に存在しない敵の仮IDがあります。 definitionId={missing.EnemyDefinitionId}", nameof(floors));
        }
    }

    public IReadOnlyList<MirrorEnemySnapshot> Enemies => enemies;
    public IReadOnlyList<MirrorFloorPlan> Floors => floors;

    public MirrorFloorPlan GetFloor(int floorNo)
    {
        return floors.FirstOrDefault(x => x.FloorNo == floorNo)
            ?? throw new InvalidOperationException($"名鑑の祠のフロア計画が見つかりません。 floorNo={floorNo}");
    }

    public MirrorEnemySnapshot? FindEnemy(QuestEnemyDefinitionId definitionId)
    {
        return enemies.FirstOrDefault(x => x.DefinitionId == definitionId);
    }

    public IReadOnlyDictionary<QuestEnemyDefinitionId, QuestEnemyDefinition> BuildEnemyDefinitions()
    {
        return enemies.ToDictionary(x => x.DefinitionId, x => x.ToEnemyDefinition());
    }
}
