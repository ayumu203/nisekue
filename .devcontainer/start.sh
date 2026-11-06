#!/usr/bin/env bash
# Codespace の起動ごとに実行される。手動で再実行すれば全体を再起動できる:
#   bash .devcontainer/start.sh
#
# 1. ローカル Supabase を起動
# 2. EF Core マイグレーションを適用
# 3. バックエンド (:5068) とフロントエンド (:5173) をバックグラウンドで起動
#
# ログは ~/nisekue-logs/ に出力される。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOG_DIR="$HOME/nisekue-logs"
mkdir -p "$LOG_DIR"

export PATH="$PATH:$HOME/.dotnet/tools"
export ASPNETCORE_ENVIRONMENT=Development

stop_previous() {
  local name="$1"
  local pid_file="$LOG_DIR/$name.pid"
  if [[ -f "$pid_file" ]]; then
    local pid
    pid="$(cat "$pid_file")"
    if kill -0 "$pid" 2>/dev/null; then
      echo "    既存の $name (pid $pid) を停止"
      kill -- "-$pid" 2>/dev/null || kill "$pid" 2>/dev/null || true
      sleep 2
    fi
    rm -f "$pid_file"
  fi
}

echo "==> [1/4] Docker の起動待ち"
for _ in $(seq 1 60); do
  docker info >/dev/null 2>&1 && break
  sleep 2
done
docker info >/dev/null 2>&1 || { echo "Docker が起動していません"; exit 1; }

echo "==> [2/4] ローカル Supabase を起動"
# 使わないサービスは起動しない（メモリ節約）
(cd "$ROOT/supabase" && pnpm exec supabase start -x logflare,vector,imgproxy,edge-runtime,supavisor)

echo "==> [3/4] マイグレーションを適用"
(cd "$ROOT" && dotnet ef database update --project server/server.csproj --startup-project server/server.csproj)

echo "==> [4/4] バックエンドとフロントエンドを起動"
stop_previous backend
stop_previous frontend

(
  cd "$ROOT"
  setsid nohup dotnet run --project server/server.csproj --launch-profile http \
    >"$LOG_DIR/backend.log" 2>&1 &
  echo $! >"$LOG_DIR/backend.pid"
)

FRONTEND_ENV=()
if [[ -n "${CODESPACE_NAME:-}" ]]; then
  # ブラウザは 127.0.0.1:54321 に届かないため、Supabase も Vite 経由（/supabase）で叩かせる
  PUBLIC_ORIGIN="https://${CODESPACE_NAME}-5173.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
  cat >"$ROOT/client/.env.development.local" <<EOF
# .devcontainer/start.sh が自動生成（Git 管理外）
VITE_SUPABASE_URL=${PUBLIC_ORIGIN}/supabase
EOF
  FRONTEND_ENV=(
    "SUPABASE_PROXY_TARGET=http://127.0.0.1:54321"
    "__VITE_ADDITIONAL_SERVER_ALLOWED_HOSTS=.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
  )
fi

(
  cd "$ROOT/client"
  env "${FRONTEND_ENV[@]}" setsid nohup pnpm dev >"$LOG_DIR/frontend.log" 2>&1 &
  echo $! >"$LOG_DIR/frontend.pid"
)

echo "==> バックエンドの起動待ち"
for _ in $(seq 1 90); do
  curl -s -o /dev/null "http://localhost:5068" && break
  sleep 2
done

echo ""
echo "起動しました。"
if [[ -n "${PUBLIC_ORIGIN:-}" ]]; then
  echo "  ゲーム:          ${PUBLIC_ORIGIN}"
  echo "  Supabase Studio: https://${CODESPACE_NAME}-54323.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}"
else
  echo "  ゲーム:          http://localhost:5173"
  echo "  Supabase Studio: http://localhost:54323"
fi
echo "  ログ:            $LOG_DIR/"
