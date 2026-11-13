using server.domain.player;

namespace server.domain.treasuremap;

public interface ITreasureMapExpeditionRepository
{
    Task<TreasureMapExpedition?> GetAsync(TreasureMapExpeditionId id);
    Task<TreasureMapExpedition?> GetCurrentByPlayerAsync(PlayerId playerId);
    Task<IReadOnlyList<TreasureMapExpedition>> ListByPlayerAsync(PlayerId playerId);
    Task SaveAsync(TreasureMapExpedition expedition);

    /// <summary>
    /// 探索中かつ終了時刻が endsAt より後の探索だけ、終了時刻を endsAt に縮める（デバッグ用）。
    /// 他の列は更新しないので、受け取り処理と競合しても状態を上書きしない。更新したら true。
    /// </summary>
    Task<bool> TryShortenEndsAtAsync(TreasureMapExpeditionId id, DateTimeOffset endsAt);
}
