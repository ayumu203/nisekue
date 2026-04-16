# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language

- Respond in Japanese (日本語).

## Project Overview

Web game (inspired by チビクエ3) with a monorepo structure: React frontend + .NET backend + Supabase (PostgreSQL/Auth).

- **Frontend**: React 19 + Vite 7 + MUI 7 + TypeScript, data fetching via `useSWR`
- **Backend**: .NET 10 Minimal APIs + EF Core 10 (domain-driven design)
- **Database**: PostgreSQL via Supabase
- **Auth**: Supabase Auth (JWT), validated in `server/Program.cs`
- **Real-time**: SignalR (quest runs only, at `/quest-hubs/runs`)
- **Deploy**: GitHub Pages (frontend), Azure App Service (backend)

## Build & Dev Commands

### Backend (run from repo root)
```bash
dotnet restore
dotnet build server/server.csproj
dotnet run --project ./server/server.csproj          # http://localhost:5068
dotnet watch --project ./server/server.csproj run     # hot reload
dotnet test tests/server.tests/server.tests.csproj    # xUnit tests
dotnet format server/server.csproj whitespace --verify-no-changes
dotnet format server/server.csproj analyzers --verify-no-changes
```

### Frontend (run from `client/`)
```bash
pnpm dev          # http://localhost:5173, proxies /api to :5068
pnpm build
pnpm lint
pnpm format
pnpm format:check
```

### Supabase (local)
```bash
pnpx supabase start   # start local instance (PostgreSQL on :54322)
pnpx supabase status
pnpx supabase stop
```

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

- **C#**: PascalCase for types/methods/properties, camelCase for locals/args. 4-space indent.
- **TypeScript**: camelCase for variables/functions, PascalCase for components/types.
- Domain logic in `server/domain/`, framework-dependent code in `server/infrastructure/`.
- Endpoint handlers in `Program.cs` stay small; complex logic goes to domain/infrastructure.
- Frontend state fetching uses `useSWR`.

## Commit & PR

- Format: `<type>: <short description>` (feat:, fix:, docs:, refactor:, env:, file:)
- Small, purpose-scoped commits. No unrelated refactoring mixed in.
- PRs must note: scope, related issues, migration/config changes, API behavior changes, design docs updated.

## CI

GitHub Actions (`.github/workflows/ci.yml`) runs on PRs and pushes to main:
- Backend: format check -> lint -> build -> test
- Frontend: format check -> lint -> build

Migrations auto-deploy via `db-migrate-dev.yml` / `db-migrate-prod.yml`. Use Session Pooler connection strings for Supabase.
