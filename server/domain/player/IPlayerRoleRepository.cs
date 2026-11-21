namespace server.domain.player;

/// <summary>
/// プレイヤーの権限を読み書きする。ゲームの API からは変更しない。
/// Admin は Supabase から手動で設定し、Poster は管理者用ワークフロー（/internal/players/{id}/role）で付け外しする。
/// </summary>
public interface IPlayerRoleRepository
{
    /// <summary>権限を返す。プレイヤーが存在しない、権限が未設定、または未知の値の場合は null。</summary>
    Task<PlayerRole?> FindRoleAsync(PlayerId id);

    /// <summary>権限を設定する（null で解除）。プレイヤーが存在しなければ false。</summary>
    Task<bool> SetRoleAsync(PlayerId id, PlayerRole? role);
}
