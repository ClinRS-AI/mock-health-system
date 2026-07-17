---

description: "Task list template for feature implementation"
---

# Tasks: Test Data Management Dashboard Reorganization

**Input**: Design documents from `specs/feature/005-test-data-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/test-data-page-areas.md, quickstart.md

**Tests**: Included — the project constitution (Principle VII) mandates Vitest + React Testing Library coverage colocated with every component; this is not optional for this codebase.

**Organization**: Tasks are grouped by user story (mapping 1:1 to the four tabbed areas) to enable independent implementation and testing of each area.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- File paths are exact and relative to the repository root

## Path Conventions

This is the existing `frontend/` React + Vite app. All new files are flat under `frontend/src/`, matching the codebase's existing convention (no `components/` subdirectory exists anywhere today). No backend changes.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the four-tab navigational shell that every user story's content will be migrated into.

- [ ] T001 [P] Create component shell `frontend/src/TestDataCountsSection.tsx` — typed `React.FC`, default export, empty content (real content added in Phase 3).
- [ ] T002 [P] Create component shell `frontend/src/TestDataGenerationSection.tsx` — typed `React.FC`, default export, empty content (real content added in Phase 4).
- [ ] T003 [P] Create component shell `frontend/src/TestDataManipulationSection.tsx` — typed `React.FC`, default export, empty content (real content added in Phase 5).
- [ ] T004 [P] Create component shell `frontend/src/TestDataInfoDestructionSection.tsx` — typed `React.FC`, default export, empty content (real content added in Phase 6).
- [ ] T005 Wire the tab orchestrator in `frontend/src/TestDataPage.tsx` (depends on T001-T004): add `activeTab` state typed `"counts" | "generation" | "manipulation" | "info"` (default `"counts"`), a `<nav>` tab-button row reusing the exact conditional-className pattern from `frontend/src/App.tsx`'s top-level nav (`bg-sky-50 text-sky-700 border border-sky-200` when active, `text-slate-600 hover:bg-slate-50 border border-transparent` otherwise), and conditional rendering of the four shell components from T001-T004. Keep `<AdminSessionBanner />` rendered above the tabs, unconditionally. Remove nothing else from the file yet.

**Checkpoint**: The page shows four clickable tabs; clicking each shows an empty area. Nothing is broken, but no functionality has moved yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the pre-refactor baseline and prove the tab shell itself is correct before any story migrates real content into it — every story's acceptance scenarios assume "Given the admin is in the [area]" already works.

**⚠️ CRITICAL**: No user story content-migration may begin until this phase is complete.

- [ ] T006 Record the pre-refactor baseline: run `npm run lint` and `npm run test` in `frontend/` against the current (pre-Phase-1) `TestDataPage.tsx`/`TestDataPage.test.tsx`; confirm both are green and note the passing test count, so regressions introduced during extraction are attributable to this feature.
- [ ] T007 Rewrite `frontend/src/TestDataPage.test.tsx` to orchestration-only scope (per research.md Decision 5): assert all four tab labels ("Data Counts and Visualizations", "Data Generation", "Data Manipulation", "Information and Destruction") render, clicking each tab shows only that area's content and hides the other three, and the tab row itself renders correctly in both demo mode and authenticated mode. These tests must pass against the Phase 1 skeleton.

**Checkpoint**: Tab navigation is proven correct and tested. User story content-migration can now begin, in any order or in parallel (subject to the shared-file note in Dependencies below).

---

## Phase 3: User Story 1 - See data counts and visualizations at a glance (Priority: P1) 🎯 MVP

**Goal**: Deliver a dedicated, visually clear overview of patient and study counts, replacing plain-text breakdown lists with Recharts-based visual elements.

**Independent Test**: Open the Data Counts and Visualizations tab; confirm patient and study stat cards render with visual (chart) breakdowns, zero-state renders cleanly, and the refresh control reloads data without a full page reload — all independent of any generation or manipulation action ever being taken.

### Implementation for User Story 1

- [ ] T008 [US1] Move `stats`, `studiesStats` state, the `loadStats`/`loadStudiesStats` functions, and the mount-time `useEffect` (gated on `hasSession`/`isDemoMode`/`isProbeSettled`, with `DEMO_TEST_DATA_STATS` fallback in demo mode) from `frontend/src/TestDataPage.tsx` into `frontend/src/TestDataCountsSection.tsx`. Add a local `error` state scoped to this component only (per data-model.md's per-area error split — do not reuse the old shared `error` variable).
- [ ] T009 [US1] Implement the patient stat cards (patient count, duplicate patient count, recent audit event count, total staff count) and study stat cards (study count, arm count, visit count) in `frontend/src/TestDataCountsSection.tsx`, reusing the existing Tailwind card markup (`rounded-lg border border-slate-200 bg-white p-4`) from the original file.
- [ ] T010 [US1] Implement the "patients by site" and "studies by status" breakdowns as small Recharts charts in `frontend/src/TestDataCountsSection.tsx` — import `PieChart`, `Pie`, `Cell`, `ResponsiveContainer`, `Legend`, `Tooltip` from `recharts` mirroring the exact pattern in `frontend/src/MonitoringPage.tsx`, replacing the current plain `<ul>` lists (FR-005). Empty data must render an empty-state message, not an empty/broken chart (Edge Case, AC2).
- [ ] T011 [US1] Add a "Refresh stats" control in `frontend/src/TestDataCountsSection.tsx` that re-invokes both `loadStats()` and `loadStudiesStats()` without a full page reload (FR-006, AC3).
- [ ] T012 [US1] Remove the migrated stats state, handlers, and JSX (patient stats grid, study stats grid, and the standalone "Test data management" header/refresh-stats block) from `frontend/src/TestDataPage.tsx`; update the orchestrator's "counts" tab to render the now-complete `TestDataCountsSection`.
- [ ] T013 [P] [US1] Write `frontend/src/TestDataCountsSection.test.tsx` covering: stats render with a visual element alongside numeric values (AC1); zero/empty data renders an empty-state message, not an error (AC2); clicking Refresh reloads counts without a full page reload (AC3); demo mode renders `DEMO_TEST_DATA_STATS` without making a live API call.

**Checkpoint**: The Data Counts and Visualizations tab is fully functional, tested, and independently verifiable per quickstart.md step 3.

---

## Phase 4: User Story 2 - Generate test data in one focused area (Priority: P2)

**Goal**: Consolidate every data-creation action (bulk patients, bulk studies, staff, audit events, manual single patient) into one area with no destructive controls present.

**Independent Test**: Open the Data Generation tab; successfully generate bulk patients, bulk studies, staff, recent audit events, and a manual single patient, each producing its own result summary — with no reset or lookup/edit control visible anywhere in the tab.

### Implementation for User Story 2

- [ ] T014 [US2] Move `generateOptions`, `generateResult`, `loadingGenerate` state and the `handleGenerate` function (bulk patient generation) from `frontend/src/TestDataPage.tsx` into `frontend/src/TestDataGenerationSection.tsx`, adding a local `error` state scoped to this component.
- [ ] T015 [US2] Move `studiesGenerateOptions`, `studiesGenerateResult`, `loadingGenerateStudies` state and `handleGenerateStudies` (bulk study generation) into `frontend/src/TestDataGenerationSection.tsx`.
- [ ] T016 [US2] Move `staffGenerateOptions`, `staffGenerateResult`, `loadingGenerateStaff` state and `handleGenerateStaff` into `frontend/src/TestDataGenerationSection.tsx`.
- [ ] T017 [US2] Move `auditEventsGenerateOptions`, `auditEventsGenerateResult`, `loadingGenerateAuditEvents` state and `handleGenerateRecentAuditEvents` into `frontend/src/TestDataGenerationSection.tsx`.
- [ ] T018 [US2] Move `addForm`, `addResult`, `loadingAdd` state and `handleAddPatient` (manual single-patient creation form) into `frontend/src/TestDataGenerationSection.tsx`.
- [ ] T019 [US2] Remove the migrated generation state, handlers, and JSX (bulk patient form, bulk study form, staff form, audit-events form, manual-add-patient form) from `frontend/src/TestDataPage.tsx`; update the orchestrator's "generation" tab to render the now-complete `TestDataGenerationSection`.
- [ ] T020 [P] [US2] Write `frontend/src/TestDataGenerationSection.test.tsx` covering all 5 acceptance scenarios: bulk patient generation shows a result summary (AC1); bulk study generation shows a result summary (AC2); staff and audit-event generation each show independent result summaries (AC3); manual patient creation shows returned id/UID (AC4); assert no reset/destructive control is rendered anywhere in this component (AC5); and in demo mode, all generation controls remain disabled/inert and no live API call fires (FR-018).

**Checkpoint**: The Data Generation tab is fully functional, tested, and independently verifiable per quickstart.md step 4. User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - View and edit specific records (Priority: P3)

**Goal**: Consolidate patient and study lookup/detail/edit into one area, separate from bulk generation and destructive actions.

**Independent Test**: Open the Data Manipulation tab; look up a patient by ID/UID/email or at random, view and edit its full detail and save; separately look up a study by name/identifier/protocol number or at random and view its full detail.

### Implementation for User Story 3

- [ ] T021 [US3] Move `lookupForm`, `lookupResult`, `lookupNotFound`, `loadingLookup` state and `handleLookupPatient`/`handleGetRandomPatient` from `frontend/src/TestDataPage.tsx` into `frontend/src/TestDataManipulationSection.tsx`, adding a local `error` state scoped to this component.
- [ ] T022 [US3] Move `isEditingLookup`, `lookupEditJson`, `savingLookupMode` state and `handleSavePatientRecord` (view/edit/save/save-with-audit for the looked-up patient) into `frontend/src/TestDataManipulationSection.tsx`.
- [ ] T023 [US3] Move `studiesLookupForm`, `studiesLookupResult`, `studiesLookupNotFound`, `loadingStudiesLookup` state and `handleLookupStudy`/`handleGetRandomStudy` into `frontend/src/TestDataManipulationSection.tsx`.
- [ ] T024 [US3] Remove the migrated lookup/edit state, handlers, and JSX (the "Lookup patient" details block including edit/save controls, and the "Lookup study" details block) from `frontend/src/TestDataPage.tsx`; update the orchestrator's "manipulation" tab to render the now-complete `TestDataManipulationSection`.
- [ ] T025 [P] [US3] Write `frontend/src/TestDataManipulationSection.test.tsx` covering all 4 acceptance scenarios: patient lookup shows full details or a not-found message (AC1); editing and saving a patient record persists and is reflected in the display (AC2); study lookup shows full details or a not-found message (AC3); "get random" returns a randomly selected patient or study (AC4); assert no bulk-generation or reset control is rendered anywhere in this component (FR-012); and in demo mode, all lookup/edit controls remain disabled/inert and no live API call fires (FR-018).

**Checkpoint**: The Data Manipulation tab is fully functional, tested, and independently verifiable per quickstart.md step 5. User Stories 1, 2, and 3 all work independently.

---

## Phase 6: User Story 4 - View connection info and safely reset data in an isolated danger zone (Priority: P4)

**Goal**: Consolidate connection/instance information with an isolated, confirmation-gated "danger zone" holding both destructive reset actions.

**Independent Test**: Open the Information and Destruction tab; confirm connection info (API base, SOAP endpoint/WSDL, report pkeys) displays with working copy controls; confirm resetting patient data or study data requires an explicit confirmation step and clears only the targeted domain's data.

### Implementation for User Story 4

- [ ] T026 [US4] Move `soapPkeys`, `soapPkeysError`, `loadingSoapPkeys` state, the `loadSoapPkeys` function, the derived `apiBaseUrl`/`versionedJsonBase`/`soapPostUrl`/`soapWsdlUrl` values, and the `copyToClipboard` helper from `frontend/src/TestDataPage.tsx` into `frontend/src/TestDataInfoDestructionSection.tsx`.
- [ ] T027 [US4] Move `loadingReset` state and `handleReset` (reset patient data) into `frontend/src/TestDataInfoDestructionSection.tsx`. Add a new `resetConfirming` boolean state implementing the two-step inline confirm interaction from research.md Decision 3 (first click reveals a confirm/cancel affordance; only the confirm click invokes `handleReset`) — satisfies FR-015's "explicit confirmation step before the destructive request is sent."
- [ ] T028 [US4] Move `loadingResetStudies` state and `handleResetStudies` (reset study data) into `frontend/src/TestDataInfoDestructionSection.tsx`, adding an analogous `resetStudiesConfirming` boolean state with the same two-step confirm interaction.
- [ ] T029 [US4] Build a visually separated "danger zone" sub-section in `frontend/src/TestDataInfoDestructionSection.tsx` (e.g., a distinctly bordered/colored container, matching existing `rose`-accent button styling already used for the reset buttons) containing both reset controls, clearly demarcated from the connection-info content rendered above it (FR-014).
- [ ] T030 [US4] Remove the migrated connection-info/reset state, handlers, and JSX (the "API & SOAP (this instance)" block, "Reset patients" block, "Reset study data" block) from `frontend/src/TestDataPage.tsx`; update the orchestrator's "info" tab to render the now-complete `TestDataInfoDestructionSection`. After this task, `frontend/src/TestDataPage.tsx` contains only the orchestrator shell — no leftover migrated content.
- [ ] T031 [P] [US4] Write `frontend/src/TestDataInfoDestructionSection.test.tsx` covering all 5 acceptance scenarios: connection info and SOAP pkeys display with copy controls (AC1); clicking "Reset patient data" requires confirmation before the request fires (AC2); confirming a patient reset clears patient data only (AC3, assert the request payload/endpoint targets patients only); confirming a study reset clears study data only (AC4); assert no generation or lookup control is rendered anywhere in this component (AC5); and in demo mode, both reset controls remain disabled/inert and no live API call fires (FR-018).

**Checkpoint**: All four areas are fully functional, tested, and independently verifiable. `TestDataPage.tsx` is now purely an orchestrator. SC-002 (100% of prior functionality present, each in exactly one area) should hold.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validate the feature end-to-end against the spec's non-functional requirements and keep project documentation in sync.

- [ ] T032 [P] Verify mobile-viewport usability (SC-005, spec.md Edge Cases): resize to a narrow viewport (e.g., 375px) and confirm all four tabs remain reachable and usable; adjust Tailwind responsive classes on the tab nav in `frontend/src/TestDataPage.tsx` if needed (e.g., `flex flex-wrap gap-3`, matching `App.tsx`'s existing responsive nav pattern).
- [ ] T033 Update `README.md` and `API-CONNECT.md` per the constitution's documentation-sync rule (Development Workflow) if either references the Test Data page's prior single-section layout or frontend tab structure.
- [ ] T034 Run the full `frontend/` suite (`npm run lint`, `npm run test`) and confirm zero regressions: all scenarios present in the original `TestDataPage.test.tsx` (per T006's baseline count) now pass from their new colocated files (SC-002).
- [ ] T035 Execute the full manual walkthrough in `specs/feature/005-test-data-dashboard/quickstart.md` (all 9 steps) against a running dev server (`npm run dev`), including the demo-mode (step 7) and mobile-viewport (step 8) checks.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. T001-T004 are parallel; T005 depends on all four.
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories.
- **User Stories (Phase 3-6)**: All depend on Foundational (Phase 2) completion. Each story's component-file tasks (e.g., T008-T011, T013) are independent of the other stories' component files and can proceed in parallel.
- **Polish (Phase 7)**: Depends on all four user stories being complete.

### Shared-file constraint (important)

T012, T019, T024, and T030 (one per story) all edit the same shared file, `frontend/src/TestDataPage.tsx`, to remove that story's migrated content and wire its tab. While the bulk of each story's work (its own section component + test file) is fully parallelizable across stories, these four specific cleanup tasks must be serialized relative to each other (done one at a time, in any order) to avoid merge conflicts on the shared orchestrator file.

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories.
- **User Story 2 (P2)**: No dependencies on other stories.
- **User Story 3 (P3)**: No dependencies on other stories.
- **User Story 4 (P4)**: No dependencies on other stories.

All four stories read/write entirely disjoint state and handlers (confirmed in data-model.md); none needs another story's component to exist to be independently correct and testable.

### Within Each User Story

- State/handler-move tasks precede the "remove from TestDataPage.tsx + wire tab" task.
- The component test task ([P]) can be written in parallel with the move tasks, but should assert against the final migrated component, so treat it as the last task completed within the story even though it has no file-level dependency on the others.

### Parallel Opportunities

- T001-T004 (Phase 1) in parallel.
- Once Phase 2 completes, all four stories' component-file work (T008-T011/T013, T014-T018/T020, T021-T023/T025, T026-T029/T031) can proceed in parallel across different developers/sessions — only the four TestDataPage.tsx cleanup tasks need serializing.
- T013, T020, T025, T031 (the four test-file tasks) are each independently parallel to everything except their own story's move tasks.
- T032 and T033 (Phase 7) are independent of each other.

---

## Parallel Example: Phase 1 Setup

```bash
# Launch all four component shells together:
Task: "Create component shell frontend/src/TestDataCountsSection.tsx"
Task: "Create component shell frontend/src/TestDataGenerationSection.tsx"
Task: "Create component shell frontend/src/TestDataManipulationSection.tsx"
Task: "Create component shell frontend/src/TestDataInfoDestructionSection.tsx"
```

## Parallel Example: User Stories (post-Foundational)

```bash
# With four developers, each can own one story's component + test file end to end:
Developer A: T008, T009, T010, T011, T013 (US1) — then serialize T012 with the others
Developer B: T014, T015, T016, T017, T018, T020 (US2) — then serialize T019 with the others
Developer C: T021, T022, T023, T025 (US3) — then serialize T024 with the others
Developer D: T026, T027, T028, T029, T031 (US4) — then serialize T030 with the others
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (four-tab shell, all empty).
2. Complete Phase 2: Foundational (baseline + orchestration tests).
3. Complete Phase 3: User Story 1 (Data Counts and Visualizations).
4. **STOP and VALIDATE**: Run `TestDataCountsSection.test.tsx`, verify quickstart.md step 3 manually. The other three tabs remain empty placeholders at this point — that is expected and non-breaking, since no pre-existing functionality has been removed yet (it still lives, un-migrated, until each story's own phase runs).

### Incremental Delivery

1. Setup + Foundational → four-tab shell ready.
2. Add User Story 1 → validate independently (MVP).
3. Add User Story 2 → validate independently.
4. Add User Story 3 → validate independently.
5. Add User Story 4 → validate independently. At this point `TestDataPage.tsx` is a pure orchestrator and SC-002 holds (100% of original functionality relocated).
6. Phase 7: Polish, doc-sync, full regression pass, quickstart walkthrough.

### Parallel Team Strategy

With multiple developers, after Phase 2 completes, each developer can own one story's component and test file (see Parallel Example above); only the four short `TestDataPage.tsx` cleanup edits need to be coordinated/serialized, since all four target the same file.

---

## Notes

- [P] tasks touch different files with no dependencies on incomplete tasks.
- [Story] label maps each task to its user story for traceability against spec.md's acceptance scenarios.
- No backend changes exist in this task list — this is a frontend-only reorganization (FR-017); do not add backend tasks.
- Every "move" task is a pure relocation of existing, working code — no new business logic is introduced except the FR-005 chart rendering (T010) and the FR-015 confirm-before-reset interaction (T027, T028), both of which are called out explicitly.
- Commit after each task or logical group (per the existing session's git workflow).
