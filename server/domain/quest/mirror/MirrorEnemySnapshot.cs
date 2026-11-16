using server.domain.move;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

/// <summary>
/// 名鑑の祠でラン開始時に固定した、敵として出るプレイヤーのスナップショット。
/// ラン中に元のプレイヤーが変わっても影響しない。
/// </summary>
public sealed class MirrorEnemySnapshot(
    QuestEnemyDefinitionId definitionId,
    PlayerId sourcePlayerId,
    string name,
    string imagePath,
    Job job,
    int level,
    int rebirthCount,
    Status status,
    IEnumerable<MoveId> moveIds)
{
    private readonly MoveId[] moveIds = moveIds?.ToArray() ?? throw new ArgumentNullException(nameof(moveIds));

    public QuestEnemyDefinitionId DefinitionId { get; } = definitionId;
    public PlayerId SourcePlayerId { get; } = sourcePlayerId;
    public string Name { get; } = name;
    public string ImagePath { get; } = imagePath;
    public Job Job { get; } = job;
    public int Level { get; } = level;
    public int RebirthCount { get; } = rebirthCount;
    public Status Status { get; } = status ?? throw new ArgumentNullException(nameof(status));
    public IReadOnlyList<MoveId> MoveIds => moveIds;

    public QuestEnemyDefinition ToEnemyDefinition()
    {
        // AI タイプは名鑑の祠では使わない（行動は職業から決める）。既定値として Aggressive を入れておく。
        return new QuestEnemyDefinition(DefinitionId, Name, Level, Status, ImagePath, EnemyAiType.Aggressive, moveIds);
    }
}
