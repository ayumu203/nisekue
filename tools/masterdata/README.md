# masterdata

スプレッドシートを正として、`server/resources` 配下の CSV を書き出すスクリプトです。CSV はリポジトリに置きません（`.gitignore` 対象）。

| スプレッドシート | 用途 | ID の設定先 |
| --- | --- | --- |
| `[staging] nisekue masterdata` | ステージング。数値の調整はこちらだけで行う | `dev` Environment の Variable `MASTERDATA_SPREADSHEET_ID` |
| `[prod] nisekue masterdata` | 本番・PR の CI。直接編集せず、`sync-masterdata-to-prod.yml` で同期する | リポジトリ単位の Variable `MASTERDATA_SPREADSHEET_ID` |

## 本番・ステージングへの反映

バックエンドのデプロイは、実行のたびに GitHub Actions 上でスプレッドシートを読み込み、整合性テストを通してからビルドします。取り込みの処理は共通部品 `.github/actions/masterdata-import` にまとめてあり、各デプロイはそれを呼ぶだけです。

| 環境 | ワークフロー | マスタデータの渡し方 |
| --- | --- | --- |
| 本番（Oracle VM） | `deploy-backend-vm-prod.yml`（`prod` への push・手動実行） | Actions で取り込んだ CSV を VM に送り、VM でコミットを checkout した後に `server/resources` に置いてから `docker compose up -d --build`。Google の認証情報は VM に置かない |
| dev（Azure App Service） | `deploy-backend-dev.yml`（`main` への push・手動実行） | 取り込んだ CSV を含めて `dotnet publish` する |

- スプレッドシートを編集したら、デプロイのワークフローを手動実行すれば反映されます。本番は `prod` ブランチでのみ実行できます
- コードのデプロイでも毎回最新のスプレッドシートが入るので、反映し忘れは起きません。逆に、編集途中のデータもそのとき入ります
- サーバーが起動できないデータ（数値の列に文字、存在しない ID の参照など）は整合性テストで検出し、サーバーに触る前にデプロイを止めます。値を変えただけでは止まりません

GitHub Actions の設定:

- Secret `GOOGLE_SERVICE_ACCOUNT_JSON`: サービスアカウントキー（JSON の中身）。スプレッドシートをサービスアカウントのメールアドレスに閲覧者として共有しておく
- Variable `MASTERDATA_SPREADSHEET_ID`: 本番のスプレッドシートの ID。**リポジトリ単位**で登録する（PR の CI のジョブは環境を指定しないため）
  - ステージングで別のスプレッドシートを使うときは、ステージングの Environment に同じ名前の Variable を設定して上書きする

VM 上で手動で `docker compose up -d --build` した場合は、前回のデプロイで置いた CSV がそのまま使われます（`.gitignore` 対象なので checkout では消えません）。

## ステージングから本番への同期

バランス調整などの数値はステージング用スプレッドシートで編集し、ステージングで確認できたら本番用へ同期します。本番用スプレッドシートは直接編集しません。

- ワークフロー `sync-masterdata-to-prod.yml`（手動実行、main のみ）。ステージング用スプレッドシートのメニュー「nisekue → 本番へ同期」からも起動できる
  1. ステージング用スプレッドシートを main のコードで整合性テスト（`dev` Environment）
  2. 本番用スプレッドシートとの差分をジョブのサマリーに出し、ステージング用の内容で上書き（`prod` Environment）。`_index` と、そこに載っているタブだけを対象にする。数式は数式、テキストはテキストのまま写す
  3. 続けて `deploy-backend-vm-prod.yml` を `prod` ブランチで起動（入力 `deploy` で切り替え）
- 入力 `dry_run` を付けると、差分の確認だけで書き換えない
- 手元でも実行できる: `node tools/masterdata/src/syncToProduction.mjs --from <ステージング用ID> --to <本番用ID> [--dry-run]`

必要な設定:

- サービスアカウントを本番用スプレッドシートの**編集者**にする（ステージング用は閲覧者のまま）
- `dev` Environment の Variable `MASTERDATA_SPREADSHEET_ID` をステージング用の ID で上書きする（本番用と同じだとワークフローが止まる）
- （任意）`prod` Environment に承認者（Required reviewers）を付けると、GitHub で承認するまで本番用スプレッドシートを書き換えない

## PR の CI でのチェック

`ci-backend.yml` の「Master data」ジョブが、PR ごとに次を確認します。

1. スプレッドシートを書き出し、テスト用 CSV（`tests/server.tests/fixtures/resources`）とファイル構成・ヘッダー（列名と順番）が一致しているか（`src/structure.mjs`）。値の違いは見ません
2. PR のコードのローダーで、スプレッドシートのデータを読み込めるか（整合性テスト）

機能追加でテスト用 CSV の列を変えたりローダーを変えたりしたのに、スプレッドシートを直し忘れているとここで落ちます。スプレッドシートを直してから CI を再実行してください。

- 列を変える PR のためにスプレッドシートを先に直すと、その PR をマージするまでは他の PR でこのチェックが落ちます

## スプレッドシートの構成

- `_index` タブ: 1列目がタブ名、2列目が書き出し先の CSV パス（`server/resources/` 配下の `.csv` のみ）
- それ以外のタブ: 1行目がヘッダー、2行目以降がデータ

書き出しのルール:

- 列数はヘッダー行の長さで決まります。ヘッダーより右にデータがあるとエラーになります
- 完全に空の行は無視します
- 数値は数値のまま、テキストは入力した表記のまま書き出します。`0.30` のように末尾のゼロを残したい値はテキストとして入力してください
- サーバーのローダーは各行を `,` で分割するため、カンマや改行を含むセルはエラーになります
- 改行コードは LF、ファイル末尾に改行を付けます

## Codespaces

起動スクリプト（`.devcontainer/start.sh`）がマスタデータを用意します。Codespaces 用の Secret に `GOOGLE_SERVICE_ACCOUNT_JSON` と `MASTERDATA_SPREADSHEET_ID` を登録していればスプレッドシートから取得し、なければ（または取得に失敗したら）テスト用 CSV を使います。

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
node tools/masterdata/src/structure.mjs tests/server.tests/fixtures/resources server/resources  # 形の突き合わせを手元で
```
