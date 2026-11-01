#!/usr/bin/env bash
# Codespace 作成時に 1 回だけ実行される。依存関係のインストールのみ行う。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

export PATH="$PATH:$HOME/.dotnet/tools"

if ! command -v pnpm >/dev/null 2>&1; then
  npm install -g pnpm
fi

echo "==> dotnet-ef をインストール"
dotnet tool install --global dotnet-ef --version 10.0.3 || dotnet tool update --global dotnet-ef --version 10.0.3

echo "==> バックエンドの依存関係を復元"
dotnet restore server/server.csproj

echo "==> フロントエンドの依存関係をインストール"
(cd client && pnpm install --frozen-lockfile)

echo "==> Supabase CLI をインストール"
(cd supabase && pnpm install --frozen-lockfile)

echo "==> 完了"
