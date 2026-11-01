# masterdata

スプレッドシート「nisekue マスタデータ」を正として、`server/resources` 配下の CSV を書き出すスクリプトです。

## 全体の流れ

1. スプレッドシートを編集する
2. GitHub Actions の「Release Master Data Snapshot」（`masterdata-release.yml`）を手動実行する
   - スプレッドシートを CSV に書き出し、本物のデータで整合性テスト（`MasterDataIntegrityTests`）を実行
   - 通ったら GitHub Release（タグ `masterdata-<連番>`、例: `masterdata-3`）にスナップショット `masterdata.json` を公開
   - `masterdata.lock` をその連番に更新する PR を作成し、CI を起動（未マージの更新 PR があるときは実行できない）
3. PR をマージすると、デプロイ時に `masterdata.lock` の版が取得されてビルドに含まれる

ロールバックは `masterdata.lock` を前の版に戻す PR を出すだけです（スプレッドシートを戻す必要はありません）。

スナップショットの連番はゲームのバージョンとは独立しています。ゲームのバージョンは prod へのリリース時に Git タグ（`vX.Y.Z`）で付けます。

## スプレッドシートの構成

- `_index` タブ: 1列目がタブ名、2列目が書き出し先の CSV パス（`server/resources/` 配下の `.csv` のみ）
- それ以外のタブ: 1行目がヘッダー、2行目以降がデータ

書き出しのルール:

- 列数はヘッダー行の長さで決まります。ヘッダーより右にデータがあるとエラーになります
- 完全に空の行は無視します
- 数値は数値のまま、テキストは入力した表記のまま書き出します。`0.30` のように末尾のゼロを残したい値はテキストとして入力してください
- サーバーのローダーは各行を `,` で分割するため、カンマや改行を含むセルはエラーになります
- 改行コードは LF、ファイル末尾に改行を付けます

## ローカルで使う

確定済みの版（`masterdata.lock`）を取得する。認証は不要です。

```bash
pnpm masterdata:pull
```

スプレッドシートの最新（未公開の編集を含む）をそのまま書き出す場合は、次の「スプレッドシートから同期する」を使います。

## スプレッドシートから同期する

最初に `tools/masterdata/.env.example` を `tools/masterdata/.env` にコピーし、`MASTERDATA_SPREADSHEET_ID` にスプレッドシートの ID を書きます。

リポジトリのルートで実行します。スプレッドシートの最新の内容で `server/resources` の CSV が上書きされます。

```bash
pnpm masterdata:sync
```

認証には Google の Application Default Credentials を使います。どちらかを設定してください。

- サービスアカウントキーを使う: `export GOOGLE_APPLICATION_CREDENTIALS=/path/to/key.json`
- 自分の Google アカウントを使う（スプレッドシートの閲覧権限が必要）:

  ```bash
  gcloud auth application-default login \
    --scopes=https://www.googleapis.com/auth/cloud-platform,https://www.googleapis.com/auth/spreadsheets.readonly
  ```

書き出し後は `dotnet test tests/server.tests/server.tests.csproj -p:UseMasterDataFixtures=false --filter "FullyQualifiedName~MasterDataIntegrityTests"` で整合性を確認できます。`-p:UseMasterDataFixtures=false` を付けないと、テスト用 CSV（`tests/server.tests/fixtures/resources`）に対して実行されます。

## 環境変数

| 変数 | 内容 |
| --- | --- |
| `MASTERDATA_SPREADSHEET_ID` | 対象のスプレッドシート ID（必須）。`tools/masterdata/.env` に書いてもよい |
| `GOOGLE_SERVICE_ACCOUNT_JSON` | サービスアカウントキーの JSON 文字列（GitHub Actions 用）。指定すると ADC より優先 |
| `MASTERDATA_REPOSITORY` | `pull` でスナップショットを取得するリポジトリ。省略時は `ayumu203/nisekue` |

## GitHub Actions の設定

- Secret `GOOGLE_SERVICE_ACCOUNT_JSON`: サービスアカウントキー（JSON の中身）。スプレッドシートをサービスアカウントのメールアドレスに閲覧者として共有しておく
- Variable `MASTERDATA_SPREADSHEET_ID`: スプレッドシートの ID
- Settings > Actions > General の「Allow GitHub Actions to create and approve pull requests」を有効にする

デプロイ（`deploy-backend-*.yml`、`docker-build.yml`）は `masterdata.lock` があればその版を取得してからビルドします。サーバーで `docker compose build` する場合も、先に `pnpm masterdata:pull` を実行してください。

## テスト

```bash
pnpm --dir tools/masterdata install
pnpm --dir tools/masterdata test
```
