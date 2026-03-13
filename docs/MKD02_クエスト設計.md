# クエスト設計ドラフト

## 1. 目的

本ドキュメントは `docs/MKD01_クエスト機能定義.md` を、既存の `Player` / `Move` ドメインと `internal` スキーマ運用へ接続するための設計ドラフトである。
現時点では以下を目的とする。

* クエスト機能の責務境界を定義する。
* 永続化が必要な情報と、クエスト実行中だけ必要な情報を切り分ける。
* `docs/MMD01_CoreDomain.mmd` と `docs/MMD11_ER図.mmd` を更新する前段として、用語と状態遷移を固める。

## 2. 今回の主要決定事項

### 2.1 `QuestRoom` と `QuestRun` の責務境界

* `QuestRoom` は募集と参加確定までを扱う。
* `QuestRun` は開始後の戦闘進行のみを扱う。
* 開始後の離脱、放置による自動行動化、成功失敗判定は `QuestRun` 側の責務とする。
* `QuestRoom` は開始後も参照用に残すが、進行状態は持たない。

### 2.2 出撃人数

* 盤面は `前衛 / 中衛 / 後衛` × `左列 / 右列` の 6 マスとする。
* 出撃人数の上限は 6 人とする。
* 出撃人数の下限は 4 人とする。
* ソロは人間プレイヤー 1 人、マルチは人間プレイヤー 2 人以上 6 人以下で開始可能とする。
* 開始時点の出撃人数が 4 人未満なら、NPC を補充して 4 人に揃える。
* 4 人以上 6 人以下で出撃可能な場合は、空きマスがあっても NPC で 6 人まで埋めない。

### 2.3 放置時の扱い

* 「キック」は冒険用テーブルの状態としては持たない。
* 60 秒未入力は退場ではなく、`QuestRunPartyMemberState.ActionMode = AutoAttackOnly` への移行として扱う。
* `AutoAttackOnly` から `Manual` へ戻すには、参加者本人の明示的な手動復帰操作と、オーナー承認の両方を必要とする。

### 2.4 スナップショット分離

* `QuestRoom` の参加者情報と、クエスト開始時の戦闘スナップショットは分離する。
* 待機中の参加者は `QuestParticipant` が持ち、開始時点の戦闘能力は `QuestRunPartyMemberSnapshot` が持つ。

## 3. 前提

### 3.1 既存ドメインの再利用

* プレイヤーの恒久情報は既存の `CoreDomain.Player` を正とする。
* 技マスタは既存の `MoveDomain.Move` を利用し、`move_id` は既存 CSV マスタを参照する。
* `Player` はクライアント表示用の `ImagePath` を持ち、`internal.players` に保存する。
* `Move` は必要な場合だけクライアント表示用の `EffectImagePath` を持てる。値は既存 CSV マスタに保持し、未設定を許容する。
* `QuestEnemyDefinition` は敵 CSV の `ImagePath` を参照し、クライアントで敵画像表示に利用する。
* クエスト開始時に `Player` の `Job` / `Status` / `MoveSet` / `ImagePath` をスナップショット化し、クエスト中はそのスナップショットを参照する。

### 3.2 配置方針

* 配置はジョブによって制限しない。
* これはジョブ被り時に配置不能を避けるためである。
* 一方で各ジョブには適正配置が存在し、性能差によって自然に有利不利が生じる前提で設計する。
* パーティ全体の配置決定権はオーナーが持つ。

### 3.3 スコープ

今回のドラフトでは以下をスコープ内とする。

* ルーム募集
* 参加者配置
* NPC 補充
* 階層進行
* ターン入力と同時解決
* 放置による自動操作移行
* ページ離脱後の復帰に必要な永続化

今回のドラフトでは以下をスコープ外または簡略化対象とする。

* アイテム所持品の永続化
* クエスト報酬の最終反映先テーブル
* 敵 AI の詳細アルゴリズム
* 戦闘ログの完全リプレイ
* 装備システムの詳細

## 4. 設計方針

### 4.1 境界づけられたコンテキスト

クエスト機能は新規に `QuestDomain` として切り出す。
ただし以下は既存コンテキストを参照する。

* `CoreDomain.PlayerId`
* `CoreDomain.Job`
* `CoreDomain.Status`
* `MoveDomain.MoveId`
* `MoveDomain` の各種 enum

### 4.2 開始境界

クエスト開始時の処理は以下の順で扱う。

1. `QuestRoom.CanStart()` が成立する。
2. `QuestNpcAssignmentService.AssignForStart(room, minPartyMemberCount)` が、出撃人数を 4 人に満たすための NPC テンプレートを選択する。
3. `QuestRoom.AddNpcParticipants(npcTemplates)` で NPC 参加者を確定追加する。
4. `QuestRoom.CloseRecruitment()` で募集を締める。
5. `QuestSnapshotFactory.Create(roomParticipants, players, npcTemplates)` が、開始時点の `QuestRunPartyMemberSnapshot` を生成する。
6. `QuestRunFactory.Create(stageDefinition, snapshots, now)` が `QuestRun` を生成する。
7. `QuestRun.InitializeFirstFloor()` が初期敵配置、初期ターン、初期行動期限を設定する。

`QuestRoom` 自体はこの時点で `Closed` になり、その後は参照用に近い扱いとする。

### 4.3 スナップショット方針

クエスト中にプレイヤー本体の `Status` や `MoveSet` や `ImagePath` が変更されても、進行中クエストには反映しない。
理由は以下の通り。

* ページ再接続時に同一状態を復元しやすい。
* クエスト途中で外部更新が入ると再現性と整合性が崩れる。
* 将来的に装備や一時バフが増えても、開始時点の戦闘能力として閉じ込められる。

### 4.4 JSON 利用方針

* 集約の不変条件判定に使う主キー関係、状態遷移、参加者識別、階層番号、ターン番号は通常カラムで持つ。
* HP / MP / 行動モード / 行動可能ターンのような進行制御の骨格は通常カラムで持つ。
* 種類追加が頻繁な状態異常、バフ、一時的な派生パラメータ、最新ターンの解決結果は `jsonb` に寄せる。
* 行動入力の対象指定は、対象 actor id ではなく `BattlePosition` を基準に保持する。

### 4.5 1ルーム1出撃方針

* 1 つの `QuestRoom` は 1 回の出撃にのみ対応する。
* 再挑戦時は新規 `QuestRoom` を作成する。
* そのため `quest_runs.room_id` は `UNIQUE` を前提とする。

### 4.6 配置責務とレイヤ配置

* `QuestNpcAssignmentService` と `QuestSnapshotFactory` は、既存プレイヤー情報や NPC テンプレート取得を伴うため、アプリケーション層の協調コンポーネントとして扱う。
* `QuestRunFactory` は、入力値が揃った後に `QuestRun` を組み立てるファクトリとして扱う。
* ドメインモデル図では依存関係の詳細な表記はこの文書の責務外とし、概念と責務に集中する。

### 4.7 戦闘ドメインとの接続方針

* 隊列による射程制御は `QuestDomain` 固有の要件として扱い、`Training` には持ち込まない。
* 汎用の `BattleDomain` は、位置情報が渡された場合だけ隊列射程を有効化できる設計とする。
* `QuestBattleFactory` は、`QuestRunPartyMemberSnapshot.StartPosition` と `QuestEnemyPlacement.Position` から `BattleFieldContext` を組み立て、`BattleService` に渡す。
* `BattleFieldContext` がない場合、`BattleDomain` は従来どおり `AttackRange` による件数制御だけを行う。
* `Training` は `BattleFieldContext` を渡さない利用形態とし、前衛・中衛・後衛による射程制御を適用しない。
* 隊列射程の最終判定責務は `BattleActionResolver` 直書きではなく、専用の `BattleTargetingResolver` に分離する。
* 現在の `BattleTargetingResolver` は、前衛を相手前衛まで、中衛を相手前衛・中衛まで、後衛を相手全行まで到達可能とみなし、プレイヤー/敵で同じルールを適用する。
* 現在の `BattleTargetingResolver` は、物理/魔法/回復/バフで射程ルールを分けず、位置情報と `AttackRange` だけで対象を決める。
* `BattleActionResult` はログ表示と演出再生のため、`ActorId` に加えて `ActionKind` と `MoveId` を持つ。
* これによりクエスト側は、ターン解決後の各行動について「だれが」「何の行動を」「どの技で」行ったかを結果 DTO だけで把握できる。
* 単体攻撃や単体対象技の入力は「どのマスを狙ったか」を `BattlePosition` で保持し、解決時にその座標にいる対象へ適用する。
* 範囲攻撃は、入力時に選んだ `BattlePosition` を起点として、`AttackRange` と `BattleFieldContext` に従って最終対象を展開する。

### 4.8 API / 通知方針

* クライアント向けの進行状態は、ドメインモデルや永続化モデルをそのまま返さず、公開用 DTO として `QuestRunDetailResponse` に射影して返す。
* `QuestRunDetailResponse` には、パーティ進行の把握、戦闘画面の再描画、直前ターン演出の再生に必要な情報を含める。
* 一方で、未公開の内部管理情報、楽観ロック用 `version`、内部 ID 対応のうちクライアントで不要なもの、敵 AI 内部値やサーバー都合のメタ情報は含めない。
* 直前ターンの解決結果は `QuestRunDetailResponse.LastTurnResults` として返し、`quest_runs.last_turn_results_json` をその公開用 View に対応づける。
* 行動入力 API は `POST /quest/runs/{runId}/commands` を主入口とし、コマンド受信後に未入力者が 0 人なら、そのリクエスト内でターン解決まで行う。
* ターン解決後の最新状態通知は SignalR を用いて行う。クライアントは `QuestRunHub` へ接続し、`runId` 単位のグループへ参加する。
* サーバーはターン解決完了後、対象 `runId` グループへ `QuestRunDetailResponse` を broadcast する。
* `POST /quest/runs/{runId}/commands` の HTTP 応答は受付結果の返却を主とし、画面更新の正本は SignalR 通知とする。
* `GET /quest/runs/{runId}` は初期表示や再接続復元のために `QuestRunDetailResponse` を返す読み取り API として残す。
* `POST /quest/runs/{runId}/manual-control/request` と `POST /quest/runs/{runId}/manual-control/approve` は、状態更新後に同様に `QuestRunDetailResponse` を SignalR 通知できる構成とする。

### 4.9 `QuestRunHub` イベント契約

`QuestRunHub` は SignalR Hub として実装し、クライアントは接続後に `runId` ごとのグループ購読を行う。

#### クライアント -> サーバー

* `SubscribeRun(runId)`
  * 指定した `runId` のグループへ参加する。
  * 参加成功後、サーバーは現在状態を `QuestRunSnapshot` で返してよい。
* `UnsubscribeRun(runId)`
  * 指定した `runId` のグループから離脱する。

#### サーバー -> クライアント

* `QuestRunSnapshot`
  * 接続直後または購読直後に現在状態を 1 回返すイベント。
  * payload は `QuestRunDetailResponse` とする。
* `QuestRunUpdated`
  * コマンド登録後のターン解決、手動復帰申請、手動復帰承認、その他進行状態変更時に返すイベント。
  * payload は `QuestRunDetailResponse` とする。
* `QuestRunError`
  * 購読失敗、認可失敗、不正な `runId` 指定などを返すイベント。
  * payload は `code`, `message` を持つ簡易エラー DTO とする。

初期実装では `QuestRunSnapshot` と `QuestRunUpdated` の payload を統一し、クライアントは受信した `QuestRunDetailResponse` で画面状態を丸ごと差し替える前提とする。

### 4.10 `QuestRunDetailResponse` View 案

`QuestRunDetailResponse` は、クエスト画面の初期描画、再接続復元、進行中更新通知を同一形で扱うための公開 View とする。

#### ルート項目

* `RunId`
* `RoomId`
* `StageId`
* `Status` (`InProgress`, `Succeeded`, `Failed`, `Aborted`)
* `Floor`
* `Turn`
* `PartyMembers`
* `Enemies`
* `PendingCommands`
* `ChatMessages`
* `Rewards`
* `LastTurnResults`

#### `Floor`

* `CurrentFloorNo`
* `IsBossFloor`

#### `Turn`

* `CurrentTurnNo`
* `ActionDeadlineAt`
* `WaitingParticipantIds`

#### `PartyMembers`

各要素は `QuestPartyMemberView` とし、以下を持つ。

* `ParticipantId`
* `Type` (`Player`, `Npc`)
* `DisplayName`
* `ImagePath`
* `Position`
* `CurrentHp`
* `CurrentMp`
* `MaxHp`
* `MaxMp`
* `IsDead`
* `CanActFromTurn`
* `ActionMode` (`Manual`, `AutoAttackOnly`)
* `ActiveEffects`

#### `Enemies`

各要素は `QuestEnemyView` とし、以下を持つ。

* `EnemyInstanceId`
* `EnemyDefinitionId`
* `Name`
* `ImagePath`
* `Position`
* `CurrentHp`
* `CurrentMp`
* `MaxHp`
* `MaxMp`
* `IsDead`
* `ActiveEffects`

#### `PendingCommands`

各要素は `QuestPendingCommandView` とし、以下を持つ。

* `ParticipantId`
* `TurnNo`
* `ActionKind` (`NormalAttack`, `UseMove`, `Guard`, `Wait`, `LeaveQuest`, `Escape`)
* `MoveId`
* `SelectedTargetPosition`
* `IsAutoSubmitted`
* `SubmittedAt`

`PendingCommands` は初期実装では入力済みコマンド内容まで含めるが、後に秘匿要件が出た場合は `SubmittedParticipantIds` のような縮約 View へ差し替えられる余地を残す。

#### `ChatMessages`

各要素は `QuestChatMessageView` とし、以下を持つ。

* `SenderParticipantId`
* `DisplayName`
* `ImagePath`
* `Message`
* `SentAt`

#### `Rewards`

`QuestRewardView` として以下を持つ。

* `Exp`

#### `ActiveEffects`

パーティ / 敵の状態異常とバフは `QuestActiveEffectView` として表現し、以下を持つ。

* `EffectType`
* `DisplayName`
* `RemainingTurns`
* `Stacks`

### 4.11 `LastTurnResults` View 案

`LastTurnResults` は、直前ターンの演出再生、行動ログ表示、階層遷移 / クエスト終了演出の判断に必要な情報を返す。

#### `QuestLastTurnResultsView`

* `TurnNo`
* `ResolvedAt`
* `Actions`
* `FloorTransition`
* `RunTransition`

#### `Actions`

各要素は `QuestResolvedActionView` とし、以下を持つ。

* `ActorParticipantId`
* `ActorEnemyInstanceId`
* `ActorDisplayName`
* `ActionKind` (`NormalAttack`, `UseMove`, `Guard`, `Wait`, `LeaveQuest`, `Escape`)
* `MoveId`
* `MoveName`
* `TargetSummaries`
* `Logs`

味方と敵のどちらが行動主体でも扱えるよう、`ActorParticipantId` と `ActorEnemyInstanceId` は排他的に利用する。

#### `TargetSummaries`

各要素は `QuestActionTargetResultView` とし、以下を持つ。

* `TargetParticipantId`
* `TargetEnemyInstanceId`
* `TargetDisplayName`
* `ResultType` (`Hit`, `Miss`, `Guarded`, `Healed`, `BuffApplied`, `AilmentApplied`, `Defeated`)
* `HpChange`
* `MpChange`
* `AppliedEffects`
* `RemovedEffects`
* `IsDeadAfterAction`

#### `FloorTransition`

階層遷移が発生した場合のみ `QuestFloorTransitionView` を返し、以下を持つ。

* `PreviousFloorNo`
* `CurrentFloorNo`
* `FloorCleared`
* `BossFloorReached`

#### `RunTransition`

クエスト状態変化の有無を表す `QuestRunTransitionView` として、以下を持つ。

* `PreviousStatus`
* `CurrentStatus`
* `QuestEnded`

### 4.12 ルーム一覧 API 方針

参加可能ルーム一覧の表示のため、`GET /quest/rooms` を追加する。

#### 用途

* 募集中ルームの一覧表示
* 参加画面でのステージ選択後の候補表示
* 再接続導線での自分の参加中ルーム確認

#### クエリ条件案

* `stageId`
* `mode` (`Solo`, `Multi`)
* `status` (`Recruiting`, `Closed`)
* `ownerPlayerId`
* `page`
* `pageSize`

初期実装では、`status` 未指定時は `Recruiting` を既定値とする。
並び順は `createdAt desc` を既定とし、ページングは `page = 1`, `pageSize = 20` を初期値とする。
`pageSize` は最大 100 までに丸める。

#### レスポンス View

`QuestRoomSummaryResponse` として、各要素に以下を持つ。

* `RoomId`
* `StageId`
* `StageName`
* `Mode`
* `Status`
* `OwnerPlayerId`
* `OwnerDisplayName`
* `ParticipantCount`
* `MinPartyMemberCount`
* `MaxPartyMemberCount`
* `CreatedAt`

一覧用途では詳細な配置情報や参加者全件は返さず、ルームカード描画に必要な情報へ絞る。

### 4.13 手動復帰 API 方針

手動復帰は HTTP API で要求を受け付け、状態更新後は SignalR で `QuestRunDetailResponse` を通知する。

#### `POST /quest/runs/{runId}/manual-control/request`

`AutoAttackOnly` に移行した参加者本人が、自分の手動復帰を申請するための API とする。

リクエスト:

* `ParticipantId`

認可:

* 実行者本人のみ呼び出せる。
* 対象参加者が `AutoAttackOnly` である場合のみ受け付ける。

結果:

* 手動復帰申請中状態を `QuestRun` に反映する。
* 更新後は `QuestRunUpdated` で `QuestRunDetailResponse` を対象 `runId` グループへ通知する。
* HTTP 応答は受付成否のみを返す。

#### `POST /quest/runs/{runId}/manual-control/approve`

オーナーが手動復帰申請を承認するための API とする。

リクエスト:

* `ParticipantId`

認可:

* ルームオーナーのみ呼び出せる。
* 対象参加者に未処理の手動復帰申請がある場合のみ受け付ける。

結果:

* 対象参加者の `ActionMode` を `Manual` へ戻す。
* 更新後は `QuestRunUpdated` で `QuestRunDetailResponse` を対象 `runId` グループへ通知する。
* HTTP 応答は受付成否のみを返す。

#### 手動復帰表示のための View

`QuestRunDetailResponse.PartyMembers` の各要素に、以下の復帰状態項目を追加してよい。

* `ManualControlRequestStatus` (`None`, `Pending`)

初期実装では申請履歴全件ではなく、現在ターン時点の最新状態だけを返せばよい。

### 4.14 タイムアウト進行の起動方針

未入力者が 0 人になった時点のターン解決は `POST /quest/runs/{runId}/commands` 内で実行する。
一方で、誰も追加操作しないまま `ActionDeadlineAt` を超過したケースに備え、タイムアウト進行の起動契機を定める。

初期実装では以下の方針を採る。

* サーバー側に定期実行ジョブを用意し、期限切れの `QuestRun` を走査する。
* 期限切れを検知した `QuestRun` について、未入力の `Manual` 参加者を `AutoAttackOnly` へ切り替え、自動コマンドを投入する。
* その結果として未入力者が 0 人になれば、その場でターン解決まで行う。
* 解決後は `QuestRunUpdated` で `QuestRunDetailResponse` を通知する。

`GET /quest/runs/{runId}` では副作用を持たせず、読み取り専用とする。
これにより、画面再表示やポーリング取得によって進行が偶発的に進むことを避ける。

### 4.15 `QuestRun` 認可モデル方針

初期実装では、進行中クエストの認可は `QuestRun` 単体では閉じず、`QuestRoom` の参加者情報を参照して判定する。
将来的には `QuestRun` 側で完結できる形へ寄せる余地を残す。

#### 方針

* `commands`、`chat`、`manual-control/request` など本人起点 API は、`QuestRun.RoomId` から `QuestRoom` を参照し、`ParticipantId` と `PlayerId` の対応で認可する。
* `manual-control/approve` のようなオーナー権限が必要な API も、`QuestRoom.OwnerId` を参照して判定する。
* `QuestRun` 自体は進行画面用のスナップショット情報を持つが、認可用の `PlayerId` 対応表は初期実装では保持しない。

#### 認可判断の例

* `POST /quest/runs/{runId}/commands`
  * `QuestRoom` 上で実行者に対応する `ParticipantId` のコマンド送信のみ許可する。
* `POST /quest/runs/{runId}/manual-control/request`
  * `QuestRoom` 上で実行者に対応する `ParticipantId` の復帰申請のみ許可する。
* `POST /quest/runs/{runId}/manual-control/approve`
  * `QuestRoom.OwnerId` と一致する実行者のみ許可する。
* クエスト中チャット
  * `QuestRoom` に紐づく参加者本人のみ投稿を許可する。

この方針により、初期実装では既存のルーム参加情報をそのまま認可に利用する。
将来 `QuestRun` 側へ認可スナップショットを持たせる場合は、この節を更新する。

## 5. ドメインモデル案

### 5.1 集約一覧

| 集約 | 種別 | 主責務 |
| --- | --- | --- |
| `QuestStageDefinition` | Aggregate Root | ステージのマスタ定義を持つ。実行中状態と区別するため `Definition` を付ける |
| `QuestRoom` | Aggregate Root | 募集状態、参加者、配置、開始条件、募集終了までのライフサイクルを管理する |
| `QuestRun` | Aggregate Root | 1 回の出撃実行を表し、開始後の階層進行、戦闘状態、行動入力、放置による自動操作、成功失敗を管理する |

`QuestStageDefinition` はマスタ定義であることを明確にするため `QuestStage` ではなくこの名前を採用する。
`QuestRun` は実行インスタンスそのものを表すため、動作主体を想起させる `QuestRunner` ではなく `Run` を採用する。

### 5.2 値オブジェクト / enum 案

* `BattleRow`
  * `Front`
  * `Middle`
  * `Back`
* `BattleColumn`
  * `Left`
  * `Right`
* `BattlePosition`
  * `BattleRow Row`
  * `BattleColumn Column`
* `QuestFloorRewardRule`
  * `decimal ExpRate`
  * `decimal GoldRate`

### 5.3 `QuestStageDefinition` 案

#### エンティティ

* `QuestStageDefinition`
  * `QuestStageId Id`
  * `string StageCode`
  * `string Name`
  * `int RecommendedLevel`
  * `int MinPartyMemberCount`
  * `int MaxPartyMemberCount`
  * `IReadOnlyList<QuestFloorDefinition> Floors`
  * `bool IsActive`
* `QuestFloorDefinition`
  * `int FloorNo`
  * `FloorType FloorType` (`Normal`, `Boss`)
  * `IReadOnlyList<QuestEnemyPlacement> Placements`
  * `QuestFloorRewardRule RewardRule`
* `QuestEnemyPlacement`
  * `int PlacementNo`
  * `QuestEnemyDefinitionId EnemyDefinitionId`
  * `BattlePosition Position`

#### 用語補足

* `StageCode` は API やフロントエンドから参照しやすい安定識別子であり、表示名とは別に持つ。例: `beginner-forest`。
* `MinPartyMemberCount` は開始時に満たすべき最低出撃人数であり、本ドラフトでは 4 を想定する。
* `MaxPartyMemberCount` は盤面に配置可能な最大人数であり、本ドラフトでは 6 を想定する。
* `QuestStageId` / `QuestEnemyDefinitionId` / `QuestNpcTemplateId` は CSV マスタで人が扱いやすい連番 `int` を使う。
* `QuestEnemyDefinitionId` は敵マスタを指す識別子であり、実行中の敵個体を指す `QuestEnemyInstanceId` と区別するため `Definition` を付ける。

#### 主な責務

* 階層数とボス階層を定義する。
* 各階層の敵出現位置を定義する。
* 最低出撃人数と出撃上限を定義する。

### 5.4 `QuestRoom` 案

#### 役割

`QuestRoom` は待機室の集約であり、クエスト開始前の募集・参加・配置だけを扱う。
開始後の進行状態は持たない。

#### エンティティ

* `QuestRoom`
  * `QuestRoomId Id`
  * `PlayerId OwnerId`
  * `QuestStageId StageId`
  * `QuestRoomMode Mode` (`Solo`, `Multi`)
  * `QuestRoomStatus Status` (`Recruiting`, `Closed`)
  * `FormationLayout Formation`
  * `IReadOnlyList<QuestParticipant> Participants`
  * `QuestRoomCloseReason? CloseReason` (`Started`, `Cancelled`, `Expired`)
  * `DateTimeOffset CreatedAt`
  * `DateTimeOffset? ClosedAt`
* `QuestParticipant`
  * `QuestParticipantId Id`
  * `ParticipantType Type` (`Player`, `Npc`)
  * `PlayerId? PlayerId`
  * `QuestNpcTemplateId? NpcTemplateId`
  * `string DisplayName`
  * `ParticipantStatus Status` (`Joined`, `Disconnected`, `Left`)
  * `BattlePosition Position`
  * `bool IsOwner`
  * `DateTimeOffset JoinedAt`
  * `DateTimeOffset? LastSeenAt`
  * `DateTimeOffset? LeftAt`
* `FormationLayout`
  * 6 マスの占有状態を持つ値オブジェクト

#### 用語補足

* `QuestParticipantId` はルーム参加単位の識別子であり、NPC も含めて一意に扱うため `PlayerId` とは分ける。
* 同一プレイヤーでも別ルームに参加すれば別の `QuestParticipantId` を持つ。
* `QuestParticipant.Status` の `Disconnected` は募集フェーズにおける接続状態を表し、開始後の放置 / 自動操作状態とは無関係とする。

#### 主な振る舞い

* `AddPlayer(playerId, displayName)`
* `AssignPosition(participantId, position)`
* `MarkDisconnected(participantId, at)`
* `MarkReconnected(participantId, at)`
* `RemoveParticipant(participantId, at)`
* `CanStart()`
* `AddNpcParticipants(npcTemplates)`
* `CloseRecruitment(reason, at)`

#### 不変条件

* オーナーは常に 1 人。
* `Recruiting` 中のみ参加者追加・配置変更が可能。
* 同一マスに複数参加者は配置できない。
* `Solo` はオーナー 1 人で開始可能。
* `Multi` は人間プレイヤー 2 人以上で開始可能。
* NPC 補充は `CloseRecruitment()` の前に行い、`Closed` になった後は参加者構成を変更できない。

### 5.5 開始時スナップショット

#### `QuestRunPartyMemberSnapshot`

`QuestParticipant` とは別に、開始時点の戦闘能力を固定化するためのオブジェクトを持つ。

* `QuestParticipantId ParticipantId`
* `ParticipantType Type`
* `string DisplayName`
* `string? ImagePath`
* `Job Job`
* `Status BaseStatus`
* `MoveSet MoveSet`
* `BattlePosition StartPosition`
* `ActionMode InitialActionMode` (`Manual`, `AutoAttackOnly`)

#### 役割

* 待機中の参加者情報と戦闘能力を分離する。
* `QuestRun` はこのスナップショットだけを参照して開始できる。
* 将来、装備補正や一時バフを開始時に織り込む拡張点にする。
* `MoveSet` はクエスト開始時点で使用可能な技スロット構成を表す。詳細効果は `MoveDomain.Move` を参照し、クエスト固有の使用制約がある場合はスナップショット側で補助情報を持つ余地を残す。
* `ImagePath` はクライアントの戦闘表示に使う見た目情報であり、進行中クエスト中のプレイヤー画像差し替えの影響を受けないよう開始時点で固定する。

### 5.6 `QuestRun` 案

#### 役割

`QuestRun` はクエスト開始後の進行を表す集約である。
ただし内部責務が太くなりやすいため、内部構造を以下の概念に分けて扱う前提とする。

* `QuestFloorState`: 現在階層、階層遷移、階層敵配置
* `QuestBattleState`: 味方状態、敵状態、戦闘不能、状態異常、バフ
* `QuestTurnState`: ターン番号、入力期限、入力済みコマンド
* `QuestTrapCollection`: 階層持ち越し罠
* `QuestRewardAccumulator`: 累積経験値

これらは初期段階では `QuestRun` 配下の内部オブジェクトとして扱い、別集約には分けない。

#### エンティティ

* `QuestRun`
  * `QuestRunId Id`
  * `QuestRoomId RoomId`
  * `QuestStageId StageId`
  * `QuestRunStatus Status` (`InProgress`, `Succeeded`, `Failed`, `Aborted`)
  * `QuestFloorState FloorState`
  * `QuestBattleState BattleState`
  * `QuestTurnState TurnState`
  * `QuestTrapCollection Traps`
  * `QuestRewardAccumulator Rewards`
  * `IReadOnlyList<QuestChatMessage> ChatMessages`
  * `DateTimeOffset StartedAt`
  * `DateTimeOffset? EndedAt`
* `QuestFloorState`
  * `int CurrentFloorNo`
  * `bool IsBossFloor`
* `QuestTurnState`
  * `int CurrentTurnNo`
  * `DateTimeOffset ActionDeadlineAt`
  * `IReadOnlyList<QuestTurnCommand> PendingCommands`
* `QuestBattleState`
  * `IReadOnlyList<QuestRunPartyMemberState> PartyMembers`
  * `IReadOnlyList<QuestEnemyState> Enemies`
* `QuestRunPartyMemberState`
  * `QuestParticipantId ParticipantId`
  * `int CurrentHp`
  * `int CurrentMp`
  * `bool IsDead`
  * `int CanActFromTurn`
  * `ActionMode ActionMode` (`Manual`, `AutoAttackOnly`)
  * `IReadOnlyList<ActiveAilment>`
  * `IReadOnlyList<ActiveBuff>`
* `QuestEnemyState`
  * `QuestEnemyInstanceId Id`
  * `QuestEnemyDefinitionId EnemyDefinitionId`
  * `BattlePosition Position`
  * `int CurrentHp`
  * `int CurrentMp`
  * `bool IsDead`
  * `IReadOnlyList<ActiveAilment>`
  * `IReadOnlyList<ActiveBuff>`
* `QuestTurnCommand`
  * `QuestParticipantId ParticipantId`
  * `int TurnNo`
  * `ActionKind ActionKind` (`NormalAttack`, `UseMove`, `Guard`, `Wait`, `LeaveQuest`, `Escape`)
  * `MoveId? MoveId`
  * `BattlePosition? SelectedTargetPosition`
  * `DateTimeOffset SubmittedAt`
  * `bool IsAutoSubmitted`
* `QuestTrapState`
  * `QuestTrapId Id`
  * `QuestParticipantId SourceParticipantId`
  * `MoveId MoveId`
  * `int ExpiresAfterFloorNo`
  * `bool IsTriggered`
* `QuestRewardAccumulator`
  * `int Exp`
* `QuestChatMessage`
  * `QuestParticipantId SenderParticipantId`
  * `string DisplayName`
  * `string? ImagePath`
  * `string Message`
  * `DateTimeOffset SentAt`

#### 行動の意味

* `LeaveQuest`: 個人の退出を表す。
* `Escape`: パーティ全体の撤退を表す。

補足:

* `QuestTurnCommand` は入力として保持するが、クライアントのログ表示や技エフェクト表示は、最終的には `BattleActionResult` に含まれる `ActorId` / `ActionKind` / `MoveId` を基準に行う。
* これにより行動順の並び替え後でも、解決順どおりにログと演出を再生できる。
* 単体攻撃は `SelectedTargetPosition` によって対象マスを指定する。通常攻撃でも「正面以外の敵マスを狙う」入力を許可できる。
* 解決時に対象マスのユニットが不在または無効化されている場合の扱いは、戦闘ロジック側で不発または代替対象選択として判定する。

#### 主な振る舞い

* `SubmitCommand(participantId, command, now)`
* `SwitchToAutoActionForTimeout(now)`
* `RequestManualControl(participantId)`
* `ApproveManualControl(participantId, ownerId)`
* `ResolveTurn()`
* `ApplyBattleResolution(resolution, actorMap)`
* `AdvanceFloor()`
* `ApplyTrapsOnFloorStart()`
* `MarkSucceeded()`
* `MarkFailed()`
* `Abort(reason)`
* `AddChatMessage(message)`

#### 不変条件

* 生存中かつ `Manual` の参加者は 1 ターンに 1 回だけ入力できる。
* `CanActFromTurn > CurrentTurnNo` の参加者は入力できない。
* `ActionDeadlineAt` を超過した参加者は退場させず `AutoAttackOnly` に切り替える。
* 階層をまたいでも HP / MP / 状態異常 / バフは引き継ぐ。
* 死亡者は自動復活しない。
* 蘇生された場合は `CanActFromTurn = CurrentTurnNo + 1` とする。
* `Failed` は、生存していて `LeaveQuest` しておらず、かつ `AutoAttackOnly` でもない参加者が 0 人になった時点で成立する。
* `Escape` 成功時の終了区分は `Failed` とする。
* そのターンで敵を全滅させた場合は `isFloorCleared = true` とし、最終階層であれば `Succeeded` を成立させる。
* クエスト中チャットは `QuestRun` が保持し、送信者表示名、画像パス、本文、送信時刻を進行中状態として保持する。

### 5.7 補助マスタ案

#### `QuestNpcTemplate`

出撃人数が最低人数を満たさない場合に補充するため、NPC テンプレートマスタを持つ。

* `QuestNpcTemplateId Id`
* `string Name`
* `Job Job`
* `BattleRow PreferredRow`
* `int Level`
* `Status BaseStatus`
* `IReadOnlyList<MoveId> MoveIds`
* `NpcRole` (`FrontGuard`, `MidSupport`, `RearHealer` など)

#### `QuestEnemyDefinition`

敵はトレーニング敵とは用途が異なるため、当面は別マスタとする。

* `QuestEnemyDefinitionId Id`
* `string Name`
* `int Level`
* `Status Status`
* `string ImagePath`
* `EnemyAiType AiType`
* `IReadOnlyList<MoveId> MoveIds`

補足:

* `QuestEnemyDefinition.ImagePath` は敵 CSV に保持する表示用画像パスであり、クライアントはこれを使って敵画像を描画する。
* `EnemyAiType` はボス / 雑魚の区別ではなく、各敵がどの行動を優先するかを表す行動方針である。階層種別は `FloorType` で表し、同一階層内で単体火力役、盾役、支援役のような役割差を持つ敵編成を表現できるようにする。
* 技演出は `MoveDomain.Move.EffectImagePath` を参照し、未設定ならエフェクト画像表示を行わない。
* 技演出の再生契機はターン解決後の `BattleActionResult` とし、ダメージや状態変化の反映後にクライアントで表示する。

### 5.8 ドメインサービス案

| サービス | 役割 |
| --- | --- |
| `QuestRoomService` | ルーム作成、参加、配置変更、開始可否判定 |
| `QuestNpcAssignmentService` | 最低出撃人数を満たすために NPC テンプレートを選択する |
| `QuestSnapshotFactory` | `QuestParticipant` と `Player` / `QuestNpcTemplate` から開始時スナップショットを生成する |
| `QuestRunFactory` | 開始時スナップショットとステージ定義から `QuestRun` を生成する |
| `QuestRunService` | 行動受付、放置による自動操作移行、ターン解決、階層遷移を行う |

### 5.9 リポジトリ案

* `IQuestStageRepository`
* `IQuestRoomRepository`
* `IQuestRunRepository`
* `IQuestNpcTemplateRepository`
* `IQuestEnemyDefinitionRepository`

## 6. 状態遷移ドラフト

### 6.1 ルーム

1. `Recruiting`
2. `Closed`

補足:

* 開始可否は状態ではなく `QuestRoom.CanStart()` で判定する。
* `Closed` は `Started` / `Cancelled` / `Expired` を `CloseReason` で区別する。
* 開始後の成功失敗は `QuestRoom` では表現しない。

### 6.2 進行中クエスト

1. `InProgress`
2. `Succeeded` または `Failed`
3. 必要なら中断系として `Aborted`

補足:

* `Failed` 条件は「継続可能な味方が 0 人」を基本とする。
* 本ドラフトでは `継続可能な味方` を「生存していて `LeaveQuest` しておらず、かつ `AutoAttackOnly` でもない参加者」と定義する。
* `Escape` 成功時は `Failed` に含める。
* `Aborted` は障害、運営操作、将来の明示的中断要求などに備えた状態として残す。

## 7. 永続化設計ドラフト

### 7.1 永続化分類

* マスタ系: ステージ、階層、敵、NPC テンプレートは CSV で管理する
* 募集系: ルーム、参加者
* 進行系: クエスト実行、開始時スナップショット、戦闘状態、行動入力、罠、報酬

### 7.2 マスタ系 CSV 案

クエストのコンテンツ追加速度を優先し、ステージ・敵・NPC テンプレートは DB テーブルではなく CSV で管理する。
既存の `CsvTrainingEnemyRepository` と同様に、クエストも CSV リポジトリを用いる。

想定ファイル:

* `server/resources/quest/stages.csv`
* `server/resources/quest/stage_floors.csv`
* `server/resources/quest/floor_enemy_spawns.csv`
* `server/resources/quest/enemies.csv`
* `server/resources/quest/enemy_moves.csv`
* `server/resources/quest/npc_templates.csv`
* `server/resources/quest/npc_moves.csv`

想定リポジトリ:

* `CsvQuestStageRepository`
* `CsvQuestEnemyDefinitionRepository`
* `CsvQuestNpcTemplateRepository`

CSV 採用理由:

* Git 管理下でコンテンツ差分をレビューしやすい。
* ステージや敵を追加するたびに DB seed や migration を増やさずに済む。
* 既存のトレーニング敵定義と同じ運用パターンに寄せられる。

### 7.3 募集系テーブル案

#### `internal.quest_rooms`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `id` | uuid | PK |
| `owner_player_id` | uuid | FK `players.id` |
| `stage_id` | int | CSV の `QuestStageDefinition.Id` を参照 |
| `mode` | int | `Solo` / `Multi` |
| `status` | int | `Recruiting` / `Closed` |
| `version` | int | NOT NULL, 楽観ロック用の更新バージョン |
| `close_reason` | int | `Started` / `Cancelled` / `Expired`, NULL 可 |
| `created_at` | timestamptz | NOT NULL |
| `closed_at` | timestamptz | NULL |

#### `internal.quest_room_participants`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `id` | uuid | PK |
| `room_id` | uuid | FK `quest_rooms.id` |
| `participant_type` | int | `Player` / `Npc` |
| `player_id` | uuid | NULL, FK `players.id` |
| `npc_template_id` | int | NULL, CSV の `QuestNpcTemplate.Id` を参照 |
| `display_name` | varchar(100) | NOT NULL |
| `battle_row` | int | NOT NULL |
| `battle_column` | int | NOT NULL |
| `participant_status` | int | `Joined` / `Disconnected` / `Left` |
| `is_owner` | boolean | NOT NULL |
| `joined_at` | timestamptz | NOT NULL |
| `last_seen_at` | timestamptz | NULL |
| `left_at` | timestamptz | NULL |

制約案:

* `player_id` は `participant_type = Player` のとき必須。
* `npc_template_id` は `participant_type = Npc` のとき必須。
* `UNIQUE(room_id, battle_row, battle_column)`。
* `UNIQUE(room_id, player_id)` ただし `player_id IS NOT NULL`。
* `quest_rooms.version` を楽観ロックに使い、同時参加更新は stale write を拒否する。

### 7.4 進行系テーブル案

#### `internal.quest_runs`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `id` | uuid | PK |
| `room_id` | uuid | UNIQUE, FK `quest_rooms.id` |
| `stage_id` | int | CSV の `QuestStageDefinition.Id` を参照 |
| `status` | int | `InProgress` / `Succeeded` / `Failed` / `Aborted` |
| `current_floor_no` | int | NOT NULL |
| `current_turn_no` | int | NOT NULL |
| `action_deadline_at` | timestamptz | NOT NULL |
| `last_resolved_turn_no` | int | NULL |
| `last_turn_results_json` | jsonb | NULL, 最新ターンの解決結果のみ保持 |
| `chat_messages_json` | jsonb | NOT NULL, クエスト中チャットのメッセージ配列 |
| `started_at` | timestamptz | NOT NULL |
| `ended_at` | timestamptz | NULL |

#### `internal.quest_run_party_snapshots`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `run_id` | uuid | PK, FK `quest_runs.id` |
| `participant_id` | uuid | PK, FK `quest_room_participants.id` |
| `participant_type` | int | `Player` / `Npc` |
| `display_name` | varchar(100) | NOT NULL |
| `image_path` | varchar(255) | NULL |
| `job` | int | NOT NULL |
| `start_row` | int | NOT NULL |
| `start_column` | int | NOT NULL |
| `max_hp` | int | NOT NULL |
| `max_mp` | int | NOT NULL |
| `strength` | int | NOT NULL |
| `defense` | int | NOT NULL |
| `intelligence` | int | NOT NULL |
| `luck` | int | NOT NULL |
| `speed` | int | NOT NULL |
| `move_set_json` | jsonb | NOT NULL |
| `initial_action_mode` | int | NOT NULL |

#### `internal.quest_run_party_members`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `run_id` | uuid | PK, FK `quest_runs.id` |
| `participant_id` | uuid | PK, FK `quest_room_participants.id` |
| `current_hp` | int | NOT NULL |
| `current_mp` | int | NOT NULL |
| `is_dead` | boolean | NOT NULL |
| `can_act_from_turn` | int | NOT NULL |
| `action_mode` | int | `Manual` / `AutoAttackOnly` |
| `active_effects_json` | jsonb | 状態異常・バフを保持 |
| `derived_parameters_json` | jsonb | 一時的な補正値や将来拡張パラメータを保持 |
| `updated_at` | timestamptz | NOT NULL |

#### `internal.quest_run_enemies`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `run_id` | uuid | PK, FK `quest_runs.id` |
| `enemy_instance_id` | uuid | PK |
| `floor_no` | int | NOT NULL |
| `enemy_definition_id` | int | CSV の `QuestEnemyDefinition.Id` を参照 |
| `battle_row` | int | NOT NULL |
| `battle_column` | int | NOT NULL |
| `current_hp` | int | NOT NULL |
| `current_mp` | int | NOT NULL |
| `is_dead` | boolean | NOT NULL |
| `active_effects_json` | jsonb | 状態異常・バフを保持 |
| `derived_parameters_json` | jsonb | 一時的な補正値や将来拡張パラメータを保持 |

`quest_run_enemies` は進行中クエストの復元に必要な敵状態を保持するテーブルとし、完全な戦闘履歴保存は目的としない。

#### `internal.quest_turn_commands`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `run_id` | uuid | PK, FK `quest_runs.id` |
| `turn_no` | int | PK |
| `participant_id` | uuid | PK, FK `quest_room_participants.id` |
| `action_kind` | int | NOT NULL |
| `move_id` | int | NULL |
| `target_row` | int | NULL |
| `target_column` | int | NULL |
| `submitted_at` | timestamptz | NOT NULL |
| `is_auto_submitted` | boolean | NOT NULL |

#### `internal.quest_floor_traps`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `run_id` | uuid | PK, FK `quest_runs.id` |
| `trap_id` | uuid | PK |
| `source_participant_id` | uuid | FK `quest_room_participants.id` |
| `move_id` | int | NOT NULL |
| `expires_after_floor_no` | int | NOT NULL |
| `is_triggered` | boolean | NOT NULL |

#### `internal.quest_reward_summaries`

| カラム | 型 | 備考 |
| --- | --- | --- |
| `run_id` | uuid | PK, FK `quest_runs.id` |
| `exp` | int | NOT NULL |

### 7.5 DB 設計上の判断

#### プレイヤーの恒久成長値は直接更新しない

クエスト中の HP / MP / バフ / 状態異常は `players` テーブルへ書かない。
クエスト終了時に、今回は経験値のみを恒久情報へ反映する。

#### 表示用画像は恒久情報とスナップショットを併用する

`players.image_path` は恒久的なプロフィール兼戦闘表示画像として保持する。
一方で進行中クエストの表示整合性を保つため、開始時点の `image_path` は `quest_run_party_snapshots` にも複製して固定する。

#### 技エフェクト画像は CSV オプション項目として扱う

技エフェクト画像は既存の技 CSV における任意項目として扱い、DB テーブル追加は行わない。
クライアントは `EffectImagePath` が設定されている技だけ画像演出を表示し、未設定技は従来どおりテキストや既定演出のみで処理する。
行動解決結果には `MoveId` を含め、クライアントはその `MoveId` を使って `EffectImagePath` を参照する。

#### 行動解決結果はログ表示に必要な識別子を持つ

`BattleActionResult` は `ActorId` に加えて `ActionKind` と `MoveId` を持つ。
これによりクエスト側は入力コマンドの再解釈に依存せず、解決順どおりの行動ログと技演出を構築できる。

#### サーバーは最新ターンの解決結果のみ保持する

サーバーは完全な戦闘ログを永続化せず、`quest_runs.last_turn_results_json` に最新ターンの解決結果のみを保持する。
クライアントは画面表示用に複数ターンのログをメモリ保持してよいが、再接続時の復元対象は最新ターン結果までとする。

#### クエスト中チャットは JSON 配列で保持する

クエスト中チャットは単なる文字列配列ではなく、送信者表示名、送信者画像パス、本文、送信時刻を持つメッセージ配列として `quest_runs.chat_messages_json` に保持する。
これによりクライアントは、追加のプレイヤー参照なしにクエスト画面上でチャット投稿者名とアイコンをそのまま描画できる。

#### 放置は参加者除外でなく行動モード変更で表現する

冒険中のプレイヤーは `quest_room_participants` から削除しない。
放置状態は `quest_run_party_members.action_mode = AutoAttackOnly` によって表現する。

## 8. ルールと永続化の対応表

| 要件 | 主担当集約 | 永続化先 |
| --- | --- | --- |
| オーナーのみ開始できる | `QuestRoom` | `quest_rooms`, `quest_room_participants` |
| ソロは即開始、マルチは 2 人以上で開始 | `QuestRoom` | `quest_rooms` |
| 開始時に不足人数だけ NPC を補充して最低 4 人編成にする | `QuestRoom` + `QuestSnapshotFactory` | `quest_room_participants`, `quest_run_party_snapshots` |
| クエスト完了時に `LeaveQuest` していない参加者全員へ同一経験値を配る | `QuestRun` | `quest_reward_summaries` |
| オーナーが全体配置を決める | `QuestRoom` | `quest_room_participants` |
| HP / MP / 状態異常を階層間で維持 | `QuestRun` | `quest_run_party_members`, `quest_run_enemies` |
| 全員入力後に行動解決 | `QuestRun` | `quest_turn_commands` |
| 60 秒未入力で自動操作へ移行 | `QuestRun` | `quest_run_party_members`, `quest_turn_commands` |
| ページを閉じても再接続で参加継続できる | `QuestRun`（参照元として `QuestRoom`） | 募集系 + 進行系テーブル |
| 中衛の罠を次階層へ持ち越す | `QuestRun` | `quest_floor_traps` |

## 9. 未確定事項

### 9.1 優先度高

* 手動復帰申請状態を専用テーブルで持つか、既存の進行状態 JSON / 列へ畳み込むか。
  API 契約は固めたが、永続化方式はまだ設計選択の余地がある。
* タイムアウト進行ジョブの実行粒度。
  何秒間隔で走査するか、1 回の処理で何件まで進めるか、排他制御をどう行うかは実装設計で決める必要がある。
* 将来 `QuestRun` 側へ認可スナップショットを持たせるか。
  初期実装では `QuestRoom` 参照で認可するが、進行中 API の独立性を高めるために `QuestRun` 側へ寄せるかは後続論点として残る。

フロントエンド実装の初期段階では、最低でも次を先に解消することを推奨する。

* `GET /quest/rooms`
* `GET /quest/runs/{runId}` の `QuestRunDetailResponse`
* `POST /quest/runs/{runId}/commands`
* `POST /quest/runs/{runId}/manual-control/request`
* `POST /quest/runs/{runId}/manual-control/approve`
* `QuestRunHub`

### 9.2 優先度中

* ボス階層到達前の回復・準備フェーズを設けるか。
* `QuestBattleState` など内部概念を将来別集約へ分離する必要があるか。

## 10. 次の更新対象

このドラフトをもとに、次に反映すべき対象は以下。

1. `docs/MMD01_CoreDomain.mmd`
2. `docs/MMD11_ER図.mmd`
3. 必要なら `docs/MMD21_Enum.mmd`

特に以下を明示的に反映する。

* `QuestRoom` は募集専用、`QuestRun` は進行専用という責務境界
* `QuestParticipant` と `QuestRunPartyMemberSnapshot` の分離
* 放置状態を `ActionMode` で表現し、`Kicked` を冒険用状態から排除する方針
* 最低 4 人、最大 6 人の出撃人数前提
