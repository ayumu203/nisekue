using server.domain.player;

namespace server.application.debug;

/// <summary>
/// プレイヤーがデバッグ機能を使えるかを判定する。
/// - 設定 Debug:Enabled が false なら誰も使えない
/// - 設定 Debug:AlwaysEnabled が true なら誰でも使える（ローカル）
/// - それ以外は role が Admin のプレイヤーだけ使える
/// </summary>
public class DebugAccessService(DebugSettings settings, IPlayerRoleRepository playerRoleRepository)
{
    public async Task<bool> CanUseAsync(PlayerId playerId)
    {
        if (!settings.Enabled)
        {
            return false;
        }

        if (settings.AlwaysEnabled)
        {
            return true;
        }

        return await playerRoleRepository.FindRoleAsync(playerId) == PlayerRole.Admin;
    }
}
