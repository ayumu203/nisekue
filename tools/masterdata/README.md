# masterdata

スプレッドシート「nisekue マスタデータ」を正として、`server/resources` 配下の CSV を書き出すスクリプトです。CSV はリポジトリに置きません（`.gitignore` 対象）。

## 本番・dev 環境への反映

バックエンドのデプロイ（`deploy-backend-dev.yml`・`deploy-backend-prod.yml`）は、実行のたびにスプレッドシートを読み込んでからビルドします。

- スプレッドシートを編集したら、GitHub Actions でデプロイのワークフローを手動実行すれば反映されます（dev 環境・本番環境それぞれ）
- コードのデプロイでも毎回最新のスプレッドシートが入るので、反映し忘れは起きません。逆に、編集途中のデータもそのとき入ります
- サーバーが起動できないデータ（数値の列に文字、存在しない ID の参照など）は整合性テストで検出し、デプロイを止めます。値を変えただけでは止まりません

GitHub Actions の設定:

- Secret `GOOGLE_SERVICE_ACCOUNT_JSON`: サービスアカウントキー（JSON の中身）。スプレッドシートをサービスアカウントのメールアドレスに閲覧者として共有しておく
- Variable `MASTERDATA_SPREADSHEET_ID`: スプレッドシートの ID。環境（`dev`・`prod`）ごとに設定すれば、dev 環境と本番で別のスプレッドシートを使える

サーバーで `docker compose build` する場合は、先に `pnpm masterdata:sync` を実行してください。

## スプレッドシートの構成

- `_index` タブ: 1列目がタブ名、2列目が書き出し先の CSV パス（`server/resources/` 配下の `.csv` のみ）
- それ以外のタブ: 1行目がヘッダー、2行目以降がデータ

書き出しのルール:

- 列数はヘッダー行の長さで決まります。ヘッダーより右にデータがあるとエラーになります
- 完全に空の行は無視します
- 数値は数値のまま、テキストは入力した表記のまま書き出します。`0.30` のように末尾のゼロを残したい値はテキストとして入力してください
- サーバーのローダーは各行を `,` で分割するため、カンマや改行を含むセルはエラーになります
- 改行コードは LF、ファイル末尾に改行を付けます

## ローカルで同期する

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

## テスト

```bash
pnpm --dir tools/masterdata install
pnpm --dir tools/masterdata test
```
