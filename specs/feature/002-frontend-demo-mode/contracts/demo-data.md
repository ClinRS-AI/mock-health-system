# Demo Data Contract

**Date**: 2026-06-07 | **Plan**: [../plan.md](../plan.md)

This feature introduces no new API endpoints. The contract surface is the public exports of `src/demoData.ts` — the module that supplies all pre-defined mock content for demo mode pages.

## Module: `src/demoData.ts`

All exports are typed directly against the interfaces in `src/api.ts`. No additional types are introduced.

### `DEMO_AUTH_SETTINGS`

```ts
import type { AuthSettings } from "./api";

export const DEMO_AUTH_SETTINGS: AuthSettings
```

Used by `AuthSettingsPage` when `isDemoMode` is true.

---

### `DEMO_MONITORING_SUMMARIES`

```ts
import type { MonitoredRequestSummary } from "./api";

export const DEMO_MONITORING_SUMMARIES: MonitoredRequestSummary[]
```

25 entries. Used by `MonitoringPage` in place of `getMonitoredRequests()` when `isDemoMode` is true.

---

### `DEMO_MONITORING_STATS`

```ts
import type { MonitoringStats } from "./api";

export const DEMO_MONITORING_STATS: MonitoringStats
```

Used by `MonitoringPage` in place of `getMonitoringStats()` when `isDemoMode` is true.

---

### `DEMO_TEST_DATA_STATS`

```ts
import type { PatientTestDataStats } from "./api";

export const DEMO_TEST_DATA_STATS: PatientTestDataStats
```

Used by `TestDataPage` in place of `getPatientTestDataStats()` when `isDemoMode` is true.

---

## Context API Extension: `AdminSessionContext`

The `AdminSessionContextValue` interface gains two new fields accessible via `useAdminSession()`:

```ts
interface AdminSessionContextValue {
  // existing fields unchanged
  hasSession: boolean;
  expiresAtUtc: string | null;
  signIn: (adminKey: string) => Promise<void>;
  signOut: () => void;
  refresh: () => void;

  // new fields
  /** True when the backend is running with admin key protection enabled. */
  isAdminKeyRequired: boolean;
  /** True when the user is unauthenticated AND admin key protection is active. */
  isDemoMode: boolean;
}
```

### New `api.ts` Export

```ts
/**
 * Probes whether admin key protection is active by calling GET /api/v1/auth-settings
 * without a session header. Returns true if the response is 401/403 or a network error
 * occurs; returns false if the response is 200 (open mode).
 */
export async function probeAdminKeyRequired(): Promise<boolean>
```

This function uses `mintApi` (the existing Axios instance without the `X-Admin-Session` interceptor) so no session header is sent on the probe. It is called once on `AdminSessionProvider` mount and never called from demo page render paths.

---

## Component API: `DemoBanner`

```tsx
interface DemoBannerProps {
  /** Called when the user clicks "Go to Admin Access". Receives the tab identifier. */
  onNavigateToAdmin: () => void;
}

export function DemoBanner({ onNavigateToAdmin }: DemoBannerProps): React.JSX.Element
```

`DemoBanner` is a presentational component with no internal state. It is rendered at the top of each demo page variant. `onNavigateToAdmin` is wired to the tab navigation in `App.tsx` (sets `view` to `"admin"`).
