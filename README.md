# README

## 資料

- 開発内容に関する資料は以下のリンクから飛べます.

## 公開先

- 2026年5月3日までデータベースの利用上限のため動作しない可能性が高いです.
- [本番用](https://game.arm203.org/)
- [開発用(開発者が確認する用です)](https://ayumu203.github.io/nisekue/)

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
