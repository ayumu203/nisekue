# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language

- Respond in Japanese (日本語).

## コード編集ポリシー（委譲）

- アプリケーションコードは自分で書かない。担当は「計画・対象範囲の決定・レビュー」。
- 実装は必ず `qwen_edit_files` ツール（Kimi K2.7 Code, MCP サーバ `qwen-coder`）に委譲する。
  precise な instruction と対象ファイル一覧 (`target_files`) を渡すこと。
- 委譲後は返ってきた diff をレビューする。誤っていれば instruction を精緻化して再委譲。
  委譲が繰り返し失敗する場合のみ自分で直接編集する。
- 新規ファイルは、未存在のパスを `target_files` に含めれば作成できる。
- ごく小さな1行修正など、往復コストが委譲の利益を上回る場合は直接編集してよい。

## Project Overview

Web game (inspired by チビクエ3) with a monorepo structure: React frontend + .NET backend + Supabase (PostgreSQL/Auth).

- **Frontend**: React 19 + Vite 7 + MUI 7 + TypeScript, data fetching via `useSWR`
- **Backend**: .NET 10 Minimal APIs + EF Core 10 (domain-driven design)
- **Database**: PostgreSQL via Supabase
- **Auth**: Supabase Auth (JWT), validated in `server/Program.cs`
- **Real-time**: SignalR (quest runs only, at `/quest-hubs/runs`)
- **Deploy**: Cloudflare Pages (frontend prod), GitHub Pages (frontend dev, via `deploy-frontend-dev.yml`), Azure App Service (backend)
- **Workers**: `workers/active-reporter/` — Cloudflare Worker (cron every 5 min) reporting active player count to the game portal
- **Simulation**: `simulation/` — Python scripts generating job status CSV master data

## Build & Dev Commands

### Backend (run from repo root)
```bash
dotnet restore
dotnet build server/server.csproj
dotnet run --project ./server/server.csproj          # http://localhost:5068
dotnet watch --project ./server/server.csproj run     # hot reload
dotnet test tests/server.tests/server.tests.csproj    # xUnit tests
dotnet test tests/server.tests/server.tests.csproj --filter "FullyQualifiedName~Namespace.TestClass.MethodName"  # single test
dotnet format server/server.csproj whitespace --verify-no-changes
dotnet format server/server.csproj analyzers --verify-no-changes
```

### Frontend (run from `client/`)
```bash
pnpm dev          # http://localhost:5173, proxies /api to :5068 (/api prefix stripped)
pnpm build        # tsc -b (type check) then vite build
pnpm lint
pnpm format
pnpm format:check  # Prettier: singleQuote, no semi, trailingComma all, printWidth 120
```

### Supabase (local)
```bash
pnpx supabase start   # start local instance (PostgreSQL on :54322)
pnpx supabase status
pnpx supabase stop
```

Local connection string: `Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres`

### EF Core Migrations
```bash
dotnet ef migrations add <Name> --project server/server.csproj --startup-project server/server.csproj --output-dir infrastructure/migrations
dotnet ef database update --project server/server.csproj --startup-project server/server.csproj
```
Install tool if missing: `dotnet tool install --global dotnet-ef --version 10.0.3`

## Architecture

### Backend Layers (server/)
- **`domain/`**: Core entities, value objects, repository interfaces. Business logic lives here.
- **`application/`**: Application services orchestrating domain + infrastructure.
- **`infrastructure/`**: EF Core DbContext (`AppDbContext.cs`), repository implementations (`Db*Repository`), migrations.
- **`endpoints/`**: Minimal API handlers. Keep thin; delegate to application/domain.
- **`shared/`**: DTOs and shared types.
- **`resources/`**: CSV master data (moves, quest rewards, enemy spawns, etc.) loaded at startup.

### Frontend Layers (client/src/)
- **`api/`**: API client (`endpoints.ts`, `http.ts`) with Zod schemas for validation.
- **`components/`**: UI components organized by domain (quest/, player/, chat/, etc.).
- **`pages/`**: Route pages (React Router 7).
- **`contexts/`**: Auth context (`AuthProvider.tsx`).
- **`hooks/`**: Custom hooks including `useQuestRunHub.ts` for SignalR.
- **`schema/`**: Zod validation schemas.
- **`lib/`**: Pure utility functions (auth helpers, storage, formatters).
- **`client/locale/<domain>/*.json`**: UI strings imported directly by components (not a runtime i18n system).

### Adding a new frontend API endpoint
Update these four locations in order:
1. `client/src/schema/*` — Zod request/response schemas
2. `client/src/api/endpoints.ts` — contract definition (path, method, schemas)
3. `client/src/api/*.ts` — fetch + error handling function
4. `client/src/pages/*` or `components/*` — `useSWR` call or mutation

## Design-First Rule

Before implementing changes to `server/domain/` or `server/infrastructure/`, update the relevant design documents first:
- Domain model: `docs/<domain>/*.mmd` (Mermaid diagrams)
- DB schema changes: `docs/db/01_ER図.mmd`
- Quest changes: `docs/quest/04_クエストドメイン.mmd`
- Maintenance/cleanup: `docs/maintenance/01_クリーンアップドメイン.mmd`
- CSV data does not need ER diagram entries.

## Testing

- Framework: xUnit + FluentAssertions in `tests/server.tests/`
- File naming: `<TargetClass>Tests.cs`
- Method naming: `MethodName_Condition_ExpectedResult`
- Focus: domain behavior, repository edge cases, authenticated endpoint responses.

## Coding Conventions

- **C#**: PascalCase for types/methods/properties, camelCase for locals/args. 4-space indent. Namespaces use lowercase dot-separated form: `server.domain.battle`, `server.tests`.
- **TypeScript**: camelCase for variables/functions, PascalCase for components/types.
- Domain logic in `server/domain/`, framework-dependent code in `server/infrastructure/`.
- Endpoint handlers in `Program.cs` stay small; complex logic goes to domain/infrastructure.
- Frontend state fetching uses `useSWR`.
- Master data (CSV) repositories are registered as `Singleton`; DB repositories are `Scoped`.

## Commit & PR

- Format: `<type>: <short description>` (feat:, fix:, docs:, refactor:, env:, file:)
- Small, purpose-scoped commits. No unrelated refactoring mixed in.
- PRs must note: scope, related issues, migration/config changes, API behavior changes, design docs updated.

## CI

GitHub Actions (`.github/workflows/ci.yml`) runs on PRs and pushes to main:
- Backend: format check -> lint -> build -> test
- Frontend: format check -> lint -> build

Migrations auto-deploy via `db-migrate-dev.yml` / `db-migrate-prod.yml`. Use Session Pooler connection strings for Supabase.

Scheduled maintenance workflows (dev/prod pairs): `cleanup-quest-data-*.yml`, `cleanup-expired-market-listings-*.yml`, `ranking-rebuild-*.yml`.
