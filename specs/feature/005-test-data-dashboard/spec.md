# Feature Specification: Test Data Management Dashboard Reorganization

**Feature Branch**: `005-test-data-dashboard`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "I'd like to start on the next feature to improve the UI for the test data management section of the application. Right now everything related to test data is under a single header, but that leaves the UI cluttered. I'd like to update it to make it look nicer and be more intuitive. There are 4 different functions that would be logical ways to break the functionality up. 1. Data Counts and Visualizations - shows the counts of the various entities that exist in the current test data. There is data for patients as well as data for studies. These are mostly simple counts, but it would be nice to have something more visual to depict this data. 2. Data Generation - Allows users to create entities of various types, including manual generation of patients. 3. Data manipulation - Allows users to view detailed information and make edits. 4. Information and Distruction - Details about the connection information and a "danger zone" which allows users to reset the data. Everything that is currently on the Test data management page should have a home in one of these 4 categories."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See data counts and visualizations at a glance (Priority: P1)

As an admin, when I open the test data page, I want a dedicated, visually clear overview of how much data currently exists (patients, studies, and their sub-entities) so I can quickly gauge the state of the environment without scanning past generation or destructive controls.

**Why this priority**: This is the landing view for the reorganized page and the most frequently referenced information — every other action (generate, manipulate, reset) is taken in the context of "what exists right now." It also directly answers the user's specific request for a "more visual" depiction of counts.

**Independent Test**: Can be fully tested by opening the test data page, navigating to the Data Counts and Visualizations area, and confirming patient counts (total, duplicates, staff, recent audit events, by-site breakdown) and study counts (total, arms, visits, by-status breakdown) are visible with a visual (non-plain-text-only) treatment, independent of whether any generation or manipulation action is ever taken.

**Acceptance Scenarios**:

1. **Given** the admin has an active session and test data exists, **When** they view the Data Counts and Visualizations area, **Then** they see patient counts and study counts each presented with a visual element (e.g., a chart, graph, or visual proportion indicator) alongside the numeric values, not as plain numbers alone.
2. **Given** no patient or study data exists yet, **When** the admin views this area, **Then** counts display as zero and breakdown lists show an empty-state message rather than an error.
3. **Given** the admin clicks a refresh control, **When** the refresh completes, **Then** all counts and visualizations update to reflect current data without a full page reload.

---

### User Story 2 - Generate test data in one focused area (Priority: P2)

As an admin, I want every way of creating test data (bulk patients, bulk studies, staff, audit events, and a single manually-specified patient) grouped in one place, separate from destructive actions, so I can populate the environment without the risk of misclicking a reset button.

**Why this priority**: Generation is the most frequently used capability on this page, and today its controls sit directly adjacent to same-entity reset buttons (e.g., "Reset patients" and "Generate patients" are side-by-side), creating a real risk of an accidental destructive click. Consolidating generation away from destruction removes that risk and is high-value even before the visual/manipulation improvements land.

**Independent Test**: Can be fully tested by navigating to the Data Generation area and successfully generating bulk patients, bulk studies, staff, recent audit events, and a single manual patient — with no reset or lookup/edit controls present in this area.

**Acceptance Scenarios**:

1. **Given** the admin is in the Data Generation area, **When** they submit the bulk patient generation form (count, duplicate percentage, optional seed), **Then** patients are created and a result summary is shown in that same area.
2. **Given** the admin is in the Data Generation area, **When** they submit the bulk study generation form (count, optional seed), **Then** studies with sub-resources are created and a result summary is shown in that same area.
3. **Given** the admin is in the Data Generation area, **When** they generate staff or recent audit events, **Then** each produces its own result summary without navigating away from the area.
4. **Given** the admin is in the Data Generation area, **When** they submit the manual single-patient form (first name, last name, email), **Then** one patient is created and its id/UID are shown.
5. **Given** the admin is in the Data Generation area, **When** they look for a way to delete or reset existing data, **Then** no such control is present in this area.

---

### User Story 3 - View and edit specific records (Priority: P3)

As an admin, I want to look up an individual patient or study and view or edit its full details in an area dedicated to record-level inspection, separate from bulk generation and destructive actions.

**Why this priority**: This is used for targeted debugging/verification rather than routine data population, so it's lower-frequency than generation, but it's still core, existing functionality that must have a clear, uncluttered home.

**Independent Test**: Can be fully tested by navigating to the Data Manipulation area, looking up a patient by ID/UID/email (or a random one), viewing its full JSON detail, editing a field, and saving — and separately looking up a study by name/identifier/protocol number (or a random one) and viewing its full detail.

**Acceptance Scenarios**:

1. **Given** the admin is in the Data Manipulation area, **When** they search for a patient by ID, UID, or email, **Then** the matching patient's full details are displayed, or a not-found message appears if there is no match.
2. **Given** a patient record is displayed, **When** the admin chooses to edit it, makes a change, and saves, **Then** the update is persisted and reflected in the displayed record.
3. **Given** the admin is in the Data Manipulation area, **When** they search for a study by name, identifier, or protocol number, **Then** the matching study's full details are displayed, or a not-found message appears if there is no match.
4. **Given** the admin is in the Data Manipulation area, **When** they request a random patient or a random study, **Then** a randomly selected existing record of that type is displayed.

---

### User Story 4 - View connection info and safely reset data in an isolated danger zone (Priority: P4)

As an admin, I want connection/instance information and the destructive "reset all data" actions kept together in one clearly separated, low-traffic area, so destructive actions are never one accidental click away from routine generation or lookup work.

**Why this priority**: Destructive actions carry the highest risk if triggered accidentally, so isolating them is important, but this is the lowest-frequency area of the page (consulted occasionally for connection details, used rarely for resets), making it appropriate to build last.

**Independent Test**: Can be fully tested by navigating to the Information and Destruction area, confirming the JSON API base URL, SOAP endpoint/WSDL URLs, and SOAP report pkeys are visible and copyable, and separately confirming that resetting patient data or study data (each requiring an explicit confirmation step) clears only the targeted domain's data.

**Acceptance Scenarios**:

1. **Given** the admin is in the Information and Destruction area, **When** they view it, **Then** they see the JSON API base URL, SOAP POST endpoint, SOAP WSDL URL, and current SOAP report pkeys, each with a copy control.
2. **Given** the admin is in the Information and Destruction area, **When** they click "Reset patient data," **Then** the system requires an explicit confirmation before any data is deleted.
3. **Given** the admin confirms a patient data reset, **When** the reset completes, **Then** all patient-related data is cleared and study data is untouched.
4. **Given** the admin confirms a study data reset, **When** the reset completes, **Then** all study-domain data is cleared and patient data is untouched.
5. **Given** the admin is in the Information and Destruction area, **When** they look for generation or lookup controls, **Then** none are present in this area.

### Edge Cases

- What happens when the admin has no active session (demo mode)? All four areas must still render with their read-only/demo content; any control that would call a live API (generate, reset, lookup, edit) must be visibly disabled or otherwise inert, consistent with current demo-mode behavior.
- What happens when a generation or reset request fails? The originating area shows an inline error message; other areas are unaffected.
- What happens when the admin navigates away from an area while a generation/reset request is still in flight? The request continues to run; returning to that area later reflects its outcome (result or error).
- What happens on narrow (mobile-width) viewports? All four areas remain reachable and usable — the reorganization must not depend on horizontal space that isn't available on small screens.
- What happens when stats fail to load? The Data Counts and Visualizations area shows an inline error rather than blocking the rest of the page.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The test data page MUST organize all existing test-data functionality into exactly four distinct, clearly labeled areas: "Data Counts and Visualizations," "Data Generation," "Data Manipulation," and "Information and Destruction."
- **FR-002**: Every control, form, and display currently present on the test data page MUST be relocated into exactly one of the four areas; no functionality may be removed, and no functionality may appear in more than one area.
- **FR-003**: The system MUST let the admin move between the four areas using a tabbed interface, where selecting a tab shows only that area's content and hides the other three.
- **FR-004**: The Data Counts and Visualizations area MUST display current patient counts (total, duplicates, recent audit events, total staff, patients by site) and study counts (total studies, arms, visits, studies by status) sourced from the same statistics currently shown on the page.
- **FR-005**: The Data Counts and Visualizations area MUST present count data using lightweight visual stat elements — icon-accented count cards plus inline proportion/progress-style bars for breakdown lists (e.g., patients by site, studies by status) — in addition to the existing numeric values, without introducing a **new** charting library dependency (Recharts, already used elsewhere in this frontend, may be reused).
- **FR-006**: The Data Counts and Visualizations area MUST provide a control to refresh all displayed counts without a full page reload.
- **FR-007**: The Data Generation area MUST contain: bulk patient generation (count, duplicate percentage, optional seed), bulk study generation (count, optional seed), staff generation (count, optional seed), recent audit event generation (count, optional seed), and manual single-patient creation (first name, last name, email).
- **FR-008**: The Data Generation area MUST NOT contain any destructive (reset) controls.
- **FR-009**: Each generation action MUST show its own result summary (counts inserted, totals after) or error message in the Data Generation area without requiring navigation elsewhere.
- **FR-010**: The Data Manipulation area MUST contain patient lookup (by ID, UID, or email, plus "get random"), full patient detail view, and patient record editing (including the existing save and save-with-audit options).
- **FR-011**: The Data Manipulation area MUST contain study lookup (by name, identifier, or protocol number, plus "get random") and full study detail view.
- **FR-012**: The Data Manipulation area MUST NOT contain bulk generation or reset controls.
- **FR-013**: The Information and Destruction area MUST display the JSON API base URL, SOAP POST endpoint URL, SOAP WSDL URL, and current SOAP report pkeys, each with a copy-to-clipboard control, matching what is currently shown.
- **FR-014**: The Information and Destruction area MUST contain a clearly demarcated "danger zone" holding the reset-patient-data and reset-study-data controls, and no other area may contain a reset control.
- **FR-015**: Resetting patient data or study data MUST require an explicit confirmation step before the destructive request is sent, where none exists today.
- **FR-016**: Resetting patient data MUST continue to leave study data untouched, and resetting study data MUST continue to leave patient data untouched.
- **FR-017**: All relocated controls MUST preserve their existing behavior (API calls, validation, loading states, success/error messaging) — this feature changes organization and presentation only, not underlying functionality.
- **FR-018**: When the admin has no active session (demo mode), all four areas MUST remain viewable, and any control that would perform a live write/read against the backend MUST remain disabled or inert, consistent with current demo-mode behavior.
- **FR-019**: The reorganized layout MUST remain usable at narrow (mobile-width) viewport sizes, with all four areas reachable.

### Key Entities

- **Data Counts and Visualizations area**: A page section that aggregates and visually presents read-only statistics for patients and studies; sources existing stats data, adds no new data of its own.
- **Data Generation area**: A page section grouping all entity-creation actions (bulk and manual) for patients, studies, staff, and audit events.
- **Data Manipulation area**: A page section grouping lookup, detail-view, and edit actions for individual patient and study records.
- **Information and Destruction area**: A page section grouping read-only connection/instance information with an isolated "danger zone" containing all destructive reset actions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin unfamiliar with the page can correctly identify which of the four areas contains a given piece of existing functionality (e.g., "where do I reset study data?") in under 10 seconds, based on area labeling alone.
- **SC-002**: 100% of controls present on the test data page before this change are present after this change, each in exactly one of the four areas.
- **SC-003**: The number of destructive (reset) controls reachable without an explicit confirmation step is zero, down from the current state where reset actions execute immediately on click.
- **SC-004**: Admins can distinguish patient data volume from study data volume at a glance (within 5 seconds) from the Data Counts and Visualizations area without reading detailed numbers.
- **SC-005**: The page remains fully navigable and every area remains reachable on a mobile-width viewport (no functionality becomes unreachable due to horizontal space constraints).

## Assumptions

- This feature is a reorganization and presentation upgrade only; no new test-data capabilities (new entity types, new generation options, new edit fields) are introduced beyond what already exists on the page today.
- Demo-mode behavior (read-only, disabled write/generate/reset controls, existing demo fallback data) is preserved as-is, just redistributed across the four new areas.
- The "danger zone" reset actions will gain a simple explicit-confirmation step (e.g., a confirm prompt or a two-step confirm control) before executing, since none exists today and the user's own "danger zone" framing implies this safeguard is expected; the exact confirmation mechanism is an implementation detail left to the plan.
- Visual polish is scoped to reorganizing content into the four areas and adding a visual treatment to the counts (per FR-005); a full design-system or branding overhaul is out of scope.
- All existing API endpoints, request/response shapes, and validation rules for generation, lookup, editing, and reset are unchanged — this is a frontend-only reorganization.
