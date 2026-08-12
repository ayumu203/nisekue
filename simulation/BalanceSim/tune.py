#!/usr/bin/env python3
"""BalanceSim の実測JSONから enemies.csv の次候補を書き出す簿記スクリプト。

戦闘計算は行わない(ダメージ式の逆算のみ)。判断材料はハーネスの実測値:
  - 敵ごとの「味方への1行動ダメージ」標本(相手の守備/知力込み) → 攻撃ステータスを閉形式で逆算
      物理: damage = raw - 守備 (rawは攻撃ステに線形) → raw* = maxHp/目標発数 + 守備
      知力: damage = raw·ap/(ap+相手知力) → ap* を2次方程式で解く
  - フロアごとの実測パーティDPS(クリア済フロアは総HP÷ターン=オーバーキルなし) → HP予算按分

調整対象: max_hp / ダメージ技が参照するステータス / speed(初回のみ1/2)。
攻撃に使われないステータスは触らない(知力=魔法防御の設計を維持)。
"""
import csv, json, math, statistics, collections, argparse, os, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "server", "resources")

BOSS_TOL_TARGET = 6.0    # ボス: 被弾6発(設計意図)
NORMAL_TOL_TARGET = 8.0  # 通常: ボスよりゆるく(ハーネス側仮定)
NORMAL_TURNS = 5.0
BOSS_TURNS = 15.0
ATK_CLAMP = (0.2, 5.0)   # 閉形式解なので広め
HP_CLAMP = (0.4, 2.5)


def load_csv(path):
    with open(path, encoding="utf-8") as f:
        return list(csv.DictReader(f))


def enemy_attack_stats():
    """敵ごとに、ダメージ技が参照するステータス列名の集合(実装のフォールバック規則に従う)。"""
    moves = {int(r["move_id"]): r for r in load_csv(os.path.join(RES, "move", "move_master.csv"))}
    effects = collections.defaultdict(list)
    for r in load_csv(os.path.join(RES, "move", "move_effects.csv")):
        effects[int(r["move_id"])].append(r)
    # 通常攻撃(BattleActionResolver.ResolveNormalAttack)は常に Strength 参照なので全敵が対象。
    used = collections.defaultdict(lambda: {"strength"})
    for r in load_csv(os.path.join(RES, "quest", "enemies.csv")):
        used[int(r["id"])].add("strength")
    for r in load_csv(os.path.join(RES, "quest", "enemy_moves.csv")):
        eid = int(r["enemy_definition_id"])
        for mid in (int(x) for x in r["move_ids"].split("|")):
            mv = moves.get(mid)
            if not mv:
                continue
            for e in effects.get(mid, []):
                if e["effect_type"] != "Damage":
                    continue
                if not e["attack_stat"]:
                    # ResolveAttackStat: 未指定の Attack 技は「知力と力の高い方」を実行時に選ぶため
                    # 片方だけ下げても もう片方へ切り替わるだけ。両方を調整対象にする。
                    used[eid] |= {"strength", "intelligence"} if mv["move_category"] == "Attack" else {"intelligence"}
                    continue
                col = {"Strength": "strength", "Intelligence": "intelligence", "Defense": "defense",
                       "Speed": "speed", "Luck": "luck",
                       "StrengthIntelligence": "strength"}.get(e["attack_stat"])
                if col:
                    used[eid].add(col)
    return used


def solve_physical(samples, tol_target):
    """factor = (D* + 守備) / (raw実測)。raw = D + 守備(D>1)。D==1 は raw ≤ 守備+1 の上界扱い。"""
    fs = []
    for d, hp, dfn, _ in samples:
        d_star = max(1.0, hp / tol_target)
        raw = d + dfn if d > 1 else dfn + 1
        fs.append((d_star + dfn) / raw)
    return statistics.median(fs)


def solve_intelligence(samples, tol_target, ap):
    """damage = r·ap²/(ap+itl) から r を推定し、目標 damage* を満たす ap* を解く。"""
    fs = []
    for d, hp, _, itl in samples:
        d_star = max(1.0, hp / tol_target)
        r = d * (ap + itl) / (ap * ap) if ap > 0 else 0
        if r <= 0:
            continue
        ap_star = (d_star + math.sqrt(d_star * d_star + 4 * r * d_star * itl)) / (2 * r)
        fs.append(ap_star / ap)
    return statistics.median(fs) if fs else 1.0


def feedback(args):
    """ステージ単位の実測クリア率に直接フィードバックする制御ループ。

    中間指標(被弾発数/ターン数)への個別最適化は、5フロア消耗戦での攻撃・回復収支を
    表現できず収束しなかったため、受け入れ基準そのもの(クリア率)を主目標にする。
    敵同士の相対バランスはステージ一括係数なので保たれる。
    """
    trials = json.load(open(args.json_path))
    enemies_path = os.path.join(RES, "quest", "enemies.csv")
    rows = load_csv(enemies_path)
    by_id = {int(r["id"]): r for r in rows}
    spawns = collections.defaultdict(list)
    for r in load_csv(os.path.join(RES, "quest", "floor_enemy_spawns.csv")):
        spawns[(int(r["stage_id"]), int(r["floor_no"]))].append(int(r["enemy_definition_id"]))
    boss_floor = {}
    for r in load_csv(os.path.join(RES, "quest", "stage_floors.csv")):
        boss_floor[(int(r["stage_id"]), int(r["floor_no"]))] = r["floor_type"] == "Boss"
    atk_stats = enemy_attack_stats()
    head_text = subprocess.run(["git", "-C", ROOT, "show", "HEAD:server/resources/quest/enemies.csv"],
                               capture_output=True, text=True, check=True).stdout
    head_defense = {int(r["id"]): int(r["defense"]) for r in csv.DictReader(head_text.splitlines())}

    agg = collections.defaultdict(lambda: {"n": 0, "c": 0, "nt": [], "bt": [], "tol": []})
    for t in trials:
        a = agg[t["StageId"]]
        a["n"] += 1
        if t["Outcome"] == "Succeeded":
            a["c"] += 1
        a["nt"] += t["NormalFloorTurns"]
        a["bt"] += t["BossFloorTurns"]
        a["tol"] += t["BossHitTolerances"]

    # フロア単位のターン分布。ステージ集約だと「大半は1Tで溶けるが特定フロアだけ膠着」を
    # 打ち消し合って見逃すため、HPはフロアごとに制御する。未クリアのフロアも標本に含める。
    floor_turns = collections.defaultdict(list)
    for t in trials:
        for f in t["Floors"]:
            floor_turns[(t["StageId"], f["FloorNo"])].append(f["Turns"] if f["Cleared"] else max(f["Turns"], 60))

    # ステージ→(normal敵集合, boss専用敵集合)。複数ステージ出現の敵は最初のステージ基準。
    stage_normal = collections.defaultdict(set)
    stage_boss = collections.defaultdict(set)
    for (sid, fno), ids in spawns.items():
        for i in ids:
            (stage_boss if boss_floor.get((sid, fno)) else stage_normal)[sid].add(i)
    primary_stage = {}
    for sid in sorted({k[0] for k in spawns}):
        for i in stage_normal[sid] | stage_boss[sid]:
            primary_stage.setdefault(i, sid)

    changed = collections.Counter()
    for sid in sorted(agg):
        a = agg[sid]
        clear = a["c"] / a["n"] if a["n"] else 0
        # 平均だと「倒しきれず数百ターン膠着したフロア」1件に引きずられ、
        # 実際は1ターンで溶けている大多数を見誤るため中央値で見る(膠着はクリア率側で拾う)。
        nt = statistics.median(a["nt"]) if a["nt"] else 0
        bt = statistics.median(a["bt"]) if a["bt"] else 0
        stall = sum(1 for x in a["nt"] + a["bt"] if x >= 60) / max(1, len(a["nt"]) + len(a["bt"]))
        tol = statistics.median(a["tol"]) if a["tol"] else float("nan")

        atk_f = 1.0
        tc = args.target_clear
        if clear < tc - 0.35:
            atk_f = 0.6
        elif clear < tc - 0.05:
            atk_f = 0.8
        elif clear > tc + 0.1:
            atk_f = 1.3
        elif not math.isnan(tol) and tol < 5:
            atk_f = 0.85
        elif not math.isnan(tol) and tol > BOSS_TOL_TARGET * 1.5:
            # 敵の攻撃が薄すぎて「殴り合い」になっていない。クリア率が高い間は上げる。
            atk_f = 1.3
        # 膠着(60T超)は「HPが高い」ではなく「守備が高くパーティの物理が1しか通らない」ことで起きる。
        # HPを下限まで削っても解消しないので、守備そのものを緩める。
        def_f = 0.7 if stall >= 0.05 else 1.0

        # HPはフロア単位。p75 を見るのは、大半のフロアが即溶けでも一部が膠着する分布を潰すため。
        hp_factor = {}
        for fno in sorted(n for (s, n) in floor_turns if s == sid):
            v = sorted(floor_turns[(sid, fno)])
            p75 = v[min(len(v) - 1, int(len(v) * 0.75))]
            target = BOSS_TURNS if boss_floor.get((sid, fno)) else NORMAL_TURNS
            f = 1.0
            if p75 > target * 1.3:
                f = max(0.25, target / p75)
            elif clear >= tc - 0.2 and 0 < p75 < target * 0.7:
                f = min(1.4, target / p75)
            for i in spawns[(sid, fno)]:
                hp_factor.setdefault(i, []).append(f)
        # 未到達フロア(標本なし)には同ステージの実測係数の最小値を当てる。到達できないまま据え置くと永久に測れない。
        seen = {f for fs in hp_factor.values() for f in fs}
        fallback = min(seen) if seen else 1.0
        for i in stage_normal[sid] | stage_boss[sid]:
            hp_factor.setdefault(i, [fallback])

        # ゲイン: 1.0 未満で係数を1へ寄せる。終盤の振動を抑えて落ち着かせるための減衰。
        g = args.gain
        atk_f = 1 + (atk_f - 1) * g
        def_f = 1 + (def_f - 1) * g
        hp_factor = {i: [1 + (f - 1) * g for f in v] for i, v in hp_factor.items()}

        print(f"stage{sid:>2}: clear{clear * 100:4.0f}% 通常{nt:5.1f}T ボス{bt:5.1f}T 被弾{tol:6.1f}発"
              f" 膠着{stall * 100:3.0f}% -> atk×{atk_f:.2f} def×{def_f:.2f}"
              f" hp×{statistics.mean([statistics.mean(v) for v in hp_factor.values()]):.2f}")

        for i in stage_normal[sid] | stage_boss[sid]:
            if primary_stage.get(i) != sid or i not in by_id:
                continue
            r = by_id[i]
            hp_f = statistics.mean(hp_factor[i])
            if hp_f != 1.0:
                old = int(r["max_hp"])
                new = max(20, round(old * hp_f))
                if new != old:
                    r["max_hp"] = str(new)
                    changed["max_hp"] += 1
            if def_f != 1.0:
                old = int(r["defense"])
                # 守備は毎回0.7倍を掛け続けると全敵が1になり、防御ステータスが機能しなくなる。
                # HEAD 比15%を下限として、ステータスとしての意味を残す。
                new = max(1, round(int(head_defense.get(i, old)) * 0.15), round(old * def_f))
                if new != old:
                    r["defense"] = str(new)
                    changed["defense"] += 1
            if atk_f != 1.0:
                for col in atk_stats.get(i, {"strength"}):
                    old = int(r[col])
                    new = max(1, round(old * atk_f))
                    if new != old:
                        r[col] = str(new)
                        changed[col] += 1

    print(f"変更列: {dict(changed)}")
    if not args.dry_run:
        with open(enemies_path, "w", encoding="utf-8", newline="") as f:
            w = csv.DictWriter(f, fieldnames=rows[0].keys(), lineterminator="\n")
            w.writeheader()
            w.writerows(rows)
        print(f"書き込み: {enemies_path}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("json_path")
    ap.add_argument("--halve-speed", action="store_true", help="初回のみ: 速度を1/2にする")
    ap.add_argument("--feedback", action="store_true", help="クリア率フィードバック制御で調整")
    ap.add_argument("--gain", type=float, default=1.0, help="係数の減衰(1.0未満で振動を抑える)")
    ap.add_argument("--target-clear", type=float, default=0.7, help="目標クリア率(下げると難易度が上がる)")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    if args.feedback:
        feedback(args)
        return

    trials = json.load(open(args.json_path))
    enemies_path = os.path.join(RES, "quest", "enemies.csv")
    rows = load_csv(enemies_path)
    by_id = {int(r["id"]): r for r in rows}
    spawns = collections.defaultdict(list)
    for r in load_csv(os.path.join(RES, "quest", "floor_enemy_spawns.csv")):
        spawns[(int(r["stage_id"]), int(r["floor_no"]))].append(int(r["enemy_definition_id"]))
    boss_floor = {}
    for r in load_csv(os.path.join(RES, "quest", "stage_floors.csv")):
        boss_floor[(int(r["stage_id"]), int(r["floor_no"]))] = r["floor_type"] == "Boss"

    atk_stats = enemy_attack_stats()

    # ---- 1) 攻撃ステータス: 標本から閉形式で逆算 ----
    samples_by_enemy = collections.defaultdict(list)
    on_boss = set()
    for t in trials:
        for h in t["EnemyHits"]:
            samples_by_enemy[h["EnemyDefId"]].append(
                (h["Damage"], h["TargetMaxHp"], h["TargetDefense"], h["TargetIntelligence"]))
            if h["IsBossFloor"]:
                on_boss.add(h["EnemyDefId"])

    atk_factor = {}   # eid -> {col: factor}
    for eid, samples in samples_by_enemy.items():
        target = BOSS_TOL_TARGET if eid in on_boss else NORMAL_TOL_TARGET
        cols = atk_stats.get(eid, {"strength"})
        factors = {}
        for col in cols:
            if col == "intelligence":
                f = solve_intelligence(samples, target, int(by_id[eid]["intelligence"]))
            else:
                f = solve_physical(samples, target)
            factors[col] = min(max(f, ATK_CLAMP[0]), ATK_CLAMP[1])
        atk_factor[eid] = factors

    # 未測定の敵には同レベル帯の中央倍率を外挿(列種別ごと)。
    band = collections.defaultdict(lambda: collections.defaultdict(list))
    for eid, factors in atk_factor.items():
        for col, f in factors.items():
            band[int(by_id[eid]["level"])][col].append(f)
    band_med = {lv: {col: statistics.median(v) for col, v in cols.items()} for lv, cols in band.items()}
    # 外挿は「一度も調整されたことのない敵」に限る(調整済み未測定の敵へ重ね掛けすると振動する)。
    state_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out", "tuned_ids.json")
    tuned_ids = set(json.load(open(state_path))) if os.path.exists(state_path) else set()
    for eid, r in by_id.items():
        if eid in atk_factor or eid in tuned_ids:
            continue
        lv = int(r["level"])
        if lv not in band_med:
            continue
        cols = atk_stats.get(eid, {"strength"})
        factors = {}
        for col in cols:
            src = band_med[lv].get(col) or band_med[lv].get("strength")
            if src:
                factors[col] = src
        if factors:
            atk_factor[eid] = factors
    tuned_ids |= set(atk_factor)

    # ---- 2) HP予算: クリア済フロアの実効DPS(総HP÷ターン)優先で按分 ----
    floor_dps = collections.defaultdict(list)
    stage_dps = collections.defaultdict(list)
    for t in trials:
        for f in t["Floors"]:
            if f["Turns"] <= 0:
                continue
            if f["Cleared"] and f["TotalEnemyHp"] > 0:
                dps = f["TotalEnemyHp"] / f["Turns"]
            elif f["DamageDealt"] > 0:
                dps = f["DamageDealt"] / f["Turns"]
            else:
                continue
            floor_dps[(t["StageId"], f["FloorNo"])].append(dps)
            stage_dps[t["StageId"]].append(dps)

    hp_targets = collections.defaultdict(list)
    for key, ids in spawns.items():
        dps_list = floor_dps.get(key) or stage_dps.get(key[0])
        if not dps_list or not ids:
            continue
        dps = statistics.median(dps_list)
        budget = dps * (BOSS_TURNS if boss_floor.get(key) else NORMAL_TURNS)
        total = sum(int(by_id[i]["max_hp"]) for i in ids if i in by_id)
        if total <= 0:
            continue
        for i in ids:
            if i in by_id:
                hp_targets[i].append(budget * int(by_id[i]["max_hp"]) / total)

    # ---- 3) 書き出し ----
    changed = collections.Counter()
    report = []
    for r in rows:
        eid = int(r["id"])
        line = []
        if eid in hp_targets:
            old = int(r["max_hp"])
            ratio = statistics.mean(hp_targets[eid]) / old
            ratio = min(max(ratio, HP_CLAMP[0]), HP_CLAMP[1])
            new = max(20, round(old * ratio))
            if new != old:
                r["max_hp"] = str(new)
                changed["max_hp"] += 1
                line.append(f"HP{old}->{new}")
        for col, f in sorted(atk_factor.get(eid, {}).items()):
            old = int(r[col])
            new = max(1, round(old * f))
            if new != old:
                r[col] = str(new)
                changed[col] += 1
                line.append(f"{col[:3]}{old}->{new}")
        if args.halve_speed:
            old = int(r["speed"])
            new = max(1, old // 2)
            if new != old:
                r["speed"] = str(new)
                changed["speed"] += 1
                line.append(f"spd{old}->{new}")
        if line:
            report.append(f"  {eid:>3} {r['name']:<14} " + " ".join(line))

    print(f"変更列: {dict(changed)}")
    for line in report[:12]:
        print(line)
    if len(report) > 12:
        print(f"  ... 他{len(report) - 12}体")

    if not args.dry_run:
        with open(enemies_path, "w", encoding="utf-8", newline="") as f:
            w = csv.DictWriter(f, fieldnames=rows[0].keys(), lineterminator="\n")
            w.writeheader()
            w.writerows(rows)
        print(f"書き込み: {enemies_path}")
        json.dump(sorted(tuned_ids), open(state_path, "w"))


if __name__ == "__main__":
    main()
