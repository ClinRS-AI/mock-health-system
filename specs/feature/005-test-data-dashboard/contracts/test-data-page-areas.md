# Contract: Test Data Page Areas

This feature has no REST/backend API surface (FR-017 — zero changes to `src/api.ts` or any backend endpoint). The "contract" formalized here is the UI-level boundary between the four tabbed areas: which controls live in which tab, and the hard constraints on cross-area leakage. This is the canonical reference for `/speckit-tasks` and for reviewers checking FR-001–FR-016 compliance.

## Tab: Data Counts and Visualizations

**Contains**:
- Patient stat cards: patient count, duplicate patient count, recent audit event count, total staff count
- Patient breakdown: patients by site (visual — Recharts)
- Study stat cards: study count, arm count, visit count
- Study breakdown: studies by status (visual — Recharts)
- "Refresh stats" control (re-fetches both patient and study stats)

**MUST NOT contain**: any control that creates, edits, or deletes data (no generation forms, no reset buttons, no lookup/edit forms).

**Backing API calls** (unchanged): `getPatientTestDataStats()`, `getStudyTestDataStats()`.

## Tab: Data Generation

**Contains**:
- Bulk patient generation form (`totalCount`, `duplicatePercentage`, `seed`) + result summary
- Bulk study generation form (`totalCount`, `seed`) + result summary
- Staff generation form (`count`, `seed`) + result summary
- Recent audit event generation form (`count`, `seed`) + result summary
- Manual single-patient form (`firstName`, `lastName`, `email`) + result (id/UID)

**MUST NOT contain**: reset/destructive controls, lookup/view/edit controls, stat visualizations.

**Backing API calls** (unchanged): `generateTestPatients()`, `generateTestStudies()`, `generateTestStaff()`, `generateRecentAuditEvents()`, `addTestPatient()`.

## Tab: Data Manipulation

**Contains**:
- Patient lookup (by ID / UID / email) + "get random patient"
- Patient full-detail view (read-only JSON) and edit mode (editable JSON textarea, Save / Save with Audit)
- Study lookup (by name / identifier / protocol number) + "get random study"
- Study full-detail view (read-only JSON; no edit — matches current functionality, no edit capability exists for studies today)

**MUST NOT contain**: bulk generation controls, reset controls, connection info.

**Backing API calls** (unchanged): `lookupTestPatient()`, `getRandomTestPatient()`, `updateTestPatient()`, `lookupTestStudy()`, `getRandomTestStudy()`.

## Tab: Information and Destruction

**Contains**:
- Connection info: JSON API base URL, SOAP POST endpoint, SOAP WSDL URL (each with copy-to-clipboard)
- SOAP report pkeys list (with copy-to-clipboard per key) + refresh control
- **Danger zone** (visually separated sub-section): "Reset patient data" and "Reset study data," each requiring an explicit confirmation step before the request fires

**MUST NOT contain**: generation forms, lookup/edit forms, stat visualizations.

**Backing API calls** (unchanged): `getSoapReportPkeys()`, `getConfiguredApiBaseUrl()`, `resetTestPatients()`, `resetTestStudies()`.

## Cross-cutting rules

1. **No duplication**: a control/handler exists in exactly one tab's component file. (Verifiable via `grep` for handler/state names across the four new files — each name should appear in exactly one.)
2. **No new/changed API calls**: every function imported from `api.ts` in the new components must already exist there today with an unchanged signature.
3. **Demo mode**: every write/generate/reset/lookup control must remain disabled or inert when `isDemoMode` is true, in every tab, matching current behavior.
4. **Independent tabs**: switching tabs must not trigger network requests for a tab the user is not viewing (each section fetches its own data on its own mount, not eagerly from the orchestrator).
