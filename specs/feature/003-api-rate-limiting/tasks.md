# Tasks: Configurable API Rate Limiting

**Input**: Design documents from `specs/feature/003-api-rate-limiting/`

**Prerequisites**: [plan.md](plan.md) (required), [spec.md](spec.md) (user stories), [research.md](research.md) (decisions), [data-model.md](data-model.md) (entities + DTOs), [contracts/auth-settings-api.md](contracts/auth-settings-api.md) (API changes)

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel with other [P] tasks (different files, no in-flight dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4 maps to spec.md user stories)
- Exact file paths in every description

---

## Phase 1: Setup

**Purpose**: Extend the persistent entity before any code that reads from it can be written

- [x] T001 Add `RateLimitEnabled` (bool, default false), `RateLimitPerSecond` (int, default 10), and `RateLimitPerMinute` (int, default 300) properties to `AuthSettings` class in `backend/MockHealthSystem.Infrastructure/Data/Entities/AuthSettings.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: In-memory counter store and middleware wired into the pipeline — all user story enforcement depends on this

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T002 Generate EF migration `AddRateLimitColumnsToAuthSettings` by running `backend/scripts/run-ef.sh migrations add AddRateLimitColumnsToAuthSettings` from repo root; verify generated migration adds 3 columns with correct defaults and updates `AppDbContextModelSnapshot.cs`
- [x] T003 [P] Create `PerIpCounters.cs` in `backend/MockHealthSystem.Api/RateLimiting/PerIpCounters.cs` — plain reference type with four fields: `SecondWindowStartTick` (long), `SecondCount` (int), `MinuteWindowStartTick` (long), `MinuteCount` (int)
- [x] T004 [P] Create `IRateLimitCounterStore.cs` in `backend/MockHealthSystem.Api/RateLimiting/IRateLimitCounterStore.cs` — interface with two methods: `(bool Allowed, int RetryAfterSeconds) CheckAndIncrement(string ip, int perSecond, int perMinute)` and `void ResetAll()`
- [x] T005 Implement `RateLimitCounterStore.cs` in `backend/MockHealthSystem.Api/RateLimiting/RateLimitCounterStore.cs` — `ConcurrentDictionary<string, PerIpCounters>`, per-IP `lock(counter)`, fixed tumbling windows via `DateTime.UtcNow.Ticks`, `RetryAfterSeconds = max(secondsUntilSecondReset, secondsUntilMinuteReset)` when not allowed (depends on T003, T004)
- [x] T006 [P] Create `backend/MockHealthSystem.Tests/Unit/RateLimitCounterStoreTests.cs` with unit tests covering: new-IP counter starts at zero; within-per-second limit is allowed; at-per-second limit boundary is rejected; second-window resets after 1 second elapses; within-per-minute limit is allowed; at-per-minute boundary is rejected; `ResetAll()` clears all counters; `RetryAfterSeconds` = longer window when both limits exhausted; = per-second reset when only per-second exceeded; = per-minute reset when only per-minute exceeded (depends on T005)
- [x] T007 Create `RateLimitingMiddleware.cs` in `backend/MockHealthSystem.Api/Middleware/RateLimitingMiddleware.cs` — resolves `IAuthSettingsService` and `IRateLimitCounterStore` from DI; classifies paths (admin = `/api/v1/auth-settings`, `/api/v1/monitoring`, `/api/v1/test-data`, `/api/v1/admin`); admin paths use fixed built-in limits (120/sec, 5000/min); API paths skip when `!settings.RateLimitEnabled`; calls `CheckAndIncrement` with configured limits; on 429: sets `Retry-After` header and writes camelCase `ApiErrorResponse` JSON body (`status: 429`, `title: "Too Many Requests"`, `detail: "Rate limit exceeded. Retry after {N} seconds."`) (depends on T005)
- [x] T008 Register `IRateLimitCounterStore` as singleton and insert `app.UseMiddleware<RateLimitingMiddleware>()` after `app.UseMiddleware<RequestLoggingMiddleware>()` and before `app.UseAuthorization()` in `backend/MockHealthSystem.Api/Program.cs` (depends on T005, T007)

**Checkpoint**: Counter store and middleware are wired — rate limiting enforcement is active when `RateLimitEnabled = true` in DB

---

## Phase 3: User Story 1 — Configure and Enforce API Rate Limits (Priority: P1) 🎯 MVP

**Goal**: Admin saves per-second and per-minute limits; API clients receive 429 once either threshold is crossed; settings survive server restart

**Independent Test**: Enable rate limiting via PUT /api/v1/auth-settings with `rateLimitPerSecond: 1`, call GET /api/v1/patients twice within one second from the same IP, confirm the second call returns 429 with a `Retry-After` header

- [x] T009 [P] [US1] Add `RateLimitEnabled` (bool), `RateLimitPerSecond` (int), `RateLimitPerMinute` (int) properties to both `AuthSettingsViewModel` and `AuthSettingsUpdateModel` in `backend/MockHealthSystem.Api/Models/Auth/AuthSettingsModels.cs`
- [x] T010 [US1] Update `AuthSettingsController.GetAsync()` to populate the three new fields from the loaded `AuthSettings` entity; update `UpdateAsync()` to map `model.RateLimitEnabled/PerSecond/PerMinute` onto `existing`, add validation (400 if either limit ≤ 0 when rate limiting would be enabled after save), inject `IRateLimitCounterStore` and call `_rateLimitCounterStore.ResetAll()` after `SaveChangesAsync()`, and populate the three new fields in the returned `AuthSettingsViewModel` in `backend/MockHealthSystem.Api/Controllers/AuthSettingsController.cs` (depends on T008, T009)
- [x] T011 [P] [US1] Add `[ProducesResponseType(StatusCodes.Status429TooManyRequests)]` to all action methods in `backend/MockHealthSystem.Api/Controllers/PatientsController.cs`, `HealthController.cs`, `AuthController.cs`, and `SystemController.cs`
- [x] T012 [P] [US1] Add `rateLimitEnabled: boolean`, `rateLimitPerSecond: number`, `rateLimitPerMinute: number` to the `AuthSettings` interface and optional counterparts to `UpdateAuthSettingsRequest` interface in `frontend/src/api.ts`
- [x] T013 [US1] Add `rateLimitEnabled: boolean`, `rateLimitPerSecond: number`, `rateLimitPerMinute: number` to `FormState` type with defaults (`false`, `10`, `300`); update `applySettingsToForm()` to set these fields; update `handleSubmit()` to include all three fields in the `UpdateAuthSettingsRequest` payload in `frontend/src/AuthSettingsPage.tsx` (depends on T012)
- [x] T014 [US1] Add a "Rate Limiting" section to the `AuthSettingsPage` form (after the OAuth section, before the save button row): enable/disable checkbox labeled "Enable rate limiting"; per-second number input (min=1, `disabled` when not enabled or in demo mode) labeled "Requests per second (per IP)"; per-minute number input (min=1, `disabled` when not enabled or in demo mode) labeled "Requests per minute (per IP)"; helper text "Limits apply per caller IP address. Admin interface requests are never counted." in `frontend/src/AuthSettingsPage.tsx` (depends on T013)
- [x] T015 [P] [US1] Add `rateLimitEnabled: true`, `rateLimitPerSecond: 10`, `rateLimitPerMinute: 300` to `DEMO_AUTH_SETTINGS` constant in `frontend/src/demoData.ts`
- [x] T016 [P] [US1] Create `backend/MockHealthSystem.Tests/Integration/RateLimitEndpointTests.cs` with tests: 429 returned on patient endpoint when per-second limit = 1 and two requests sent in same window; 429 body is valid `ApiErrorResponse` with `status: 429` and `title: "Too Many Requests"`; `Retry-After` header is present and positive integer on 429 response; rate limiting disabled by default (no `AuthSettings` row seeded) — burst of requests returns no 429s (depends on T010)
- [x] T017 [P] [US1] Update `backend/MockHealthSystem.Tests/Integration/AuthSettingsEndpointTests.cs` to assert that `GET /api/v1/auth-settings` response includes `rateLimitEnabled`, `rateLimitPerSecond`, `rateLimitPerMinute` fields with correct defaults; `PUT` round-trips new rate limit values correctly (depends on T010)
- [x] T018 [P] [US1] Update `frontend/src/AuthSettingsPage.test.tsx`: Rate Limiting section renders with correct values loaded from mock settings response; saving with enabled=true includes `rateLimitEnabled`, `rateLimitPerSecond`, `rateLimitPerMinute` in the PUT payload; per-second and per-minute inputs show the loaded values; demo mode: rate limit inputs are not interactive (depends on T014, T015)

**Checkpoint**: Rate limiting is configurable end-to-end — settings save via UI, enforce via middleware, persist to DB

---

## Phase 4: User Story 2 — Disable Rate Limiting for Unrestricted Testing (Priority: P1)

**Goal**: Admin can toggle rate limiting off; all subsequent API requests succeed regardless of rate; re-enabling restores previously configured limits

**Independent Test**: Enable rate limiting with per-second = 1, disable via PUT, send burst of requests to GET /api/v1/patients — all succeed with no 429s; PUT settings again with `rateLimitEnabled: true`, confirm limits are re-enforced

- [x] T019 [US2] Verify the Rate Limiting section shows per-second and per-minute inputs regardless of whether the toggle is on or off (inputs disabled-but-visible when toggle is off, so users can set limits before enabling); confirm save with `rateLimitEnabled: false` does not clear stored limit values — update form UX in `frontend/src/AuthSettingsPage.tsx` if inputs are incorrectly hidden when disabled (depends on T014)
- [x] T020 [US2] Extend `backend/MockHealthSystem.Tests/Integration/RateLimitEndpointTests.cs` with tests: disabling rate limiting (PUT `rateLimitEnabled: false`) stops 429s — client that was at limit can request again; re-enabling rate limiting restores enforcement; counter reset after save — client at limit before settings save can request again immediately after save (depends on T016)
- [x] T021 [US2] Extend `frontend/src/AuthSettingsPage.test.tsx`: saving with `rateLimitEnabled: false` sends `false` in payload; saved limit values (10/300) are preserved in form state after disabling; disabling renders per-second and per-minute inputs as disabled HTML elements (not hidden) (depends on T018)

**Checkpoint**: Toggling rate limiting on/off works correctly; limit values are never silently erased by the toggle

---

## Phase 5: User Story 3 — Actionable Feedback on Rate Limit Errors (Priority: P2)

**Goal**: 429 responses carry a `Retry-After` header that accurately reflects when the client can next succeed; when both windows are exhausted, the header reflects the longer (per-minute) window

**Independent Test**: Set per-second = 1, per-minute = 2, exhaust per-minute limit; observe 429 with `Retry-After` > 1 (reflecting minutes-window reset, not the 1-second window); wait the indicated duration; confirm next request succeeds

- [x] T022 [US3] Verify `RateLimitingMiddleware` correctly computes `Retry-After = max(secondsUntilSecondReset, secondsUntilMinuteReset)` and that the `detail` string in `ApiErrorResponse` quotes the same number as the header; add an inline comment if the max-window logic is non-obvious in `backend/MockHealthSystem.Api/Middleware/RateLimitingMiddleware.cs` (depends on T007)
- [x] T023 [P] [US3] Extend `backend/MockHealthSystem.Tests/Unit/RateLimitCounterStoreTests.cs` with `RetryAfterSeconds` precision tests: only per-second exceeded → value ≤ 1; only per-minute exceeded → value proportional to remaining minute window; both exceeded → value reflects minute window (longer), not second window (shorter) (depends on T006)
- [x] T024 [P] [US3] Extend `backend/MockHealthSystem.Tests/Integration/RateLimitEndpointTests.cs` with test: exhaust per-minute limit (set perMinute=2, send 3 requests across two seconds); assert `Retry-After` header value > 1 second, confirming it reflects the minute window reset, not the per-second window (depends on T020)

**Checkpoint**: `Retry-After` values are meaningful and guide clients to retry at the right time

---

## Phase 6: User Story 4 — Admin UI Unaffected by API Rate Limits (Priority: P1)

**Goal**: API rate limit = 1 req/sec; admin endpoints (auth-settings, monitoring, test-data, admin sessions) never return 429 from the configurable limit

**Independent Test**: Set per-second limit = 1, exhaust the API rate limit from the same IP, then call `GET /api/v1/auth-settings` — it must return 200, not 429

- [x] T025 [P] [US4] Extend `backend/MockHealthSystem.Tests/Integration/RateLimitEndpointTests.cs` with tests: `GET /api/v1/auth-settings` returns 200 when API per-second limit is 1 and already exhausted from same IP; `GET /api/v1/monitoring/requests` returns 200 in same conditions; `GET /api/v1/test-data/patients/stats` returns 200 in same conditions (depends on T020)
- [x] T026 [P] [US4] Verify that `RateLimitingMiddleware` admin-path constant set exactly matches the four admin path prefixes documented in the plan (`/api/v1/auth-settings`, `/api/v1/monitoring`, `/api/v1/test-data`, `/api/v1/admin`) — add a test in `RateLimitEndpointTests.cs` confirming a path NOT in the admin set (e.g., `/api/v1/patients`) does receive 429 after exhausting the limit (depends on T025)

**Checkpoint**: All four user stories fully implemented and independently tested

---

## Phase 7: Polish & Cross-Cutting Concerns

- [x] T027 [P] Update `README.md` Configuration Reference section: add `RateLimitEnabled`, `RateLimitPerSecond`, `RateLimitPerMinute` to the auth settings description; note that 429 with `Retry-After` is possible on API data endpoints when rate limiting is enabled
- [x] T028 [P] Check if `API-CONNECT.md` exists at repo root; if it does, add a note that API endpoints may return 429 Too Many Requests with a `Retry-After` header when rate limiting is enabled, and describe the `ApiErrorResponse` shape
- [x] T029 Run full backend test suite from repo root: `cd backend && dotnet test MockHealthSystem.sln`; all tests must pass
- [x] T030 Run full frontend test suite: `cd frontend && npm run test`; all tests must pass
- [x] T031 Run frontend lint: `cd frontend && npm run lint`; zero warnings or errors

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 (T001) — **BLOCKS all user stories**
- **Phase 3 (US1)**: Depends on Phase 2 complete — primary MVP increment
- **Phase 4 (US2)**: Depends on Phase 3 (T014, T016) — toggle and disable-path tests build on US1
- **Phase 5 (US3)**: Depends on Phase 2 (T007) — Retry-After logic is in middleware
- **Phase 6 (US4)**: Depends on Phase 2 (T007) — admin-path logic is in middleware
- **Phase 7 (Polish)**: Depends on all story phases complete

### User Story Dependencies

- **US1 (P1)**: Can start as soon as Phase 2 is complete
- **US2 (P1)**: Depends on US1 (T014, T016) — tests the toggle and disable scenarios built in US1
- **US3 (P2)**: Depends only on Phase 2 (T007) — Retry-After logic already lives in the middleware
- **US4 (P1)**: Depends only on Phase 2 (T007) — admin path logic already lives in the middleware

**Note**: US3 and US4 can begin in parallel with US1/US2 once Phase 2 is done, since their implementation is already in T007 and they primarily add targeted tests.

### Within Each Phase (task ordering)

```
Phase 2: T002 → (T003 || T004) → T005 → (T006 || T007) → T008
Phase 3: (T009 || T011 || T012 || T015) → (T010 → T013 → T014) → (T016 || T017 || T018)
Phase 4: T019 → T020 → T021
Phase 5: T022 → (T023 || T024)
Phase 6: T025 → T026
Phase 7: (T027 || T028) → T029 → T030 → T031
```

---

## Parallel Opportunities

### Phase 2 parallel block (after T002)

```
Task: T003 — Create PerIpCounters.cs
Task: T004 — Create IRateLimitCounterStore.cs interface
```

### Phase 3 parallel block (after T005, before T010 depends on T009)

```
Task: T009 — Add DTO fields in AuthSettingsModels.cs
Task: T011 — Add [ProducesResponseType(429)] to controllers
Task: T012 — Extend frontend api.ts interfaces
Task: T015 — Update demoData.ts DEMO_AUTH_SETTINGS
```

### Phase 3 test parallel block (after T010, T014)

```
Task: T016 — Create RateLimitEndpointTests.cs
Task: T017 — Update AuthSettingsEndpointTests.cs
Task: T018 — Update AuthSettingsPage.test.tsx
```

---

## Implementation Strategy

### MVP First (User Story 1 only — highest value)

1. Complete Phase 1 (T001)
2. Complete Phase 2 (T002–T008) — foundation
3. Complete Phase 3 (T009–T018) — full configure + enforce flow
4. **STOP AND VALIDATE**: `dotnet test` passes, `npm run test` passes, manually enable rate limiting via UI and confirm 429s from curl
5. Ship if ready

### Incremental Delivery

1. Phase 1 + Phase 2 → foundation wired
2. Phase 3 (US1) → rate limiting configurable and enforced; MVP deliverable
3. Phase 4 (US2) → disable toggle verified with tests
4. Phase 5 (US3) → Retry-After precision confirmed
5. Phase 6 (US4) → admin exemption confirmed
6. Phase 7 → documentation + final test run

### Parallel Team Strategy

After Phase 2 completes:
- Developer A: Phase 3 (US1) — DTOs, controller, frontend form
- Developer B: Phase 5 (US3) + Phase 6 (US4) — middleware verification + targeted tests

---

## Notes

- [P] tasks touch different files with no in-flight dependencies — safe to run concurrently
- Rate limiting is **off by default** (no `AuthSettings` row in tests → `RateLimitEnabled = false`) — existing tests pass without modification
- Each `IsolatedWebApplicationFactory` starts with empty counters; tests that need specific rate limit behavior must seed `AuthSettings` via the update endpoint or direct DB seeding
- The EF migration (T002) should be run immediately after T001 is committed; verify `AppDbContextModelSnapshot.cs` is updated
- `ResetAll()` is called on every settings save — tests that span a save must account for counter reset
- Retry-After is always ≥ 1 (a value of 0 would signal "retry immediately", which is incorrect for a rate-limited window)
