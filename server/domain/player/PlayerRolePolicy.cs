using server.shared.constants.player;

namespace server.domain.player;

/// <summary>
/// 権限ごとにできることを判定する。
/// </summary>
public static class PlayerRolePolicy
{
    /// <summary>Poster を付与するのに必要なレベル。</summary>
    public const int PosterRequiredLevel = PlayerConstants.MaxLevel;

    /// <summary>Poster を付与するのに必要な転生回数（レベル条件を満たさない場合）。</summary>
    public const int PosterRequiredRebirthCount = 1;

    /// <summary>スレッドへ画像を投稿できるか。Admin と Poster のみ。</summary>
    public static bool CanPostThreadImage(PlayerRole? role) =>
        role is PlayerRole.Admin or PlayerRole.Poster;

    /// <summary>Poster を付与できる条件（レベル100以上、または転生1回以上）を満たしているか。</summary>
    public static bool IsEligibleForPoster(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return player.Level >= PosterRequiredLevel || player.RebirthCount >= PosterRequiredRebirthCount;
    }
}
