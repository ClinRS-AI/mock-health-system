# Quickstart: Clinical Conductor API Mock System

Get the mock running and making your first authenticated CC API call in under
10 minutes.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0+ | [Download](https://dotnet.microsoft.com/download) |
| Node.js | LTS (20+) | npm included |
| PostgreSQL | 14+ | Local install or Docker |

---

## 1. Clone and configure

```bash
git clone <repo-url>
cd mock-health-system
```

**Backend config** (`backend/.env` — copy from `backend/.env.example`):
```env
POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=mockhealthsystem_db;Username=mockhealthsystem_user;Password=yourpassword
BACKEND_URL=http://localhost:5001
FRONTEND_URL=http://localhost:5176

# Optional — set to require admin key for admin routes:
# AUTH_SETTINGS_ADMIN_KEY=your-admin-key-here

# Required for SOAP report endpoint:
SOAP_REPORT_PASSWORD=your-soap-password
```

**Frontend config** (`frontend/.env` — copy from `frontend/.env.example`):
```env
VITE_API_BASE_URL=http://localhost:5001
```

---

## 2. Initialize the database

```bash
# Create the database and role (run once per environment)
backend/scripts/init-db.sh

# Apply EF migrations
dotnet ef database update \
  --project backend/MockHealthSystem.Infrastructure \
  --startup-project backend/MockHealthSystem.Api
```

> **Proxy issue?** Use the helper script instead:
> `backend/scripts/run-ef.sh database update`

---

## 3. Start the backend

```bash
cd backend
dotnet restore
dotnet run --project MockHealthSystem.Api
# → Listening on http://localhost:5001
```

Verify it's running:
```bash
curl http://localhost:5001/api/v1/health
# → Mock Health System API is running.
```

---

## 4. Start the frontend

```bash
cd frontend
npm install
npm run dev
# → http://localhost:5176
```

Open `http://localhost:5176` in your browser. You should see the admin interface
with tabs: **Auth settings**, **Monitoring**, and **Test data**.

---

## 5. Configure authentication

Open the **Auth settings** tab and select the mode your integration client uses:

| Mode | What to set | Client credential |
|------|-------------|------------------|
| **CCAPIKey** (CC default) | Set the CCAPIKey value | `CCAPIKey: <value>` header |
| **None** | No credentials needed | — |
| **Bearer** | Set the bearer token | `Authorization: Bearer <token>` |
| **OAuth** | Set clientId + clientSecret | POST to `/auth/token` first |

---

## 6. Generate synthetic patients

Open the **Test data** tab and click **Generate patients**. Start with 25–50
patients. The generation is immediate and the patients are queryable via the API
within seconds.

---

## 7. Make your first CC API call

With CCAPIKey mode configured (key: `my-test-key`):

```bash
# List patients (OData paged)
curl http://localhost:5001/api/v1/patients/odata \
  -H "CCAPIKey: my-test-key"

# Get a specific patient
curl http://localhost:5001/api/v1/patients/1 \
  -H "CCAPIKey: my-test-key"

# Search patients
curl -X POST http://localhost:5001/api/v1/patients/search \
  -H "CCAPIKey: my-test-key" \
  -H "Content-Type: application/json" \
  -d '{"name": "Smith"}'

# Get patient conditions
curl http://localhost:5001/api/v1/patients/1/conditions \
  -H "CCAPIKey: my-test-key"
```

---

## 8. Debug with monitoring

After making API calls, open the **Monitoring** tab in the admin interface. Every
call appears with method, path, status, and response time. Click any entry to
expand the full request and response bodies.

---

## Running tests

```bash
# Backend (no Postgres required)
cd backend
dotnet test

# Frontend
cd frontend
npm run test
```

---

## Admin key setup (shared/public deployments)

When deploying to a server shared with your team, set `AUTH_SETTINGS_ADMIN_KEY`
in the environment. The admin interface will prompt for this key once per browser
session. The raw key is never sent beyond the initial session mint — all subsequent
admin calls use a short-lived JWT (default 30 minutes).

---

## SOAP report endpoint

```bash
# List available report PKeys
curl http://localhost:5001/api/v1/test-data/soap/report-pkeys \
  -H "X-Admin-Session: <your-admin-jwt>"

# Call the SOAP endpoint
curl -X POST http://localhost:5001/soap/report \
  -H "Content-Type: text/xml; charset=utf-8" \
  -H "SOAPAction: RunReport" \
  -d '<?xml version="1.0"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
               xmlns:rep="http://clinrs.com/report">
  <soap:Body>
    <rep:RunReport>
      <rep:Password>your-soap-password</rep:Password>
      <rep:PKey>PatientReport</rep:PKey>
    </rep:RunReport>
  </soap:Body>
</soap:Envelope>'
```

---

## Common issues

| Problem | Cause | Fix |
|---------|-------|-----|
| `dotnet ef` returns 403 | Proxy env var set | Use `backend/scripts/run-ef.sh` |
| "Already up to date" but schema missing | Migration recorded but not applied | Run `backend/scripts/run-undo-migration.sh`, then `database update` |
| Admin routes return 403 | `AUTH_SETTINGS_ADMIN_KEY` is set | Sign in via Admin access tab first |
| SOAP returns fault "Invalid password" | `SOAP_REPORT_PASSWORD` mismatch | Check `backend/.env` value matches your request |
| Patients return 401 | Auth mode is not None | Configure auth mode or add correct credential header |
