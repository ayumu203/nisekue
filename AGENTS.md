# リポジトリ運用ガイド

## 重要

- 応答は`日本語`でお願いします.

## プロジェクト構成
このリポジトリは Web ゲーム向けのモノレポ構成です（フロントエンド + バックエンド + Supabase）。
- `client/`: React + Vite + MUI のフロントエンド実装。データ取得は基本的に `useSWR` を利用します。
- `server/`: .NET 10 Minimal API サーバー本体。
- `server/Program.cs`: サーバーのエントリーポイント。DI 設定、認証、CORS、HTTP エンドポイント定義を担当します。
- `server/domain/`: コアドメインモデルとリポジトリインターフェースを配置します（例: `domain/player/Player.cs`, `domain/chat/IChatRoomRepository.cs`）。
- `server/infrastructure/`: 永続化と外部連携を担当します。EF Core の DbContext、エンティティ、リポジトリ実装、マイグレーションを含みます。
- `server/Properties/launchSettings.json`: ローカル実行プロファイルと開発用環境変数を定義します。
- `server/appsettings*.json`: 実行時設定（接続文字列、ログなど）を管理します。機密情報はコミットしないでください。
- `docs/`: 設計ドキュメント（ドメインモデル、ER 図など）。
- `supabase/`: Supabase ローカル開発用の設定・関連ファイル。

## プロダクト前提
- 本プロジェクトはチビクエ3風の Web ゲーム開発を目的とします。
- フロントエンドは React + Vite + MUI を採用し、ゲーム処理は HTTP API 経由で実行します。
- フロントエンドのデータフェッチは `useSWR` を基本方針とします。
- サーバーは HTTP API を提供し、フロントエンドから呼び出される前提です。
- リアルタイム性が必要になった機能のみ SignalR の導入を検討してください（初期段階での過剰導入は避ける）。
- インフラは Supabase（DB・認証）を前提にします。
- 公開先はフロントエンドを GitHub Pages、バックエンドを Azure App Service とします。

## 設計先行ルール（必須）
- `server/domain/` や `server/infrastructure/` の構造に影響する実装に着手する前に、必ず設計ドキュメントを更新してください。
- 対象ドキュメントは、変更対象のドメインに対応する設計図です。少なくとも共通設計として 、関連するドメインのMMDファイル、DB設計の変更を伴う場合は `docs/MMD11_ER図.mmd` を確認してください。
- クエスト関連の設計変更では、`docs/MMD06_QuestDomain.mmd` を優先して更新してください。
- GitHub Actions などから起動するメンテナンス・クリーンアップ処理の変更では、`docs/MMD07_CleanupDomain.mmd` も更新してください。
- ただしCSVの設計をER図へ記載する必要はないです.
- 変更の起点は「実装」ではなく「設計更新」です。設計更新後に実装へ進めてください。
- PR には、更新対象にした設計ドキュメントの一覧と、その差分理由を明記してください。

## ビルド・テスト・開発コマンド
リポジトリルートで実行します。
### フロントエンド
- `pnpm dev`（`client/` 配下）: フロントエンド開発サーバーを起動します。
- Codex 実行環境では `node` / `npm` / `pnpm` が利用できない場合があります。フロントエンドのビルド・起動・型チェックの最終確認は、必要に応じてローカル環境でも実施してください。

### バックエンド
- `dotnet restore`: NuGet パッケージを復元します。
- `dotnet build`: サーバーをビルドし、型エラーを確認します。
- `dotnet run --project ./server/server.csproj`: ローカルで API を起動します。
- `dotnet watch --project ./server/server.csproj run`: ローカル開発用のホットリロードを実行します。
- `dotnet test`: テストを実行します（現在はテストプロジェクト未コミット）。
- `dotnet ef migrations add <MigrationName> --project server/server.csproj --startup-project server/server.csproj --output-dir infrastructure/migrations`: マイグレーションを作成します。
- `dotnet ef database update --project server/server.csproj --startup-project server/server.csproj`: 設定済みデータベースにマイグレーションを適用します。
- `dotnet-ef` 未導入時は `dotnet tool install --global dotnet-ef --version 10.0.3` を先に実行してください。

### Supabase（ローカル）
- `pnpx supabase init`: Supabase プロジェクトを初期化します（初回のみ）。
- `pnpx supabase start`: ローカル Supabase を起動します。
- `pnpx supabase status`: ローカル Supabase の状態を確認します。
- `pnpx supabase stop`: ローカル Supabase を停止します。

## マイグレーション運用
- 公開環境の EF Core マイグレーションは GitHub Actions の `.github/workflows/db-migrate-dev.yml` / `.github/workflows/db-migrate-prod.yml` により自動適用します。
- GitHub Environments (`dev`, `prod`) の `SUPABASE_DB_CONNECTION_STRING` には Session Pooler の接続文字列を設定します。
- Azure App Service の `ConnectionStrings__Supabase` も Session Pooler を使用します。
- スキーマ変更を含む PR では、マイグレーションファイルと適用方針（手動/CI）を明記してください。

## 定期ジョブ運用
- GitHub Actions を使った定期ジョブを追加・変更する場合は、対応するサーバー側の起動経路と認可方法を合わせて更新してください。
- マーケット期限切れ出品の削除は GitHub Actions から内部メンテナンス API を呼び出して実行します。
- 定期ジョブの追加時は、必要な GitHub Secrets / アプリ設定キーを README と PR に明記してください。

## コーディング規約・命名規則
- C# 標準規約に従います。インデントは 4 スペース、型/メソッド/プロパティは `PascalCase`、ローカル変数/引数は `camelCase` を使用します。
- バックエンドではドメインロジックを `server/domain/` に、DB・フレームワーク依存コードを `server/infrastructure/` に分離してください。
- リポジトリ名は役割が分かる命名にします（例: `IPlayerRepository`, `DbPlayerRepository`）。
- `server/Program.cs` のエンドポイントハンドラは小さく保ち、複雑な処理は `server/domain/` または `server/infrastructure/` に移してください。
- フロントエンドでは既存の React + Vite + MUI 構成に沿い、状態取得は `useSWR` 方針を優先してください。

## テスト方針
- 推奨スタックは、ユニットテストに xUnit + FluentAssertions、エンドポイント検証に ASP.NET Core 統合テストです。
- テストは `tests/` 配下の別プロジェクトに配置します（例: `tests/server.tests`）。
- テストファイル名は `<TargetClass>Tests.cs`、テストメソッド名は `MethodName_Condition_ExpectedResult` を推奨します。
- ドメイン挙動、リポジトリエッジケース、認証付きエンドポイントのレスポンスを重点的にカバーしてください。

## コミット・Pull Request 方針
- 既存のコミット形式 `<type>: <short description>` に従ってください（履歴例: `feat:`, `fix:`, `docs:`, `refactor:`, `env:`, `file:`）。
- コミットは小さく、目的単位で分け、無関係なリファクタリングを同梱しないでください。
- PR には以下を含めてください。
  - 目的とスコープ
  - 関連 Issue/PR 番号
  - マイグレーションや設定変更の有無（Supabase / アプリ設定 / デプロイ設定を含む）
  - API 挙動の変更点（必要に応じてリクエスト/レスポンス例）
  - フロントエンド挙動の変更点（必要に応じて画面差分や操作手順）
