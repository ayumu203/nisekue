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

## DB マイグレーション (EF Core)

- 開発環境では手動で実行.
- CI/CD では GitHub Actions (`.github/workflows/db-migrate.yml`) で実行.

### 開発環境での手動適用

```bash
# 初回のみ (dotnet-ef の導入)
dotnet tool install --global dotnet-ef --version 10.0.3

# PATH 反映後に migration 適用
dotnet ef database update \
  --project server/server.csproj \
  --startup-project server/server.csproj
```

### 新しい migration を作る場合

```bash
dotnet ef migrations add <MigrationName> \
  --project server/server.csproj \
  --startup-project server/server.csproj \
  --output-dir infrastructure/migrations
```

## Supabase

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

## 認証の動作確認

1. Supabase の `ANON_KEY` を取得

```bash
cd ./supabase
pnpx supabase status -o env
```

1. ユーザー作成（初回のみ）

```bash
ANON_KEY="<1で表示されたANON_KEY>"
curl -s -X POST "http://127.0.0.1:54321/auth/v1/signup" \
  -H "apikey: $ANON_KEY" \
  -H "Content-Type: application/json" \
  -d '{"email":"email","password":"password"}'
```

1. JWT を取得

```bash
TOKEN=$(curl -s -X POST "http://127.0.0.1:54321/auth/v1/token?grant_type=password" \
  -H "apikey: $ANON_KEY" \
  -H "Content-Type: application/json" \
  -d '{"email":"email","password":"password"}' | jq -r '.access_token')
```

1. 認証付き API にアクセス

```bash
curl -i "http://localhost:5068/player" \
  -H "Authorization: Bearer $TOKEN"
```
