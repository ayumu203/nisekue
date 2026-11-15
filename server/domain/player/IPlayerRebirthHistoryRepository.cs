namespace server.domain.player;

public interface IPlayerRebirthHistoryRepository
{
    Task<IReadOnlyList<PlayerRebirthStatusHistory>> GetByPlayerAsync(PlayerId playerId);

    /// <summary>プレイヤーの転生履歴をすべて削除する（デバッグのテンプレート取り込みで転生回数を 0 に戻すとき用）。</summary>
    Task DeleteByPlayerAsync(PlayerId playerId);
}
