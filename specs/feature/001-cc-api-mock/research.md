# Research: Clinical Conductor API Mock System

**Phase 0 — Brownfield As-Built Technology Decisions**

> All decisions below document choices already made and implemented in the existing
> codebase. This is a record of what exists and why, not a forward-looking proposal.

---

## Decision 1: .NET 10 Web API as Backend Platform

**Decision**: ASP.NET Core on .NET 10 with a 3-project solution structure
(Api / Infrastructure / Tests).

**Rationale**: .NET 10 is the current LTS-track release providing long-term support,
strong EF Core integration, and native OpenAPI/Swagger tooling. The 3-project split
keeps domain logic (entities, migrations) isolated from the HTTP layer (controllers,
middleware), enabling tests to exercise the infrastructure without a running HTTP
server.

**Alternatives considered**:
- Node/Express: Rejected — the team's primary stack is .NET; type-safe EF Core
  entities align better with a strongly-typed CC API contract.
- Single-project .NET: Rejected — mixing EF migrations and HTTP concerns in one
  project complicates `dotnet ef` CLI usage and test isolation.

---

## Decision 2: Runtime-Switchable Authentication via MockAuthHandler

**Decision**: A single custom `AuthenticationHandler<AuthenticationSchemeOptions>`
(`MockAuthHandler`) reads the active `AuthSettings` from the database on each
request and applies the corresponding validation logic (None / Bearer / CCAPIKey /
OAuth).

**Rationale**: Integration developers need to switch auth modes frequently without
restarting the server. Database-backed settings with an in-memory cache
(`IAuthSettingsService`) allow instant mode switching via the admin API while
keeping per-request overhead minimal (cache hit after first read).

**Alternatives considered**:
- Separate middleware per auth mode: Rejected — would require restart to change
  modes; middleware registration is a startup-time concern.
- Static environment-variable auth: Rejected — too inflexible for a tool whose
  primary purpose is auth mode simulation.

---

## Decision 3: EF Core with PostgreSQL (Production) / InMemory+SQLite (Tests)

**Decision**: `AppDbContext` uses PostgreSQL via Npgsql in production and
Development; tests use EF Core InMemory or SQLite depending on the test host
configuration. Provider selection is controlled by `ASPNETCORE_ENVIRONMENT` and
`Testing:UseSqlite` config flag.

**Rationale**: PostgreSQL is the production-grade choice for data durability and
referential integrity. InMemory/SQLite for tests avoids requiring a running
Postgres instance in CI, while `IsolatedWebApplicationFactory` ensures each test
class gets its own context (no shared state leakage between test runs).

**Alternatives considered**:
- SQLite for all environments: Rejected — PostgreSQL-specific behavior (JSONB,
  schema, migration constraints) would not be exercised.
- Testcontainers (real Postgres in CI): Considered for the future; current
  InMemory approach is sufficient given the constraint that tests must pass
  without external dependencies.

---

## Decision 4: Bogus Faker for Synthetic Patient Data

**Decision**: The `PatientFakerService` uses the Bogus library (v35.6) to generate
realistic but entirely fictitious patient demographics, with configurable duplicate
percentage to simulate near-duplicate matching scenarios.

**Rationale**: Bogus provides deterministic seeded generation (reproducible test
data), locale-aware names and addresses, and extensive faker categories covering
all required demographic fields. The `duplicatePercentage` parameter allows
developers to test deduplication logic in their integration clients.

**Alternatives considered**:
- Hard-coded seed data: Rejected — limited variety; cannot test edge cases like
  long names, international characters, or address formats.
- AutoFixture: Rejected — less control over realistic value constraints (e.g.,
  valid DOB ranges, gender-appropriate names).

---

## Decision 5: Admin Session JWT Rather Than Per-Request Admin Key

**Decision**: The raw `AUTH_SETTINGS_ADMIN_KEY` is only accepted at
`POST /api/v1/admin/sessions`. All other admin routes require a short-lived HS256
JWT (`X-Admin-Session` header) minted by that endpoint.

**Rationale**: Sending the raw admin key on every request increases the window for
interception and complicates key rotation (any logged request would expose it).
Short-lived JWTs (default 30 minutes, configurable) limit exposure without
requiring the user to re-enter the key frequently during a work session.

**Alternatives considered**:
- Basic Auth on every request: Rejected — exposes credential on every call;
  harder to rotate without disrupting all active sessions.
- Cookie-based session: Rejected — adds CSRF surface; JWT in a custom header
  is simpler for API clients and avoids cookie SameSite complexities.

---

## Decision 6: Request Logging with 4 KB Body Cap

**Decision**: `RequestLoggingMiddleware` buffers and stores request and response
bodies, capped at 4,096 bytes. Requests originating from the frontend (detected
via `Origin`/`Referer` matching `FRONTEND_URL`) are excluded from logging to
avoid flooding the log with admin UI traffic.

**Rationale**: Full body logging is essential for debugging integration issues, but
unbounded body capture would create memory pressure and excessive database growth.
4 KB covers the vast majority of clinical API payloads. Frontend traffic exclusion
keeps the monitoring view focused on integration client requests.

**Alternatives considered**:
- No body logging: Rejected — developers need to see exactly what was sent to
  debug serialization and schema issues.
- Per-endpoint body size limits: Rejected — adds complexity; flat 4 KB cap is
  predictable and sufficient.

---

## Decision 7: React + Vite + Tailwind SPA for Admin Interface

**Decision**: Single-page application built with React 18, Vite 5, TypeScript, and
Tailwind CSS. Session state managed via `AdminSessionContext` (React context) with
token persistence in `sessionStorage`.

**Rationale**: The admin interface has minimal routing needs (tabbed layout, no
deep linking required) making a full SPA appropriate. Vite provides fast HMR for
development. Tailwind's utility-first approach enforces visual consistency without
a component library dependency. `sessionStorage` (not `localStorage`) means admin
sessions expire when the browser tab is closed, which is appropriate security
behavior for a shared server scenario.

**Alternatives considered**:
- Server-rendered (Razor Pages): Rejected — the team's frontend is React; a React
  SPA allows independent deployment and faster iteration.
- localStorage for session: Rejected — persists across browser restarts; session
  expiry on tab close is more appropriate for an admin key equivalent.

---

## Decision 8: SOAP Report Endpoint

**Decision**: A SOAP endpoint (`/soap/report`) accepts XML envelopes, validates a
password credential, looks up a named `ReportQueryDefinition` by PKey, executes
the SQL via `ReportExecutionService`, and returns an XML response. WSDL is served
at `/soap/report?wsdl`.

**Rationale**: Some CC consumers use a SOAP report interface that executes named
SQL reports. The mock must support this surface so developers building SOAP
report integrations can test their XML serialization, credential handling, and
response parsing without a real CC server.

**Alternatives considered**:
- REST-only (drop SOAP): Rejected — SOAP report consumers are a named use case;
  dropping SOAP would leave them without a mock target.

---

## Gap Analysis (Spec vs Implementation)

| Spec Requirement | Implementation Status | Notes |
|------------------|-----------------------|-------|
| FR-001: 4 auth modes | ✅ Complete | None/Bearer/CCAPIKey/OAuth all implemented in MockAuthHandler |
| FR-002: CC API endpoints (patient portal + notification) | ✅ Complete | Full patient CRUD + 10 sub-resource collections; OData list + search |
| FR-003: SOAP report endpoint | ✅ Complete | Password-auth, named report execution, WSDL served |
| FR-004: Web-based admin interface | ✅ Complete | React SPA with Auth, Monitoring, TestData tabs |
| FR-005: Synthetic patient generation | ✅ Complete | Configurable count, duplicate %, seed; Bogus-based |
| FR-006: Patient lookup | ✅ Complete | By ID, UID, email, name fragment, random |
| FR-007: Data reset | ✅ Complete | `POST /test-data/patients/reset` truncates all patient tables |
| FR-008: Request logging | ✅ Complete | RequestLoggingMiddleware; 4KB cap; credential exclusion |
| FR-009: Monitoring with filtering and stats | ✅ Complete | Filter by path/status/time; aggregated stats endpoint |
| FR-010: Admin key → session JWT | ✅ Complete | POST /admin/sessions mints JWT; all other admin routes require it |
| FR-011: Open admin routes when no key set | ✅ Complete | AdminRequestValidator bypasses check when AUTH_SETTINGS_ADMIN_KEY unset |
| FR-012: Environment-only configuration | ✅ Complete | All config via env vars / .env file / appsettings.json |
| FR-013: Common web security protections | ✅ Complete | CORS allowlist, credential redaction, no wildcard default |
| FR-014: Health check endpoint | ✅ Complete | GET /api/v1/health (anonymous) |

**Result**: No functional gaps. All spec requirements are fully implemented.
