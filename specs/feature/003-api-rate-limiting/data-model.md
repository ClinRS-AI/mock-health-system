# Data Model: Configurable API Rate Limiting

**Phase 1 output for feature/003-api-rate-limiting**

---

## Entity Changes

### AuthSettings (existing entity — extended)

**File**: `backend/MockHealthSystem.Infrastructure/Data/Entities/AuthSettings.cs`

Three columns added to the existing singleton row (Id = 1):

| Column | Type | Default | Constraints |
|---|---|---|---|
| `RateLimitEnabled` | `bool` | `false` | Not null |
| `RateLimitPerSecond` | `int` | `10` | Not null, ≥ 1 when enabled |
| `RateLimitPerMinute` | `int` | `300` | Not null, ≥ 1 when enabled |

**Default state is disabled** so that existing deployments are unaffected when the migration runs.

---

## New In-Memory Types (no persistence)

### PerIpCounters

Holds the fixed-window counters for one IP address. Lives in the `RateLimitCounterStore` singleton. Not persisted.

| Field | Type | Description |
|---|---|---|
| `SecondWindowStartTick` | `long` | `DateTime.UtcNow.Ticks` when the current 1-second window opened |
| `SecondCount` | `int` | Requests counted in the current second window |
| `MinuteWindowStartTick` | `long` | `DateTime.UtcNow.Ticks` when the current 1-minute window opened |
| `MinuteCount` | `int` | Requests counted in the current minute window |

**Locking**: Each `PerIpCounters` instance is the lock target for reads and writes. No global lock.

### CheckResult (value type or named tuple)

Returned by `IRateLimitCounterStore.CheckAndIncrement()`.

| Field | Type | Description |
|---|---|---|
| `Allowed` | `bool` | True if the request is within limits |
| `RetryAfterSeconds` | `int` | Seconds until the longest outstanding window resets; 0 when `Allowed = true` |

---

## DTO Changes

### AuthSettingsViewModel (extended)

**File**: `backend/MockHealthSystem.Api/Models/Auth/AuthSettingsModels.cs`

Add to the response DTO:

| Property | Type | Description |
|---|---|---|
| `RateLimitEnabled` | `bool` | Whether rate limiting is active |
| `RateLimitPerSecond` | `int` | Requests per second per IP (only meaningful when enabled) |
| `RateLimitPerMinute` | `int` | Requests per minute per IP (only meaningful when enabled) |

### AuthSettingsUpdateModel (extended)

**File**: `backend/MockHealthSystem.Api/Models/Auth/AuthSettingsModels.cs`

Add to the request DTO (nullable — only updated when provided):

| Property | Type | Description |
|---|---|---|
| `RateLimitEnabled` | `bool?` | Set to enable/disable rate limiting |
| `RateLimitPerSecond` | `int?` | New per-second limit (must be ≥ 1 when provided with `RateLimitEnabled = true`) |
| `RateLimitPerMinute` | `int?` | New per-minute limit (must be ≥ 1 when provided with `RateLimitEnabled = true`) |

**Validation** added to `AuthSettingsController.UpdateAsync()`:
- If `RateLimitEnabled = true` AND `RateLimitPerSecond` is being set: must be ≥ 1
- If `RateLimitEnabled = true` AND `RateLimitPerMinute` is being set: must be ≥ 1
- If the existing `RateLimitEnabled` would remain `true` after the update and the stored limits are already ≥ 1: no additional validation needed

---

## Frontend Type Changes

### AuthSettings interface (extended)

**File**: `frontend/src/api.ts`

```typescript
export interface AuthSettings {
  // ... existing fields ...
  rateLimitEnabled: boolean;
  rateLimitPerSecond: number;
  rateLimitPerMinute: number;
}
```

### UpdateAuthSettingsRequest interface (extended)

**File**: `frontend/src/api.ts`

```typescript
export interface UpdateAuthSettingsRequest {
  // ... existing fields ...
  rateLimitEnabled?: boolean;
  rateLimitPerSecond?: number;
  rateLimitPerMinute?: number;
}
```

### FormState (AuthSettingsPage internal state)

**File**: `frontend/src/AuthSettingsPage.tsx`

```typescript
type FormState = {
  // ... existing fields ...
  rateLimitEnabled: boolean;
  rateLimitPerSecond: number;
  rateLimitPerMinute: number;
};
```

Default: `rateLimitEnabled: false, rateLimitPerSecond: 10, rateLimitPerMinute: 300`

---

## EF Core Migration

One migration required:

```
AddRateLimitColumnsToAuthSettings
```

Adds three columns to the `AuthSettings` table:
- `RateLimitEnabled` (boolean, NOT NULL, default false)
- `RateLimitPerSecond` (integer, NOT NULL, default 10)
- `RateLimitPerMinute` (integer, NOT NULL, default 300)

**Generate via**:
```bash
backend/scripts/run-ef.sh migrations add AddRateLimitColumnsToAuthSettings
```

---

## State Transitions

### Rate Limit Counter lifecycle

```
Server start       → ConcurrentDictionary empty; all IPs get fresh counters on first request
Settings save      → ResetAll() called; all per-IP counters cleared
Server restart     → ConcurrentDictionary empty (in-memory only; settings reloaded from DB)
Per-second window  → Counter auto-resets when a request arrives after the 1-second tick elapses
Per-minute window  → Counter auto-resets when a request arrives after the 60-second tick elapses
```

### Request flow

```
Incoming request
  → Is path an admin path? → Yes → Check built-in admin limits (120/sec, 5000/min)
                                      → Exceeded? → 429 with Retry-After
                                      → Within limits? → next()
  → No
  → RateLimitEnabled? → No → next()
  → Yes
  → Get or create PerIpCounters for caller IP
  → lock(counter)
      → Is second window elapsed? → reset SecondWindowStartTick + SecondCount = 0
      → Is minute window elapsed? → reset MinuteWindowStartTick + MinuteCount = 0
      → SecondCount >= RateLimitPerSecond OR MinuteCount >= RateLimitPerMinute?
          → 429: Retry-After = max(secondsUntilSecondReset, secondsUntilMinuteReset)
      → Increment SecondCount and MinuteCount
  → next()
```
