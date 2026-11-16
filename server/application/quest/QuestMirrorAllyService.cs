using server.domain.player;
using server.domain.quest;

namespace server.application.quest;

/// <summary>名鑑の祠で、最低出撃人数に足りない分の味方をほかのプレイヤーから選ぶ。</summary>
public class QuestMirrorAllyService(IMirrorCandidateRepository candidateRepository)
{
    /// <summary>味方を選ぶときのレベル差の範囲。</summary>
    public const int AllyLevelRange = 10;

    public async Task<IReadOnlyList<Player>> PickAllyPlayersAsync(Player owner, int count)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (count <= 0)
        {
            return [];
        }

        var ids = await candidateRepository.GetAllyPlayerIdsAsync(owner.Id, owner.Level, AllyLevelRange, count);
        if (ids.Count == 0)
        {
            return [];
        }

        // 取得した順（範囲内のランダム → レベルが近い順）を保つ。
        // 味方は自動行動で技を使うので、技セット込みで読む。
        var players = await candidateRepository.GetPlayersWithMovesAsync(ids);
        var playerById = players.ToDictionary(x => x.Id);
        return ids
            .Where(playerById.ContainsKey)
            .Select(id => playerById[id])
            .Where(player => player.Id != owner.Id)
            .Take(count)
            .ToArray();
    }

    public static IReadOnlyList<QuestPlayerNpc> ToPlayerNpcs(IEnumerable<Player> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        return players
            .Select(player => new QuestPlayerNpc(player.Id, player.Name, MirrorJobRowPolicy.ResolvePreferredRow(player.Job)))
            .ToArray();
    }
}
