namespace server.domain.player;

/// <summary>
/// プレイヤーの権限を読み取る。権限はゲームの API からは変更せず、Supabase から手動で設定する。
/// </summary>
public interface IPlayerRoleRepository
{
    /// <summary>権限を返す。プレイヤーが存在しない、権限が未設定、または未知の値の場合は null。</summary>
    Task<PlayerRole?> FindRoleAsync(PlayerId id);
}
