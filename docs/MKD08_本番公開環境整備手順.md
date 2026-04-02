# MKD08 本番公開環境整備手順

## 1. 目的

- 本番公開用のフロントエンド、バックエンド、DB を開発環境から切り離して運用できるようにする。
- デプロイの起点を `main` ではなく `prod` ブランチへ移す。
- フロントエンドは Cloudflare Pages、バックエンドは Azure App Service、DB は本番用 Supabase プロジェクトで運用する。
- GitHub Actions により、`prod` ブランチ反映時に本番用デプロイを自動で適用できるようにする。
- DB マイグレーションは GitHub Actions により環境別に自動適用できるようにする。

## 2. 現状

### 2.1 URL

- フロントエンド本番: `https://game.arm203.org`
- フロントエンド開発: `http://localhost:5173`
- バックエンド本番ベース URL: `https://api.arm203.org`
- バックエンド開発ベース URL: `https://www.arm203.org`
- バックエンドローカル開発: `http://localhost:5068`

### 2.2 現在の構成

- フロントエンドデプロイは GitHub Pages 向け workflow を使用している。
- バックエンドデプロイは Azure App Service 向け workflow を使用している。
- DB マイグレーションは GitHub Actions により環境別に自動適用できる。
- workflow のトリガーは現在 `main` ブランチ基準である。
- バックエンドの CORS は `AllowAnyOrigin` であり、本番向けに未調整である。
- フロントエンドの API ベース URL は `VITE_API_BASE_URL` で切り替える。

## 3. 目標構成

### 3.1 ブランチ運用

- 開発用ブランチ: `main`
- 本番反映用ブランチ: `prod`
- 本番環境への自動反映は `prod` ブランチ push を起点にする。

### 3.2 インフラ構成

- フロントエンド本番:
  - Cloudflare Pages
- バックエンド本番:
  - Azure App Service の本番用アプリを新規作成
- DB 本番:
  - 本番専用 Supabase プロジェクトを新規作成

### 3.3 採用公開経路

- ブラウザは Cloudflare 配下の URL のみを直接叩く。
- Azure App Service の `azurewebsites.net` をフロントエンドから直接呼ばせない。
- API はサブドメイン分離で Cloudflare 配下へ置く。

#### 採用構成

- フロント: `https://game.arm203.org`
- API: `https://api.arm203.org`

### 3.4 採用方針

- 現時点ではサブドメイン分離を採用する。
- Cloudflare Pages と API の責務が明確になり、CORS 設定も簡単になるため。
- URL 割り当ては以下とする。
- 本番フロント: `https://game.arm203.org`
- 本番バックエンド: `https://api.arm203.org`
- 開発バックエンド: `https://www.arm203.org`

## 4. 設計原則

### 4.1 環境分離

- 開発環境と本番環境で以下を分離する。
  - Azure App Service
  - Supabase プロジェクト
  - GitHub Actions Secrets / Variables
  - Cloudflare Pages の環境変数

### 4.2 Cloudflare を入口に固定

- ユーザーアクセスは Cloudflare 経由に限定する。
- Azure App Service 側には Access Restrictions を設定し、Cloudflare からの到達だけを許可する。
- `api.arm203.org` には Cloudflare WAF を適用する。
- Cloudflare Access は管理系導線が必要になるまで導入しない。
- Cloudflare Tunnel は採用しない。

### 4.3 デプロイ順序

- 本番 DB migration
- 本番 backend deploy
- 本番 frontend deploy

- 新スキーマを前提とする backend が先に出ると壊れる可能性があるため、DB を先に更新する。
- frontend は API 互換が保たれている限り backend の後でよい。

## 5. 実施手順

### 5.1 Supabase 本番プロジェクトの作成

1. 本番専用 Supabase プロジェクトを作成する。
2. 接続文字列を取得する。
3. Supabase URL と anon key を取得する。
4. GitHub の production 用 secret / variable に登録する。

登録対象:

- `SUPABASE_DB_CONNECTION_STRING`
- `VITE_SUPABASE_URL`
- `VITE_SUPABASE_ANON_KEY`

### 5.1.1 DB 接続

- GitHub Actions の DB migration は Session Pooler を使う。
- Azure App Service の通常アプリ接続も Session Pooler を使う。
- GitHub `dev` / `prod` Environment secret `SUPABASE_DB_CONNECTION_STRING` には Session Pooler の接続文字列を設定する。
- Azure App Service `ConnectionStrings__Supabase` にも Session Pooler の接続文字列を設定する。

接続文字列例:

- Session Pooler
  - `Host=aws-1-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password="<db password>";SSL Mode=Require;Trust Server Certificate=true`

### 5.2 Azure App Service 本番アプリの作成

1. 本番用 App Service を新規作成する。
2. `ConnectionStrings__Supabase` に本番 DB の接続文字列を設定する。
3. `Supabase__ProjectUrl` と `Supabase__JwtAudience` を本番値で設定する。
4. 必要なメンテナンストークン類を本番値で設定する。
5. Publish Profile を GitHub Secrets に登録する。

登録対象例:

- `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND_PROD`

### 5.2.1 Maintenance Token の設定

- Azure App Service では `Settings > Environment variables` に設定する。
- キー名は `Maintenance__MarketCleanupToken` とする。
- development と production で別の値を使う。
- GitHub Actions 側の `MARKET_CLEANUP_TOKEN` と App Service 側の `Maintenance__MarketCleanupToken` は同じ環境内で一致させる。

設定対応:

- 開発 App Service
  - `Maintenance__MarketCleanupToken=<development 用 token>`
- 本番 App Service
  - `Maintenance__MarketCleanupToken=<production 用 token>`

### 5.3 Cloudflare Pages 本番プロジェクトの作成

1. `client/` をビルド対象にした Cloudflare Pages プロジェクトを作成する。
2. GitHub リポジトリ連携を有効にする。
3. Production Branch を `prod` に設定する。
4. Build 設定を以下で登録する。
   - Framework preset: `Vite`
   - Root directory: `client`
   - Build command: `pnpm run build`
   - Build output directory: `dist`
5. 環境変数を Production 用で設定する。

設定対象:

- `VITE_SUPABASE_URL`
- `VITE_SUPABASE_ANON_KEY`
- `VITE_API_BASE_URL`

### 5.4 API 到達経路の設計

1. Cloudflare に `api.arm203.org` を追加する。
2. DNS / Proxy を Cloudflare 配下に置く。
3. origin を Azure App Service に向ける。
4. `api.arm203.org` に Cloudflare WAF を適用する。
5. `VITE_API_BASE_URL=https://api.arm203.org` を本番値に設定する。
6. backend の CORS は `https://game.arm203.org` のみ許可する。

### 5.5 Cloudflare Pages 自動デプロイの構成

1. Cloudflare Pages に本番用 Pages プロジェクトを作成する。
2. GitHub リポジトリを接続する。
3. Production Branch を `prod` に設定する。
4. `prod` ブランチ更新時に Cloudflare Pages が自動 build / deploy する。
5. Production 環境変数を Cloudflare Pages 側へ登録する。
   - `VITE_API_BASE_URL=https://api.arm203.org`
   - `VITE_SUPABASE_URL=<production supabase url>`
   - `VITE_SUPABASE_ANON_KEY=<production anon key>`
6. `game.arm203.org` を Pages の Custom Domain として設定する。
7. `prod` ブランチ更新時に Cloudflare Pages の本番 deploy が走ることを確認する。

### 5.6 GitHub Actions の分離

#### frontend deploy

- frontend の本番 deploy は Cloudflare Pages の GitHub 連携に委譲する。
- Production Branch は `prod` に設定する。
- 既存 `.github/workflows/deploy-frontend-dev.yml` は開発用 GitHub Pages workflow のため、本番 deploy には使わない。

#### backend deploy

- `.github/workflows/deploy-backend-prod.yml` を追加する。
- `Apply DB Migrations - Production` 成功後に backend を deploy する。
- Publish Profile は本番用 secret に切り替える。
- Environment 名は `production` を使う。

#### db migrate

- `.github/workflows/db-migrate-prod.yml` を追加する。
- `prod` ブランチ push から本番 Supabase へ migration を自動適用する。
- `dev` / `prod` ともに GitHub Environment secret `SUPABASE_DB_CONNECTION_STRING` へ Session Pooler の接続文字列を登録する。
- workflow は `dotnet ef database update` を実行し、適用対象の migration がなければ `No migrations were applied` で終了する。
- schema 変更を含む backend deploy の前に起動する。

### 5.6.1 migration 運用

- migration の主経路は GitHub Actions の `db-migrate-dev.yml` / `db-migrate-prod.yml` とする。
- Azure App Service の通常アプリ接続は Session Pooler を使う。

GitHub Actions での運用手順:

1. `main` または `prod` に必要な migration ファイルが入っていることを確認する。
2. 対象 Environment (`dev` または `prod`) の `SUPABASE_DB_CONNECTION_STRING` に Session Pooler の接続文字列が設定されていることを確認する。
3. `main` または `prod` へ push して `db-migrate-dev.yml` または `db-migrate-prod.yml` を自動起動させる。
4. `Apply migrations` ステップで `No migrations were applied` または migration 完了ログを確認する。
5. その後 backend deploy を行う。

#### cleanup worker

- `.github/workflows/cleanup-expired-market-listings-dev.yml` を開発用 cleanup worker とする。
- `.github/workflows/cleanup-expired-market-listings-prod.yml` を本番用 cleanup worker とする。
- 両方とも内部メンテナンス API `POST /internal/market/listings/cleanup-expired` を呼び出す。
- development / production の Environment を分離して、`VITE_API_BASE_URL` と `MARKET_CLEANUP_TOKEN` を切り替える。
- `.github/workflows/cleanup-game-data-dev.yml` を開発用の手動全削除 workflow とする。
- `POST /internal/development/cleanup-game-data` を呼び出し、Development 環境のゲームデータだけを削除する。

### 5.7 CORS の引き締め

- 現在の `AllowAnyOrigin` は本番では使用しない。
- 本番では許可オリジンを設定化し、環境変数または appsettings から注入する。
- `server/appsettings.Production.json` には `https://game.arm203.org` を既定値として保持する。

候補:

- `Cors__AllowedOrigins__0=https://game.arm203.org`
- `Cors__AllowedOrigins__1=http://localhost:5173`
- `Cors__AllowedOrigins__2=https://ayumu203.github.io`

- 本番 App Service では `https://game.arm203.org` のみ設定する。
- 開発環境では `http://localhost:5173` と `https://ayumu203.github.io` を設定する。

## 6. GitHub Secrets / Variables 設計

### 6.1 GitHub Environments

- `dev`
- `prod`

### 6.2 production へ置く値

- `SUPABASE_DB_CONNECTION_STRING` (`Session Pooler`)
- `AZURE_WEBAPP_PUBLISH_PROFILE_BACKEND_PROD`
- `VITE_SUPABASE_URL`
- `VITE_SUPABASE_ANON_KEY`
- `VITE_API_BASE_URL`
- `MARKET_CLEANUP_TOKEN`

### 6.3 development へ置く値

- 既存の dev App Service 用 publish profile
- 開発 Supabase 用 Session Pooler connection string
- 開発向け `VITE_*` 値
- 開発向け `MARKET_CLEANUP_TOKEN`

### 6.4 `MARKET_CLEANUP_TOKEN` の書き場所

- GitHub では `Settings > Environments > dev > Secrets and variables > Actions` に登録する。
- GitHub では `Settings > Environments > prod > Secrets and variables > Actions` に登録する。
- Azure App Service では `Settings > Environment variables` に登録する。

対応関係:

- GitHub `dev` Environment secret
  - `MARKET_CLEANUP_TOKEN=<development 用 token>`
- 開発 App Service environment variable
  - `Maintenance__MarketCleanupToken=<development 用 token>`
- GitHub `dev` Environment secret
  - `SUPABASE_DB_CONNECTION_STRING=<development 用 Session Pooler connection string>`
- 開発 App Service environment variable
  - `ConnectionStrings__Supabase=<development 用 Session Pooler connection string>`
- GitHub `prod` Environment secret
  - `MARKET_CLEANUP_TOKEN=<production 用 token>`
- 本番 App Service environment variable
  - `Maintenance__MarketCleanupToken=<production 用 token>`
- GitHub `prod` Environment secret
  - `SUPABASE_DB_CONNECTION_STRING=<production 用 Session Pooler connection string>`
- 本番 App Service environment variable
  - `ConnectionStrings__Supabase=<production 用 Session Pooler connection string>`

## 7. Cloudflare Pages 自動デプロイまでの作業フロー

1. Cloudflare ダッシュボードで Pages プロジェクトを作成する。
2. GitHub リポジトリを接続し、Production Branch を `prod` に設定する。
3. Build 設定を `client` / `pnpm run build` / `dist` で保存する。
4. `game.arm203.org` を Custom Domain として設定する。
5. Cloudflare Pages の Production 環境変数に `VITE_API_BASE_URL`、`VITE_SUPABASE_URL`、`VITE_SUPABASE_ANON_KEY` を登録する。
6. `prod` ブランチへ push し、Cloudflare Pages が自動 build / deploy することを確認する。
7. `https://game.arm203.org` へアクセスし、`https://api.arm203.org` に通信できることを確認する。

## 8. リリースフロー

1. `main` で開発・検証する。
2. 本番に反映する内容を `prod` へマージする。
3. `prod` push を契機に DB migration を実行する。
4. DB migration 成功後に backend を deploy する。
5. frontend 変更を含む `prod` push では Cloudflare Pages が frontend を deploy する。
6. 本番 URL で疎通確認を行う。

## 9. 確認項目

### 9.1 backend

- `https://api.arm203.org` で本番 API 応答できるか。
- `https://www.arm203.org` で開発 API 応答できるか。
- Supabase 認証トークンを検証できるか。
- App Service の接続文字列が本番 DB を向いているか。
- Cloudflare 経由以外の直接アクセスを遮断できているか。

### 9.2 frontend

- `https://game.arm203.org` で本番画面が配信されるか。
- 本番画面から本番 API を叩けるか。
- `localhost:5173` 向け設定が残っていないか。

### 9.3 DB

- `db-migrate-dev.yml` / `db-migrate-prod.yml` が `main` / `prod` push で自動起動して migration を適用できるか。
- GitHub Environment secret `SUPABASE_DB_CONNECTION_STRING` が Session Pooler で疎通できるか。

## 10. 実装時の変更対象

- `.github/workflows/deploy-frontend-dev.yml`
- `.github/workflows/deploy-backend-dev.yml`
- `.github/workflows/deploy-backend-prod.yml`
- `.github/workflows/db-migrate-dev.yml`
- `.github/workflows/db-migrate-prod.yml`
- `.github/workflows/cleanup-expired-market-listings-dev.yml`
- `.github/workflows/cleanup-expired-market-listings-prod.yml`
- `.github/workflows/cleanup-game-data-dev.yml`
- `server/Program.cs`
- `server/appsettings*.json`
- `client/vite.config.ts`
- `README.md`

必要に応じて追加:

- Cloudflare Pages 設定
- Cloudflare 側の DNS / Proxy / Worker 設定
- Azure App Service の Access Restrictions

## 11. 未決事項

- `prod` ブランチへの反映を手動マージだけにするか、GitHub Release と連動させるか。

## 12. 今回の推奨結論

- 本番用 Supabase は別プロジェクトで新規作成する。
- 本番用 Azure App Service は別アプリで新規作成する。
- フロントエンド本番は Cloudflare Pages へ移す。
- デプロイ起点は `prod` ブランチにする。
- DB migration は `prod` push 時に GitHub Actions で先に適用する。
- API 公開経路は `https://api.arm203.org` を採用する。
- Cloudflare 側の防御は WAF を採用する。
- CORS は `AllowAnyOrigin` をやめ、環境別ホワイトリストへ切り替える。
- 本番 backend / DB migration は `-prod` 付き GitHub Actions を追加して `prod` ブランチ起点で実行する。
- 本番 frontend deploy は Cloudflare Pages の GitHub 連携で `prod` ブランチ起点にする。
