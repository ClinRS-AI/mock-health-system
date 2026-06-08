# Implementation Plan: Frontend Demo Mode

**Branch**: `002-frontend-demo-mode` | **Date**: 2026-06-07 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-frontend-demo-mode/spec.md`

## Summary

Frontend Demo Mode delivers a read-only, pre-populated view of the Auth Settings, Monitoring, and Test Data pages for unauthenticated visitors. When an admin key is configured on the server, users without a valid session see each page's full UI populated with realistic static mock data, along with a persistent banner directing them to authenticate. Demo mode is detected via a single probe call on app load; all demo page content is served from in-memory constants with zero backend requests. The feature is purely frontend — no backend changes are required.

## Technical Context

**Language/Version**: TypeScript 5.4 / React 18

**Primary Dependencies**: React 18, Tailwind CSS 3.4, Axios 1.7 (open-mode probe via existing `mintApi`), Recharts 3.7, Vitest 2.0, React Testing Library 16, MSW 2.4

**Storage**: N/A — all demo data is module-level in-memory constants in `src/demoData.ts`; no new persistence layer

**Testing**: Vitest + React Testing Library + MSW v2. New tests cover: `DemoBanner` renders with correct content and link; each page renders its demo variant when `isDemoMode=true`; pages render live content when `isDemoMode=false`; open-mode probe correctly sets `isAdminKeyRequired`.

**Target Platform**: Web browser — current versions of Chrome, Firefox, and Safari; desktop and mobile viewports (320px minimum width)

**Project Type**: Frontend-only enhancement to the existing React SPA (`frontend/`)

**Performance Goals**: Demo pages render in under 1 second with zero backend requests generated from demo page views

**Constraints**:
- Zero API calls from demo page render paths — all demo content served from in-memory `demoData.ts`
- All demo data must be synthetic — no real PHI, no real patient identifiers
- Demo mode banner must be visible without scrolling at 320px viewport width and above
- Button clicks on demo pages must not produce JavaScript errors or trigger backend requests

**Scale/Scope**: 3 existing page components modified, 1 new shared banner component, session context extended, 1 new `api.ts` function; no backend changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Healthcare Domain Fidelity | ✅ Pass | All demo data in `demoData.ts` is synthetic — fictional counts, dates, paths; no names, DOBs, or real identifiers |
| II. Authenticated-by-Default | ✅ Pass | Demo mode IS the unauthenticated state: pages show read-only mock data, not live admin data; backend admin routes remain unchanged and protected |
| III. Integration-First Testing | ✅ Pass | Frontend tests use MSW v2 handlers; `renderWithAdminSession` helper extended to support `isDemoMode`; no inline fetch stubs |
| IV. API Versioning & Stability | ✅ Pass | No new API routes; open-mode probe reuses existing `GET /api/v1/auth-settings` contract |
| V. Observability by Default | ✅ Pass | No change to `RequestLoggingMiddleware`; demo pages generate zero logged requests |
| VI. Code Quality | ✅ Pass | TypeScript strict; demo data exports typed against existing `api.ts` interfaces; `DemoBanner` styled with Tailwind only; no `any` |
| VII. Testing Standards | ✅ Pass | Vitest + RTL + `userEvent.setup()`; test files colocated with components; behavioral assertions only |
| VIII. User Experience Consistency | ✅ Pass | `DemoBanner` visible without scroll; demo pages render immediately with no loading state; error display pattern unchanged for probe failures |
| IX. Security | ✅ Pass | No credential exposure; open-mode probe uses `mintApi` instance (no `X-Admin-Session` header sent); CORS unchanged |
| X. Performance | ✅ Pass | Demo pages: zero network requests, in-memory data with no async cost; open-mode probe: single call on mount, cached for session lifetime |

**Documentation Workflow**: Per constitution §Development Workflow, `README.md` must be reviewed and updated after this feature — the unauthenticated user experience (authentication behaviour section) has changed.

**Gate result**: ✅ All 10 principles satisfied. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/002-frontend-demo-mode/
├── plan.md              # This file
├── research.md          # Phase 0: open-mode detection, expiry timer, mobile, no-op buttons
├── data-model.md        # Phase 1: demo data constants schema
├── quickstart.md        # Phase 1: how to experience demo mode locally
├── contracts/
│   └── demo-data.md     # Phase 1: demo data shape (typed against api.ts interfaces)
└── tasks.md             # Phase 2 output (/speckit-tasks — not yet generated)
```

### Source Code (frontend only)

```text
frontend/src/
├── demoData.ts              # NEW: static mock data constants (AuthSettings, Monitoring, TestData)
├── DemoBanner.tsx           # NEW: "Demo Mode" banner with CTA linking to Admin Access tab
├── DemoBanner.test.tsx      # NEW: RTL behavioral tests for banner
├── AdminSessionContext.tsx  # MODIFIED: add isAdminKeyRequired (probe), isDemoMode, expiry timer
├── AuthSettingsPage.tsx     # MODIFIED: render demo variant when isDemoMode is true
├── AuthSettingsPage.test.tsx# MODIFIED: add demo mode test cases
├── MonitoringPage.tsx       # MODIFIED: render demo variant when isDemoMode is true
├── MonitoringPage.test.tsx  # MODIFIED: add demo mode test cases
├── TestDataPage.tsx         # MODIFIED: render demo variant when isDemoMode is true
├── TestDataPage.test.tsx    # MODIFIED: add demo mode test cases
└── api.ts                   # MODIFIED: add probeAdminKeyRequired() using mintApi
```

**Structure Decision**: Frontend-only changes (Option 2 web application layout, frontend side only). All demo data is co-located in `demoData.ts`; the banner is a shared presentational component; page changes are conditional branches in existing components rather than new page files.

## Complexity Tracking

No constitution violations — this section is intentionally empty.
