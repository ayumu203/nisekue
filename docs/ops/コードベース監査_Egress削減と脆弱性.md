# コードベース監査レポート — DB Egress 削減 / レスポンス高速化 / 冗長コード / 脆弱性

対象: `server/`（.NET 10 Minimal API + EF Core 10 + Supabase/PostgreSQL）
作成日: 2026-06-11
観点: ①DB Egress 削減につながる処理改善、②レスポンス高速化、③冗長コード、④安全でないクエリ・更新処理、⑤その他の脆弱性

> 本レポートは「現状の問題点」と「推奨対応」をまとめたものです。コードの変更は行っていません。深刻度は **High / Medium / Low** で示します。各項目に `file:line` を付しています。

---

## エグゼクティブサマリ（優先度順）

| # | 深刻度 | 分類 | 概要 |
|---|--------|------|------|
| 1 | **High** | 更新の安全性 | ゴールド/ステータス更新（転職ロードマップ解放・装備合成・アイテム使用）に行ロック/楽観ロックが無く、`IMemoryCache` 経由のstale読み取りと併さって **二重消費・複製** が発生し得る |
| 2 | **High** | Egress/性能 | `DbQuestRunRepository.SaveAsync` がコマンド送信のたびに全子テーブルを **全削除→全再INSERT**。1ターンあたり大量の読み書きが発生 |
| 3 | **High** | Egress/性能 | `quest_runs` に `(Status, ActionDeadlineAt)` インデックスが無く、タイムアウト監視が **2秒間隔でシーケンシャルスキャン** |
| 4 | **Medium** | 性能/Egress | クエスト進行のレスポンス（`MapQuestRunDetailAsync`）が毎回フルペイロード。SignalR更新＋ポーリングで頻発。`GetActiveByPlayerAsync` は2往復ロード |
| 5 | **Medium** | 脆弱性 | `GET /chat/room?ownerId=` に所有者チェックが無く、任意プレイヤーの個人チャットを閲覧可能 |
| 6 | **Medium** | Egress | `/players`・`/market/listings` 等が全件取得＋メモリ内フィルタ。ページングなし |
| 7 | **Medium** | 冗長/安全性 | `SecureEquals` が2実装あり、`RankingEndpoints` 側は **非定数時間比較** |
| 8 | **Low** | 性能 | チャット・スレッドのプロフィール解決が **N+1**（`GetPlayerAsync` ループ） |
| 9 | **Low** | 脆弱性/運用 | CORS が `AllowAnyHeader/AllowAnyMethod`、レート制限なし、保守トークンが全機能で共用 |

---

## 1. DB Egress 削減につながる処理

### 1-1. 【High】クエストRun保存の「全削除→全再INSERT」パターン
`server/infrastructure/quest/run/DbQuestRunRepository.cs:210` `ReplaceChildrenAsync`

コマンド送信・ターン解決・チャット追加など **あらゆる `SaveAsync` 呼び出しのたびに**、以下を毎回行っています。

```
QuestRunPartyMembers      … 全件 SELECT → RemoveRange → 全件 AddRange
QuestRunPartySnapshots    … 同上（スナップショットは不変なのに毎回再作成）
QuestRunEnemies           … 同上
QuestTurnCommands         … 同上
QuestFloorTraps           … 同上
QuestRewardSummaries      … SELECT → UPDATE/INSERT
```

- `SaveAsync` は `SubmitCommandAsync`（`QuestRunService.cs:61`）/`AddChatMessageAsync`/`RequestManualControlAsync` などからターンごと・操作ごとに呼ばれます。
- 特に **PartySnapshots はクエスト中ほぼ不変**（職業・初期ステータス・MoveSet・ペット）なのに、参加者×（読み取り＋削除＋挿入）が毎回往復します。チャット1通の追加でも全パーティ・全敵・全コマンドが再構築されます。
- これは Egress（読み取り行数）と書き込み量の両方を膨らませる最大要因です。

**推奨:**
- 変更のあったエンティティのみ追跡更新する（EFの変更追跡を使い、`AsNoTracking` ロード→ドメイン→全消去ではなく、追跡ロード＋差分更新へ）。
- スナップショット類は初回のみINSERTし、以後は読み取り・再書き込みしない。
- 可変なのは `PartyMembers`（HP/MP/状態異常）・`Enemies`・`TurnCommands` 程度なので、これらだけを差分更新する。
- チャット追加専用パス（`AddChatMessageAsync`）は `chat_messages_json` カラムのみUPDATEし、子テーブルに触れない専用メソッドを設ける。

### 1-2. 【High】`quest_runs` のタイムアウト監視インデックス欠如
`server/application/quest/QuestRunTimeoutBackgroundService.cs:9`（2秒間隔）
→ `DbQuestRunRepository.ListExpiredAsync` `:60`
→ `WHERE Status = InProgress AND ActionDeadlineAt <= now`

`quest_runs` のインデックスは `RoomId` ユニークのみ（`AppDbContext.cs:520`、ModelSnapshot で確認済み）。**`Status` / `ActionDeadlineAt` のインデックスが無いため、2秒ごとにテーブル全件スキャン** が走ります。行数が増えるほどEgressと負荷が線形に悪化します。

> 対照的に `pet_battle_runs` には `ix_pet_battle_runs_status_deadline`（`AppDbContext.cs:743`）が貼られており、こちらは適切です。クエスト側も同等のインデックスを追加すべきです。

**推奨:** 部分インデックスを追加（`WHERE status = <InProgress>` の filtered index が最も効率的）。
```
questRun.HasIndex(x => new { x.Status, x.ActionDeadlineAt })
        .HasFilter($"status = {(int)QuestRunStatus.InProgress}");
```

### 1-3. 【High】`ListExpiredAsync` の N+1 個別ロード
`DbQuestRunRepository.ListExpiredAsync:60`

期限切れRunのIDを取得後、`foreach` で1件ずつ `GetAsync`（=1-1の重いフルロード×7クエリ）を呼びます。タイムアウトRunが複数あると、2秒ごとに「件数 × 7クエリ × 全子テーブル」が走ります。Pet Battle 側 `ProcessExpiredRunsAsync` も同様（`DbPetBattleRunRepository.cs:60`）。

**推奨:** 対象RunIDをまとめ、子テーブルを `WHERE RunId IN (...)` で一括取得してメモリ内で組み立てる。

### 1-4. 【Medium】クエスト進行レスポンスのフルペイロード化と二重ロード
- `QuestResponseMapper.MapQuestRunDetailAsync`（`:204`）は毎回 `questEnemyDefinitionRepository.GetAllAsync()` を呼び `ToDictionary` を再構築（Singleton CSVなのでDB Egressは無いが、CPUとGC負荷）。このマッパーは **SignalR の毎更新＋クライアントのポーリング** で呼ばれます（`client/src/pages/Quest.tsx:353` は募集中2秒間隔）。
- `DbQuestRunRepository.GetActiveByPlayerAsync`（`:27`）は「RunIdをJOINで特定」→その後 `GetAsync` で **改めてフルロード**。1リクエストでRun本体を2回読みます。
- `GET /quest/runs/active` はクライアントから定期的に叩かれるため、上記が積み重なります。

**推奨:**
- マッパーで使う敵定義辞書はリクエスト単位でキャッシュ、または `MemoryCache` 化（Singletonで起動時に1度だけ辞書化）。
- `GetActiveByPlayerAsync` は特定したRunエンティティをそのまま使い、子テーブルだけ追加取得する（再フルロードを避ける）。
- 差分配信（前ターンからの差分のみ送る）を検討。

### 1-5. 【Medium】全件取得＋メモリ内フィルタ
- `IMarketListingRepository.GetActiveAsync`（`DbMarketListingRepository.cs:21`）は **全アクティブ出品を取得**し、`ItemEndpoints.cs:499` で自分の出品をメモリ内 `Where` 除外。出品数が増えるほどEgress増。
- `GET /players`（`PlayerEndpoints.cs:17`）は **全プレイヤーを返す**（ページングなし）。`GetAllAsync` は5分キャッシュ（`DbPlayerRepository.cs:92`）されるが、各リクエストで全件を戦闘力計算しながらシリアライズ。
- `GET /training/pvp-opponents`（`TrainingEndpoints.cs`）は `GetPvpOpponentsAsync`（`DbPlayerRepository.cs:62`）で **レベル上限以下の全プレイヤー** を取得。

**推奨:** DB側でフィルタ・ページング・上限件数を適用。`/players` はカーソル/ページング導入、または必要列のみ投影。

### 1-6. 【Medium】`/player` GET が毎回 `last_active_at` を書き込み
`PlayerEndpoints.cs:79`

```csharp
await db.Players.Where(p => p.Id == playerId.Value.Value)
    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastActiveAt, now));
```

読み取りAPIのたびに **書き込みが発生**（write amplification）。さらに直前の `playerMoveSetSanitizer.TrySanitize` がtrueだと `SaveAsync` も走り得ます。`/player` はホーム表示などで頻繁に呼ばれるため、無視できない書き込み量です。

**推奨:** `last_active_at` の更新は「前回更新から N 分以上経過時のみ」へ間引く（例: メモリキャッシュで最終更新時刻を持つ）。アクティブ人数集計は5分閾値（`MaintenanceEndpoints.cs:37`）なので、1〜2分粒度で十分。

### 1-7. 【Low】Singleton CSV辞書の毎リクエスト再構築
`equipmentRepository.GetAllAsync().ToDictionary(...)` / `itemRepository.GetAllAsync().ToDictionary(...)` が各エンドポイント（`PlayerEndpoints.cs:234`、`ItemEndpoints.cs:44,47` 等）で毎回行われます。DB Egressは無いものの、リクエストごとの辞書再構築はCPU/GC負荷。

**推奨:** リポジトリ側で `IReadOnlyDictionary` をキャッシュ提供する（Singletonなので安全）。

---

## 2. レスポンス高速化につながる内容

### 2-1. 【Low/Medium】チャット・スレッドのプロフィール解決が N+1
- `ChatService.GetRoomAsync`（`chatService.cs`）: 送信者IDごとに `GetPlayerAsync` をループ呼び出し。
- `ThreadService.LoadProfilesAsync`（`ThreadService.cs`）: `foreach` で `GetPlayerAsync` を1件ずつ。
- いずれも `GetPlayerAsync` は5分キャッシュ付き（`DbPlayerRepository.cs:20`）なので暖まればDB往復は減るが、キャッシュミス時は人数分の往復。

> 対照的に `GlobalChatService.BuildViewAsync` は `GetPlayersAsync`（バッチ `WHERE Id IN`）を使っており、こちらが正しいパターンです。チャット/スレッドも同方式へ統一すべき。

**推奨:** `IPlayerRepository.GetPlayersAsync(ids)` でバッチ取得に統一。

### 2-2. 【Medium】ランキング再集計の負荷
`RankingAggregationService.RebuildAsync`（`:13`）は全プレイヤー取得＋クエストクリア/宝の地図/ペットレートの複数GROUP BY＋数百エントリINSERT。6時間ごと（`RankingRebuildBackgroundService.cs`）かつトランザクション内で完結しており、頻度的には許容範囲ですが、`ranking_entries` の一意インデックスが広い複合（`SnapshotId, RankingType, PeriodKind, CombatIndexRank, RankPosition`、`AppDbContext.cs:678`）で挿入コストが高め。問題が顕在化したらバッチINSERTの調整を。

### 2-3. 【Low】戦闘力ランクの再計算
`RankingReadService.GetLatestAsync` / `/players` / `/rankings` で、保存済みスコアとは別に **読み取り時に毎回 `CombatIndexCalculator.Calculate` を再実行** してランクを算出。計算自体は軽量だが、全件×毎リクエスト。集計時に算出済みの `CombatIndexRank` を保存・返却すれば再計算を省ける。

---

## 3. 冗長なコード

| 箇所 | 内容 |
|------|------|
| `MaintenanceEndpoints.cs:170` と `RankingEndpoints.cs` の `SecureEquals` | 同名メソッドが2実装。Maintenance側は `CryptographicOperations.FixedTimeEquals`（定数時間）だが、Ranking側は自前ループで **非定数時間**（4-2参照）。共通化すべき |
| `MaintenanceEndpoints.cs` の保守トークン検証 | 4つのエンドポイントで「トークン未設定チェック→ヘッダ取得→`SecureEquals`」をコピペ。フィルタ/拡張メソッドに集約可能 |
| `ItemEndpoints.cs:832` `IsTreasureMapItemAsync` / `ResolveTreasureMapItemIdsAsync` | 1リクエスト内で複数回 `treasureMapRepository.GetAllAsync()` を解決し直す（`/items`、`/market/listings` 等）。1度解決して使い回すべき |
| `PlayerEndpoints.cs` / `ItemEndpoints.cs` の `MapToDomain`/`ApplyEntity` | プレイヤー装備・アイテムスタックのマッピングが複数ファイルに重複。リポジトリ層へ集約可能 |
| `MapToPlayerResponse` 内のジョブ表示オブジェクト生成 | `new { code, value, displayName, description }` が各エンドポイントに散在。ヘルパー化済みの `GetJobDisplayName` 同様、ジョブDTO生成も共通化できる |
| `BuildRewardTendency`/`BuildRewardCandidates`（`TreasureMapEndpoints.cs:626`） | 0埋めの空オブジェクト生成が重複 |

---

複数のミューテーションが **「キャッシュからプレイヤー取得 → ドメインで消費 → SaveAsync」** の Read-Modify-Write を、**行ロックも楽観的並行制御も無し** で行っています。`PlayerEntity` には並行トークン（`xmin`/version）がありません。
## 4. 安全でないクエリ・更新処理（最重要）

### 4-1. 【High】ゴールド/ステータス更新の競合・二重消費

該当箇所:
- 転職ロードマップ解放: `JobRoadmapService.cs:111` `player.SpendGold(goldCost)` → `SaveAsync`
- 装備合成: `ItemEndpoints.cs:262` `player.SpendGold(goldCost)` → `SaveAsync`
- アイテム使用（ステータスブースト/転職/経験値倍率/マップ解放）: `ItemEndpoints.cs:200` 付近
- 転生: `PlayerRebirthService.cs`

問題:
1. `GetPlayerAsync` は **5分キャッシュ**（`DbPlayerRepository.cs:23`、`PlayerCacheConstants.CacheTtl`）。並行リクエストや直前更新後に **古いゴールド残高** を読み、`SaveAsync` で全カラム上書きするため **ロストアップデート** が起きる。
2. 同一プレイヤーが2リクエストを同時送信すると、両方が同じ残高を読んで両方成功し、**ゴールドの二重使用やアイテム複製** につながる（経済破綻リスク）。

> 正しく対処している箇所との対比が明確です。プレゼント送信（`PlayerEndpoints.cs:524` `FOR UPDATE`）、マーケット購入（`ItemEndpoints.cs:583` `FOR UPDATE`）、ペット育成（`DbPetTrainingExecutor.cs` `FOR UPDATE`）、宝の地図受取（`TreasureMapEndpoints.cs:251` `FOR UPDATE` + advisory lock）は **明示的に行ロックを取得** しています。ゴールドを動かす全経路を同水準に揃えるべきです。

**推奨:**
- ゴールド/ステータスを変更する全経路を、`FOR UPDATE` で `players` 行をロックする単一トランザクション（あるいは `PlayerEntity` に並行トークン＋リトライ）に統一する。
- 少なくとも更新系では **キャッシュをバイパスして実DBから読む**。

### 4-2. 【Medium】`RankingEndpoints.SecureEquals` が非定数時間
`RankingEndpoints.cs` 末尾の `SecureEquals` は、長さ不一致で即 `false` を返し、ループ内で `char` をXORします。`MaintenanceEndpoints` の `CryptographicOperations.FixedTimeEquals`（定数時間）と挙動が異なり、**保守トークンに対するタイミング攻撃** の余地が理論上残ります。同一トークンを扱うのに実装が割れているのも保守上の危険。

**推奨:** `FixedTimeEquals` ベースの実装に一本化。

### 4-3. 【Low】宝の地図報酬シードが予測可能
`TreasureMapEndpoints.cs:598` `BuildRewardSeed = HashCode.Combine(expeditionId, playerId, mapId)` を `new Random(seed)` に使用。`HashCode.Combine` はプロセス内でランダム化されるものの32bitであり、報酬抽選が決定的になります。経済上クリティカルでなければ許容ですが、レア報酬を扱うなら `RandomNumberGenerator` ベースを検討。

### 4-4. 補足: RawSQL のインジェクション安全性は良好
`FromSqlInterpolated` / `ExecuteSqlInterpolated` / `SqlQueryRaw("... {0}", arg)` はいずれもパラメータ化されており、文字列連結による注入は確認されませんでした（`DbPlayerRepository.cs:140`、`DbChatRoomRepository`、`TreasureMapEndpoints.cs:194` 等）。この点は適切です。

---

## 5. その他の脆弱性・運用リスク

### 5-1. 【Medium】個人チャットの所有者チェック欠如
`ChatEndpoints.cs:12` `GET /chat/room?ownerId=` は **任意の `ownerId`** を受け取り、所有者本人かの検証なくメッセージ一覧を返します。他プレイヤー宛の個人チャット内容を閲覧可能です。

> 「プレイヤー訪問（ゲストブック）」として意図的に公開している可能性もあります（送信は `IsAnonymousUser` チェックあり、`ChatEndpoints.cs:42`）。**意図を確認**し、非公開が正なら閲覧時も本人/関係者チェックを追加してください。

### 5-2. 【Low】匿名ユーザー制限が投稿系のみ
`EndpointHelpers.IsAnonymousUser` はチャット/スレッド投稿でのみ検査（`ChatEndpoints.cs:42`、`ThreadEndpoints.cs:63`）。マーケット出品・プレゼント送信・ペット操作などの状態変更系では未チェック。匿名アカウントに許可すべきでない操作があれば、ミューテーション全体で方針を統一すべき。

### 5-3. 【Low】CORS が広すぎる / レート制限なし
`Program.cs:53` で `AllowAnyHeader().AllowAnyMethod()`。オリジンは限定されているものの、メソッド/ヘッダは絞れます。また全体で **レート制限（Rate Limiting）が未導入** で、4-1の二重消費やスパム投稿、ポーリング過多に対する防御がありません。`AddRateLimiter` の導入を推奨。

### 5-4. 【Low】保守トークンの共用
`Maintenance:MarketCleanupToken` 一つで、アクティブ人数集計・マーケット清掃・クエストデータ削除・ランキング再集計・**開発用ゲームデータ全削除**（`MaintenanceEndpoints.cs:73`）まで認可しています。`cleanup-game-data` はループバック以外ではトークン necessary ですが、破壊力が大きいため別トークン/別経路にするのが安全。

### 5-5. 【Low】クエストチャットのメッセージ長が無制限
`QuestChatMessage`（`domain/quest/run/QuestChatMessage.cs:15`）は必須・トリムのみで **最大長チェックが無い**。一方、通常チャットは `ChatConstants.MessageMaxLength=200` で制限（`ChatEndpoints.cs:47`）。クエストチャットは `chat_messages_json`（jsonb）に蓄積され、1-1の全再書き込み対象でもあるため、長文連投で **jsonb肥大化→Egress増** を招きます。最大長と保持件数の上限を設けるべき。

---

## 付録: 良好な実装（参考）

監査の公平性のため、適切に実装されている箇所も記録します。
- **行ロックの正しい使用**: プレゼント送信・マーケット購入・ペット育成・宝の地図受取（`FOR UPDATE` / advisory lock）。
- **バッチ取得**: `GlobalChatService` の `GetPlayersAsync`、マーケット出品装備の `LoadListedPlayerEquipmentSnapshotMapAsync`（`WHERE Id IN`）。
- **Pet Battle のインデックス設計**: `ix_pet_battle_runs_status_deadline` ほか部分ユニークインデックスが適切。
- **定数時間比較**: `MaintenanceEndpoints.SecureEquals`。
- **パラメータ化クエリ**: 生SQLは全て補間/プレースホルダ経由。
- **直列化分離レベル**: チャット保存が `Serializable` + 競合リトライ指示（`DbChatRoomRepository`）。

---

## 推奨対応の優先順位

1. **#4-1 ゴールド更新の競合対策**（経済の整合性に直結。最優先）
2. **#1-1 クエストRun保存の差分更新化** + **#1-2 タイムアウト用インデックス追加**（Egress削減の最大効果）
3. **#1-3 / #1-4 N+1・二重ロードの解消**
4. **#5-1 チャット所有者チェック**（意図確認の上）
5. **#4-2 / #3 SecureEquals 一本化**、**#1-6 last_active_at 間引き**、**#5-3 レート制限導入**
