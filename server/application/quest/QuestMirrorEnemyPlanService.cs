using server.domain.move;
using server.domain.move.enums;
using server.domain.player;
using server.domain.quest;
using server.shared.constants.player;

namespace server.application.quest;

/// <summary>
/// 名鑑の祠のラン開始時に、敵として出すプレイヤーを集めて全フロアの敵配置を確定する。
/// </summary>
public class QuestMirrorEnemyPlanService(
    IMirrorCandidateRepository candidateRepository,
    IPlayerEquipmentRepository playerEquipmentRepository,
    IEquipmentRepository equipmentRepository,
    IMoveRepository moveRepository,
    EquipmentStatusResolver equipmentStatusResolver,
    CombatIndexCalculator combatIndexCalculator)
{
    private const string CharacterImageDirectory = "image/character/";

    public async Task<MirrorEnemyPlan> CreateAsync(QuestStageDefinition stage, PlayerId selfId)
    {
        ArgumentNullException.ThrowIfNull(stage);
        if (!stage.IsMirror)
        {
            throw new InvalidOperationException($"名鑑の祠ではないステージです。 stageId={stage.Id.Value}");
        }

        var config = stage.MirrorConfig
            ?? throw new InvalidOperationException($"名鑑の祠の調整値（mirror_configs）がありません。 stageId={stage.Id.Value}");
        var topIds = await candidateRepository.GetTopPlayerIdsWithTiesAsync(config.TopPlayerCount);
        var playerIds = topIds.Append(selfId).Distinct().ToArray();
        var players = await candidateRepository.GetPlayersWithMovesAsync(playerIds);
        var equipments = await equipmentRepository.GetAllAsync();

        var candidates = new List<MirrorCandidate>(players.Count);
        foreach (var player in players)
        {
            var equipped = await playerEquipmentRepository.GetEquippedByPlayerAsync(player.Id);
            candidates.Add(ToCandidate(player, equipped, equipments));
        }

        var self = candidates.FirstOrDefault(x => x.PlayerId == selfId)
            ?? throw new KeyNotFoundException($"プレイヤーが見つかりません。 playerId={selfId}");
        var topIdSet = topIds.ToHashSet();
        var topCandidates = candidates.Where(x => topIdSet.Contains(x.PlayerId)).ToArray();
        var excludedMoveIds = await GetExcludedMoveIdsAsync();

        return new MirrorEnemyPlanBuilder(config).Build(topCandidates, self, stage.Floors, excludedMoveIds);
    }

    private MirrorCandidate ToCandidate(Player player, IReadOnlyList<PlayerEquipment> equipped, IReadOnlyList<Equipment> equipments)
    {
        var effectiveStatus = equipmentStatusResolver.BuildEffectiveStatus(player.Status, player.Job, equipped, equipments);
        return new MirrorCandidate(
            player.Id,
            player.Name,
            ResolveImagePath(player.ImagePath),
            player.Job,
            player.Level,
            player.RebirthCount,
            effectiveStatus,
            combatIndexCalculator.Calculate(effectiveStatus),
            player.MoveSet.GetLearnedMoveIds());
    }

    /// <summary>敵側の画像は public 配下のパスで返すので、キャラ画像のディレクトリを付ける。</summary>
    internal static string ResolveImagePath(string? imagePath)
    {
        var normalized = string.IsNullOrWhiteSpace(imagePath)
            ? PlayerImageCatalog.DefaultFileName
            : imagePath.Trim().TrimStart('/');

        return normalized.StartsWith("image/", StringComparison.Ordinal)
            ? normalized
            : CharacterImageDirectory + normalized;
    }

    /// <summary>敵に使わせない技（即死技）。</summary>
    private async Task<IReadOnlySet<int>> GetExcludedMoveIdsAsync()
    {
        var moves = await moveRepository.GetAllMovesAsync();
        return moves
            .Where(move => move.Effects.Any(effect =>
                effect.EffectType == MoveEffectType.Ailment &&
                effect.Ailment?.AilmentType == AilmentType.InstantDeath))
            .Select(move => move.Id.Id)
            .ToHashSet();
    }
}
