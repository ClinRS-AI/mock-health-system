# Implementation Plan: Test Data Management Dashboard Reorganization

**Branch**: `feature/005-test-data-dashboard` | **Date**: 2026-07-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/feature/005-test-data-dashboard/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Reorganize the existing `TestDataPage.tsx` (1 flat, ~1,270-line page) into four tabbed areas — Data Counts and Visualizations, Data Generation, Data Manipulation, and Information and Destruction — with no backend or API changes. All existing state, handlers, and API calls are redistributed unchanged into per-area components; the only new behavior is (1) tab-based navigation, (2) lightweight Recharts-based visual elements for the count breakdowns, and (3) an explicit confirmation step before the two destructive reset actions.

## Technical Context

**Language/Version**: TypeScript (strict mode), React 19, Vite 6

**Primary Dependencies**: React, Tailwind CSS (utility classes only), Recharts 3.7 (already a project dependency, used in `MonitoringPage.tsx`), Axios (via `src/api.ts`)

**Storage**: N/A — no new persistence; reuses existing backend endpoints unchanged

**Testing**: Vitest + React Testing Library v16 + `@testing-library/user-event` v14, MSW v2 for API interception (per constitution Principle III/VII)

**Target Platform**: Browser (admin SPA), part of the existing `frontend/` Vite app

**Project Type**: Web application (frontend-only change; backend untouched)

**Performance Goals**: No regression vs. current page. Each section component fetches its own data on mount, consistent with the existing `App.tsx` top-level nav pattern (Decision 1, research.md) — switching away from and back to a tab is expected to re-fetch that area's data, exactly as revisiting "Monitoring" or another top-level nav item does today. Within a single mount, initial-load stats fetches remain concurrent (already fire-and-forget in parallel today).

**Constraints**: No new frontend dependencies (Recharts is already present); no changes to `src/api.ts` function signatures, request/response shapes, or backend endpoints (FR-017); must preserve demo-mode behavior exactly (FR-018)

**Scale/Scope**: Single admin page; splits 1 monolithic component into 1 orchestrator + 4 area components (plus their colocated tests), touching only `frontend/src/`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle VI (Code Quality) — Frontend**: PASS. No new API calls are added, so no new functions are needed in `api.ts`; existing typed functions are reused as-is. All new components use TypeScript strict mode, Tailwind-only styling (matching the existing `TestDataPage.tsx` class patterns), and `interface`/`type` per existing conventions.
- **Principle VII (Testing Standards) — Frontend**: PASS. New area components get colocated `*.test.tsx` files (e.g., `TestDataGenerationSection.test.tsx` next to `TestDataGenerationSection.tsx`), using Vitest + RTL + `userEvent.setup()`, asserting on user-visible text/roles per existing `TestDataPage.test.tsx` patterns. Existing `TestDataPage.test.tsx` scenarios are redistributed to the new colocated test files rather than deleted.
- **Principle VIII (UX Consistency)**: PASS, with one active decision — **Data visualization MUST use Recharts with `ResponsiveContainer`** (constitution, Principle VIII) governs FR-005. Recharts is already a project dependency (used in `MonitoringPage.tsx`'s `PieChart`), so satisfying this principle does not conflict with the spec's "no new charting dependency" intent — see research.md for the reconciliation. Granular per-operation loading booleans are preserved (they already exist per-handler in the current code and are carried over unchanged into their respective area components).
- **Principle X (Performance)**: PASS. The three initial stat/pkey loads (`loadStats`, `loadSoapPkeys`, `loadStudiesStats`) already fire concurrently (unawaited calls in sequence); this plan preserves that behavior. Tab switching must not re-trigger these loads (state lives in the orchestrator or is fetched once), avoiding redundant requests — captured as a task-level requirement.
- **Principle IX (Security)**: N/A — no new admin routes, no new auth surface.
- **Documentation sync (Development Workflow)**: The constitution requires README.md/API-CONNECT.md updates for changes to "frontend tab structure." This feature introduces a new tab-like structure *within* the Test Data page (not the top-level app nav), but README.md's admin-UI walkthrough (if it references the Test Data page layout) must be checked and updated during implementation — captured as a task.

No violations requiring justification. Complexity Tracking section is not needed.

**Post-Design Re-check** (after Phase 0/1 artifacts): Confirmed PASS on all gates above. research.md Decision 2 resolved the one open question (Recharts usage) without introducing a new dependency or contradicting the spec's clarification; data-model.md's per-area `error` state split further strengthens Principle VIII compliance (each area's errors are now independently scoped, correcting a pre-existing minor gap where a single global `error` state spanned unrelated handlers). No new violations were introduced during design.

## Project Structure

### Documentation (this feature)

```text
specs/feature/005-test-data-dashboard/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/            # Phase 1 output (/speckit-plan command)
│   └── test-data-page-areas.md
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── TestDataPage.tsx                        # becomes the tab orchestrator (nav + shared layout)
│   ├── TestDataPage.test.tsx                    # trimmed to orchestrator/navigation-level tests
│   ├── TestDataCountsSection.tsx                # NEW — Data Counts and Visualizations area
│   ├── TestDataCountsSection.test.tsx           # NEW
│   ├── TestDataGenerationSection.tsx            # NEW — Data Generation area
│   ├── TestDataGenerationSection.test.tsx       # NEW
│   ├── TestDataManipulationSection.tsx          # NEW — Data Manipulation area
│   ├── TestDataManipulationSection.test.tsx     # NEW
│   ├── TestDataInfoDestructionSection.tsx       # NEW — Information and Destruction area
│   ├── TestDataInfoDestructionSection.test.tsx  # NEW
│   ├── api.ts                                    # UNCHANGED (all functions already exist)
│   └── demoData.ts                               # UNCHANGED
backend/                                          # UNTOUCHED by this feature
```

**Structure Decision**: Flat `frontend/src/` layout matching the existing project convention (no `components/` subdirectory is used anywhere in this codebase). `TestDataPage.tsx` is kept as the entry point referenced by `App.tsx` and becomes a thin orchestrator holding the active-tab state and rendering one of the four new sibling section components, each colocated with its own test file per Principle VII.

## Complexity Tracking

*No Constitution Check violations — this section is not applicable.*
