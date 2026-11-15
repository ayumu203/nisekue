using server.domain.battle;
using server.domain.battle.enums;
using server.domain.player;
using server.domain.quest.enums;

namespace server.domain.quest;

/// <summary>
/// 名鑑の祠の敵の組み立て（DB に触らない純粋なロジック）。
/// docs/quest/06_名鑑の祠設計.md の「2. 敵の選出」を実装する。
/// </summary>
public sealed class MirrorEnemyPlanBuilder
{
    /// <summary>敵として選ぶ上位の人数。</summary>
    public const int TopCount = 20;

    /// <summary>通常フロア1つあたりの敵の数。</summary>
    public const int EnemiesPerNormalFloor = 5;

    /// <summary>ボスフロアで自分と一緒に出る上位の人数。</summary>
    public const int BossCompanionCount = 3;

    /// <summary>通常フロアでの1人あたりの最大登場回数（ボスフロアの分を1回残す）。</summary>
    public const int MaxNormalAppearances = 2;

    /// <summary>仮の敵IDの開始値。CSV の敵ID・エンドレスのテンプレIDと衝突しない範囲。</summary>
    public const int FirstDefinitionId = 1_000_001;

    private static readonly BattleRow[] Rows = [BattleRow.Front, BattleRow.Middle, BattleRow.Back];
    private static readonly BattleColumn[] Columns = [BattleColumn.Left, BattleColumn.Right];

    /// <summary>
    /// 上位プレイヤー（同点分を含めて多めに渡してよい）と自分から、全フロアの敵配置を作る。
    /// </summary>
    /// <param name="candidates">敵の候補。自分が含まれていてもよい。</param>
    /// <param name="self">挑戦するプレイヤー自身。ボスフロアに必ず出る。</param>
    /// <param name="floors">ステージのフロア定義（Normal / Boss）。</param>
    /// <param name="excludedMoveIds">敵に使わせない技（即死技など）。</param>
    public MirrorEnemyPlan Build(
        IReadOnlyList<MirrorCandidate> candidates,
        MirrorCandidate self,
        IReadOnlyList<QuestFloorDefinition> floors,
        IReadOnlySet<int> excludedMoveIds)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(self);
        ArgumentNullException.ThrowIfNull(floors);
        ArgumentNullException.ThrowIfNull(excludedMoveIds);
        if (floors.Count == 0)
        {
            throw new ArgumentException("フロア定義が空です。", nameof(floors));
        }

        var ranked = RankTop(candidates);
        var rankIndex = ranked
            .Select((candidate, index) => (candidate.PlayerId, index))
            .ToDictionary(x => x.PlayerId, x => x.index);

        // 下位から強くなるように、順位の低い順のリストを作る。
        var weakestFirst = ranked.AsEnumerable().Reverse().ToList();
        if (weakestFirst.Count == 0)
        {
            weakestFirst.Add(self);
        }

        var normalAppearances = new Dictionary<PlayerId, int>();
        var pointer = 0;
        var membersByFloor = new List<(QuestFloorDefinition Floor, IReadOnlyList<MirrorCandidate> Members)>();

        foreach (var floor in floors.OrderBy(x => x.FloorNo))
        {
            if (floor.FloorType == FloorType.Boss)
            {
                var bossMembers = new List<MirrorCandidate> { self };
                bossMembers.AddRange(ranked
                    .Where(x => x.PlayerId != self.PlayerId)
                    .Take(BossCompanionCount));
                membersByFloor.Add((floor, bossMembers));
                continue;
            }

            var members = PickNormalFloorMembers(weakestFirst, normalAppearances, ref pointer);
            membersByFloor.Add((floor, members));
        }

        var snapshots = new Dictionary<PlayerId, MirrorEnemySnapshot>();
        var floorPlans = new List<MirrorFloorPlan>(membersByFloor.Count);
        foreach (var (floor, members) in membersByFloor)
        {
            foreach (var member in members)
            {
                if (!snapshots.ContainsKey(member.PlayerId))
                {
                    snapshots[member.PlayerId] = CreateSnapshot(member, FirstDefinitionId + snapshots.Count, excludedMoveIds);
                }
            }

            // 順位の高い順に希望列へ入れる。ボスフロアは自分を先頭にする。
            var ordered = members
                .Select((member, index) => (member, index))
                .OrderBy(x => floor.FloorType == FloorType.Boss && x.member.PlayerId == self.PlayerId ? 0 : 1)
                .ThenBy(x => rankIndex.TryGetValue(x.member.PlayerId, out var rank) ? rank : int.MaxValue)
                .ThenBy(x => x.index)
                .Select(x => x.member)
                .ToArray();
            var positions = AssignPositions(ordered);
            var placements = ordered
                .Select((member, index) => new QuestEnemyPlacement(index + 1, snapshots[member.PlayerId].DefinitionId, positions[index]))
                .ToArray();
            floorPlans.Add(new MirrorFloorPlan(floor.FloorNo, floor.FloorType == FloorType.Boss, placements));
        }

        return new MirrorEnemyPlan(snapshots.Values, floorPlans);
    }

    /// <summary>転生回数→レベル→戦闘指数→プレイヤーID の順で上位を取る。</summary>
    public static IReadOnlyList<MirrorCandidate> RankTop(IEnumerable<MirrorCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .GroupBy(x => x.PlayerId)
            .Select(x => x.First())
            .OrderByDescending(x => x.RebirthCount)
            .ThenByDescending(x => x.Level)
            .ThenByDescending(x => x.CombatIndex)
            .ThenBy(x => x.PlayerId.Value)
            .Take(TopCount)
            .ToArray();
    }

    /// <summary>
    /// 職業の希望列に、渡された順（順位の高い順）で入れる。
    /// 希望列が埋まっていたら後ろの列、後衛まで埋まっていたら前の列の空きへ。
    /// </summary>
    public static IReadOnlyList<BattlePosition> AssignPositions(IReadOnlyList<MirrorCandidate> orderedMembers)
    {
        ArgumentNullException.ThrowIfNull(orderedMembers);
        if (orderedMembers.Count > Rows.Length * Columns.Length)
        {
            throw new ArgumentException("1フロアに置ける敵の数を超えています。", nameof(orderedMembers));
        }

        var occupied = new HashSet<BattlePosition>();
        var positions = new List<BattlePosition>(orderedMembers.Count);
        foreach (var member in orderedMembers)
        {
            var preferredRowIndex = Array.IndexOf(Rows, MirrorJobRowPolicy.ResolvePreferredRow(member.Job));
            var rowOrder = Rows.Skip(preferredRowIndex)
                .Concat(Rows.Take(preferredRowIndex).Reverse());

            var position = rowOrder
                .SelectMany(row => Columns.Select(column => new BattlePosition(row, column)))
                .First(candidate => !occupied.Contains(candidate));
            occupied.Add(position);
            positions.Add(position);
        }

        return positions;
    }

    private static IReadOnlyList<MirrorCandidate> PickNormalFloorMembers(
        IReadOnlyList<MirrorCandidate> weakestFirst,
        Dictionary<PlayerId, int> normalAppearances,
        ref int pointer)
    {
        var members = new List<MirrorCandidate>(EnemiesPerNormalFloor);
        var picked = new HashSet<PlayerId>();

        // 1人ずつずらしながら、同じフロアに同じ人を入れず、通常フロアの登場上限を守って埋める。
        for (var checkedCount = 0; checkedCount < weakestFirst.Count && members.Count < EnemiesPerNormalFloor; checkedCount++)
        {
            var candidate = weakestFirst[pointer % weakestFirst.Count];
            pointer++;
            if (picked.Contains(candidate.PlayerId) ||
                normalAppearances.GetValueOrDefault(candidate.PlayerId) >= MaxNormalAppearances)
            {
                continue;
            }

            members.Add(candidate);
            picked.Add(candidate.PlayerId);
            normalAppearances[candidate.PlayerId] = normalAppearances.GetValueOrDefault(candidate.PlayerId) + 1;
        }

        // 人数が少なく上限で1体も置けないときは、上限を外して1体だけ置く（敵0体のフロアは作らない）。
        if (members.Count == 0)
        {
            var fallback = weakestFirst[pointer % weakestFirst.Count];
            pointer++;
            members.Add(fallback);
            normalAppearances[fallback.PlayerId] = normalAppearances.GetValueOrDefault(fallback.PlayerId) + 1;
        }

        return members;
    }

    private static MirrorEnemySnapshot CreateSnapshot(MirrorCandidate candidate, int definitionId, IReadOnlySet<int> excludedMoveIds)
    {
        return new MirrorEnemySnapshot(
            new QuestEnemyDefinitionId(definitionId),
            candidate.PlayerId,
            candidate.Name,
            candidate.ImagePath,
            candidate.Job,
            candidate.Level,
            candidate.RebirthCount,
            candidate.EffectiveStatus,
            candidate.MoveIds.Where(x => !excludedMoveIds.Contains(x.Id)).ToArray());
    }
}
