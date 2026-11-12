# スプレッドシートからステージングへ反映する

ステージング用スプレッドシートに「nisekue」メニューを追加し、ターミナルを開かずにステージングへ反映できるようにします。

| メニュー | 動き |
| --- | --- |
| ステージングに反映 | `deploy-backend-dev.yml` を main で起動。ステージング用スプレッドシートを取り込み、整合性テストを通ったらステージングのバックエンドをデプロイ |
| 直近の反映結果を見る | 最後の実行の状態（実行中・成功・失敗）と URL を表示 |
| 本番へ昇格 | `promote-masterdata.yml` を起動（ステージング用の内容で本番用スプレッドシートを上書きして本番へデプロイ。`prod` Environment の承認が必要） |
| 直近の昇格結果を見る | 同上 |

サーバーはマスタデータを起動時に読み込むので、反映には再デプロイが必要です。押してから数分かかります。

## 運用の決まり

- 数値を調整するのは**ステージング用スプレッドシートだけ**。本番用スプレッドシートは直接編集しない
- ステージングで確認できたら「本番へ昇格」で本番用に反映する
- PR の CI（`ci.yml` の Master data）はリポジトリ単位の `MASTERDATA_SPREADSHEET_ID`（＝本番用）を見る。列を増やす・変える PR のときは、ステージング用と本番用の両方のスプレッドシートを直す

## 設定する値と取得元

| 設定場所 | 名前 | 値 | 取得元 |
| --- | --- | --- | --- |
| GitHub → Settings → Environments → `dev` → Environment variables | `MASTERDATA_SPREADSHEET_ID` | ステージング用スプレッドシートの ID | スプレッドシートの URL の `/d/` と `/edit` の間 |
| GitHub → Settings → Environments → `prod` → Deployment protection rules | Required reviewers | 自分の GitHub アカウント | — |
| Apps Script → プロジェクトの設定 → スクリプト プロパティ | `GITHUB_TOKEN` | fine-grained personal access token | GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens で作成（下の 3） |
| 同上 | `STAGING_SPREADSHEET_ID` | ステージング用スプレッドシートの ID | `MASTERDATA_SPREADSHEET_ID`（dev）と同じ値 |
| ステージング用スプレッドシートの共有 | 閲覧者 | サービスアカウントのメールアドレス | Secret `GOOGLE_SERVICE_ACCOUNT_JSON` の `client_email`。Secret は GitHub から読めないので、元の JSON キー（Google Cloud コンソール → IAM と管理 → サービス アカウント）か、本番用スプレッドシートの共有設定に並んでいる `...iam.gserviceaccount.com` のアドレスを見る |
| 本番用スプレッドシートの共有 | 編集者（`promote-masterdata.yml` を使う場合） | 同上 | 同上 |

リポジトリ単位の `GOOGLE_SERVICE_ACCOUNT_JSON`（Secret）と `MASTERDATA_SPREADSHEET_ID`（Variable、本番用）は既存のものをそのまま使う。

## 初回の設定

### 1. ステージング用スプレッドシートを作る

1. 本番用スプレッドシート「nisekue マスタデータ」を開き、ファイル → コピーを作成。名前は「nisekue マスタデータ（ステージング）」など
2. 共有で、サービスアカウント（`GOOGLE_SERVICE_ACCOUNT_JSON` の `client_email`）を**閲覧者**として追加
3. URL の `/d/` と `/edit` の間がスプレッドシートの ID

### 2. ステージングが新しいスプレッドシートを読むようにする

GitHub → Settings → Environments → `dev` → Environment variables に `MASTERDATA_SPREADSHEET_ID` を追加し、1 の ID を入れます。

- リポジトリ単位の `MASTERDATA_SPREADSHEET_ID`（本番用）はそのまま残す。`dev` Environment の値が優先される
- 設定後に一度「ステージングに反映」（または Actions から「Deploy Backend - Development」）を実行し、ログの Import master data で 1 の ID が使われていることを確認

### 3. GitHub のトークンを作る

GitHub → Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token

- Repository access: Only select repositories → `ayumu203/nisekue`
- Permissions: Repository permissions → **Actions: Read and write**（ほかは不要）
- 有効期限が切れたらメニューが 401 のエラーを出すので、作り直してスクリプト プロパティを差し替える

### 4. Apps Script を貼る

1. ステージング用スプレッドシートで 拡張機能 → Apps Script
2. `Code.gs` の中身を同じファイルに貼り付け
3. プロジェクトの設定 → 「`appsscript.json` マニフェスト ファイルをエディタで表示する」をオンにし、`appsscript.json` を同じ内容にする
4. プロジェクトの設定 → スクリプト プロパティに追加
   - `GITHUB_TOKEN`：3 のトークン
   - `STAGING_SPREADSHEET_ID`：1 の ID（これと一致しないスプレッドシートではメニューが動かない。本番用にスクリプトが紛れ込んでも誤って起動しない）
5. スプレッドシートを再読み込み → メニュー「nisekue」→「直近の反映結果を見る」を一度実行して権限を許可

`Code.gs` を変更したら、このフォルダの控えも更新してください。
