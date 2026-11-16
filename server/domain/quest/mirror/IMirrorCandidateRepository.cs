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

    /// <summary>
    /// 指定したプレイヤーを、技セット込みでまとめて返す。
    /// IPlayerRepository.GetPlayersAsync は技を読まないので、敵・味方の組み立てにはこちらを使う。
    /// </summary>
    Task<IReadOnlyList<Player>> GetPlayersWithMovesAsync(IEnumerable<PlayerId> ids);

    /// <summary>
    /// 味方として補充するプレイヤーIDを返す。excludedPlayerId 以外で、レベル差 levelRange 以内からランダムに選び、
    /// 足りなければレベル差が小さい順に足す。
    /// </summary>
    Task<IReadOnlyList<PlayerId>> GetAllyPlayerIdsAsync(PlayerId excludedPlayerId, int level, int levelRange, int count);
}
