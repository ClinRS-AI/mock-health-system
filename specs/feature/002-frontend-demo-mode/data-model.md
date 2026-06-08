# Data Model: Frontend Demo Mode

**Date**: 2026-06-07 | **Plan**: [plan.md](plan.md)

This feature has no database entities — it is entirely frontend. The "data model" consists of the static in-memory constants defined in `src/demoData.ts`, each typed against existing interfaces in `src/api.ts`.

---

## Demo Auth Settings

**Type**: `AuthSettings` (from `api.ts`)

Represents the pre-defined authentication configuration displayed on the Auth Settings page in demo mode.

```
mode:                        "CCAPIKey"
bearerToken:                 null
oAuthClientId:               null
oAuthClientSecret:           null
accessTokenLifetimeMinutes:  60
refreshTokenLifetimeDays:    30
hasAnyTokens:                true
```

**Rationale**: CCAPIKey is the primary CC authentication mechanism. Showing it as active gives visitors the most representative view of real-world usage. `hasAnyTokens: true` ensures the UI renders credential-related UI elements rather than their empty state.

---

## Demo Monitoring Summaries

**Type**: `MonitoredRequestSummary[]` (from `api.ts`)

Represents the list of request log entries displayed on the Monitoring page in demo mode.

**Shape of each entry**:

| Field | Description |
|-------|-------------|
| `id` | Sequential integer (1–25) |
| `createdAtUtc` | ISO 8601 timestamp; entries span the past 24 hours in reverse-chronological order |
| `method` | Mix of `GET` (majority) and `POST` |
| `path` | Realistic paths: `/api/v1/patients`, `/api/v1/patients/{id}/conditions`, `/api/v1/auth-settings`, `/api/v1/health`, `/api/v1/test-data/patients/generate` |
| `statusCode` | Weighted mix: `200` (~84%), `401` (~5%), `404` (~4%), `500` (~2%), `204` (~5%) |
| `durationMs` | Realistic spread: 20–280ms |
| `origin` | `"http://localhost:5176"` (consistent with dev frontend URL) |

**Count**: 25 entries.

---

## Demo Monitoring Stats

**Type**: `MonitoringStats` (from `api.ts`)

Represents the aggregate statistics displayed on the Monitoring page in demo mode.

```
requestCount:              147
averageDurationMs:         43
percentile95DurationMs:    112
maxDurationMs:             287
statusBreakdown:
  - statusCode: 200,  count: 130
  - statusCode: 204,  count: 7
  - statusCode: 401,  count: 6
  - statusCode: 404,  count: 3
  - statusCode: 500,  count: 1
```

**Rationale**: `requestCount: 147` represents a realistic light-to-moderate usage session. The breakdown is weighted heavily toward 2xx to reflect a healthy integration test run, with a small number of auth and not-found errors to demonstrate the monitoring capability.

---

## Demo Test Data Stats

**Type**: `PatientTestDataStats` (from `api.ts`)

Represents the statistics and patient data displayed on the Test Data page in demo mode.

```
patientCount:              47
duplicatePatientCount:     12
recentAuditEventCount:     234
totalStaffCount:           8
patientsBySite:
  - siteName: "General Hospital",        count: 18
  - siteName: "North Clinic",            count: 14
  - siteName: "East Medical Center",     count: 10
  - siteName: "West Branch",             count:  5
```

**Rationale**: 47 patients across 4 sites with 12 duplicates and 234 audit events represents a realistic mid-session test dataset. All site names are fictional. The total `patientsBySite` count (47) intentionally matches `patientCount` to avoid confusing discrepancies in the UI.

---

## Session State Extension

**Context**: `AdminSessionContextValue` (in `AdminSessionContext.tsx`)

Two new fields are added to the existing context value:

| Field | Type | Description |
|-------|------|-------------|
| `isAdminKeyRequired` | `boolean` | True when the open-mode probe determines the server requires an admin key. False in open/local-dev mode. Defaults to `false` until the probe completes; `true` if the probe fails due to a network error. |
| `isDemoMode` | `boolean` | Derived: `!hasSession && isAdminKeyRequired`. True when the user is unauthenticated and admin key protection is active. Components read this to switch between demo and live rendering. |

**Probe timing**: `isAdminKeyRequired` is resolved by a single `GET /api/v1/auth-settings` call (via `mintApi`) on `AdminSessionProvider` mount. A loading state (`isProbing: boolean`) is available to prevent flash of incorrect content before the probe resolves.

---

## State Transitions

```
App loads
  └─ Probe fires (isProbing = true)
       ├─ 200 OK     → isAdminKeyRequired = false, isDemoMode = false  [open mode]
       ├─ 401 / 403  → isAdminKeyRequired = true,  isDemoMode = true   [protected, no session]
       └─ Network err → isAdminKeyRequired = true,  isDemoMode = true   [offline, show demo]

User signs in (signIn())
  └─ hasSession = true  → isDemoMode = false  [live mode]
  └─ setTimeout(refresh, expiryMs) scheduled

Session expires (setTimeout fires)
  └─ refresh() called → hasSession = false → isDemoMode = true  [reverts to demo]

User signs out (signOut())
  └─ hasSession = false → isDemoMode = isAdminKeyRequired  [demo if key configured]
```
