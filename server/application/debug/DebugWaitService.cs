using server.application.player;
using server.domain;
using server.domain.player;
using server.domain.treasuremap;
using server.domain.treasuremap.enums;

namespace server.application.debug;

/// <summary>
/// デバッグ機能で、待ち時間（クエストの再参加待ち・ペットバトルの再戦待ち・宝の地図の探索）を即時に解除する。
/// 利用可否の判定は呼び出し側（DebugEndpoints のフィルター）で行う。
/// </summary>
public class DebugWaitService(
    IPlayerMutationService playerMutationService,
    ITreasureMapExpeditionRepository treasureMapExpeditionRepository)
{
    /// <summary>クエストの再参加待ちとペットバトルの再戦待ちを解除する。</summary>
    public Task<Player> ResetCooldownsAsync(PlayerId playerId)
    {
        return playerMutationService.MutateAsync(playerId, player =>
        {
            player.SetQuestCooldownUntil(null);
            player.SetPetBattleCooldownUntil(null);
            return Task.FromResult(player);
        });
    }

    /// <summary>
    /// 探索中の宝の地図の終了時刻を now まで進める。報酬の抽選は通常の取得・受け取り処理に任せる。
    /// 探索中のものが無ければ DomainException（400）。
    /// </summary>
    public async Task<TreasureMapExpedition> CompleteTreasureMapAsync(PlayerId playerId, DateTimeOffset now)
    {
        var expedition = await treasureMapExpeditionRepository.GetCurrentByPlayerAsync(playerId);
        if (expedition is null || expedition.Status != TreasureMapExpeditionStatus.InProgress)
        {
            throw new DomainException("探索中の宝の地図がありません。");
        }

        var remaining = expedition.EndsAt - now;
        if (remaining > TimeSpan.Zero)
        {
            expedition.AdvanceTime(remaining);
            await treasureMapExpeditionRepository.SaveAsync(expedition);
        }

        return expedition;
    }
}
