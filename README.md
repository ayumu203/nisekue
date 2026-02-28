# README

## 作るもの




- チビクエ3(https://3.chibiquest.net/main3.php)風のゲーム開発.
- Web上で動作.
- 冒険(素材獲得), 農業(作物を育てて出荷・食べるとステータス上昇), 装備作成等を実装. 

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

## フロントエンド

- 以下バージョンは参考.
```bash
$ node -v
v25.7.0
$ npm -v
11.10.1
```
- 以下でフロントエンドを起動.
```bash
pnpm dev
```

## バックエンドサーバ

- `.NET Core 10.0` で動作.

```bash
$ dotnet run --project ./server/server.csproj 
```
