## Mock Health System

This repository is a **lightweight mock health system** to develop against. It provides API endpoints backed by a data repository, using:

- **Backend**: .NET 10 Web API with a clean architecture (API, Application, Infrastructure layers).
- **Frontend**: React + Vite + TypeScript + Tailwind CSS.
- **Database**: Postgres via Entity Framework Core.
- **Configuration**: Environment variables loaded from `backend/.env` (and `frontend/.env` for the frontend).

---

### Getting started

1. **Prerequisites**  
   Install locally: [.NET 10 SDK](https://dotnet.microsoft.com/download), [Node.js LTS](https://nodejs.org/) (npm/pnpm/yarn), and [Postgres](https://www.postgresql.org/) (or use Docker).

2. **Backend setup**
   - `cd backend`
   - Copy `.env.example` to `.env`.
   - Set `POSTGRES_CONNECTION_STRING` to your Postgres connection string (and adjust `BACKEND_URL` / `FRONTEND_URL` if needed).
   - **Create the initial database and user** (once per environment): from the repo root run `backend/scripts/init-db.sh`. This creates the `mockhealthsystem_db` database and `mockhealthsystem_user` role using the password from `POSTGRES_CONNECTION_STRING`; it requires Postgres superuser access (e.g. `postgres`). See [Initial database setup](#initial-database-setup) below if you need to run `psql` manually or use `USE_SUDO_POSTGRES=1`.
   - Run `dotnet restore`.
   - **Migrations:** If you have pending model changes (e.g. new entities), add a migration first: `dotnet ef migrations add YourMigrationName --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api` (or use `backend/scripts/run-ef.sh migrations add YourMigrationName` from repo root). Then run `dotnet ef database update` (or `backend/scripts/run-ef.sh database update`) to apply.
   - Start the API: `dotnet run --project MockHealthSystem.Api` (default: `http://localhost:5000`).

3. **Frontend setup**
   - `cd frontend`
   - Copy `.env.example` to `.env` or `.env.local`.
   - Set `VITE_API_BASE_URL` to your backend URL (e.g. `http://localhost:5000`).
   - Run `npm install`.
   - Start the dev server: `npm run dev` (default: `http://localhost:5173`).

4. **Verify**  
   Open the frontend in your browser; use the “Check API status” action to confirm the API is reachable.

---

### Project structure

- `backend/` – .NET solution and projects; `backend/.env` for backend config (from `backend/.env.example`).
- `frontend/` – React/Vite/Tailwind app; `frontend/.env` or `.env.local` for frontend config (from `frontend/.env.example`).

### Configuration reference

- **Backend** (`backend/.env`): `POSTGRES_CONNECTION_STRING`, `BACKEND_URL` (default `http://localhost:5000`), `FRONTEND_URL` (default `http://localhost:5173`). Also uses `appsettings.json` and system environment variables.
- **Frontend** (`frontend/.env` or `.env.local`): `VITE_API_BASE_URL` (e.g. `http://localhost:5000`).

### Initial database setup

The script `backend/scripts/init-db.sh` creates the Postgres database (`mockhealthsystem_db`) and role (`mockhealthsystem_user`) used by the app. It reads `backend/.env` and uses the password from `POSTGRES_CONNECTION_STRING`.

- **From repo root:** `backend/scripts/init-db.sh`
- **Custom host/port/admin user:** `backend/scripts/init-db.sh localhost 5432 postgres`
- **Peer auth (e.g. Linux postgres user):** `USE_SUDO_POSTGRES=1 backend/scripts/init-db.sh` runs only `psql` as the `postgres` OS user so no admin password is needed.
- **Run SQL manually:** copy `backend/scripts/init-db.sql`, replace `__APP_PASSWORD__` with your app user password, then run with `psql -h localhost -p 5432 -U postgres -f init-db.sql`.

Ensure `backend/.env` exists and contains `POSTGRES_CONNECTION_STRING` (see `backend/.env.example`) before running the script.

### EF migrations and proxy (403 / proxy tunnel)

If `dotnet build` succeeds but `dotnet ef database update` or `dotnet ef migrations add` fails with **403** and a message about a proxy tunnel (e.g. `http://127.0.0.1:42205/`), your shell is using a proxy that the EF tools inherit. That proxy is often set by your IDE or environment (e.g. `HTTP_PROXY` / `HTTPS_PROXY`), not by this repo.

**Run EF without the proxy for one command:**

```bash
cd backend
HTTP_PROXY= HTTPS_PROXY= dotnet ef database update --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api
```

Or use the helper script (from repo root):

```bash
backend/scripts/run-ef.sh database update
```

To add a migration without the proxy:

```bash
HTTP_PROXY= HTTPS_PROXY= dotnet ef migrations add YourMigrationName --project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api
# or
backend/scripts/run-ef.sh migrations add YourMigrationName
```

To see if a proxy is set: `echo $HTTP_PROXY $HTTPS_PROXY`. To clear it for the whole terminal session: `unset HTTP_PROXY HTTPS_PROXY`.

### Known vulnerabilities (frontend)

Running `npm audit` in `frontend/` may report **moderate** vulnerabilities in the **ajv** dependency (used by ESLint). These come from ESLint’s own dependencies; there is no patched release that fixes them without breaking the current ESLint setup. The template uses an **override** in `frontend/package.json` to fix **minimatch** (high severity); the remaining ajv advisories are accepted as known, low-risk (ReDoS in dev-only tooling). You can run `npm audit` in `frontend/` for details and re-evaluate as dependencies are updated.

