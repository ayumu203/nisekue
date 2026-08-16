#!/usr/bin/env python3
"""調整後 enemies.csv の「ステージ内の相対関係の崩れ」をならす仕上げスクリプト。

フィードバック調整はフロア単位に係数を掛けるため、下限(HP20)に張り付いた敵と
そうでない敵が同じステージに混在し、元データの設計(ボスは硬い/雑魚は柔らかい)が壊れる。
ここでは「ステージ×(通常/ボス)ごとの達成倍率の幾何平均」を求め、
HEAD の値にその倍率を掛け直すことで、絶対水準は調整結果のまま相対比を元設計に戻す。
"""
import csv, collections, math, os, subprocess, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "server", "resources")
COLS = ("max_hp", "strength", "defense", "intelligence")
FLOOR = {"max_hp": 20, "strength": 1, "defense": 1, "intelligence": 1}


def load(path):
    with open(path, encoding="utf-8") as f:
        return list(csv.DictReader(f))


def main():
    enemies_path = os.path.join(RES, "quest", "enemies.csv")
    new = {int(r["id"]): r for r in load(enemies_path)}
    head_text = subprocess.run(
        ["git", "-C", ROOT, "show", "HEAD:server/resources/quest/enemies.csv"],
        capture_output=True, text=True, check=True).stdout
    old = {int(r["id"]): r for r in csv.DictReader(head_text.splitlines())}

    boss_floor = {}
    for r in load(os.path.join(RES, "quest", "stage_floors.csv")):
        boss_floor[(int(r["stage_id"]), int(r["floor_no"]))] = r["floor_type"] == "Boss"
    group = {}
    for r in load(os.path.join(RES, "quest", "floor_enemy_spawns.csv")):
        sid, fno, eid = int(r["stage_id"]), int(r["floor_no"]), int(r["enemy_definition_id"])
        # ステージ単位で集約する。通常/ボスで分けるとボス群の標本が1体になり幾何平均が暴れるうえ、
        # 「ボスは雑魚より硬い」という比率は HEAD 側が既に持っているので保たれる。
        group.setdefault(eid, sid)

    ratios = collections.defaultdict(lambda: collections.defaultdict(list))
    for eid, g in group.items():
        if eid not in new or eid not in old:
            continue
        for c in COLS:
            o, n = int(old[eid][c]), int(new[eid][c])
            if o > 0 and n > FLOOR[c]:  # 下限に張り付いた敵は倍率の標本にしない
                ratios[g][c].append(n / o)

    for eid, g in group.items():
        if eid not in new or eid not in old:
            continue
        for c in COLS:
            samples = ratios[g][c]
            if not samples:
                continue
            f = math.exp(sum(math.log(x) for x in samples) / len(samples))
            new[eid][c] = str(max(FLOOR[c], round(int(old[eid][c]) * f)))

    rows = sorted(new.values(), key=lambda r: int(r["id"]))
    with open(enemies_path, "w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=rows[0].keys(), lineterminator="\n")
        w.writeheader()
        w.writerows(rows)
    for g in sorted(ratios):
        print(f"stage{g:>3} " + " ".join(
            f"{c}×{math.exp(sum(math.log(x) for x in ratios[g][c]) / len(ratios[g][c])):.3f}"
            for c in COLS if ratios[g][c]))


if __name__ == "__main__":
    main()
