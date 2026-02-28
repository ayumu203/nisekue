# README

## 作るもの

- チビクエ3(https://3.chibiquest.net/main3.php)風のゲーム開発.
- Web上で動作.
- 冒険(素材獲得), 農業(作物を育てて出荷・食べるとステータス上昇), 装備作成等を実装. 

## システムのアーキテクチャ構成

### フロントエンド

- React + Vite
- MUI(Material UI)
- ゲームの処理等はHTTPによるリクエストとレスポンスで行う.
- 基本的にはuseSWRを利用.

### バックエンド

- .NET(Minimal API)
- EF Core(Entity Framework Core)
- SignalR ※ もしリアルタイム性が必要になれば.

### インフラ

- Supabase(DB・認証)
- GitHub Pages(フロントエンド公開先)
- Azure App Service(バックエンド公開先)

