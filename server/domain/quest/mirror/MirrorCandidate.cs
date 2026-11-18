using server.domain.move;
using server.domain.player;

namespace server.domain.quest;

/// <summary>
/// 名鑑の祠で敵（または味方）になりうるプレイヤー。
/// ステータスは装備込みの実効ステータス、戦闘指数もそこから計算済みのもの。
/// </summary>
public sealed class MirrorCandidate(
    PlayerId playerId,
    string name,
    string imagePath,
    Job job,
    int level,
    int rebirthCount,
    Status effectiveStatus,
    int combatIndex,
    IEnumerable<MoveId> moveIds)
{
    private readonly MoveId[] moveIds = moveIds?.ToArray() ?? throw new ArgumentNullException(nameof(moveIds));

    public PlayerId PlayerId { get; } = playerId;
    public string Name { get; } = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("名前は必須です。", nameof(name)) : name.Trim();
    public string ImagePath { get; } = string.IsNullOrWhiteSpace(imagePath) ? throw new ArgumentException("画像は必須です。", nameof(imagePath)) : imagePath.Trim();
    public Job Job { get; } = job;
    public int Level { get; } = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level), "レベルは1以上である必要があります。");
    public int RebirthCount { get; } = rebirthCount >= 0 ? rebirthCount : throw new ArgumentOutOfRangeException(nameof(rebirthCount), "転生回数は0以上である必要があります。");
    public Status EffectiveStatus { get; } = effectiveStatus ?? throw new ArgumentNullException(nameof(effectiveStatus));
    public int CombatIndex { get; } = combatIndex;
    public IReadOnlyList<MoveId> MoveIds => moveIds;
}
