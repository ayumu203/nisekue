#!/usr/bin/env python3
"""エンドレス戦闘モードのバランスシミュレーション。

敵ステータス = アーキタイプ基準 S_base × 成長倍率 f(n)=1+a*n^p を、
既存のプレイヤー成長モデル（job_levelup_growths.csv）から算出したソロ戦力と
突き合わせ、以下を出力する。

  1. 敵ステータス推移（floorごと / アーキタイプ別 / ボス）
  2. int 溢れ前の安全上限フロア cap
  3. プレイヤーレベル別の想定到達深度（フルHP前提 / 消耗前提）
  4. 推奨パラメータ確認用のコンソールサマリ

設計仕様: docs/quest/05_エンドレスモード設計.md

注意（既知の簡略化）:
  - 装備補正は含めない（実プレイヤーは装備分だけ強い＝安全マージン）。
  - クリティカル / 状態異常 / バフ / 回復は無視（同上のマージン）。
  - ダメージ式は server/domain/battle/.../BattleDamageCalculator.cs の物理計算に準拠。
"""
from __future__ import annotations

import csv
import math
from dataclasses import dataclass, field
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
GROWTH_CSV = ROOT / "server" / "resources" / "player" / "job_levelup_growths.csv"
OUTPUT_DIR = ROOT / "simulation"

INT_MAX = 2_147_483_647

STAT_KEYS = ["max_hp", "max_mp", "strength", "defense", "intelligence", "luck", "speed"]

# server/endpoints/PlayerEndpoints.cs のプレイヤー作成初期値
INITIAL_STATUS = {
    "max_hp": 24, "max_mp": 8, "strength": 7,
    "defense": 5, "intelligence": 5, "luck": 3, "speed": 4,
}

# 代表的なソロ物理アタッカーの職パス（強さの基準）。
# 各 (job_code, この職で到達する player_level 上限) を順に辿る。
PHYSICAL_PATH = [
    ("Warrior", 100),
    ("OniWarrior", 200),
    ("GrandWarrior", 300),
    ("Shogun", 10_000),
]


# ============================================================
# チューニング対象パラメータ（シミュレーションで調整する本体）
# ============================================================
@dataclass(frozen=True)
class EndlessParams:
    # 成長倍率 f(n) = 1 + a * n^p
    a: float = 0.02
    p: float = 2.0
    # フロア帯・ボス
    floors_per_theme: int = 10
    theme_count: int = 5
    boss_interval: int = 10
    boss_extra_multiplier: float = 1.5
    # 報酬（緩やか）: 実効Level ≈ floor の線形系
    reward_rate: float = 1.0
    # int 溢れ判定の天井（ここに達した floor を cap とする。INT_MAX に余裕を持たせる）
    overflow_ceiling: int = 1_000_000_000


# アーキタイプ floor1 基準ステータス（S_base）と攻撃技。
#   attack_stat: "strength"(物理) / "intelligence"(魔法)。実ダメージ式に対応。
#   power_rate : 選定した既存技の power_rate（深層でも効くよう FixedPower 単独は不採用）。
#     攻撃=ウーキーストライク(127, 物理 1.30) / 防御=締め付ける(125, 物理 1.30)
#     支援=フレアライン(203, 魔法 0.85) / ボス=パルスブレイク(129, 物理 1.50)
#   魔法は相手 def を無視し相手 int を魔防として参照するため、物理アタッカー(低int)には
#   支援型が通りやすい。支援は power_rate と基準 int を抑えて一強化を回避する。
@dataclass(frozen=True)
class Archetype:
    name: str
    base: dict[str, int]
    power_rate: float
    attack_stat: str = "strength"


ARCHETYPES = [
    Archetype("攻撃", {"max_hp": 200, "max_mp": 20, "strength": 40, "defense": 20,
                        "intelligence": 12, "luck": 18, "speed": 30},
              power_rate=1.30, attack_stat="strength"),
    Archetype("防御", {"max_hp": 360, "max_mp": 15, "strength": 25, "defense": 46,
                        "intelligence": 12, "luck": 12, "speed": 16},
              power_rate=1.30, attack_stat="strength"),
    Archetype("支援", {"max_hp": 220, "max_mp": 40, "strength": 20, "defense": 22,
                        "intelligence": 28, "luck": 25, "speed": 22},
              power_rate=0.85, attack_stat="intelligence"),
]
BOSS = Archetype("ボス", {"max_hp": 650, "max_mp": 40, "strength": 55, "defense": 35,
                          "intelligence": 25, "luck": 25, "speed": 28},
                 power_rate=1.50, attack_stat="strength")

# プレイヤー（物理アタッカー）の攻撃技係数（物理・Strength 依存）
PLAYER_POWER_RATE = 1.30


# ============================================================
# プレイヤー成長モデル
# ============================================================
@dataclass(frozen=True)
class JobGrowth:
    code: str
    growth: dict[str, int]


def load_job_growths() -> dict[str, JobGrowth]:
    growths: dict[str, JobGrowth] = {}
    with GROWTH_CSV.open("r", encoding="utf-8", newline="") as fp:
        for row in csv.DictReader(fp):
            growths[row["job_code"]] = JobGrowth(
                code=row["job_code"],
                growth={k: int(row[k]) for k in STAT_KEYS},
            )
    return growths


def player_status_at(level: int, growths: dict[str, JobGrowth]) -> dict[str, int]:
    """物理パスに沿って player_level=level 時点のステータスを算出（装備なし）。"""
    status = dict(INITIAL_STATUS)
    current = 1
    for job_code, phase_end in PHYSICAL_PATH:
        job = growths[job_code]
        upto = min(level, phase_end)
        for _ in range(current, upto):  # current→upto まで毎レベル成長
            for k in STAT_KEYS:
                status[k] += job.growth[k]
        current = upto
        if current >= level:
            break
    return status


# ============================================================
# 敵スケーリング
# ============================================================
def growth_factor(params: EndlessParams, floor_no: int) -> float:
    return 1.0 + params.a * (floor_no ** params.p)


def theme_no(params: EndlessParams, floor_no: int) -> int:
    """1始まり。floor50超はテーマ循環。"""
    band = (floor_no - 1) // params.floors_per_theme
    return (band % params.theme_count) + 1


def is_boss_floor(params: EndlessParams, floor_no: int) -> bool:
    return floor_no % params.boss_interval == 0


def scaled_status(base: dict[str, int], factor: float) -> dict[str, int]:
    return {k: min(INT_MAX, int(base[k] * factor)) for k in STAT_KEYS}


# ============================================================
# 戦闘シミュレーション（ソロ vs 敵群、フォーカスファイア）
# ============================================================
def physical_damage(attacker_str: int, power_rate: float, defender_def: int) -> int:
    """BattleDamageCalculator 物理: max(1, fixed + round(str*rate) - def)。fixed=0。"""
    base = round(attacker_str * power_rate)
    return max(1, base - defender_def)


def magic_damage(attacker_int: int, power_rate: float, defender_int: int) -> int:
    """BattleDamageCalculator 魔法(Intelligence): base = round(int*rate)、
    ratio = atk_int/(atk_int+def_int)（相手 int を魔防として参照）、dmg = max(1, base*ratio)。fixed=0。"""
    base = round(attacker_int * power_rate)
    denom = attacker_int + defender_int
    if denom <= 0:
        return 1
    return max(1, round(base * attacker_int / denom))


@dataclass
class Combatant:
    hp: int
    strength: int
    defense: int
    speed: int
    power_rate: float
    intelligence: int = 0
    attack_stat: str = "strength"

    def damage_to(self, target: "Combatant") -> int:
        if self.attack_stat == "intelligence":
            return magic_damage(self.intelligence, self.power_rate, target.intelligence)
        return physical_damage(self.strength, self.power_rate, target.defense)


def simulate_floor(player: Combatant, enemies: list[Combatant], max_rounds: int = 500) -> bool:
    """そのフロアをクリアできれば True。player.hp は呼び出し側で管理（消耗モデル用に破壊的更新）。"""
    living = [e for e in enemies]
    for _ in range(max_rounds):
        if not living:
            return True
        if player.hp <= 0:
            return False
        # 速度順（プレイヤーと敵を一括ソート）。同速はプレイヤー優先。
        actors = [("player", player)] + [("enemy", e) for e in living]
        actors.sort(key=lambda t: (t[1].speed, t[0] == "player"), reverse=True)
        for side, actor in actors:
            if side == "player":
                if player.hp <= 0:
                    return False
                if not living:
                    return True
                target = living[0]  # フォーカスファイア（先頭から確実に処理）
                dmg = player.damage_to(target)
                target.hp -= dmg
                if target.hp <= 0:
                    living.remove(target)
            else:
                if actor.hp <= 0:
                    continue
                dmg = actor.damage_to(player)
                player.hp -= dmg
        if not living:
            return True
        if player.hp <= 0:
            return False
    return False


def make_enemies_for_floor(params: EndlessParams, floor_no: int) -> list[Combatant]:
    factor = growth_factor(params, floor_no)
    if is_boss_floor(params, floor_no):
        st = scaled_status(BOSS.base, factor * params.boss_extra_multiplier)
        return [Combatant(st["max_hp"], st["strength"], st["defense"], st["speed"],
                          BOSS.power_rate, st["intelligence"], BOSS.attack_stat)]
    # 通常フロア: 最大3体（最も負荷の高いケースで評価）を全アーキタイプから構成
    enemies: list[Combatant] = []
    for arche in ARCHETYPES:  # 攻撃/防御/支援 各1体 = 3体（最大負荷）
        st = scaled_status(arche.base, factor)
        enemies.append(Combatant(st["max_hp"], st["strength"], st["defense"], st["speed"],
                                 arche.power_rate, st["intelligence"], arche.attack_stat))
    return enemies


def deepest_reachable(
    params: EndlessParams,
    player_base: dict[str, int],
    *,
    attrition: bool,
    cap: int,
) -> int:
    """そのプレイヤーが到達できる最深フロア。
    attrition=False: 各フロア開始時フルHP（楽観）。
    attrition=True : HPはフロアを跨いで持ち越し（回復なし・悲観）。
    """
    carried_hp = player_base["max_hp"]
    for floor_no in range(1, cap + 1):
        start_hp = carried_hp if attrition else player_base["max_hp"]
        player = Combatant(start_hp, player_base["strength"], player_base["defense"],
                           player_base["speed"], PLAYER_POWER_RATE,
                           player_base["intelligence"], "strength")
        enemies = make_enemies_for_floor(params, floor_no)
        if not simulate_floor(player, enemies):
            return floor_no - 1
        carried_hp = player.hp
    return cap


# ============================================================
# cap（int 溢れ）算出
# ============================================================
def overflow_cap(params: EndlessParams) -> int:
    """最大の基準ステ（ボスHP×追加倍率）が overflow_ceiling を超える直前の floor。"""
    max_base = BOSS.base["max_hp"] * params.boss_extra_multiplier
    floor_no = 1
    while floor_no < 100_000:
        if max_base * growth_factor(params, floor_no) > params.overflow_ceiling:
            return floor_no - 1
        floor_no += 1
    return floor_no


# ============================================================
# 出力
# ============================================================
def write_enemy_curve(params: EndlessParams, cap: int, sample_cap: int) -> None:
    path = OUTPUT_DIR / "endless_enemy_curve.csv"
    rows = []
    floors = sorted(set(list(range(1, min(60, sample_cap) + 1)) +
                        list(range(10, sample_cap + 1, 10))))
    for n in floors:
        factor = growth_factor(params, n)
        row = {"floor": n, "theme": theme_no(params, n),
               "is_boss": is_boss_floor(params, n), "f_n": round(factor, 3)}
        for arche in ARCHETYPES:
            st = scaled_status(arche.base, factor)
            row[f"{arche.name}_hp"] = st["max_hp"]
            row[f"{arche.name}_str"] = st["strength"]
            row[f"{arche.name}_def"] = st["defense"]
        bst = scaled_status(BOSS.base, factor * params.boss_extra_multiplier)
        row["boss_hp"] = bst["max_hp"]
        row["boss_str"] = bst["strength"]
        row["reward_exp"] = int(n * params.reward_rate)  # 実効Level≈floor の線形系
        rows.append(row)
    with path.open("w", encoding="utf-8", newline="") as fp:
        writer = csv.DictWriter(fp, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)
    print(f"  -> {path.name} ({len(rows)} rows)")


def write_reachable_depth(params: EndlessParams, growths, cap: int, sample_cap: int) -> None:
    path = OUTPUT_DIR / "endless_reachable_depth.csv"
    levels = [10, 50, 100, 200, 300, 500, 1000, 2000, 3000, 5000, 10000]
    rows = []
    for lv in levels:
        base = player_status_at(lv, growths)
        opt = deepest_reachable(params, base, attrition=False, cap=sample_cap)
        att = deepest_reachable(params, base, attrition=True, cap=sample_cap)
        rows.append({
            "player_level": lv, "max_hp": base["max_hp"], "strength": base["strength"],
            "defense": base["defense"], "speed": base["speed"],
            "reach_full_hp": opt, "reach_attrition": att,
        })
    with path.open("w", encoding="utf-8", newline="") as fp:
        writer = csv.DictWriter(fp, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)
    print(f"  -> {path.name}")
    print("\n  プレイヤーLv別 到達深度（フルHP / 消耗）:")
    for r in rows:
        print(f"    Lv{r['player_level']:>5}  str={r['strength']:>8}  def={r['defense']:>8}  "
              f"hp={r['max_hp']:>9}  ->  full={r['reach_full_hp']:>4}  attr={r['reach_attrition']:>4}")


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    params = EndlessParams()
    growths = load_job_growths()

    cap = overflow_cap(params)
    # 到達深度の探索上限（cap が極端に大きい場合の計算量制限）
    sample_cap = min(cap, 2000)

    print("=" * 64)
    print("エンドレス バランスシミュレーション")
    print("=" * 64)
    print(f"  f(n) = 1 + {params.a} * n^{params.p}")
    print(f"  ボス周期={params.boss_interval} 追加倍率×{params.boss_extra_multiplier} "
          f"テーマ={params.theme_count}×{params.floors_per_theme}floor")
    print(f"  f(1)={growth_factor(params,1):.3f}  f(10)={growth_factor(params,10):.3f}  "
          f"f(50)={growth_factor(params,50):.3f}  f(100)={growth_factor(params,100):.3f}")
    print(f"  int溢れ安全上限 cap = floor {cap}  (探索上限={sample_cap})")
    print("-" * 64)
    write_enemy_curve(params, cap, sample_cap)
    write_reachable_depth(params, growths, cap, sample_cap)
    print("=" * 64)


if __name__ == "__main__":
    main()
