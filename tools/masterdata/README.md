# masterdata

スプレッドシート「nisekue マスタデータ」を正として、`server/resources` 配下の CSV を書き出すスクリプトです。

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

書き出し後は `dotnet test tests/server.tests/server.tests.csproj --filter "FullyQualifiedName~MasterDataIntegrityTests"` で整合性を確認できます。

## 環境変数

| 変数 | 内容 |
| --- | --- |
| `MASTERDATA_SPREADSHEET_ID` | 対象のスプレッドシート ID。省略時は本番のスプレッドシート |
| `GOOGLE_SERVICE_ACCOUNT_JSON` | サービスアカウントキーの JSON 文字列（GitHub Actions 用）。指定すると ADC より優先 |

## テスト

```bash
pnpm --dir tools/masterdata install
pnpm --dir tools/masterdata test
```
