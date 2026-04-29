# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A lightweight mock healthcare API and admin frontend for development and integration testing. The backend exposes a realistic health domain (patients, conditions, medications, procedures) with configurable authentication modes. The frontend is an admin UI for managing auth settings, viewing request logs, and generating synthetic patient data.

## Commands

### Backend (.NET 10)

```bash
cd backend
dotnet restore
dotnet run --project MockHealthSystem.Api   # starts on http://localhost:5000
dotnet test                                  # run all tests
```

**EF Core migrations** — use the helper script to avoid proxy-related `dotnet ef` failures:
```bash
backend/scripts/run-ef.sh migrations add MigrationName
backend/scripts/run-ef.sh database update
```

Or directly:
```bash
dotnet ef migrations add MigrationName --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api
dotnet ef database update --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api
```

**Database setup** (first time):
```bash
# Copy and edit backend/.env from backend/.env.example (set POSTGRES_CONNECTION_STRING)
backend/scripts/init-db.sh
dotnet ef database update --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api
```

### Frontend (React + Vite)

```bash
cd frontend
npm install
npm run dev       # http://localhost:5176
npm run build
npm run lint
npm run test      # Vitest single run
npm run test:watch
```

## Architecture

### Backend (Clean-ish Layers)

Three projects in `MockHealthSystem.sln`:

- **MockHealthSystem.Api** — ASP.NET Core host. Controllers (all versioned under `/api/v1/`), middleware, DI setup in `Program.cs`, and `Services/` (auth settings, patient faker, patient mapping).
- **MockHealthSystem.Infrastructure** — EF Core `AppDbContext` with 40+ DbSets, all domain entities in `Data/Entities/`, and migrations.
- **MockHealthSystem.Tests** — Integration tests via `WebApplicationFactory` with an in-memory EF database. No PostgreSQL required for tests.

**Authentication** lives in `Api/Authentication/MockAuthHandler.cs`. Three modes stored in the database (`AuthSettings` entity) and cached in-memory: `None`, `Bearer` (single shared token), and `OAuth` (client credentials + refresh). Admin endpoints are additionally protected by the `X-Admin-Key` header (env: `AUTH_SETTINGS_ADMIN_KEY`).

**Request logging** — `RequestLoggingMiddleware` records every API call into `ApiRequestLog`. The `/api/v1/monitoring/*` endpoints expose these logs and stats.

### Frontend

Single-page app. `App.tsx` renders four tab pages:
- `AuthSettingsPage` — configure auth mode and tokens
- `MonitoringPage` — view request logs and stats
- `TestDataPage` — generate/reset/look up synthetic patients

All API calls go through `src/api.ts` (Axios client, typed). Backend URL is set via `VITE_API_BASE_URL` in `frontend/.env`.

## EF Core Migration Gotchas

- Generated migrations require `[DbContext(typeof(AppDbContext))]` and `[Migration("YYYYMMDDHHMMSS_MigrationName")]` attributes to be discoverable — always generate via CLI, not by hand.
- If EF reports "already up to date" but the schema is missing changes: the migration ID may be recorded in `__EFMigrationsHistory` without the DDL having run. Use `backend/scripts/run-undo-migration.sh` to remove the row, then re-run `database update`.
- `AppDbContextModelSnapshot.cs` must stay in sync with the current model or EF will report pending model changes on the next migration.
- Shell scripts that need `psql` must load `backend/.env` and parse `POSTGRES_CONNECTION_STRING` into individual `PG*` env vars before calling `psql` — the shell does not load `.env` automatically.

## Key Conventions

**Backend:**
- PascalCase for classes/methods/public members, camelCase for locals/private fields.
- All async I/O with `async`/`await`.
- Global exception handling in `ExceptionHandlingMiddleware` — don't swallow exceptions locally.
- Model validation via Data Annotations + `ModelValidationFilter` action filter.

**Frontend:**
- Strict TypeScript (`any` is off-limits). Use `interface` for extensible object shapes, `type` for unions/derived types.
- All styling via Tailwind utility classes — no inline styles except for dynamic values.
- API calls only from `api.ts`; components handle loading/error state from responses.
- Test behavior and user-facing outcomes (React Testing Library), not implementation details.
