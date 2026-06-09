# Implementation Plan: Clinical Conductor API Mock System

**Branch**: `feature/spec-kit-setup` | **Date**: 2026-05-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-cc-api-mock/spec.md`

> **Brownfield Note**: This is a retrospective plan for an existing, fully-implemented
> system. All phases document the as-built architecture. No functional gaps exist
> between the spec and the current implementation. Future tasks will cover
> enhancements, not initial construction.

## Summary

The CC API Mock System is a self-hosted .NET 10 Web API + React SPA that lets
developers test CC integrations without production access. It mirrors the Public CC
API patient data surface (REST + SOAP), supports all four CC authentication modes
switchable at runtime, generates synthetic patient and clinical data via a web-based
admin interface, and logs every API request for debugging. Admin features are
optionally protected by an admin key when deployed to shared environments.

The system is fully implemented across three .NET projects (Api, Infrastructure,
Tests) and a React/Vite frontend. No placeholder endpoints exist — all implemented
routes return structurally valid, synthetic responses.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend); TypeScript 5.4 / React 18 (frontend)

**Primary Dependencies**:
- Backend: ASP.NET Core, EF Core 10, Asp.Versioning 8.1, Bogus 35.6, System.IdentityModel.Tokens.Jwt 8.3, SoapCore (SOAP), Swashbuckle 6.6, DotNetEnv 3.1
- Frontend: Vite 5, Axios 1.7, Recharts 3.7, Tailwind CSS 3.4, Vitest 2.0, React Testing Library 16, MSW 2.4

**Storage**: PostgreSQL 16 via Npgsql EF Core 10; SQLite or InMemory for tests

**Testing**:
- Backend: xUnit with `IsolatedWebApplicationFactory` (integration) and direct unit tests; no external DB required
- Frontend: Vitest + React Testing Library + MSW (mock service worker)

**Target Platform**: Linux/Windows server (self-hosted); developer localhost

**Project Type**: Web service (REST API + SOAP) + Single-page Admin App

**Performance Goals**:
- Synthetic data generation: 100 patients with clinical records in < 30 seconds
- Request log appearance: < 2 seconds after request completes
- Auth mode changes: effective within 5 seconds, no restart required

**Constraints**:
- Request body logging capped at 4 KB per request to bound memory/storage impact
- Credential values (admin key, OAuth secrets) must never appear in request logs
- All patient data must be synthetic (Bogus faker); no real PHI permitted
- Admin key and DB credentials supplied only via environment variables / `.env`

**Scale/Scope**: Single-tenant; one team or developer per deployed instance

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Healthcare Domain Fidelity | ✅ Pass | Bogus faker generates all patient data; no seed files with real names/dates; `PatientFakerService` is the only data entry point |
| II. Authenticated-by-Default | ✅ Pass | Admin routes protected by `[RequiresAdminAuth]`; raw admin key accepted only at `POST /admin/sessions`; `MockAuthHandler` enforces the active auth mode on all patient routes |
| III. Integration-First Testing | ✅ Pass | `IsolatedWebApplicationFactory` used across all integration tests; in-memory EF for tests; auth-matrix test class (`PatientAndSystemEndpointAuthMatrixTests`) covers all 4 modes; MSW used in frontend |
| IV. API Versioning & Stability | ✅ Pass | `Asp.Versioning` with `[ApiVersion("1.0")]` on all controllers; 8 EF migrations generated via CLI; `AppDbContextModelSnapshot.cs` in sync |
| V. Observability by Default | ✅ Pass | `RequestLoggingMiddleware` logs every request; `ExceptionHandlingMiddleware` propagates all exceptions; `ILogger<T>` used in services; `/monitoring/*` endpoints expose logs |
| VI. Code Quality | ✅ Pass | PascalCase throughout; explicit DTOs for all request/response; `[ProducesResponseType]` on controllers; TypeScript strict mode; all API calls in `api.ts` |
| VII. Testing Standards | ✅ Pass | xUnit `[Fact]`; `ApiErrorAssertions` helpers; RTL behavioral assertions (`screen.getByText`, `waitFor`); `userEvent.setup()` pattern in frontend tests |
| VIII. User Experience Consistency | ✅ Pass | Granular loading booleans in MonitoringPage; `text-red-700 bg-red-50` error pattern; `Promise.all()` for parallel fetches; Recharts with `ResponsiveContainer` |
| IX. Security | ✅ Pass | CORS configured via `appsettings.json` (no wildcard default); credential redaction in request logs (bodies capped, credentials excluded); secrets via env vars |
| X. Performance | ✅ Pass | 4 KB request log body cap; EF `.Include()` used for navigation properties; client-side detail caching in MonitoringPage; bounded batch sizes on patient generation |

**Gate result**: ✅ All 10 principles satisfied. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/001-cc-api-mock/
├── plan.md              # This file
├── research.md          # Phase 0: as-built technology decisions
├── data-model.md        # Phase 1: full entity model
├── quickstart.md        # Phase 1: developer setup guide
├── checklists/
│   └── requirements.md  # Spec quality checklist (all items passing)
├── contracts/
│   ├── health-api.md    # Phase 1: patient, system, auth, health endpoints
│   ├── admin-api.md     # Phase 1: auth-settings, monitoring, test-data endpoints
│   └── soap-api.md      # Phase 1: SOAP report endpoint contract
└── tasks.md             # Phase 2 output (/speckit-tasks — not yet generated)
```

### Source Code (repository root)

```text
backend/
├── MockHealthSystem.sln
├── MockHealthSystem.Api/
│   ├── Controllers/           # 9 controllers (patients, auth, health, system,
│   │                          #   admin-sessions, auth-settings, test-data,
│   │                          #   monitoring, soap-report)
│   ├── Authentication/
│   │   └── MockAuthHandler.cs # Runtime-switchable auth (None/Bearer/CCAPIKey/OAuth)
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Services/
│   │   ├── AuthSettingsService.cs     # Auth mode cache
│   │   ├── PatientFakerService.cs     # Bogus-based synthetic data
│   │   ├── PatientMappingService.cs   # Entity → ViewModel
│   │   ├── ReportExecutionService.cs  # SOAP report SQL execution
│   │   └── AdminSession/              # JWT mint/validate + request validation
│   ├── Soap/                          # SOAP contracts and service
│   ├── Swagger/                       # OpenAPI filters and security schemes
│   ├── Filters/                       # ModelValidationActionFilter
│   ├── Models/                        # 60+ DTOs (request/response view models)
│   └── Program.cs                     # Host, DI, middleware pipeline
│
├── MockHealthSystem.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs            # EF Core context (40+ DbSets)
│   │   └── Entities/                  # All entity classes
│   ├── Migrations/                    # 8 EF migrations (CLI-generated)
│   └── DesignTimeDbContextFactory.cs
│
└── MockHealthSystem.Tests/
    ├── Integration/                   # 16+ test classes via IsolatedWebApplicationFactory
    └── Unit/                          # 14+ test classes for services and middleware

frontend/
├── src/
│   ├── api.ts                         # All Axios calls (typed, centralized)
│   ├── App.tsx                        # Tab router
│   ├── AdminSessionContext.tsx        # Session state provider
│   ├── AdminSessionBanner.tsx         # Session status display
│   ├── adminSessionStore.ts           # Token persistence (sessionStorage)
│   ├── AdminAccessPage.tsx            # Admin key → session JWT exchange
│   ├── AuthSettingsPage.tsx           # Auth mode configuration UI
│   ├── MonitoringPage.tsx             # Request log viewer + stats charts
│   ├── TestDataPage.tsx               # Patient generation and management UI
│   └── test/
│       ├── setup.ts                   # MSW server setup
│       ├── server.ts                  # MSW handlers
│       └── renderWithAdminSession.tsx # Test render helper
├── package.json
└── vite.config.ts
```

**Structure Decision**: Web application layout (Option 2). Backend is a 3-project .NET
solution; frontend is a standalone Vite/React SPA. They communicate exclusively
via the REST API — no shared code or build artifacts.

## Complexity Tracking

No constitution violations — this section is intentionally empty.
