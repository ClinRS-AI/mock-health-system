<!--
Sync Impact Report
==================
Version change: 1.0.0 → 1.1.0 (MINOR: 5 new principles added; III expanded)
Modified principles:
  - III. Integration-First Testing → expanded to include actual tooling (xUnit,
    IsolatedWebApplicationFactory, Vitest, RTL, MSW, auth-matrix coverage)
Added principles:
  - VI. Code Quality
  - VII. Testing Standards
  - VIII. User Experience Consistency
  - IX. Security
  - X. Performance
Removed principles: none
Added sections: none
Removed sections: none
Templates reviewed:
  - .specify/templates/plan-template.md ✅ (Constitution Check gate is dynamically filled per feature)
  - .specify/templates/spec-template.md ✅ (no constitution-specific references)
  - .specify/templates/tasks-template.md ✅ (no constitution-specific references)
  - .specify/templates/commands/           ⚠ directory not found — no command templates present
Deferred items: none
-->

# Mock Health System Constitution

## Core Principles

### I. Healthcare Domain Fidelity

All patient and clinical data stored or exposed by this system MUST be synthetic.
No real Protected Health Information (PHI) may be introduced at any point — not in
seed data, test fixtures, logs, or generated reports. Data generation MUST use
deterministic fakers (e.g., Bogus) to produce realistic but fictitious records.
Endpoints that return patient-like data MUST clearly surface their synthetic nature
in documentation.

**Rationale**: Even a mock system that handles realistic health data can create
compliance exposure if real PHI is accidentally introduced by contributors during
development or integration testing.

### II. Authenticated-by-Default

When `AUTH_SETTINGS_ADMIN_KEY` is configured, all admin routes (auth settings,
monitoring, session management) MUST require a valid HS256 JWT in `X-Admin-Session`.
The raw admin key MUST NOT be accepted on any endpoint other than the session mint
endpoint (`POST /api/v1/admin/sessions`). Auth modes (`None`, `Bearer`, `CCAPIKey`,
`OAuth`) are configurable at runtime but MUST be stored in the database and cached
— never hardcoded. Health-data API routes MUST respect the active auth mode at all
times.

**Rationale**: The admin key is a privileged secret. Accepting it directly on
multiple routes increases attack surface and complicates key rotation.

### III. Integration-First Testing

All backend tests MUST use `IsolatedWebApplicationFactory` (an `IClassFixture<>`
wrapper over `WebApplicationFactory`) with an in-memory EF Core database. Mocking
the database layer is prohibited. Tests MUST pass without a live PostgreSQL
instance. Any endpoint that is auth-gated MUST have coverage across all four auth
modes (`None`, `Bearer`, `CCAPIKey`, `OAuth`) — typically via an auth-matrix test
class using xUnit `[Fact]` per scenario.

Frontend integration tests MUST use MSW v2 (`http.get`, `http.post`,
`HttpResponse.json`) to intercept API calls — inline `jest.mock` or manual fetch
stubs are prohibited. Test helpers (`renderWithAdminSession`) MUST be used for
components that require session context.

**Rationale**: Mock/real divergence silently masks broken migrations and auth
regressions. The auth-matrix pattern ensures all four modes are exercised on every
protected route without combinatorial test duplication.

### IV. API Versioning & Stability

All public API routes MUST be prefixed with `/api/v1/` (or a later version prefix
for breaking changes). Route versioning MUST use `Asp.Versioning` with the
`[ApiVersion("1.0")]` attribute on controllers and `api/v{version:apiVersion}/`
route templates. Breaking changes to existing contracts MUST be introduced under a
new version prefix. EF Core migrations MUST be generated via CLI or
`run-ef.sh` and MUST NOT be hand-edited after generation.
`AppDbContextModelSnapshot.cs` MUST be kept in sync with the current model.

**Rationale**: Integration clients depend on stable, predictable contract surfaces.
Hand-edited migrations and unversioned breaking changes are the most common sources
of silent runtime failures in EF-backed APIs.

### V. Observability by Default

Every inbound API request MUST be recorded by `RequestLoggingMiddleware` into
`ApiRequestLog`. Exceptions MUST propagate to `ExceptionHandlingMiddleware` — they
MUST NOT be swallowed locally. The `/api/v1/monitoring/*` endpoints MUST remain
available to expose logs and aggregate stats. Structured logging via `ILogger<T>`
MUST be used throughout the backend; unstructured `Console.WriteLine` in production
paths is prohibited.

**Rationale**: A mock system used for integration testing is most valuable when
consumers can inspect exactly what the server received and returned.

### VI. Code Quality

**Backend**: All classes, services, and controllers MUST use PascalCase; local
variables and private fields MUST use camelCase. Every controller action MUST
declare `[ProducesResponseType]` attributes for all possible status codes. All
request and response payloads MUST use explicit DTOs — anonymous objects and
`dynamic` types are prohibited. All I/O MUST be `async`/`await`; synchronous
blocking calls (`Task.Result`, `.Wait()`) are prohibited. Specific exception types
MUST be thrown (not `Exception`) so `ExceptionHandlingMiddleware` can map them to
the correct HTTP status.

**Frontend**: TypeScript strict mode is mandatory — `any` is prohibited in
production and test code. All API calls MUST be implemented as named, typed
functions in `api.ts`; direct Axios usage inside components is prohibited.
Interfaces MUST be used for extensible object shapes; `type` aliases for unions and
derived types. No inline styles except for dynamic values (e.g., computed hex
colors); all other styling MUST use Tailwind utility classes.

**Rationale**: Explicit DTOs and ProducesResponseType keep Swagger accurate. Strict
TypeScript surfaces contract mismatches at compile time rather than runtime.

### VII. Testing Standards

**Backend**: xUnit is the required test framework. Tests MUST use `[Fact]`
attributes. Custom assertion helpers (e.g., `ApiErrorAssertions.AssertApiErrorAsync`)
MUST be used for structured error-response validation rather than inline
`Assert.Equal` on raw JSON. Test class names MUST end in `Tests` and follow the
`<Subject><Context>Tests` naming convention.

**Frontend**: Vitest + React Testing Library v16 + `user-event` v14 is the required
stack. Tests MUST assert on user-visible outcomes (`screen.getByText`,
`screen.findByText`, `waitFor`) — querying by implementation detail
(class names, internal state) is prohibited. `userEvent.setup()` MUST be used for
simulated interactions rather than `fireEvent`. Test files MUST be colocated with
their component (e.g., `MonitoringPage.test.tsx` next to `MonitoringPage.tsx`).

No `console.log` or `console.error` calls may be left in test files. Tests MUST be
deterministic; random seeds or real Date.now() usage in tests requires a mock.

**Rationale**: Behavioral assertions decouple tests from implementation and survive
refactors. Standardized tooling reduces setup friction and reviewer cognitive load.

### VIII. User Experience Consistency

Every component that performs API calls MUST manage discrete loading and error
states: a loading boolean per distinct data-fetching operation (e.g., separate
`loading` and `loadingDetailId` booleans, not a single global spinner). Error
messages MUST be user-friendly — raw HTTP status codes or internal stack traces
MUST NOT be surfaced in the UI. Error display MUST use the established Tailwind
pattern: `text-red-700 bg-red-50 rounded` container.

Detail data MUST be memoized by ID (e.g., `Record<string, Detail>`) to avoid
re-fetching on expand/collapse. Independent data fetches within a single view MUST
use `Promise.all()` to run in parallel rather than sequentially. Data visualization
MUST use Recharts with `ResponsiveContainer` to ensure correct sizing across
viewports.

**Rationale**: Granular loading states prevent full-page flicker on detail
expansion. Consistent error styling ensures users can distinguish errors from
content without learning a new pattern per page.

### IX. Security

CORS MUST be configured via `Cors:AllowedOrigins` in `appsettings.json` or the
corresponding environment variable. An explicit origin allowlist MUST be used in
non-Development environments — wildcard (`*`) is only acceptable locally. Admin
session JWTs MUST use HS256 with the signing key sourced from
`ADMIN_SESSION_SIGNING_KEY` (or derived from `AUTH_SETTINGS_ADMIN_KEY` if that env
var is absent). Session TTL MUST be configurable via `AdminSession:TtlMinutes`.

`RequestLoggingMiddleware` MUST NOT log request or response body content that
contains credential values. The `adminKey` field in session-mint payloads MUST be
redacted before any log write. Secrets (`AUTH_SETTINGS_ADMIN_KEY`,
`ADMIN_SESSION_SIGNING_KEY`, `POSTGRES_CONNECTION_STRING`, `SOAP_REPORT_PASSWORD`)
MUST be supplied via environment variables or `.env` files; committing them to the
repository is prohibited.

**Rationale**: Explicit CORS allowlists and JWT signing key separation reduce the
blast radius of any single credential being compromised. Logging credential values
is a common, hard-to-detect data leak.

### X. Performance

`RequestLoggingMiddleware` buffers request and response bodies up to a 4 KB cap;
this limit MUST NOT be raised without profiling the impact on concurrent request
throughput. All EF Core queries that traverse relationships MUST use `.Include()`
(or projection) to avoid N+1 patterns; raw SQL is permitted only when EF cannot
express the query efficiently, and MUST be documented with a comment explaining why.

Frontend components MUST use parallel `Promise.all()` for independent concurrent
fetches within a single render cycle. Detail records MUST be cached client-side by
ID to prevent redundant network requests on repeat opens. Synthetic patient data
generation endpoints MUST enforce configurable upper bounds on batch size to prevent
unbounded database growth.

**Rationale**: N+1 queries and unbounded data generation are the two most likely
performance failure modes in a mock system that integrates with real client test
suites running at volume.

## Healthcare Compliance & Data Safety

- **No real PHI**: Contributors MUST verify that any data added to seed files,
  test fixtures, or example payloads consists entirely of synthetic records.
- **SOAP endpoint**: The SOAP report endpoint requires `SOAP_REPORT_PASSWORD`;
  this credential MUST be supplied via environment variable — never committed to
  the repository.
- **Secrets management**: `AUTH_SETTINGS_ADMIN_KEY`, `ADMIN_SESSION_SIGNING_KEY`,
  and `POSTGRES_CONNECTION_STRING` MUST be supplied via `.env` files that are
  listed in `.gitignore`. Committed secrets invalidate the entire protection model.
- **Logging hygiene**: `RequestLoggingMiddleware` MUST NOT log request or response
  body content that could contain credential values (e.g., the `adminKey` field in
  session mint payloads).

## Development Workflow

- **Backend**: Run via `dotnet run --project MockHealthSystem.Api`; tests via
  `dotnet test`. EF migrations via `backend/scripts/run-ef.sh` or direct CLI with
  `--project MockHealthSystem.Infrastructure --startup-project MockHealthSystem.Api`.
  `backend/.env` MUST exist (copied from `.env.example`) before the first run.
- **Frontend**: Run via `npm run dev` from `frontend/`. Strict TypeScript — `any`
  is off-limits. Styling via Tailwind utility classes only; no inline styles except
  for dynamic values. All API calls MUST be centralized in `src/api.ts`.
- **Branching**: Feature work on named branches; PRs required for changes to
  `main`. CI MUST pass before merge.
- **Code review**: Reviewers MUST verify constitution compliance — PHI-free data,
  versioned routes, no swallowed exceptions, no committed secrets, auth-matrix
  test coverage for any new protected endpoint.

## Governance

This constitution supersedes all other documented practices when conflicts arise.
Amendments require:

1. A PR containing the updated `.specify/memory/constitution.md`.
2. `CONSTITUTION_VERSION` incremented per the semantic versioning rules below.
3. `LAST_AMENDED_DATE` updated to the amendment ratification date (ISO YYYY-MM-DD).
4. A Sync Impact Report (HTML comment at top of file) documenting all changes.

**Versioning policy**:
- MAJOR: Removal or redefinition of an existing principle.
- MINOR: New principle added or a section materially expanded.
- PATCH: Clarifications, wording improvements, or non-semantic refinements.

Compliance is verified during code review for every PR. Amendments take effect
immediately upon merge of the amending PR.

**Runtime guidance**: See `CLAUDE.md` for agent-specific development commands,
EF migration gotchas, authentication flow details, and project conventions.

**Version**: 1.1.0 | **Ratified**: 2026-05-18 | **Last Amended**: 2026-05-19
