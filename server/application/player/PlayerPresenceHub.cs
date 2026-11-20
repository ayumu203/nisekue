using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace server.application.player;

/// <summary>
/// プレイ中のプレイヤーを数えるためのハブ。クライアントは画面を表示している間つなぎっぱなしにする。
/// メソッドは持たず、接続と切断だけを PlayerPresenceTracker に記録する。
/// </summary>
[Authorize]
public class PlayerPresenceHub(PlayerPresenceTracker tracker) : Hub
{
    public override Task OnConnectedAsync()
    {
        var subject = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
        if (Guid.TryParse(subject, out var playerId))
        {
            tracker.Connect(Context.ConnectionId, playerId);
        }

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        tracker.Disconnect(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
