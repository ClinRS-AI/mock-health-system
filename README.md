# Mock Health System

[![Build](https://github.com/ClinRS-AI/mock-health-system/actions/workflows/build.yml/badge.svg)](https://github.com/ClinRS-AI/mock-health-system/actions/workflows/build.yml)

A self-hosted mock of the [Clinical Conductor (CC)](https://www.clinicalconductor.com) API, built by [ClinRS](https://clinrs.ai) for developers building CC integrations. It exposes the same API surface as the CC Public API, supports all CC authentication modes, generates synthetic clinical data on demand, and provides a web-based admin interface for configuration, monitoring, and data management.

Use it to develop and test CC integrations on a local machine or shared server — without access to a production CC environment, without real patient data, and without slow environment onboarding. No clinical accuracy is claimed; this is a developer convenience tool, not a compliance-tested CC sandbox.

- **Backend**: .NET 10 Web API (Api and Infrastructure layers, EF Core, ASP.NET Core middleware).
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
   - Start the API: `dotnet run --project MockHealthSystem.Api` (default: `http://localhost:5001`).

3. **Frontend setup**
   - `cd frontend`
   - Copy `.env.example` to `.env` or `.env.local`.
   - Set `VITE_API_BASE_URL` to your backend URL (e.g. `http://localhost:5001`).
   - Run `npm install`.
   - Start the dev server: `npm run dev` (default: `http://localhost:5176`).

4. **Verify**  
   Open the frontend in your browser; use **Check API status** to confirm the API is reachable. If the server has `AUTH_SETTINGS_ADMIN_KEY` set, open **Admin access**, enter the key, and sign in so **Authentication settings**, **Monitoring**, and **Test data management** can call the API (session JWT is stored in the browser tab’s `sessionStorage`).

---

### Demo mode

When `AUTH_SETTINGS_ADMIN_KEY` is configured on the backend, unauthenticated visitors see a **Demo Mode** experience on the Authentication settings, Monitoring, and Test data management tabs. Pages are pre-populated with realistic static data and display a banner directing users to **Admin access** to sign in.

**What demo mode does:**

- Shows representative static data (25 request log entries, auth settings, test data stats) — no backend calls are made.
- All buttons and form controls are visible and clickable but produce no side effects.
- A persistent amber banner explains demo mode and links to the Admin access tab.
- Demo mode is suppressed on the Admin access tab so users can always sign in.

**When demo mode is active vs suppressed:**

| Scenario | Demo mode |
|---|---|
| `AUTH_SETTINGS_ADMIN_KEY` is set and no session exists | **Active** |
| `AUTH_SETTINGS_ADMIN_KEY` is set and user has signed in | **Suppressed** (live data loads) |
| `AUTH_SETTINGS_ADMIN_KEY` is **not** set (open/local dev mode) | **Suppressed** (live data loads) |

**Offline resilience:** Demo mode activates automatically even if the backend is offline. The frontend probes `HEAD /api/v1/auth-settings` on startup; any network error is treated the same as a 401 (protected), so demo data is shown without any dependency on a running API server.

**Returning to live mode:** Sign in via **Admin access**. The session is stored in `sessionStorage` for the browser tab and expires according to `AdminSession__TtlMinutes` (default 30 minutes). After the session expires, the page automatically reverts to demo mode.

---

### Project structure

- `backend/` – .NET solution and projects; `backend/.env` for backend config (from `backend/.env.example`).
- `frontend/` – React/Vite/Tailwind app; `frontend/.env` or `.env.local` for frontend config (from `frontend/.env.example`).

### Configuration reference

- **Backend** (`backend/.env`): `POSTGRES_CONNECTION_STRING`, `BACKEND_URL` (default `http://localhost:5001`), `FRONTEND_URL` (default `http://localhost:5176`), `SOAP_REPORT_PASSWORD` (required for SOAP report endpoint). Optional **`AUTH_SETTINGS_ADMIN_KEY`** — when set, admin routes require a valid HS256 JWT in **`X-Admin-Session`** (minted via `POST /api/v1/admin/sessions` with body `{ "adminKey": "..." }`; the raw key is only accepted at that mint endpoint). Optional **`ADMIN_SESSION_SIGNING_KEY`** — dedicated HS256 secret for those JWTs (if unset, a key is derived from `AUTH_SETTINGS_ADMIN_KEY` when that is set). Session lifetime: **`AdminSession__TtlMinutes`** (environment) or `AdminSession:TtlMinutes` in `appsettings.json` (default 30). Also uses `appsettings.json` and system environment variables.
- **Auth settings — rate limiting**: Configurable per-IP rate limiting is stored in the `AuthSettings` database row and managed from the **Authentication Settings** tab. Fields: `RateLimitEnabled` (bool, default `false`), `RateLimitPerSecond` (int, default `10`), `RateLimitPerMinute` (int, default `300`). When enabled, API data endpoints return **429 Too Many Requests** with a `Retry-After` header (seconds until the limiting window resets — always the longer of the per-second and per-minute windows). Admin interface endpoints (`/api/v1/auth-settings`, `/api/v1/monitoring`, `/api/v1/test-data`, `/api/v1/admin`) are exempt from the configurable limit and use a separate, generous built-in limit. Changing settings resets all in-memory counters immediately.
- **Frontend** (`frontend/.env` or `.env.local`): `VITE_API_BASE_URL` (e.g. `http://localhost:5001`).

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

### Full-solution test coverage (Coverlet + ReportGenerator)

Run full backend solution tests with Cobertura coverage collection and HTML/text reports:

```bash
backend/scripts/run-full-coverage.sh
```

The script runs:

- `dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/MockHealthSystem.Infrastructure/Migrations/*.cs"`
- `reportgenerator -reports:"./TestResultsFullCoverage/**/coverage.cobertura.xml" -targetdir:"./TestResultsFullCoverage/report-html" -reporttypes:"Html;TextSummary"`

Coverage excludes EF Core migration source files (`MockHealthSystem.Infrastructure.Migrations.*`) so line and branch metrics focus on application/runtime code.

Coverage artifacts are written to:

- Cobertura XML files: `./TestResultsFullCoverage/**/coverage.cobertura.xml`
- HTML report entry point: `./TestResultsFullCoverage/report-html/index.html`
- Text summary: `./TestResultsFullCoverage/report-html/Summary.txt`

### SOAP report endpoint (Clinical Conductor-style)

The backend includes a SOAP report endpoint that runs predefined SQL by `pkey` and returns tabular results.

- **WSDL**: `GET /soap/report?wsdl`
- **SOAP endpoint**: `POST /soap/report`
- **Authentication**: SOAP body includes `password`; backend validates against `SOAP_REPORT_PASSWORD`.
- **Execution model**: SOAP body includes `pkey`; backend looks up SQL in `ReportQueryDefinitions` table and executes it.
- **Safety**: only single `SELECT`/`WITH` queries are allowed.

#### SQL definition storage

The table `ReportQueryDefinitions` stores report query definitions:

- `PKey` (unique key passed by SOAP clients)
- `SqlQuery` (query text)

Seed examples are included in migration:

- `PATIENT_COUNT`
- `PATIENTS_BY_STATUS`
- `AUDIT_RECENT`
- `AUDIT_BY_STAFF`
- `AUDIT_BY_PATIENT`

#### Audit log tables for SOAP reporting

The SOAP report feature now includes audit-focused domain tables:

- `Staff` — staff directory used for audit actor references.
- `AuditEntryTypes` — lookup of audit event types (e.g. Login, Logout, Patient Updated).
- `AuditLogs` — audit entries with staff/patient/study references and timestamps.

Seeded `AuditEntryTypes` values:

- `PATIENT_UPDATED`
- `LOGIN`
- `LOGOUT`
- `PATIENT_VIEWED`
- `STAFF_PROFILE_UPDATED`
- `PATIENT_CREATED`
- `PATIENT_DELETED`

Typical audit report joins:

- `AuditLogs.AuditEntryTypeId -> AuditEntryTypes.Id`
- `AuditLogs.StaffPKey -> Staff.Id` (nullable)
- `AuditLogs.PatientPKey -> Patients.Id` (nullable)

Seeded audit report pkeys:

- `AUDIT_RECENT` — latest audit entries with staff/type details.
- `AUDIT_BY_STAFF` — audit entries grouped/sorted by staff.
- `AUDIT_BY_PATIENT` — audit entries where a patient reference exists.

#### SOAP request example

```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:rep="urn:mockhealthsystem:soap:report:v1">
  <soap:Body>
    <rep:RunReport>
      <rep:password>your-shared-password</rep:password>
      <rep:pkey>AUDIT_RECENT</rep:pkey>
    </rep:RunReport>
  </soap:Body>
</soap:Envelope>
```

#### SOAP response shape

`RunReportResponse` contains:

- `Columns` -> list of `Column` names from SQL result set
- `Rows` -> list of `Row`, each containing ordered `Value` entries

