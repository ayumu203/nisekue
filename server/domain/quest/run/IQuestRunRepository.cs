using server.domain.player;

namespace server.domain.quest;

public interface IQuestRunRepository
{
    Task<QuestRun?> GetAsync(QuestRunId id);

    // ターン解決ホットパス向けの軽量ロード。表示専用で毎ターン数KBになる last_turn_results_json を
    // 読み込まずにランを取得する（DBエグレス削減）。表示用途にはこのランを流さないこと。
    Task<QuestRun?> GetForResolutionAsync(QuestRunId id);
    Task<QuestRun?> GetByRoomIdAsync(QuestRoomId roomId);
    Task<QuestRun?> GetActiveByPlayerAsync(PlayerId playerId);
    Task<bool> ExistsActiveRunByPlayerAsync(PlayerId playerId);
    Task<IReadOnlyList<QuestRun>> ListExpiredAsync(DateTimeOffset now);
    Task SaveAsync(QuestRun run);
    Task SaveChatMessagesAsync(QuestRun run);
}
