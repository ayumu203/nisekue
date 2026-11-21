namespace server.domain.player;

/// <summary>
/// プレイヤーの権限。権限を持たない一般プレイヤーは null で表す。
/// </summary>
public enum PlayerRole
{
    /// <summary>デバッグ機能を利用できる管理者。スレッドへの画像投稿もできる。</summary>
    Admin,

    /// <summary>スレッドへ画像を投稿できるプレイヤー。管理者用ワークフローで付け外しする。</summary>
    Poster
}
