# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A lightweight mock healthcare API and admin frontend for development and integration testing. The backend exposes a realistic health domain (patients, conditions, medications, procedures) with configurable authentication modes. The frontend is an admin UI for signing in with the static admin key (minting a short-lived session JWT), managing auth settings, viewing request logs, and generating synthetic patient data.

## Commands

### Backend (.NET 10)

```bash
cd backend
dotnet restore
dotnet run --project MockHealthSystem.Api   # starts on http://localhost:5001
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

- **MockHealthSystem.Api** — ASP.NET Core host. Controllers (all versioned under `/api/v1/`), middleware, DI setup in `Program.cs`, and `Services/` (auth settings, admin session JWT mint/validate, patient faker, patient mapping).
- **MockHealthSystem.Infrastructure** — EF Core `AppDbContext` with 40+ DbSets, all domain entities in `Data/Entities/`, and migrations.
- **MockHealthSystem.Tests** — Integration tests via `WebApplicationFactory` with an in-memory EF database. No PostgreSQL required for tests.

**Authentication** lives in `Api/Authentication/MockAuthHandler.cs`. Modes stored in the database (`AuthSettings` entity) and cached in-memory include `None`, `Bearer` (single shared token), `CCAPIKey` (shared secret sent as the `CCAPIKey` header), and `OAuth` (client credentials + refresh).

**Admin API access** (auth settings, monitoring, test data): If `AUTH_SETTINGS_ADMIN_KEY` is unset, admin routes are open (local dev). If set, clients must send a valid HS256 JWT in `X-Admin-Session` (minted by `POST /api/v1/admin/sessions` with JSON body `{ "adminKey": "..." }`). The raw admin key is only accepted at the mint endpoint — it cannot be used directly on other admin routes. Signing material: prefer env `ADMIN_SESSION_SIGNING_KEY` (or `AdminSession:SigningKey` in config); if omitted, a key is derived from `AUTH_SETTINGS_ADMIN_KEY`. Session TTL: `AdminSession:TtlMinutes` in config or env `AdminSession__TtlMinutes` (see `appsettings.json` / `AdminSessionOptions`). **Test data** endpoints additionally skip admin checks when `ASPNETCORE_ENVIRONMENT` is `Development` (`TestDataController` passes `bypassAdminChecksInDevelopment: true`); **auth settings** and **monitoring** do not bypass.

**Request logging** — `RequestLoggingMiddleware` records every API call into `ApiRequestLog`. The `/api/v1/monitoring/*` endpoints expose these logs and stats.

### Frontend

Single-page app wrapped in `AdminSessionProvider` (`AdminSessionContext.tsx`). `App.tsx` renders tabbed views including:
- `AdminAccessPage` — enter static admin key once; mints session via `exchangeAdminSession`; stores JWT in `sessionStorage` (`adminSessionStore.ts`)
- `AuthSettingsPage` — configure auth mode and tokens
- `MonitoringPage` — view request logs and stats
- `TestDataPage` — generate/reset/look up synthetic patients

All API calls go through `src/api.ts` (Axios client, typed). A request interceptor on the main client adds `X-Admin-Session` when a non-expired token exists; `mintApi` is used for the mint endpoint so the session header is not sent there. Backend URL is set via `VITE_API_BASE_URL` in `frontend/.env`.

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

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan at
`specs/feature/004-study-domain/plan.md`.
<!-- SPECKIT END -->
