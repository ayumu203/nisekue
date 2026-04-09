# README

## 資料

- 開発内容に関する資料は以下のリンクから飛べます.
- [開発資料](https://github.com/ayumu203/nisekue/blob/main/docs/%E7%99%BA%E8%A1%A8%E8%B3%87%E6%96%99.pdf)

## 公開先

- [本番用](https://game.arm203.org/)
- [開発用](https://ayumu203.github.io/nisekue/)

## 作るもの

- 某ゲームのオマージュゲー開発.
- 冒険やエンドレスバトル等を実装する.

## システムのアーキテクチャ構成

### フロントエンド

- React + Vite
- MUI(Material UI)
- ![Tech Stack](https://skillicons.dev/icons?i=react,vite,mui,nodejs)

### バックエンド

- .NET(Minimal API)
- EF Core(Entity Framework Core)
- xUnit
- SignalR
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
    - `docs/quest/02_クエスト設計.md`: クエスト全体の設計ドラフト.
    - `docs/quest/03_クエストターンチャット設計.md`: クエスト中チャットのターン連動表示設計.
- MMD**系
  - 各ドメインに関してのドメインモデルを設計しています.
  - 実行・変更・修正を行った場合は追記・修正をお願いします.
  - ルールは特にないのですが、主要なクラスへのAPIとなるものは記載、前述のもので使用するようなプライベートなメソッドは記載しないことが多いです.
  - クエストや装備進行のように CSV マスタと API 両方へ影響する変更は、まず対応する MMD を更新してから実装してください.
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
