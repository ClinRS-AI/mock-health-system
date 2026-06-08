# Tasks: Frontend Demo Mode

**Input**: Design documents from `specs/002-frontend-demo-mode/`

**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/demo-data.md ✅

**Organization**: Tasks grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US4)
- All paths are relative to repository root

---

## Phase 1: Setup

**Purpose**: Confirm baseline before any changes

- [X] T001 Confirm `npm test` passes with zero failures in `frontend/` before starting work (establishes clean baseline)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Context extension, probe function, demo data, and session expiry timer — all user stories depend on these

**⚠️ CRITICAL**: No user story implementation can begin until this phase is complete

- [X] T002 [P] Add `probeAdminKeyRequired(): Promise<boolean>` export to `frontend/src/api.ts` — uses `mintApi` (no session header), returns `true` on `401`/`403` or network error, `false` on `200`
- [X] T003 [P] Create `frontend/src/demoData.ts` with four exported constants typed against existing `api.ts` interfaces: `DEMO_AUTH_SETTINGS` (`AuthSettings`), `DEMO_MONITORING_SUMMARIES` (`MonitoredRequestSummary[]`, 25 entries), `DEMO_MONITORING_STATS` (`MonitoringStats`), `DEMO_TEST_DATA_STATS` (`PatientTestDataStats`) — all values from `data-model.md`
- [X] T004 Extend `AdminSessionContextValue` interface in `frontend/src/AdminSessionContext.tsx` with `isAdminKeyRequired: boolean` and `isDemoMode: boolean` fields
- [X] T005 Implement open-mode probe in `AdminSessionProvider` in `frontend/src/AdminSessionContext.tsx`: call `probeAdminKeyRequired()` on mount, store result as `isAdminKeyRequired` state (default `false` while probing); expose `isDemoMode` as `!hasSession && isAdminKeyRequired`
- [X] T006 Add session expiry timer to `AdminSessionProvider` in `frontend/src/AdminSessionContext.tsx`: when a session is established, schedule a `setTimeout` to call `refresh()` at the exact `expiresAtUtc` timestamp; cancel prior timer in `useEffect` cleanup when `expiresAtUtc` changes

**Checkpoint**: Foundational complete — `useAdminSession()` now exposes `isDemoMode`; all demo data constants exist

---

## Phase 3: User Stories 1 + 2 — Browse Demo Pages + Banner (Priority: P1) 🎯 MVP

**Goal**: Unauthenticated visitors see all three admin pages populated with realistic mock data and a persistent banner directing them to authenticate.

**Independent Test**: Open the frontend without signing in (with admin key configured on backend). Navigate to Auth Settings, Monitoring, and Test Data tabs. Each should display a "Demo Mode" banner with realistic content in all fields, zero error states, and a working link to the Admin Access tab.

### Implementation

- [X] T007 [P] [US2] Create `frontend/src/DemoBanner.tsx` — presentational component accepting `onNavigateToAdmin: () => void` prop; renders a visually distinct banner containing a "Demo Mode" label, description text directing the user to authenticate, and a "Go to Admin access" CTA button that calls `onNavigateToAdmin`; uses Tailwind responsive classes (`flex-col` on mobile, `sm:flex-row`) so the banner is fully readable at 320px+ viewport widths
- [X] T008 [US2] Update `frontend/src/App.tsx` to import `DemoBanner` and `useAdminSession`; when `isDemoMode && ["auth", "monitoring", "testData"].includes(view)`, render `<DemoBanner onNavigateToAdmin={() => setView("admin")} />` above the active page's content in the main section; Admin Access tab (`view === "admin"`) must never render the banner
- [X] T009 [P] [US1] Add demo mode branch to `frontend/src/AuthSettingsPage.tsx`: when `isDemoMode` is true, render the form pre-populated from `DEMO_AUTH_SETTINGS` with all fields visible but with no-op `onSubmit` (`event.preventDefault()` + return) and empty `onClick` handlers on action buttons — no call to `getAuthSettings()` or `updateAuthSettings()` is made; existing live-mode behavior is unchanged
- [X] T010 [P] [US1] Add demo mode branch to `frontend/src/MonitoringPage.tsx`: when `isDemoMode` is true, render the request log list from `DEMO_MONITORING_SUMMARIES` and stats charts from `DEMO_MONITORING_STATS` — no call to `getMonitoredRequests()`, `getMonitoredRequest()`, or `getMonitoringStats()` is made; filter inputs and pagination controls render with no-op handlers; existing live-mode behavior is unchanged
- [X] T011 [P] [US1] Add demo mode branch to `frontend/src/TestDataPage.tsx`: when `isDemoMode` is true, render stats from `DEMO_TEST_DATA_STATS` and all generation/reset/lookup controls — no call to any test-data API function is made; all buttons (`Generate`, `Reset`, `Look Up`, `Add Patient`) have no-op `onClick` handlers and form submits call `event.preventDefault()` + return; existing live-mode behavior is unchanged

### Tests

- [X] T012 [P] [US2] Create `frontend/src/DemoBanner.test.tsx`: assert banner renders with visible "Demo Mode" text; assert CTA button is present; assert `onNavigateToAdmin` is called when CTA is clicked via `userEvent.setup()`; assert banner renders correctly at narrow viewport (set container width in test)
- [X] T013 [P] [US1] Add demo mode test cases to `frontend/src/AuthSettingsPage.test.tsx`: render with `isDemoMode=true` via `renderWithAdminSession` (extend helper to accept `isDemoMode`); assert form displays values from `DEMO_AUTH_SETTINGS`; assert no `api.getAuthSettings` call is made; assert no `api.updateAuthSettings` call is made when Save is clicked
- [X] T014 [P] [US1] Add demo mode test cases to `frontend/src/MonitoringPage.test.tsx`: render with `isDemoMode=true`; assert log entries from `DEMO_MONITORING_SUMMARIES` are present in the document; assert stats from `DEMO_MONITORING_STATS` are rendered; assert no `api.getMonitoredRequests` call is made
- [X] T015 [P] [US1] Add demo mode test cases to `frontend/src/TestDataPage.test.tsx`: render with `isDemoMode=true`; assert stats from `DEMO_TEST_DATA_STATS` are displayed; assert no `api.getPatientTestDataStats` call is made; assert page renders without errors

**Checkpoint**: All three admin pages show demo content; banner is visible and links to Admin Access; no backend requests generated from demo page views

---

## Phase 4: User Story 3 — Interactive Controls (Priority: P2)

**Goal**: Every button and input on demo pages is clickable and does not produce JavaScript errors or trigger network requests.

**Independent Test**: In demo mode, click every visible button on Auth Settings, Monitoring, and Test Data pages. The page must not navigate away, throw a React error, or show any error state. Network panel must show zero new requests.

### Tests

- [X] T016 [P] [US3] Add no-op button tests to `frontend/src/AuthSettingsPage.test.tsx`: in demo mode, click the "Save" button and any "Generate" / mode-switch control via `userEvent.setup()`; assert no React errors thrown, no API call made, page remains on Auth Settings view
- [X] T017 [P] [US3] Add no-op button tests to `frontend/src/MonitoringPage.test.tsx`: in demo mode, interact with filter inputs and any "Refresh" / "Load more" controls; assert no API call made, no errors thrown
- [X] T018 [P] [US3] Add no-op button tests to `frontend/src/TestDataPage.test.tsx`: in demo mode, click "Generate", "Reset", "Add Patient", and "Look Up" buttons; assert no API call made, no errors thrown, no navigation away from the page

**Checkpoint**: Interactive controls verified non-destructive in demo mode

---

## Phase 5: User Story 4 — Live Mode Transition (Priority: P2)

**Goal**: Pages switch seamlessly from demo to live mode on authentication, and revert to demo mode when the session expires.

**Independent Test**: Sign in via Admin Access while on a demo page. Without reloading, navigate back to Auth Settings — real settings load and the banner disappears. Wait for the session to expire (or fast-forward via fake timers) — the page reverts to demo mode within 3 seconds.

### Tests

- [X] T019 [US4] Add probe behavior tests to `frontend/src/AdminSessionContext` test file (create `frontend/src/AdminSessionContext.test.tsx`): use MSW handler returning `200` for `GET /api/v1/auth-settings` → assert `isDemoMode` is `false`; use MSW handler returning `401` → assert `isDemoMode` is `true`; simulate network error → assert `isDemoMode` is `true`
- [X] T020 [US4] Add session expiry timer test to `frontend/src/AdminSessionContext.test.tsx`: use Vitest fake timers (`vi.useFakeTimers()`); sign in with a short-lived token; advance time past expiry via `vi.advanceTimersByTime()`; assert `hasSession` becomes `false` and `isDemoMode` becomes `true`
- [X] T021 [P] [US4] Add live-mode transition test to `frontend/src/AuthSettingsPage.test.tsx`: start with `isDemoMode=true` (probe returns `401`); simulate sign-in setting a valid session; assert demo banner disappears and live `getAuthSettings` is called
- [X] T022 [P] [US4] Add Admin Access tab exclusion test to `frontend/src/App.test.tsx` (create if not exists): render `App` with `isDemoMode=true`; navigate to "admin" tab; assert `DemoBanner` is NOT rendered on the Admin Access tab

**Checkpoint**: Demo ↔ live mode transitions work correctly; expiry revert is covered by automated tests

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Mobile verification, documentation sync, and offline scenario validation

- [ ] T023 Manually verify `DemoBanner` and all three demo page variants render correctly at 375px viewport width (iPhone 14 size) in Chrome DevTools device emulation — banner must be fully readable without horizontal scroll
- [X] T024 [P] Update `README.md` to document demo mode: describe the unauthenticated experience, explain when demo mode is active vs suppressed (open mode), and note the offline resilience behavior — required by constitution §Development Workflow
- [ ] T025 Run the complete quickstart validation in `specs/002-frontend-demo-mode/quickstart.md` — execute all three scenarios (demo active, open mode, backend offline) and confirm each behaves as documented
- [X] T026 Run `npm run lint` and `npm test` in `frontend/` with all changes applied; confirm zero lint errors and all tests pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS all user story phases**
- **Phase 3 (US1+US2, P1)**: Depends on Phase 2 completion
- **Phase 4 (US3, P2)**: Depends on Phase 3 completion (tests build on Phase 3 components)
- **Phase 5 (US4, P2)**: Depends on Phase 2 completion (probe + timer); can run in parallel with Phase 3/4 for tests, but full integration depends on Phase 3
- **Phase 6 (Polish)**: Depends on all prior phases

### User Story Dependencies

| Story | Depends on | Can parallel with |
|-------|-----------|------------------|
| US1+US2 (Phase 3) | Phase 2 | — |
| US3 (Phase 4) | Phase 3 | US4 tests (Phase 5 T019–T020) |
| US4 (Phase 5) | Phase 2 + Phase 3 | Phase 4 |

### Within Each Phase

- T002 and T003 can run in parallel (different files)
- T004, T005, T006 must run sequentially (each builds on the previous)
- T007 can start as soon as Phase 2 completes (new file, no conflicts)
- T008 depends on T007 (needs DemoBanner to exist)
- T009, T010, T011 depend on T007 and T008; can run in parallel with each other
- T012–T015 can run in parallel with each other; each depends only on its corresponding implementation task
- T016–T018 can run in parallel; depend on T009–T011 respectively
- T019–T022 can run in parallel after Phase 2 + Phase 3 complete

### Parallel Opportunities

```bash
# Phase 2: run simultaneously
T002  # api.ts: probeAdminKeyRequired
T003  # demoData.ts: all mock constants

# Phase 3 implementation: run simultaneously after T008
T009  # AuthSettingsPage.tsx demo branch
T010  # MonitoringPage.tsx demo branch
T011  # TestDataPage.tsx demo branch

# Phase 3 tests: run simultaneously after respective implementations
T012  # DemoBanner.test.tsx
T013  # AuthSettingsPage.test.tsx demo cases
T014  # MonitoringPage.test.tsx demo cases
T015  # TestDataPage.test.tsx demo cases

# Phase 4 + Phase 5 mixed parallel
T016  # AuthSettingsPage no-op button tests
T017  # MonitoringPage no-op button tests
T018  # TestDataPage no-op button tests
T019  # AdminSessionContext probe tests
T020  # AdminSessionContext expiry timer tests
```

---

## Implementation Strategy

### MVP First (US1 + US2 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T006)
3. Complete Phase 3: US1+US2 (T007–T015)
4. **STOP and VALIDATE**: Confirm demo pages render with mock data and banner is visible
5. Deploy/demo — public visitors can browse the demo interface

### Incremental Delivery

1. Phase 1 + Phase 2 → probe, demo data, and context ready
2. Phase 3 → three demo pages + banner functional (**MVP delivered**)
3. Phase 4 → button interaction explicitly tested
4. Phase 5 → live-mode transitions and session expiry covered
5. Phase 6 → mobile verified, documentation updated, all tests green

### Solo Developer Sequence

For a single developer, recommended order:
T001 → T002+T003 (parallel) → T004 → T005 → T006 → T007 → T008 → T009+T010+T011 (parallel) → T012+T013+T014+T015 (parallel) → T016+T017+T018 (parallel) → T019+T020+T021+T022 (parallel) → T023 → T024 → T025 → T026

---

## Notes

- `[P]` tasks operate on different files with no shared write dependencies — safe to parallelize
- `[Story]` labels map to user stories in `spec.md` for full traceability
- The `renderWithAdminSession` test helper in `frontend/src/test/renderWithAdminSession.tsx` must be extended to accept `isDemoMode` and `isAdminKeyRequired` overrides so demo mode can be simulated in component tests without a real probe call
- MSW handlers for `GET /api/v1/auth-settings` returning `401` and `200` must be added to test files that test probe behavior (T019) — do not add them globally to `server.ts` as they are scenario-specific
- Vitest fake timers (`vi.useFakeTimers()` / `vi.useRealTimers()`) must be properly torn down in `afterEach` hooks to avoid leaking into other tests
- The expiry timer in `AdminSessionContext` must guard against scheduling a `setTimeout` with a negative or zero delay (token already expired on mount)
