# Phase 0 Research: Frontend Demo Mode

**Date**: 2026-06-07 | **Plan**: [plan.md](plan.md)

## Research Area 1: Open-Mode Detection Without Backend Changes

**Question**: How does the frontend detect whether an admin key is configured on the server, so demo mode can be suppressed in open/local-dev mode — without modifying the backend?

**Decision**: Probe `GET /api/v1/auth-settings` on app mount using `mintApi` (the existing Axios instance that sends no `X-Admin-Session` header).

- `200 OK` → open mode (no admin key configured); `isAdminKeyRequired = false`
- `401` or `403` → admin key configured; `isAdminKeyRequired = true`
- Network error / backend offline → assume admin key configured; `isAdminKeyRequired = true`

**Rationale**: The existing endpoint already returns `200` in open mode and `401` when an admin key is configured and no session is present — no new backend endpoint is needed. The fail-safe default (assume protected on network error) is correct for public deployments where the backend may be temporarily unreachable: demo mode should remain functional even when the server is down.

**Implementation note**: The probe runs once per app mount and its result is stored in `AdminSessionContext` state as `isAdminKeyRequired`. It is not re-run on navigation. The result persists for the browser session.

**Alternatives considered**:

| Option | Verdict |
|--------|---------|
| Add `adminKeyRequired` field to `GET /api/v1/health` response | Rejected — requires backend change; contradicts spec assumption |
| `VITE_ADMIN_KEY_REQUIRED` env var | Rejected — requires deployer configuration knowledge; easy to misconfigure |
| Attempt session mint with empty key and inspect error | Rejected — ambiguous error codes; sends unnecessary requests to the session endpoint |

---

## Research Area 2: Session Expiry Auto-Revert to Demo Mode

**Question**: The existing `AdminSessionContext` only recomputes `hasSession` when `refresh()` is explicitly called or the component mounts. How do we ensure pages revert to demo mode within 3 seconds of session expiry without polling?

**Decision**: When a session is established via `signIn()`, schedule a `setTimeout` to fire at the exact token expiry time (parsed from `expiresAtUtc`). The timer calls `refresh()`, which re-evaluates `getAdminSessionToken()` (which already clears expired tokens) and triggers a re-render. Cancel the previous timer in a `useEffect` cleanup whenever `expiresAtUtc` changes.

**Rationale**: A single precisely-timed `setTimeout` fires exactly when the token expires (within JS event-loop scheduling tolerance, typically < 100ms). This meets the 3-second SC without any recurring polling overhead. The existing `getAdminSessionToken()` in `adminSessionStore.ts` already handles expiry logic — the timer simply triggers the re-evaluation.

**Implementation note**: The timer is managed inside `AdminSessionContext` via a `useRef<ReturnType<typeof setTimeout>>` to hold the timer ID across renders. The `useEffect` that schedules the timer depends on `expiresAtUtc` and cancels any prior timer in its cleanup function.

**Alternatives considered**:

| Option | Verdict |
|--------|---------|
| Poll every 1 second | Rejected — unnecessary overhead; 1s polling for session expiry in an admin tool is excessive |
| Poll every 30 seconds | Rejected — violates 3-second SC from spec SC-006 |
| Rely on next user action to detect expiry | Rejected — violates FR-009 (must revert automatically) |

---

## Research Area 3: Mobile Viewport Support

**Question**: The existing admin interface was not designed with mobile viewports in mind. What changes are needed to make demo mode render correctly on small screens?

**Decision**: Use Tailwind responsive prefixes (`sm:`, `md:`) on the `DemoBanner` and on any layout elements in demo page variants that currently use fixed widths or non-wrapping flex rows. Verify at 320px (iPhone SE) and 375px (iPhone 14) breakpoints.

**Rationale**: The existing pages already use Tailwind, and `ResponsiveContainer` from Recharts already handles chart sizing. The main risk area is the banner layout (icon + text + CTA button in a single row), which should stack vertically on mobile. No fundamental layout change to the existing pages is needed — Tailwind responsive classes are sufficient.

**Implementation note**: `DemoBanner` uses `flex-col gap-3` on mobile and `flex-row items-center justify-between` on `sm:` and above. The CTA button uses `w-full sm:w-auto` so it fills the screen on mobile.

**Alternatives considered**:

| Option | Verdict |
|--------|---------|
| Separate mobile-specific component | Rejected — unnecessary duplication; Tailwind responsive classes are the project standard |
| Fixed-width banner with horizontal scroll | Rejected — poor UX; violates spec SC-002 (must be visible without scrolling) |

---

## Research Area 4: Demo Page Button No-Op Pattern

**Question**: Spec FR-006 says buttons must not produce errors or trigger backend requests, and the description says "functionality does not need to be responsive to the user." What is the cleanest pattern for silent no-op buttons in React?

**Decision**: In demo page variants, form `onSubmit` handlers call `event.preventDefault()` and return immediately. Standalone `onClick` handlers are replaced with `() => {}`. No toast, modal, or feedback is shown.

**Rationale**: The spec explicitly states that demo buttons "do not need to be responsive to the user" — silent no-op is the specified behavior. Adding feedback would scope-creep the feature and add implementation overhead without spec backing.

**Implementation note**: Buttons retain their visual `disabled:opacity-50` styles only if they would naturally be disabled (e.g., "Save" when no changes detected). They are NOT globally disabled in demo mode — they must remain clickable per spec FR-005.

**Alternatives considered**:

| Option | Verdict |
|--------|---------|
| Toast notification: "This is a demo — authenticate to use this feature" | Not required by spec; out of scope for this feature |
| Disable all buttons with `disabled` attribute | Rejected — contradicts FR-005 ("must be rendered and visually consistent") and spec description ("buttons should be clickable") |

---

## Research Area 5: Backend Offline Support

**Question**: FR-014 requires demo pages to function when the backend is entirely offline. FR-015 requires auth failure in that case to be graceful. What is the implementation approach?

**Decision**:
- Demo page content comes exclusively from `demoData.ts` constants — no runtime API calls. Demo pages render immediately with zero network dependency.
- The open-mode probe (Research Area 1) handles offline by defaulting to `isAdminKeyRequired = true` on network error — demo mode activates correctly when backend is down.
- The `AdminAccessPage` `signIn()` already wraps `exchangeAdminSession()` in a try/catch and sets a user-facing error message on failure. No additional handling is needed for the offline case.

**Rationale**: Because all demo data is pre-loaded as module-level constants, there is nothing to fetch — the offline case is already handled by design. The session mint failure path already has correct error handling.

**Alternatives considered**:

| Option | Verdict |
|--------|---------|
| Service worker / cache for offline support | Out of scope — overkill for a dev tool; spec does not require full offline capability, only demo page resilience |
| Detect offline state and show a specific "backend offline" message on demo pages | Out of scope — demo pages already render correctly; no special messaging needed |

---

## Summary: All Unknowns Resolved

| Unknown | Resolution |
|---------|------------|
| Open-mode detection without backend change | Probe `GET /api/v1/auth-settings` via `mintApi`; 200=open, 401/403=protected, error=protected |
| Session expiry auto-revert | `setTimeout` scheduled at exact expiry time in `AdminSessionContext`; fires `refresh()` |
| Mobile layout | Tailwind responsive prefixes on `DemoBanner`; existing Recharts `ResponsiveContainer` handles charts |
| Button no-op behavior | `event.preventDefault()` + empty handlers; no toast or feedback |
| Backend offline support | Demo data is in-memory; probe defaults to protected on error; existing auth error handling sufficient |
