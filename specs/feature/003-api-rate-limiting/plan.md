# Implementation Plan: Configurable API Rate Limiting

**Branch**: `feature/003-api-rate-limiting` | **Date**: 2026-06-08 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/feature/003-api-rate-limiting/spec.md`

## Summary

Add configurable per-IP rate limiting to the health data API endpoints. Rate limit settings (enabled flag, per-second limit, per-minute limit) are persisted as new columns on the existing `AuthSettings` entity and managed from the Authentication Settings page. A custom `RateLimitingMiddleware` enforces fixed-window (tumbling) per-IP counters in-memory and returns 429 with a `Retry-After` header when either limit is exceeded. Admin endpoints are exempt from the configurable limit and protected by a separate hard-coded generous limit instead.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript / React 18 + Vite 6 (frontend)

**Primary Dependencies**:
- Backend: ASP.NET Core 10, EF Core 10 + Npgsql, `Microsoft.Extensions.Caching.Memory`, xUnit
- Frontend: React 18, Vitest 4, React Testing Library 16, MSW 2, Axios, Tailwind CSS

**Storage**: PostgreSQL (production), in-memory EF Core (tests). Rate limit counters are in-memory only (ConcurrentDictionary singleton); not persisted.

**Testing**: xUnit + `IsolatedWebApplicationFactory` (backend), Vitest + RTL + MSW (frontend)

**Target Platform**: Linux server (Cloud Run) and local developer workstation

**Project Type**: Web service (REST API) + Single-page admin application

**Performance Goals**: Rate limit check overhead must be sub-millisecond (dictionary lookup + per-IP lock + counter compare/increment); 429 response must be returned within the same request cycle.

**Constraints**: Zero configuration changes for existing deployments — rate limiting defaults to disabled. No PostgreSQL required for tests. No server restart required when settings change.

**Scale/Scope**: Single-tenant dev mock; low concurrent request volume. Counter store does not need sharding.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Healthcare Domain Fidelity | ✅ Pass | No PHI involved; feature touches only configuration and middleware |
| II. Authenticated-by-Default | ✅ Pass | Admin routes remain protected by `IAdminRequestValidator`; rate limiting adds a layer but does not bypass auth |
| III. Integration-First Testing | ✅ Pass | New middleware and counter store have unit tests; rate limit enforcement has integration tests using `IsolatedWebApplicationFactory`. Rate limiting is off by default (settings row absent → `RateLimitEnabled = false`), so existing tests are unaffected |
| IV. API Versioning & Stability | ✅ Pass | No new routes; changes to `GET/PUT /api/v1/auth-settings` are additive (new fields). One EF migration generated via CLI |
| V. Observability by Default | ✅ Pass | 429 responses are logged by `RequestLoggingMiddleware` (rate limiting middleware is positioned after logging middleware in the pipeline) |
| VI. Code Quality | ✅ Pass | Explicit DTOs for new fields; `[ProducesResponseType(429)]` added to all affected API controllers; async throughout; no anonymous objects |
| VII. Testing Standards | ✅ Pass | xUnit `[Fact]`, `ApiErrorAssertions` for 429 body; frontend tests via MSW and RTL |
| VIII. User Experience Consistency | ✅ Pass | Loading/saving states on the new rate limit form section; error messages follow existing Tailwind pattern |
| IX. Security | ✅ Pass | No secrets exposed; `Retry-After` header contains only a duration in seconds |
| X. Performance | ✅ Pass | ConcurrentDictionary + per-IP lock is O(1) per request; no N+1 queries introduced |
| Documentation sync | ⚠️ Required | `README.md` must be updated with rate limiting configuration reference; `API-CONNECT.md` must note 429 as a possible response |

*Post-design re-check*: All gates pass. Complexity Tracking section is empty (no violations to justify).

## Project Structure

### Documentation (this feature)

```text
specs/feature/003-api-rate-limiting/
├── plan.md               ← this file
├── research.md           ← Phase 0: technical decisions
├── data-model.md         ← Phase 1: entity, DTO, frontend type changes
├── contracts/
│   └── auth-settings-api.md  ← Phase 1: API contract changes
└── tasks.md              ← Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (affected paths)

```text
backend/
├── MockHealthSystem.Infrastructure/
│   ├── Data/Entities/
│   │   └── AuthSettings.cs                       ← add 3 new properties
│   └── Migrations/
│       └── YYYYMMDDHHMMSS_AddRateLimitColumnsToAuthSettings.cs  ← generated
│
└── MockHealthSystem.Api/
    ├── Middleware/
    │   └── RateLimitingMiddleware.cs              ← new: per-IP counter check + 429
    ├── RateLimiting/
    │   ├── IRateLimitCounterStore.cs              ← new: interface
    │   ├── RateLimitCounterStore.cs               ← new: singleton ConcurrentDictionary
    │   └── PerIpCounters.cs                       ← new: counter state per IP
    ├── Models/Auth/
    │   └── AuthSettingsModels.cs                  ← add 3 fields to both DTOs
    ├── Controllers/
    │   └── AuthSettingsController.cs              ← add rate limit field mapping + validation + counter reset
    ├── Controllers/
    │   ├── PatientsController.cs                  ← add [ProducesResponseType(429)]
    │   ├── HealthController.cs                    ← add [ProducesResponseType(429)]
    │   ├── AuthController.cs                      ← add [ProducesResponseType(429)]
    │   ├── SystemController.cs                    ← add [ProducesResponseType(429)]
    │   └── ReportSoapController.cs                ← add [ProducesResponseType(429)] (SOAP; best-effort)
    └── Program.cs                                 ← register RateLimitCounterStore + add UseMiddleware<RateLimitingMiddleware>

backend/MockHealthSystem.Tests/
├── Integration/
│   ├── RateLimitEndpointTests.cs                 ← new: 429 enforcement, Retry-After, admin exemption
│   └── AuthSettingsEndpointTests.cs              ← update: round-trip new rate limit fields
└── Unit/
    └── RateLimitCounterStoreTests.cs             ← new: counter logic, window reset, ResetAll

frontend/src/
├── api.ts                                        ← extend AuthSettings + UpdateAuthSettingsRequest interfaces
├── AuthSettingsPage.tsx                          ← add Rate Limiting section to form
├── AuthSettingsPage.test.tsx                     ← add tests for new section
└── demoData.ts                                   ← add rateLimitEnabled/PerSecond/PerMinute to DEMO_AUTH_SETTINGS
```

## Implementation Sequence

### Step 1 — Backend entity + migration

1. Add `RateLimitEnabled`, `RateLimitPerSecond`, `RateLimitPerMinute` to `AuthSettings.cs`
2. Run `backend/scripts/run-ef.sh migrations add AddRateLimitColumnsToAuthSettings`
3. Verify generated migration sets correct defaults (`false`, `10`, `300`)

### Step 2 — Counter store

1. Create `backend/MockHealthSystem.Api/RateLimiting/PerIpCounters.cs`
2. Create `backend/MockHealthSystem.Api/RateLimiting/IRateLimitCounterStore.cs`
3. Create `backend/MockHealthSystem.Api/RateLimiting/RateLimitCounterStore.cs`
   - `ConcurrentDictionary<string, PerIpCounters>` keyed by IP string
   - `CheckAndIncrement(string ip, int perSecond, int perMinute)` → returns `(bool Allowed, int RetryAfterSeconds)`
   - `ResetAll()` → clears the dictionary
   - Window logic: compare `DateTime.UtcNow.Ticks` to stored window start; reset counter if window has elapsed
   - `RetryAfterSeconds` = `max(secondsUntilSecondReset, secondsUntilMinuteReset)` when not allowed

### Step 3 — Middleware

1. Create `backend/MockHealthSystem.Api/Middleware/RateLimitingMiddleware.cs`
   - Resolve `IAuthSettingsService` and `IRateLimitCounterStore` from DI
   - Classify path: admin paths = `/api/v1/auth-settings`, `/api/v1/monitoring`, `/api/v1/test-data`, `/api/v1/admin`
   - Admin paths → check built-in limits (120/sec, 5000/min); return 429 with `ApiErrorResponse` if exceeded
   - API paths → if `!settings.RateLimitEnabled`, call `next()`; else `CheckAndIncrement` and return 429 or `next()`
   - 429 response: set `Retry-After` header, write `ApiErrorResponse` JSON body with camelCase serialization

2. Register in `Program.cs`:
   - `builder.Services.AddSingleton<IRateLimitCounterStore, RateLimitCounterStore>()`
   - `app.UseMiddleware<RateLimitingMiddleware>()` after `RequestLoggingMiddleware`, before `UseAuthorization()`

### Step 4 — Auth Settings endpoint updates

1. Update `AuthSettingsModels.cs`: add new fields to `AuthSettingsViewModel` and `AuthSettingsUpdateModel`
2. Update `AuthSettingsController.GetAsync()`: populate new rate limit fields from `settings`
3. Update `AuthSettingsController.UpdateAsync()`:
   - Map `model.RateLimitEnabled`, `model.RateLimitPerSecond`, `model.RateLimitPerMinute` to `existing`
   - Validate: if rate limiting would be enabled after save, per-second and per-minute must each be ≥ 1
   - After `SaveChangesAsync()` and `InvalidateCacheAsync()`, call `_rateLimitCounterStore.ResetAll()`
   - Inject `IRateLimitCounterStore` into the controller
4. Add `[ProducesResponseType(429)]` to all action methods in `PatientsController`, `HealthController`, `AuthController`, `SystemController`

### Step 5 — Frontend

1. Extend `AuthSettings` and `UpdateAuthSettingsRequest` interfaces in `api.ts`
2. Add `rateLimitEnabled`, `rateLimitPerSecond`, `rateLimitPerMinute` to `FormState` in `AuthSettingsPage.tsx`
3. Update `applySettingsToForm()` and `handleSubmit()` to include new fields
4. Add a "Rate Limiting" section to the form (after OAuth section, before the save button row):
   - Toggle checkbox/switch for enabled/disabled
   - Number inputs for per-second (min=1) and per-minute (min=1), conditionally visible when enabled
   - Helper text explaining the limits are applied per-IP
   - Inputs disabled in demo mode (consistent with rest of form)
5. Update `DEMO_AUTH_SETTINGS` in `demoData.ts`: add `rateLimitEnabled: true, rateLimitPerSecond: 10, rateLimitPerMinute: 300`

### Step 6 — Tests

**Backend unit tests** — `RateLimitCounterStoreTests.cs`:
- Counter starts at zero for new IP
- Request within per-second limit is allowed
- Request at per-second limit boundary is rejected
- Counter resets when second window elapses
- Request within per-minute limit is allowed
- Request at per-minute limit boundary is rejected
- Counter resets when minute window elapses
- `ResetAll()` clears all counters
- `RetryAfterSeconds` = longer window when both limits exceeded
- `RetryAfterSeconds` = per-second reset when only per-second exceeded
- `RetryAfterSeconds` = per-minute reset when only per-minute exceeded

**Backend integration tests** — `RateLimitEndpointTests.cs`:
- 429 returned on API endpoint when per-second limit exceeded (seed DB with `RateLimitEnabled=true`, `RateLimitPerSecond=1`)
- `Retry-After` header present on 429 response
- 429 body matches `ApiErrorResponse` shape
- Admin endpoint not rate-limited when API per-second limit is exceeded
- Rate limiting disabled: 0 429s regardless of request count
- Saving new settings resets counters (client that was at limit can request again after save)

**Frontend tests** — update `AuthSettingsPage.test.tsx`:
- Rate Limiting section renders with correct initial values from loaded settings
- Toggle enables/disables the per-second and per-minute inputs
- Saving with rate limiting enabled includes rate limit fields in the PUT payload
- Demo mode: rate limit inputs are not interactive
- Validation: per-second input of 0 is not sent (HTML `min=1` prevents it at the UI level)

### Step 7 — Documentation

1. Update `README.md`: add rate limiting to the Configuration Reference section
2. Update `API-CONNECT.md` (if it exists): note 429 as a possible response on API data endpoints

## Complexity Tracking

*(Empty — no constitution violations to justify)*
