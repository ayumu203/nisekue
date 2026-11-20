using Microsoft.EntityFrameworkCore;
using server.infrastructure;

namespace server.application.player;

/// <summary>
/// プレイヤーの最終アクティブ時刻（Players.LastActiveAt）を記録する。
/// クライアントは画面表示中にハートビートを送り、この時刻をもとに /internal/active-player/count が「プレイ中」の人数を数える。
/// </summary>
public sealed class PlayerActivityService(AppDbContext db)
{
    /// <summary>この時間内に LastActiveAt が更新されたプレイヤーをアクティブとみなす。</summary>
    public static readonly TimeSpan ActiveWindow = TimeSpan.FromMinutes(5);

    /// <summary>書き込み量を抑えるため、前回の記録からこの時間が経つまでは更新しない。ActiveWindow より十分短くする。</summary>
    public static readonly TimeSpan RecordInterval = TimeSpan.FromMinutes(1);

    public async Task RecordAsync(Guid playerId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var threshold = now - RecordInterval;
        await db.Players
            .Where(p => p.Id == playerId && (p.LastActiveAt == null || p.LastActiveAt < threshold))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastActiveAt, now), cancellationToken);
    }
}
