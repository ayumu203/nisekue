# Copilot Instructions (nisekue)

## レビュー項目

### フォーマット
- [must], [nits] 等のプリフィックスを付与する。
- 日本語で応答する。

### 設計に関して
- 設計書に記載のない公開フィールド・メソッドを指摘する。
- 実装上、今後必要になると考えられる設計の不足・無駄を指摘する。

### 実装に関して
- わかりづらい変数名・関数名を指摘する。
- バックエンド・フロントエンドのパフォーマンスを下げる実装を指摘する（N+1、不要な副作用など）。

## Build / Test / Lint コマンド

### Backend（リポジトリルートで実行）
```bash
dotnet restore Nisekue.sln
dotnet build server/server.csproj -c Release --no-restore -warnaserror
dotnet format server/server.csproj whitespace --verify-no-changes --no-restore
dotnet format server/server.csproj analyzers --verify-no-changes --no-restore
dotnet test tests/server.tests/server.tests.csproj -c Release --no-restore --verbosity normal
```

単体テスト1件のみ実行:
```bash
dotnet test tests/server.tests/server.tests.csproj --filter "FullyQualifiedName~server.tests.ItemTests.CanUse_WhenConditionsAreSatisfied_ReturnsTrue"
```

### Frontend（`client/` で実行）
```bash
pnpm install --frozen-lockfile
pnpm run format:check
pnpm run lint
pnpm run build
pnpm run dev
```

### Supabase ローカル（リポジトリルートで実行）
```bash
pnpx supabase start
pnpx supabase status
pnpx supabase stop
```

## ハイレベルアーキテクチャ

- モノレポ構成: `client/`（React + Vite + SWR + Zod）と `server/`（.NET Minimal API + EF Core + DDD）を同居。
- バックエンドは `domain -> application -> infrastructure -> endpoints` の責務分離。`Program.cs` で DI を集約し、各 `Map*Endpoints` を束ねる。
- データソースは二系統:
  - 永続データ: Supabase(PostgreSQL) を EF Core 経由で利用（`Db*Repository`）。
  - マスタデータ: `server/resources/**/*.csv` を起動時に読み込む `Csv*Repository`（主に Singleton DI）。
- 認証は Supabase JWT。HTTP API は Bearer トークン、クエストの SignalR ハブ（`/quest-hubs/runs`）は `access_token` クエリも受ける。
- フロントは `src/api/endpoints.ts` を API 契約の中核にし、各 API 関数で request/response を Zod 検証してから UI 層へ渡す。

## このリポジトリ固有の重要な規約

- `server/domain/` または `server/infrastructure/` を変更する前に、対応する設計ドキュメント（`docs/**/*.mmd`、特に `docs/db/01_ER図.mmd` や `docs/quest/04_クエストドメイン.mmd`）を先に更新する。
- endpoint は薄く保ち、業務ロジックは `application` / `domain` に置く。`Program.cs` はルーティングと DI のオーケストレーションに集中させる。
- フロントのデータ取得は `useSWR` を前提にする。新規 API 追加時は以下をセットで更新する:
  - `client/src/schema/*`（Zod スキーマ）
  - `client/src/api/endpoints.ts`（契約定義）
  - `client/src/api/*.ts`（fetch + エラーハンドリング）
  - `client/src/pages/*` / `components/*`（SWR 利用）
- 匿名ログイン利用者の投稿制限は既存ヘルパー（`EndpointHelpers.IsAnonymousUser` / `AnonymousPostingForbidden`）の扱いに揃える。
- CI は `.github/workflows/ci.yml` に準拠（Backend: format/lint/build/test、Frontend: format/lint/build）。変更はこの流れを壊さないこと。
