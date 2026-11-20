using System.Collections.Concurrent;

namespace server.application.player;

/// <summary>
/// PlayerPresenceHub に接続中のプレイヤーをメモリ上で数える。
/// サーバーが1台構成である前提（複数台にする場合は Redis などで共有する必要がある）。
/// 再起動すると0に戻るが、クライアントが自動で再接続する。
/// </summary>
public sealed class PlayerPresenceTracker
{
    private readonly ConcurrentDictionary<string, Guid> _playerIdsByConnectionId = new();

    public void Connect(string connectionId, Guid playerId)
    {
        _playerIdsByConnectionId[connectionId] = playerId;
    }

    public void Disconnect(string connectionId)
    {
        _playerIdsByConnectionId.TryRemove(connectionId, out _);
    }

    /// <summary>接続中のプレイヤー数。同じプレイヤーが複数タブで接続していても1人と数える。</summary>
    public int CountOnlinePlayers()
    {
        return _playerIdsByConnectionId.Values.Distinct().Count();
    }
}
