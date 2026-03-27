# クエストターンチャット設計

## 1. 目的

本ドキュメントは、進行中クエストで参加者が短文メッセージを送り合える機能について、表示仕様と実装方針を整理するものである。

今回の追加要件は以下のとおりである。

* クエスト中にメッセージを送信できること。
* メッセージは各ターンに紐づくものだけ見られればよいこと。
* メッセージは送信者名、送信キャラ画像、メッセージ内容を表示すること。
* メッセージは行動ログと同じ表示領域で扱うこと。

## 2. 現状整理

現状のクエスト機能には以下が存在する。

* サーバーには `POST /quest/runs/{runId}/chat` があり、進行中クエストへメッセージを追加できる。
* `QuestChatMessage` には送信者参加者 ID、表示名、画像パス、本文、送信時刻がある。
* `QuestRunDetailResponse` には `chatMessages` が含まれている。
* フロントエンドにはクエストチャット表示 UI が未実装である。
* `QuestLastTurnResultsPanel` は直前ターンの行動ログだけを表示している。

不足している点は以下である。

* メッセージがどのターンのものか識別できない。
* 直前ターンのログと同じ文脈でメッセージを表示できない。
* フロントエンド側に送信 UI と表示 UI がない。

## 3. 仕様方針

### 3.1 ターン紐付け

各クエストメッセージは送信時点の `CurrentTurnNo` に紐づける。

このため `QuestChatMessage` に `TurnNo` を追加する。

これにより、以下を明確に扱える。

* 今ターン中に送られたメッセージ
* 直前ターンに送られたメッセージ
* それ以前の古いメッセージ

### 3.2 表示対象

UI で表示するメッセージは、原則として以下の 2 種類だけに制限する。

* 現在ターンのメッセージ
* 直前に解決されたターンのメッセージ

それ以前のターンのメッセージは UI では表示しない。

これにより、チャット履歴を長く積み上げず、ターン単位の短い連絡に用途を限定できる。

### 3.3 表示場所

メッセージ表示は既存の `QuestLastTurnResultsPanel` 系の表示領域へ統合する。

扱いは以下とする。

* 進行中でまだターン未解決の間は、現在ターンのメッセージを同パネル内へ表示する。
* ターン解決後は、直前ターンのメッセージと直前ターンの行動ログを同じパネル内へ表示する。
* クエスト終了後は、既存方針どおりログパネル自体を閉じる。

「同じパネル内で扱う」ことを要件とし、厳密な 1 件単位の時系列混在までは初期実装で求めない。
初期実装では、同じパネル内で「メッセージ群」と「行動ログ群」を連続表示すればよい。

## 4. ドメイン設計

### 4.1 `QuestChatMessage`

`QuestChatMessage` は以下を持つ。

* `TurnNo`
* `SenderParticipantId`
* `DisplayName`
* `ImagePath`
* `Message`
* `SentAt`

`DisplayName` と `ImagePath` は既存方針どおりスナップショット由来を使う。
これにより、クエスト中にプレイヤー本体の名前や画像が変更されても、当該クエスト内の表示は不変にできる。

### 4.2 `QuestRun`

`QuestRun` は `ChatMessages` を保持し続けるが、表示要件に必要なのは現在ターンと直前ターンのみである。

そのため集約側の責務は以下とする。

* 新規メッセージ追加時に `TurnNo` を付与して保持する。
* ターン解決時に、直前ターンのメッセージ一覧を `LastTurnResults` 側へ反映できるようにする。
* 解決済みの古いターンのメッセージは、不要になった時点で破棄可能とする。

初期実装では単純化のため、`QuestRun.ChatMessages` には直前ターンと現在ターンだけが残るようにトリミングする方針とする。

### 4.3 `QuestLastTurnResults`

`QuestLastTurnResults` に `ChatMessages` を追加する。

これにより、ターン解決後に返す `LastTurnResults` だけで以下を表示できる。

* そのターンに送られたメッセージ
* そのターンの行動ログ
* 階層遷移 / クエスト終了情報

## 5. API / DTO 設計

### 5.1 `QuestChatMessageView`

トップレベルの `chatMessages` は「現在ターンのメッセージ一覧」として定義する。

各要素は以下を持つ。

* `turnNo`
* `senderParticipantId`
* `displayName`
* `imagePath`
* `message`
* `sentAt`

### 5.2 `QuestLastTurnResultsView`

`QuestLastTurnResultsView` に `chatMessages` を追加する。

各要素は `QuestChatMessageView` と同一形とする。

これにより、フロントエンドは以下の単純な分岐で描画できる。

* `run.chatMessages`: 現在ターンのチャット
* `run.lastTurnResults.chatMessages`: 直前ターンのチャット
* `run.lastTurnResults.actions[].logs`: 直前ターンの行動ログ

### 5.3 送信 API

`POST /quest/runs/{runId}/chat` は継続利用する。

リクエストは現状どおり以下でよい。

* `participantId`
* `message`

`turnNo` はクライアントから受け取らず、サーバーが `run.TurnState.CurrentTurnNo` を付与する。

これにより、クライアント改ざんやターン不整合を防ぐ。

## 6. フロントエンド設計

### 6.1 コンポーネント配置

クエスト画面では新規に以下を追加する。

* メッセージ入力フォーム
* メッセージ送信ボタン
* ログパネル内のメッセージ表示ブロック

送信 UI の配置先は、戦闘中に見える範囲で既存コマンド群の近傍、またはログパネル上部とする。
初期実装では複雑化を避けるため、`QuestLastTurnResultsPanel` 直上または同パネル内下部へ置く。

### 6.2 表示形式

各メッセージは以下の形で表示する。

* 送信キャラ画像
* 送信者名
* メッセージ本文

ログと同じパネル内に置くが、見分けがつくように行動ログとはカードスタイルを分ける。

想定表示順は以下とする。

1. 直前ターンのメッセージ
2. 直前ターンの行動ログ

まだターン未解決の場合は以下とする。

1. 現在ターンのメッセージ

## 7. バックエンド実装方針

### 7.1 最小差分

既存のクエストチャット API と `QuestRun.ChatMessages` を活かし、以下の最小変更で実装する。

* `QuestChatMessage` に `TurnNo` を追加
* チャット送信時に `CurrentTurnNo` を設定
* ターン解決処理で、そのターンのチャットを `LastTurnResults` に取り込む
* `QuestResponseMapper` で `chatMessages` を現在ターンだけに絞る
* `QuestLastTurnResultsView` に `chatMessages` を追加

### 7.2 保存方針

クエストのチャットは既存どおり `quest_runs.chat_messages_json` に保存する。

今回は JSON の構造変更だけで済むため、テーブル追加は不要である。
`turn_no` を通常カラムへ切り出す必要もない。

そのため、ER 図の更新は不要とする。

## 8. 実装ステップ

1. `docs/MKD02_クエスト設計.md` の DTO / 表示仕様を更新する。
2. `docs/MMD06_QuestDomain.mmd` に `QuestChatMessage.TurnNo` と `LastTurnResults.ChatMessages` の差分を反映する。
3. サーバー側で `QuestChatMessage`、`QuestLastTurnResults`、JSON シリアライザ、レスポンスマッパーを更新する。
4. `POST /quest/runs/{runId}/chat` の送信時に `CurrentTurnNo` を付与する。
5. フロント側でクエストチャット送信フォームを実装する。
6. ログパネルに「現在ターンのメッセージ / 直前ターンのメッセージ + 行動ログ」を描画する。
7. SignalR 更新を通して、送信後にチャットが即時反映されることを確認する。

## 9. 非対象

今回の対象外は以下とする。

* 既読管理
* 長期履歴の閲覧
* クエスト外チャットとの統合
* スタンプや定型文
* チャットの削除・編集
* 1 件ごとの厳密な時系列マージ表示
