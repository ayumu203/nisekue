# Codespaces 検証環境

ローカルに環境を作らず、GitHub Codespaces 上で Supabase・バックエンド・フロントエンドを一式立ち上げて動作確認するための手順。主な用途は PR の検証。

## 使い方

1. GitHub の PR 画面（またはブランチ）で「Code」→「Codespaces」→「Create codespace on ＜ブランチ＞」
2. 作成後、自動で以下が実行される（初回は数分かかる）
   - 作成時（`.devcontainer/post-create.sh`）：dotnet-ef・バックエンド・フロントエンド・Supabase CLI の依存関係をインストール
   - 起動時（`.devcontainer/start.sh`）：ローカル Supabase 起動 → EF Core マイグレーション適用 → バックエンド（:5068）とフロントエンド（:5173）をバックグラウンド起動
3. 「ポート」タブの **Game (Vite)**（5173）を開いて動作確認
4. 終わったら Codespace を削除する（停止だけでもよいが、ストレージ枠を消費し続ける）

## 構成メモ

- ブラウザから開くのは 5173 のみ。`/api` はバックエンド、`/supabase` はローカル Supabase へ Vite がプロキシする
  - ブラウザから `127.0.0.1:54321` に届かないため、Codespaces 上でのみ `client/.env.development.local` を自動生成して `VITE_SUPABASE_URL` を `/supabase` 経由に差し替えている
  - Vite のホストチェックは `__VITE_ADDITIONAL_SERVER_ALLOWED_HOSTS` で Codespaces のドメインを許可している
- JWT の発行者は従来どおり `http://127.0.0.1:54321/auth/v1` なので、バックエンドの設定（`launchSettings.json`）は変更不要
- DB は毎回まっさら。マスターデータは CSV から読み込まれるが、プレイヤーデータは作り直しになる
- ローカル Supabase はメール確認なしで登録できる（匿名ログインも有効）
- Supabase Studio は 54323 番（ポートタブから開ける）

## よく使うコマンド

```bash
# 全体を再起動（Supabase は起動済みならそのまま）
bash .devcontainer/start.sh

# ログ
tail -f ~/nisekue-logs/backend.log
tail -f ~/nisekue-logs/frontend.log

# DB を初期状態に戻したい場合
(cd supabase && pnpm exec supabase db reset) && bash .devcontainer/start.sh
```

## 料金

- 個人アカウントの Free プランは月 120 コア時間・15GB ストレージまで無料
- `hostRequirements` で 4 コア以上を指定しているため、無料枠では月 30 時間程度が目安
- 起動を速くしたい場合は、リポジトリ設定で Codespaces の prebuild を有効にする（Actions の実行時間とストレージを消費する）

## ローカルで使う場合

VS Code の Dev Containers 拡張でもそのまま使える。この場合 `CODESPACE_NAME` が無いので、Supabase のプロキシは使わず従来どおり `http://localhost:5173` で開く。
