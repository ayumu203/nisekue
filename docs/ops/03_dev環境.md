# dev 環境

旧本番環境（Azure App Service + Supabase `Nisekue-Prod`）を dev 環境として再利用する。
以前の dev 環境（Azure App Service dev + Supabase `Nisekue`）は廃止した。

## 構成

| | dev | prod |
|---|---|---|
| 起点ブランチ | `main` | `prod` |
| フロントエンド | GitHub Pages (`deploy-frontend-dev.yml`) | Cloudflare Pages |
| バックエンド | Azure App Service（旧 prod の Web App, `deploy-backend-dev.yml`） | Oracle VM (`deploy-backend-vm-prod.yml`) |
| DB | Supabase `Nisekue-Prod`（ソウル, `pfigwkdwtyqchsiljkod`） | Supabase `Nisekue-Prod-Tokyo`（`ycxjbuhqhsupeuqdrcyu`） |
| マイグレーション | `db-migrate-dev.yml` | `db-migrate-prod.yml` |
| GitHub Environment | `dev` | `prod` |

## 切り替え手順（手作業）

### 1. GitHub Environment `dev` の値を差し替える

`Settings > Environments > dev` で次の値を旧 prod 側のものへ上書きする。

| 種別 | 名前 | 値 |
|---|---|---|
| Secret | `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND_DEV` | 旧 prod Web App の発行プロファイル（`prod` の `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND_PROD` と同じもの。Azure Portal の「発行プロファイルのダウンロード」で再取得できる） |
| Secret | `SUPABASE_DB_CONNECTION_STRING` | `Nisekue-Prod`（ソウル）の Session Pooler 接続文字列 |
| Variable / Secret | `VITE_API_BASE_URL` | 旧 prod Web App の URL |
| Variable / Secret | `VITE_SUPABASE_URL` | `https://pfigwkdwtyqchsiljkod.supabase.co` |
| Variable / Secret | `VITE_SUPABASE_ANON_KEY` | `Nisekue-Prod` の anon / publishable key |
| Secret | `MARKET_CLEANUP_TOKEN` | Web App の `Maintenance__MarketCleanupToken` と同じ値 |

### 2. 旧 prod Web App の設定を変える

Azure Portal の Web App > 環境変数で次を設定する。

- `ASPNETCORE_ENVIRONMENT=Development`
  - `appsettings.Development.json` により CORS 許可先に `https://ayumu203.github.io` が入る
  - `appsettings.Development.json` の接続文字列はローカル用なので、Web App 側の `ConnectionStrings__Supabase`（接続文字列設定）でソウルの `Nisekue-Prod` を指定しておく（環境変数が設定ファイルより優先される）
- `Cors__AllowedOrigins__*` が設定されていれば削除する（設定ファイルより優先されるため）
- `Supabase__ProjectUrl` がソウルの `Nisekue-Prod` を向いていることを確認する
- 本番のカスタムドメインが残っていれば外す

### 3. 後片付け

- `prod` Environment の `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND_PROD` を削除する（Azure 向け本番デプロイは廃止済み）
- Azure の旧 dev 用 Web App が残っていれば削除する
- Supabase の旧 dev プロジェクト `Nisekue`（シンガポール, `pudpklpqhhkozpipsyao`）を削除する
  - Dashboard > Project Settings > General > Delete project
