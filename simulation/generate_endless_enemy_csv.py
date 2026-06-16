#!/usr/bin/env python3
"""エンドレス戦闘モードのマスタ CSV を生成する。

出力:
  server/resources/quest/endless_enemy_templates.csv … 50 体の敵テンプレ
      （5 テーマ × [攻撃3 / 防御3 / 支援3 / ボス1]）
  server/resources/quest/endless_configs.csv         … ステージ単位のスケーリング設定

アーキタイプの基準ステ・成長パラメータは simulate_endless_balance.py を単一の情報源とし、
そこから import する（バランス調整との二重管理を避ける）。

設計仕様: docs/quest/05_エンドレスモード設計.md
命名規則: チビクエ風のゆるい日本語（最終帯「あらしの果て」はダーク系）。
"""
from __future__ import annotations

import csv
from pathlib import Path

from simulate_endless_balance import ARCHETYPES, BOSS, EndlessParams

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "server" / "resources" / "quest"

# エンドレスステージ ID（stages.csv に別途追加する行）。
ENDLESS_STAGE_ID = 13
# テンプレ ID 採番開始（enemies.csv の 1〜87 と衝突しない高位レンジ）。
TEMPLATE_ID_BASE = 9001

STAT_KEYS = ["max_hp", "max_mp", "strength", "defense", "intelligence", "luck", "speed"]

# EnemyAiType 名（server/domain/quest/enums/EnemyAiType.cs）への対応。
ARCHETYPE_AI_TYPE = {"攻撃": "Aggressive", "防御": "Defensive", "支援": "Supportive"}

# アーキタイプ固定の攻撃技（既存 move_master / move_effects の PowerRate 型）。
#   攻撃=ウーキーストライク(物理1.30) 防御=締め付ける(物理1.30)
#   支援=フレアライン(魔法0.85)       ボス=パルスブレイク(物理1.50)
ARCHETYPE_MOVE_ID = {"攻撃": 127, "防御": 125, "支援": 203}
BOSS_MOVE_ID = 129

# テーマ（深さ帯, 弱→強）。最終帯はダーク系。
THEMES = ["そよかぜ草原", "ざわめき森", "みぞれ峠", "ふぶき山", "あらしの果て"]

# 各テーマの敵名ドラフト（攻撃3 / 防御3 / 支援3 / ボス1）。
# チビクエ風のゆるい短いカナ名。テーマ5のみダーク系を交える。後で個別調整可。
ENEMY_NAMES: dict[int, dict[str, list[str]]] = {
    1: {  # そよかぜ草原
        "攻撃": ["ピュルン", "カゼッコ", "スイマル"],
        "防御": ["ドングリン", "マルゴロ", "ツチノコン"],
        "支援": ["ポワポワ", "タンポポン", "フワリン"],
        "ボス": ["オオカゼン"],
    },
    2: {  # ざわめき森
        "攻撃": ["トゲマル", "キバノコ", "ガサゴソ"],
        "防御": ["コケダマ", "ヤドリン", "ミキマル"],
        "支援": ["キノピコ", "ホタルン", "モリビン"],
        "ボス": ["オオモリン"],
    },
    3: {  # みぞれ峠
        "攻撃": ["ツララン", "ミゾレッコ", "シバレル"],
        "防御": ["コオリゴケ", "ユキダマ", "シモバシラン"],
        "支援": ["ミゾレビ", "ヒエヒエ", "シズクン"],
        "ボス": ["ツララオウ"],
    },
    4: {  # ふぶき山
        "攻撃": ["ユキカゼ", "フブッキー", "コゴエ"],
        "防御": ["ユキガベ", "コオリイワ", "ザンセツン"],
        "支援": ["フブキビ", "シロタエ", "コナユキン"],
        "ボス": ["オオフブキ"],
    },
    5: {  # あらしの果て（ダーク系）
        "攻撃": ["ダークカゼ", "ヤミイカズチ", "クライナリ"],
        "防御": ["ダークイワ", "ヤミガベ", "アラシゴケ"],
        "支援": ["ダークビ", "ヤミシズク", "クロイナズマ"],
        "ボス": ["ダークアラシ"],
    },
}

# 未使用 Enemy 画像（番号順 先頭 50 枚）をテーマ→アーキ順に割り当てる。
UNUSED_IMAGES = [
    2, 4, 5, 6, 9, 11, 17, 18, 19, 20, 24, 27, 28, 30, 32, 33, 34, 35, 36, 38,
    39, 41, 42, 45, 46, 47, 48, 50, 51, 52, 53, 54, 55, 56, 58, 59, 64, 65, 67, 68,
    69, 70, 71, 72, 73, 74, 75, 76, 77, 78,
]


def base_status_for(archetype_name: str) -> dict[str, int]:
    if archetype_name == "ボス":
        return dict(BOSS.base)
    for arche in ARCHETYPES:
        if arche.name == archetype_name:
            return dict(arche.base)
    raise KeyError(archetype_name)


def build_templates() -> list[dict]:
    """テーマ→[攻撃3, 防御3, 支援3, ボス1] の順に 50 体を構築。"""
    rows: list[dict] = []
    template_id = TEMPLATE_ID_BASE
    image_idx = 0
    for theme_no in range(1, 6):
        names = ENEMY_NAMES[theme_no]
        ordered = (
            [("攻撃", n) for n in names["攻撃"]]
            + [("防御", n) for n in names["防御"]]
            + [("支援", n) for n in names["支援"]]
            + [("ボス", n) for n in names["ボス"]]
        )
        for archetype_name, enemy_name in ordered:
            is_boss = archetype_name == "ボス"
            base = base_status_for(archetype_name)
            ai_type = "Aggressive" if is_boss else ARCHETYPE_AI_TYPE[archetype_name]
            move_id = BOSS_MOVE_ID if is_boss else ARCHETYPE_MOVE_ID[archetype_name]
            image_no = UNUSED_IMAGES[image_idx]
            rows.append({
                "template_id": template_id,
                "theme_no": theme_no,
                "archetype": ai_type,
                "is_boss": str(is_boss).lower(),
                "name": enemy_name,
                "base_max_hp": base["max_hp"],
                "base_max_mp": base["max_mp"],
                "base_strength": base["strength"],
                "base_defense": base["defense"],
                "base_intelligence": base["intelligence"],
                "base_luck": base["luck"],
                "base_speed": base["speed"],
                "image_path": f"image/battle/Enemy{image_no}.png",
                "move_ids": move_id,
            })
            template_id += 1
            image_idx += 1
    return rows


def write_templates(rows: list[dict]) -> None:
    path = OUTPUT_DIR / "endless_enemy_templates.csv"
    fields = ["template_id", "theme_no", "archetype", "is_boss", "name",
              "base_max_hp", "base_max_mp", "base_strength", "base_defense",
              "base_intelligence", "base_luck", "base_speed", "image_path", "move_ids"]
    with path.open("w", encoding="utf-8", newline="") as fp:
        writer = csv.DictWriter(fp, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)
    print(f"  -> {path.relative_to(ROOT)} ({len(rows)} 体)")


def write_config() -> None:
    """ステージ単位の設定。スケーリングは simulate_endless_balance の確定値を流用。"""
    p = EndlessParams()
    path = OUTPUT_DIR / "endless_configs.csv"
    fields = [
        "stage_id", "growth_coefficient_a", "growth_exponent_p", "safety_cap_floor",
        "boss_interval", "boss_extra_multiplier", "boss_reward_multiplier",
        "theme_count", "floors_per_theme", "min_enemies_per_floor", "max_enemies_per_floor",
        "reward_exp_rate", "reward_gold_rate",
    ]
    row = {
        "stage_id": ENDLESS_STAGE_ID,
        "growth_coefficient_a": p.a,
        "growth_exponent_p": p.p,
        # int 溢れセーフティ。simulate_endless_balance.overflow_cap() の確定値（実到達は 200F 台）。
        "safety_cap_floor": 7161,
        "boss_interval": p.boss_interval,
        "boss_extra_multiplier": p.boss_extra_multiplier,
        # ボスフロアの報酬補正（敵実効 Level = floor × 倍率。通常≒3体ぶんに相当させる）。
        "boss_reward_multiplier": 3.0,
        "theme_count": p.theme_count,
        "floors_per_theme": p.floors_per_theme,
        "min_enemies_per_floor": 1,
        "max_enemies_per_floor": 3,
        # 報酬係数（敵実効 Level = floor 番号[線形] に乗る ExpRate/GoldRate）。
        # 方針: アンロック層(Lv2000=stage8 周回先)の「通常ステージ周回の約半分の効率」。
        #   stage8 の 1 フロア exp ≒ 176 万(Σ敵Lv約1万×ExpRate150〜296)。その半分 ≒ 88 万。
        #   敵実効 Level=floor のため floor50・通常2体で 2*50*rate=88万 → rate≒8.8k、
        #   走破平均で約半分・深層でほぼ等倍となる 1.0e4 を採用（floor50 で約0.57倍）。
        #   gold は exp 比 約0.74 倍（stage8 の GoldRate/ExpRate 比）。
        "reward_exp_rate": 10000.0,
        "reward_gold_rate": 7500.0,
    }
    with path.open("w", encoding="utf-8", newline="") as fp:
        writer = csv.DictWriter(fp, fieldnames=fields)
        writer.writeheader()
        writer.writerow(row)
    print(f"  -> {path.relative_to(ROOT)} (stage {ENDLESS_STAGE_ID})")


def main() -> None:
    print("エンドレス マスタ CSV 生成")
    print("-" * 48)
    rows = build_templates()
    assert len(rows) == 50, f"テンプレ数が 50 でない: {len(rows)}"
    assert len({r["template_id"] for r in rows}) == 50, "template_id 重複"
    assert len({r["image_path"] for r in rows}) == 50, "画像割当 重複"
    write_templates(rows)
    write_config()
    print("-" * 48)
    print("完了")


if __name__ == "__main__":
    main()
