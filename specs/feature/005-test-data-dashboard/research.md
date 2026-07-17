# Research: Test Data Management Dashboard Reorganization

No `NEEDS CLARIFICATION` markers remain in the Technical Context — the two spec-level ambiguities (navigation pattern, visualization approach) were already resolved with the user during `/speckit-specify`. This document records the additional implementation-level decisions made while reconciling those choices with the existing codebase and constitution.

## Decision 1: Tab navigation implementation

**Decision**: Reuse the exact pattern already established in `App.tsx` for top-level navigation — a local `activeTab` state variable (string union type), a `<nav>` row of `<button>` elements with conditional Tailwind classes (`bg-sky-50 text-sky-700 border border-sky-200` when active, `text-slate-600 hover:bg-slate-50 border border-transparent` otherwise), and conditional rendering (`{activeTab === "counts" && <TestDataCountsSection ... />}`).

**Rationale**: The codebase has zero existing usage of ARIA `role="tab"`/`tablist` patterns or any tab UI library. `App.tsx`'s `view` state + button row is the only precedent for "switch between top-level sections" in this app, and it is simple, dependency-free, and already styled consistently with Tailwind. Reusing it exactly avoids introducing a second, inconsistent navigation idiom and requires no new dependency.

**Alternatives considered**:
- A UI library tab component (e.g., Headless UI, Radix) — rejected: not an existing dependency, adds bundle weight and a new pattern for a single page.
- Routed sections (React Router) — rejected: the app has no router today (`App.tsx` uses plain state), and the spec's clarification explicitly chose tabs over "persistent side/top navigation with routed sections."

## Decision 2: Recharts for FR-005 visual elements, reconciling with the spec's "no new dependency" intent

**Decision**: Implement the Data Counts and Visualizations area's breakdown lists (patients by site, studies by status) as small Recharts charts (a compact `PieChart` or horizontal `BarChart`, each wrapped in `ResponsiveContainer`, mirroring the exact import/usage pattern already in `MonitoringPage.tsx`), plus plain icon-accented stat cards (existing Tailwind card pattern, no charting needed) for single-number totals (patient count, study count, staff count, etc.).

**Rationale**: Constitution Principle VIII states data visualization **MUST use Recharts with `ResponsiveContainer`** — this is a hard gate, not a style preference. Recharts (`^3.7.0`) is already a `frontend/package.json` dependency and already used in `MonitoringPage.tsx`, so using it here does not introduce a *new* dependency — it reuses one already in the bundle. This satisfies both the constitutional mandate and the spec's clarification intent (the user chose "lightweight visual elements, no new charting dependency" specifically over "introduce a charting library," and Recharts is not being introduced — it's already there). Scope stays lightweight: only the two categorical breakdowns get small charts; simple totals stay as number+icon cards, avoiding a "dashboard of charts" that would exceed what the user asked for.

**Alternatives considered**:
- Hand-rolled CSS proportion bars (`<div style={{ width: '${pct}%' }} />`) — rejected: would violate Principle VIII's Recharts mandate and diverge from the one existing visualization precedent in the codebase (inline dynamic `style` width is also inconsistent with the "no inline styles except dynamic values" rule — defensible, but still adds a second, unprecedented visualization idiom next to an existing correct one).
- A new, different charting library — rejected: would be a genuinely new dependency, contradicting both the constitution's specific Recharts mandate and the user's stated preference to avoid new dependencies.

## Decision 3: Confirmation step for destructive resets (FR-015)

**Decision**: Use a simple two-click confirm pattern local to each reset button (e.g., clicking "Reset patient data" turns the button into a "Confirm reset?" state for a few seconds, or reveals an inline "Cancel / Yes, reset" pair) rather than a browser-native `window.confirm()` dialog or a modal component.

**Rationale**: No modal/dialog component exists anywhere in this codebase today (grep for `role="dialog"`, `<Modal`, etc. returns nothing), so introducing one would add a new UI pattern for a single use case. `window.confirm()` is trivially implementable with zero dependencies and is a well-understood, testable (mockable in Vitest/RTL) pattern, but blocks the JS thread and looks visually inconsistent with the rest of the Tailwind-styled UI. An inline two-step button (no new component, styled consistently with existing buttons, easily testable via RTL `getByRole("button", { name: /confirm/i })`) best matches the codebase's existing "no new dependency, Tailwind-only" conventions while still satisfying FR-015's "explicit confirmation step" requirement. The exact micro-interaction (timed revert vs. explicit cancel button) is left to implementation; either satisfies the functional requirement.

**Alternatives considered**:
- `window.confirm()` — rejected as primary choice: functionally sufficient but not visually/UX-consistent with the rest of the page, and harder to style/test than an inline element; kept as a fallback if the two-step button proves awkward for a given layout.
- A shared `<ConfirmDialog>` modal component — rejected: over-engineered for two buttons on one page; would be the first modal in the codebase, adding a new pattern for minimal reuse.

## Decision 4: Component split and state ownership

**Decision**: Split `TestDataPage.tsx` into 4 new sibling components (`TestDataCountsSection`, `TestDataGenerationSection`, `TestDataManipulationSection`, `TestDataInfoDestructionSection`), each owning its own local state (loading/error/result/form state for the handlers that live in that area). `TestDataPage.tsx` becomes an orchestrator that owns only `activeTab` state and passes down the two pieces of state genuinely shared across areas: the demo-mode/session flags (from `useAdminSession()`, already a shared hook) and nothing else — stats data is fetched independently by `TestDataCountsSection` itself rather than lifted to the orchestrator, since no other area reads it.

**Rationale**: Reviewing the current handler list, state is already naturally partitioned by area with no cross-area reads: generation handlers/state (`handleGenerate`, `handleGenerateStudies`, `handleGenerateStaff`, `handleGenerateRecentAuditEvents`, `handleAddPatient` and their state) are only used by generation UI; lookup/edit handlers/state (`handleLookupPatient`, `handleLookupStudy`, `handleGetRandomPatient`, `handleGetRandomStudy`, `handleSavePatientRecord` and their state) are only used by manipulation UI; `stats`/`studiesStats` are only rendered in the counts area; `soapPkeys`/URLs and `handleReset`/`handleResetStudies` are only used in the info/destruction area. This confirms FR-002's "no functionality in more than one area" is achievable with clean component boundaries and no prop-drilling of unrelated state.

**Alternatives considered**:
- Keep one large component with conditional rendering blocks (no file split) — rejected: does not address the underlying clutter (a single 1,270-line file remains hard to navigate and test), and Principle VII expects colocated, focused test files per component.
- Lift all state to `TestDataPage.tsx` and pass everything down as props — rejected: unnecessary prop-drilling since no area needs another area's state; increases re-render surface for no benefit.

**Note on the current shared `error` state**: Today, a single `error` state variable is written by all 12 handlers (reset, generate, lookup, add, save — spanning what will become three different areas) and rendered once near the top of the page. This must be split into one `error` state per new section component so that each area displays its own errors independently, consistent with FR-002 ("no functionality in more than one area") and Principle VIII's per-operation state granularity. This is a behavior-preserving refactor (each handler's error message and trigger conditions are unchanged — only which component's local state holds it changes) and is called out explicitly as a task-level concern, not a functional change.

## Decision 5: Test redistribution strategy

**Decision**: Move each existing `describe`/`it` block in `TestDataPage.test.tsx` to the new colocated test file matching where its rendered content now lives (e.g., the "Studies section" `describe` block's generate/reset/lookup tests split across `TestDataGenerationSection.test.tsx`, `TestDataInfoDestructionSection.test.tsx`, and `TestDataManipulationSection.test.tsx` respectively). `TestDataPage.test.tsx` keeps only orchestration-level tests: that all four tab labels render, that clicking a tab shows that area's content and hides the others, and demo-mode/session-gating smoke tests.

**Rationale**: Matches Principle VII's "test files MUST be colocated with their component" and preserves 100% existing test coverage (no scenario is dropped) while giving each new component focused, fast-to-locate tests.

**Alternatives considered**: Leave all tests in one file, importing the orchestrator and drilling into tabs for every scenario — rejected: contradicts the colocation principle and keeps the same "hard to navigate" problem in test code that this feature is fixing in component code.
