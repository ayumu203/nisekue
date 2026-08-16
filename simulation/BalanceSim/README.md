# BalanceSim — クエストバランス実測ハーネス

server の実戦闘コードをそのまま駆動してステージ周回を再現し、バランス指標を実測するコンソールアプリ。
戦闘計算・AI・ターン解決の**再実装は一切していない**(過去に Python 模型が実装と乖離して誤った調整を生んだ反省から)。

## 実物をそのまま使っているもの

- `BattleService` / `BattleTurnResolver` / `BattleActionResolver` / `BattleDamageCalculator`(ダメージ式・多段ヒット・クリティカル)
- `QuestBattleFactory` + `QuestEnemyActionPolicy` / `QuestAllyNpcActionPolicy`(敵AI・味方NPC AI・MP管理)
- `QuestRun` 集約(フロア進行・全滅判定・ターン管理)
- `Player` エンティティ(レベルアップ成長・技習得・転職判定)
- `EquipmentStatusResolver`(装備補正)
- 各 Csv リポジトリ(server と同一の resources を読む)

## ハーネス側の仮定(戦闘計算以外)

- プレイヤーは `ParticipantType.Npc` として参加させ、職業ロール別の実AI(`QuestAllyNpcActionPolicy`)で行動する。
  `ActionMode.Manual` を与えて「継続可能メンバー」として扱わせる(プレイヤー死亡=敗北。ソロの実挙動)。
- パーティはソロ想定: プレイヤー1 + NPC補充(`min_party_member_count - 1`)。NPC選択は実リポジトリのランダム抽選。
- 育成ルートは「証」アイテム(`item_master.csv`)の必要Lv・必要マスターに従い、現職マスター後に次職へ転職。
  5ルート(戦士系/盾系/魔法系/僧侶系/レンジャー系)。転生なし・種/しずく未使用。
- 装備は「そのステージ以下のドロップ装備から役割別スコア最良の武器+防具」を強化+0で装備。
- プレイヤーのレベルはステージの推奨レベル(上限100でクランプ)。
- 乱数はシードなし(`Random.Shared`)。試行を複数回して分布で見る。

## 使い方

```bash
dotnet run --project simulation/BalanceSim -- --trials 20 --json out.json
dotnet run --project simulation/BalanceSim -- --trials 1 --stages 1 --routes 戦士系 --debug   # 1試行の全ターンログ
```

- `--trials N` : ステージ×ルートごとの試行回数(既定20)
- `--stages 1,2` / `--routes 戦士系,魔法系` : 対象の絞り込み
- `--json path` : 試行ごとの詳細(フロア別ターン数・与ダメージ・敵ごとの被弾標本)をJSON出力
- `--debug` : ターンごとの行動・HPをダンプ

## チューニングループ(tune.py --feedback)

`tune.py --feedback` は実測JSONから `server/resources/quest/enemies.csv` の次候補を書く簿記スクリプト。
戦闘計算はせず、判断材料はすべてハーネスの実測値。受け入れ基準そのもの(クリア率)を主目標にする。

- 攻撃: ステージのクリア率と被弾許容発数から一括倍率。対象は**その敵が実際にダメージ計算で参照する列**。
  - 通常攻撃は常に Strength 参照なので、全敵で `strength` が対象。
  - `attack_stat` 未指定の Attack 技は実行時に「知力と力の高い方」を選ぶため、両方が対象
    (片方だけ下げると、もう片方に切り替わるだけで効かない)。
- HP: **フロア単位**。そのフロアのターン数 p75 と目標(通常5T/ボス15T)の比。
  ステージ集約だと「大半は1Tで溶けるが特定フロアだけ膠着」が打ち消し合って見えなくなる。
  未クリアのフロアも標本に含める(未到達フロアは同ステージの最小係数で外挿)。
- 守備: 膠着(60T超)が5%以上のとき緩める。膠着は「HPが高い」ではなく
  「守備が高くパーティの物理が1しか通らない」で起きるため、HPを削っても解消しない。
  HEAD 比15%を下限にしないと全敵の守備が1になり、防御ステータスが機能しなくなる。
- 速度: 初回のみ一律 1/2。
- `--gain 0.5` で係数を1へ寄せる。終盤はこれを入れないと 0.8↔1.3 の振動が止まらない。

```bash
dotnet run --project simulation/BalanceSim -- --trials 10 --json out/iterN.json
python3 simulation/BalanceSim/tune.py out/iterN.json --feedback --gain 0.5   # enemies.csv を更新
# 収束するまで繰り返し、最後に --trials 30 で検証
```

### smooth.py(仕上げ)

フロア単位の係数を掛け続けると、下限に張り付いた敵とそうでない敵が同じステージに混在し、
元データの相対設計(ボスは硬い/雑魚は柔らかい)が壊れる。`smooth.py` はステージごとの
達成倍率の幾何平均を求め、HEAD の値に掛け直すことで、絶対水準は調整結果のまま相対比を元設計に戻す。
実行後は難易度が上がる方向にずれるので、`--gain 0.5` で数回回して水準を戻す。

CI には含めない(ビルド・テスト対象は server / tests のみ)。
