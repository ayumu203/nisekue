# README

## 作るもの

- チビクエ3(https://3.chibiquest.net/main3.php)風のゲーム開発.
- Web上で動作.
- 冒険やエンドレスバトル、農業を実装予定.

## システムのアーキテクチャ構成

### フロントエンド

- React + Vite
- MUI(Material UI)
- ![Tech Stack](https://skillicons.dev/icons?i=react,vite,mui,nodejs)
- ゲームの処理等はHTTPによるリクエストとレスポンスで行う.
- 基本的にはuseSWRを利用.

### バックエンド

- .NET(Minimal API)
- EF Core(Entity Framework Core)
- SignalR ※ もしリアルタイム性が必要になれば.
- ![Tech Stack](https://skillicons.dev/icons?i=cs,dotnet)

### インフラ

- Supabase(DB・認証)
- GitHub Pages(フロントエンド公開先)
- Azure App Service(バックエンド公開先)
- ![Tech Stack](https://skillicons.dev/icons?i=azure,github,supabase)

## 実行方法

- 各実行方法は以下のとおりである.

### フロントエンド

- 以下バージョンは参考.

```bash
$ node -v
v25.7.0
$ npm -v
11.10.1
```

- 以下でフロントエンドを起動.

```bash
# client内で実行.
pnpm dev
```

### バックエンドサーバ

- `.NET Core 10.0` で動作.

```bash
# server内で実行.
$ dotnet run
```

### バックエンドテスト

```bash
# tests/server.tests内で実行.
$ dotnet test
```

### DB マイグレーション (EF Core)

- 開発環境では手動で実行.
- CI/CD では GitHub Actions (`.github/workflows/db-migrate.yml`) で実行.

### マーケット期限切れ出品の自動削除

- 期限切れ出品の削除は GitHub Actions の定期実行で行う.
- ワークフローは `.github/workflows/market-listing-cleanup.yml` を利用する.
- Actions からはバックエンドの内部メンテナンス API を呼び出す.
- リポジトリまたは Environment Secrets に以下を設定する.
  - `MARKET_CLEANUP_TOKEN`: 内部メンテナンス API 呼び出し用トークン
- API のベース URL は既存の `VITE_API_BASE_URL` (`vars` または `secrets`) を流用する.
- バックエンド側にも同じ値を `Maintenance:MarketCleanupToken` として設定する.

### 開発環境での手動適用

```bash
# /server で実行.
# 初回のみ (dotnet-ef の導入)
dotnet tool install --global dotnet-ef --version 10.0.3

# PATH 反映後に migration 適用
dotnet ef database update
```

### 新しい migration を作る場合

```bash
# /server で実行.
dotnet ef migrations add <MigrationName>
```

### Supabase

- 起動関連

```bash
# 初回のみ
$ pnpx supabase init
# 起動
$ pnpx supabase start
# 各種サービスの確認
$ pnpx supabase status
# 停止
$ pnpx supabase stop
```

## 各ドキュメントの概要

- MKD**系
  - 主要な機能のうち今後も大きな変更があるものに関しては機能要件・設計を整理したドキュメントを作成しています.
  - 関連する機能を変更・修正する場合はこちらも合わせて修正してください.
  - 例:
    - `docs/MKD02_クエスト設計.md`: クエスト全体の設計ドラフト.
    - `docs/MKD04_クエストターンチャット設計.md`: クエスト中チャットのターン連動表示設計.
- MMD**系
  - 各ドメインに関してのドメインモデルを設計しています.
  - 実行・変更・修正を行った場合は追記・修正をお願いします.
  - ルールは特にないのですが、主要なクラスへのAPIとなるものは記載、前述のもので使用するようなプライベートなメソッドは記載しないことが多いです.
  - 各ドメインの概要は以下のとおりです.
    - CoreDomain: プレイヤーのドメイン.
    - MoveDomain: スキル関係のドメイン.
    - ChatDomain: チャット機能のドメイン.
    - BattleDomain: 戦闘の共通ロジック.
    - TrainDomain: 訓練を回すためのドメイン.
    - QuestDomain: クエストを回すためのドメイン.
    - CleanupDomain: GitHub Actions から起動するメンテナンス系処理のドメイン.

## 開発の手順

- バックエンドからフロントエンドまで影響する場合の作業手順は基本的には以下のようになります.
  1. 仕様書の変更を行う.
  2. ドメインモデルの追記・修正を行う.
  3. server/domain のエンティティや値オブジェクトといった小さいものを実装する.
  4. server/domain のサービス等の大きいものの実装・インタフェースの追加をする.
  5. server/infrastructureにDB上での変更やインタフェースの実装を行う.
  6. server/applicationで3~5までを用いたドメインを利用したアプリ側のロジックを実装する.
  7. server/endpointsへ6で実装したロジックの呼び出しを行う.
  8. フロントエンド側で7にて作成したエンドポイントを叩くためのバリデーション等を/schema, /api, endpoint.ts等へ実装する.
  9. 適切なコンポーネントを作成して, ページへ配置を行いUIの変更を実装.

## やめてほしいこと

- 適切なレビューをしていないAIのコード.
- テストやビルド・フォーマットが通らないコード.
- 設計を変更せずに実装だけを行ったもの.
