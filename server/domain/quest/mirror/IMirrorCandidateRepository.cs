using server.domain.player;

namespace server.domain.quest;

/// <summary>名鑑の祠の敵・味方の候補になるプレイヤーを探す。</summary>
public interface IMirrorCandidateRepository
{
    /// <summary>
    /// （転生回数 desc, レベル desc）で上位 count 人目と同点の人までを含めたプレイヤーIDを返す。
    /// 同点の並べ替え（戦闘指数）は呼び出し側で行う。
    /// </summary>
    Task<IReadOnlyList<PlayerId>> GetTopPlayerIdsWithTiesAsync(int count);
}
