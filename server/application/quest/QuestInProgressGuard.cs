using server.domain.player;
using server.domain.quest;

namespace server.application.quest;

/// <summary>
/// クエスト中（進行中のランに参加している間）に許可しない操作を弾く。
/// 戦闘は開始時のスナップショットで行う一方、クリア時の報酬（経験値・レベルアップ・倍率）や捕獲率は
/// その時点のプレイヤーを参照するため、クエスト中にレベル・職業・フラグを変えられると報酬を不正に得られる。
/// </summary>
public interface IQuestInProgressGuard
{
    Task<bool> IsInQuestAsync(PlayerId playerId);

    /// <exception cref="QuestInProgressException">進行中のクエストがあるとき。</exception>
    Task EnsureNotInQuestAsync(PlayerId playerId, string message);
}

public sealed class QuestInProgressGuard(IQuestRunRepository questRunRepository) : IQuestInProgressGuard
{
    public Task<bool> IsInQuestAsync(PlayerId playerId)
    {
        return questRunRepository.ExistsActiveRunByPlayerAsync(playerId);
    }

    public async Task EnsureNotInQuestAsync(PlayerId playerId, string message)
    {
        if (await IsInQuestAsync(playerId))
        {
            throw new QuestInProgressException(message);
        }
    }
}

/// <summary>
/// クエスト中のため操作できないことを表す。既存の 409（InvalidOperationException）処理にそのまま乗せるため派生させている。
/// </summary>
public sealed class QuestInProgressException(string message) : InvalidOperationException(message);

public static class QuestInProgressMessages
{
    public const string Rebirth = "クエスト中は転生できません。";
    public const string DataReset = "クエスト中はデータリセットできません。";
    public const string ChangeJob = "クエスト中は転職できません。";
    public const string UseItem = "クエスト中はアイテムを使えません。";
    public const string Training = "クエスト中は訓練できません。";
}
