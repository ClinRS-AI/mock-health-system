# Research: Configurable API Rate Limiting

**Phase 0 output for feature/003-api-rate-limiting**

---

## Decision 1: Custom middleware vs ASP.NET built-in rate limiter

**Decision**: Custom `RateLimitingMiddleware` class.

**Rationale**: ASP.NET's built-in `AddRateLimiter` / `UseRateLimiter` reads policy configuration at DI startup time. Swapping limits dynamically (without restart) requires replacing the entire policy factory — complex and not well-documented. The custom approach pulls settings from `IAuthSettingsService` (which already has a 1-minute in-memory cache and `InvalidateCacheAsync()`), and the counter store is a singleton that exposes a `ResetAll()` method. Two types, ~100 lines each, fully testable.

**Alternatives considered**:
- ASP.NET built-in `AddRateLimiter`: rejected because dynamic reconfiguration at runtime is not a supported first-class scenario.
- Third-party library (AspNetCoreRateLimit): unnecessary complexity and dependency for a mock tool; the problem is small enough to solve in-house.

---

## Decision 2: Rate limit settings storage — extend AuthSettings vs separate entity

**Decision**: Extend the existing `AuthSettings` EF entity with three new columns: `RateLimitEnabled` (bool, default false), `RateLimitPerSecond` (int, default 10), `RateLimitPerMinute` (int, default 300).

**Rationale**: `AuthSettings` is already a singleton row (Id = 1). The spec explicitly places rate limit configuration on the Authentication Settings page. Adding three columns to the existing table avoids a new DbSet, new migration table, new service, and new pattern. The data belongs conceptually with the other configuration on that page.

**Alternatives considered**:
- Separate `RateLimitSettings` entity: adds a new DbSet, second singleton row, extra migration table, and parallel service — more code with no benefit for a three-field record.

---

## Decision 3: In-memory counter data structure

**Decision**: Singleton `RateLimitCounterStore` holding `ConcurrentDictionary<string, PerIpCounters>`, where each `PerIpCounters` object is a plain reference type protected by `lock(counter)`.

**Rationale**: Per-IP locking avoids any global lock contention. `ConcurrentDictionary.GetOrAdd` provides safe concurrent initialisation. Fixed tumbling windows (one per second, one per minute) are tracked as a start-tick + count pair per window. This structure is simple, correct under concurrency, and fast (dictionary lookup + lock + counter check + increment is sub-microsecond).

**Alternatives considered**:
- `Interlocked` operations: would require atomic read-modify-write on two fields (tick + count), which needs a spin loop — more complex with no throughput advantage at the scale of a dev mock.
- Redis or distributed cache: out of scope; the spec explicitly states in-memory counters that reset on restart.

---

## Decision 4: Window algorithm

**Decision**: Fixed (tumbling) window for both per-second and per-minute limits.

**Rationale**: The spec explicitly specifies fixed window. It is also significantly simpler to implement and reason about than sliding window: a request at t=0.9s and t=1.1s are in different windows and both succeed, even though they are 0.2s apart. This is acceptable and expected for a dev mock.

**Alternatives considered**:
- Sliding window: more accurate burst detection, but more complex (circular buffer or EWMA). Out of scope per spec.

---

## Decision 5: Admin route exemption strategy

**Decision**: Path-prefix matching inside `RateLimitingMiddleware`. Admin paths are a hard-coded constant set: `/api/v1/auth-settings`, `/api/v1/monitoring`, `/api/v1/test-data`, `/api/v1/admin`. Requests on these paths skip the configurable limit check and are instead checked against the built-in admin limits (120/sec, 5000/min per IP).

**Rationale**: The distinction between admin and API controllers already exists in `Program.cs` (Swagger DocInclusionPredicate). Using the same set of path prefixes is consistent, easy to audit, and requires no attribute metadata on controllers. Hardcoded prefixes are acceptable here since the admin route set is small and stable.

**Alternatives considered**:
- `[SkipRateLimit]` attribute on controllers: requires reflection/metadata access in middleware; overengineering for four admin controllers.
- `AllowAnonymous`-style policy: would mix rate limiting concerns with authentication concerns.

---

## Decision 6: IP address extraction

**Decision**: Use `context.Connection.RemoteIpAddress?.ToString()`. Fall back to `"unknown"` if null (skipping rate limiting for that request). In production, `UseForwardedHeaders` is already enabled (configured in `Program.cs`), which populates `RemoteIpAddress` from `X-Forwarded-For` before the middleware runs.

**Rationale**: The existing `RequestLoggingMiddleware` uses the same `context.Connection.RemoteIpAddress` field. `UseForwardedHeaders` is already in the pipeline for production. No separate IP-extraction logic is needed.

**Alternatives considered**:
- Read `X-Forwarded-For` header directly: duplicates what `UseForwardedHeaders` already does; could be spoofed if the forwarded-headers middleware isn't trusted.

---

## Decision 7: Retry-After value when both limits are exceeded

**Decision**: Return `ceil(secondsUntilMinuteWindowReset)`. The minute window takes longer to reset, so using its reset time guarantees the client's retry will succeed (assuming the client sends only one request).

**Rationale**: Confirmed by user feedback during specification. Returning the shorter (per-second) window would cause the client to retry immediately, hit the per-minute limit, and receive another 429.

**Alternatives considered**:
- Sooner-resetting window (per-second): rejected; misleads clients that retrying after 1 second will succeed.

---

## Decision 8: Middleware placement in the pipeline

**Decision**: Insert `RateLimitingMiddleware` after `RequestLoggingMiddleware` and before `UseAuthorization()`:

```
ExceptionHandling → (503 guard) → ForwardedHeaders → HSTS/HTTPS → Swagger
→ CORS → Authentication → RequestLoggingMiddleware → RateLimitingMiddleware → Authorization → Controllers
```

**Rationale**: Placing rate limiting after `RequestLoggingMiddleware` ensures that 429 responses are recorded in `ApiRequestLogs` (the logging middleware wraps `_next()` in a try/finally, so the 429 body is captured). Placing it before `Authorization` keeps authorization independent of rate limit state — a rate-limited request should not proceed to authorization at all.

**Alternatives considered**:
- Before `RequestLoggingMiddleware`: 429 responses would not be logged, reducing observability.
- After `Authorization`: controllers would receive rate-limited requests that already passed authorization — wrong layer.

---

## Decision 9: Behaviour in the Testing environment

**Decision**: Rate limiting is **off by default** in tests because `GetSettingsAsync()` returns `new AuthSettings { RateLimitEnabled = false }` when the in-memory EF database has no `AuthSettings` row (which is the initial state for every `IsolatedWebApplicationFactory`). No special bypass flag is needed.

For dedicated rate-limit integration tests, the test seeds the in-memory DB with `RateLimitEnabled = true` and low limits, then asserts 429 responses. Each `IsolatedWebApplicationFactory` instance starts with fresh (empty) counters.

**Rationale**: This is clean and consistent with how other settings tests work. No environment-specific bypass branch is needed in production code.

---

## Decision 10: 429 response body

**Decision**: Return an `ApiErrorResponse` JSON body with `status = 429`, `title = "Too Many Requests"`, and `detail = "Rate limit exceeded. Retry after {N} seconds."`. The `Retry-After` header is also set to the same `N` value.

**Rationale**: Consistent with all other error responses in the API (`ExceptionHandlingMiddleware` produces `ApiErrorResponse`). The same pattern is used for 400/401/404/500.

---

## Open items

None. All spec requirements are addressed by the decisions above.
