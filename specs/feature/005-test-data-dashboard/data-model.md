# Data Model: Test Data Management Dashboard Reorganization

This feature introduces no new backend entities, database tables, or API types. "Data model" here means the client-side component/state shape each new area component owns. All referenced types (`GeneratePatientsOptions`, `PatientTestDataStats`, `StudyViewModel`, etc.) already exist in `frontend/src/api.ts` and are unchanged.

## TestDataPage (orchestrator)

| State | Type | Purpose |
|---|---|---|
| `activeTab` | `"counts" \| "generation" \| "manipulation" \| "info"` | Which of the four areas is currently visible. Local to the orchestrator; not persisted. |

Renders `<AdminSessionBanner />` plus the tab nav, then exactly one of the four section components based on `activeTab`. Passes no area-specific state as props — each section fetches/owns its own state (see Decision 4, research.md).

## TestDataCountsSection

| State | Type | Source |
|---|---|---|
| `stats` | `PatientTestDataStats \| null` | `getPatientTestDataStats()` / demo fallback (`DEMO_TEST_DATA_STATS`) |
| `studiesStats` | `StudyTestDataStats \| null` | `getStudyTestDataStats()` |
| `error` | `string \| null` | Local error state for this area's own load/refresh failures |

Behavior: loads both stats on mount (respecting `isDemoMode`/`hasSession`, matching current `useEffect` gating); provides a "Refresh stats" action that re-runs both loads; renders patient stat cards + study stat cards, with the "patients by site" and "studies by status" breakdowns rendered as small Recharts charts (Decision 2, research.md) instead of plain `<ul>` lists.

## TestDataGenerationSection

| State | Type | Source (existing handler) |
|---|---|---|
| `generateOptions` / `generateResult` / `loadingGenerate` | `GeneratePatientsOptions` / `GeneratePatientsResult \| null` / `boolean` | `handleGenerate` → `generateTestPatients` |
| `studiesGenerateOptions` / `studiesGenerateResult` / `loadingGenerateStudies` | `GenerateStudiesOptions` / `GenerateStudiesResult \| null` / `boolean` | `handleGenerateStudies` → `generateTestStudies` |
| `staffGenerateOptions` / `staffGenerateResult` / `loadingGenerateStaff` | `GenerateStaffOptions` / `GenerateStaffResult \| null` / `boolean` | `handleGenerateStaff` → `generateTestStaff` |
| `auditEventsGenerateOptions` / `auditEventsGenerateResult` / `loadingGenerateAuditEvents` | `GenerateRecentAuditEventsOptions` / `GenerateRecentAuditEventsResult \| null` / `boolean` | `handleGenerateRecentAuditEvents` → `generateRecentAuditEvents` |
| `addForm` / `addResult` / `loadingAdd` | `{firstName, lastName, email}` / `AddTestPatientResponse \| null` / `boolean` | `handleAddPatient` → `addTestPatient` |
| `error` | `string \| null` | Local error state for this area only |

Behavior: five independent forms/actions (bulk patients, bulk studies, staff, audit events, manual patient), each with its own loading flag and result/error display, matching current handler logic exactly (FR-017). No reset controls present (FR-008).

## TestDataManipulationSection

| State | Type | Source (existing handler) |
|---|---|---|
| `lookupForm` / `lookupResult` / `lookupNotFound` / `loadingLookup` | `{id, uid, email}` / `LookupPatientResponse \| null` / `boolean` / `boolean` | `handleLookupPatient`, `handleGetRandomPatient` → `lookupTestPatient`, `getRandomTestPatient` |
| `isEditingLookup` / `lookupEditJson` / `savingLookupMode` | `boolean` / `string` / `"none" \| "save" \| "audit"` | `handleSavePatientRecord` → `updateTestPatient` |
| `studiesLookupForm` / `studiesLookupResult` / `studiesLookupNotFound` / `loadingStudiesLookup` | `{name, identifier, protocolNumber}` / `StudyViewModel \| null` / `boolean` / `boolean` | `handleLookupStudy`, `handleGetRandomStudy` → `lookupTestStudy`, `getRandomTestStudy` |
| `error` | `string \| null` | Local error state for this area only |

Behavior: two independent lookup/detail/edit flows (patient — view + edit + save/save-with-audit; study — view only, no edit exists today and none is added, matching FR-011's "view" scope). No bulk generation or reset controls present (FR-012).

## TestDataInfoDestructionSection

| State | Type | Source |
|---|---|---|
| `soapPkeys` / `soapPkeysError` / `loadingSoapPkeys` | `string[] \| null` / `string \| null` / `boolean` | `getSoapReportPkeys()` |
| *(derived, not state)* `versionedJsonBase`, `soapPostUrl`, `soapWsdlUrl` | `string` | `getConfiguredApiBaseUrl()` |
| `loadingReset` / `resetConfirming` (new) | `boolean` / `boolean` | `handleReset` → `resetTestPatients` |
| `loadingResetStudies` / `resetStudiesConfirming` (new) | `boolean` / `boolean` | `handleResetStudies` → `resetTestStudies` |

Behavior: read-only connection info (API base, SOAP endpoint/WSDL, report pkeys) with copy-to-clipboard, plus a visually separated "danger zone" containing the two reset actions, each gated by the new two-step confirm interaction (Decision 3, research.md; new `resetConfirming`/`resetStudiesConfirming` boolean state per FR-015). No generation or lookup controls present (FR-014).

## Validation Rules

No new validation is introduced. All existing client-side validation (required fields on the manual-add-patient form, numeric constraints on generation option inputs) is preserved unchanged in its relocated component (FR-017).

## State Transitions

N/A — no entity has a lifecycle/state machine. The only new transition is the reset confirm flow: `idle → confirming → (idle | resetting → idle)`, i.e., clicking "Reset X data" moves the button into a confirming state; either canceling/timing out returns to idle, or confirming triggers the existing reset request flow unchanged.
