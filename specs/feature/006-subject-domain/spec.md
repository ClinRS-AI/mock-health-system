# Feature Specification: Subject Domain

**Feature Branch**: `feature/006-subject-domain`

**Created**: 2026-07-20

**Status**: Draft

**Input**: User description: "I'd like to create a new feature / branch for the next set of functionality that will need to be included in the mock health system. The next domain is the Subject. The Subject domain is a link between a Patient and a Study. Patients can enroll multiple times in the same Study, but never be \"Active\" at the same time. Similar to the Patient and Study domains, I'd like to use the CC API endpoints for what to include in the Subject domain's initial build out. This will need new backend database tables for the subject data as well as API endpoints that mirror what is in the CC API. I also want to create faking endpoints similar to the patient and study data on the test data management section of the application. I will want to see counts for the number of subjects and a breakdown of patients by study. I'll need to be able to generate subject data that uses existing patients and studies to create the subject records. I will want to be able to lookup a subject record on the data manipulation tab, and there should be the ability to reset subject data. The reset study data and reset patient data should also clear the subject data since there will be no valid subjects left once the study or the patient data has been removed."

## Overview

The mock system today exposes the Patient domain (patients, conditions,
medications, procedures) and the Study domain (studies and their structural
sub-resources) against the Clinical Conductor (CC) Public API surface. The
Study domain's own specification explicitly deferred CC's Subject/enrollment
surface — "Study-to-Patient enrollment linkage (screening number,
randomization, treatment status)" — to a future feature. This feature adds
that surface: **Subject** — the record that links a specific Patient to a
specific Study, representing one enrollment episode.

This phase covers the core Subject record itself (the enrollment link, its
status, and the key CC fields that describe an enrollment episode), a
status-history record of how a subject's status has changed over time
(retrievable per-study via the CC API), the business rule that a patient
may have multiple enrollment episodes in the same study over time but only
one may be in an Active-category status at once (Active category:
"Prescreened", "Screened", "Randomized", "Run-in"; Inactive category:
"Screen Failed", "Non Qualified", "Dropped", "Run-in Failed", "Complete"),
and the corresponding admin test-data tooling
(generate, view counts, look up, and reset) integrated into the existing
four-tab Test Data Management dashboard.

**Why this exists**: Developers building CC integrations that touch
enrollment, screening, or study-participation reporting need a realistic
Subject surface to test against — the same way the Patient and Study domains
already let them test those integrations without a real CC environment.
Today, generated Patients and Studies exist independently with no way to
represent which patients are actually enrolled in which studies.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrieve Subject Data via the CC API (Priority: P1)

A developer building a CC integration wants to verify that their code
correctly parses subject/enrollment records. They point their client at the
mock and call the same endpoint paths as the Public CC API: a list of
subjects (filterable by patient and by study), a single subject's detail,
and — for a given study — the history of subject status changes recorded
against subjects enrolled in that study.

**Why this priority**: Read access to realistic subject data is the
foundation for every other integration behavior a developer needs to
validate — without it, nothing else in this feature has value.

**Independent Test**: A developer can retrieve a paginated list of subjects,
filter that list down to a single patient's enrollments or a single study's
enrollments, and fetch one subject by ID — all without accessing any real CC
system.

**Acceptance Scenarios**:

1. **Given** synthetic subjects exist, **When** a client GETs the subjects
   list endpoint, **Then** a paginated response is returned with the same
   schema and query/filter conventions as the Public CC API.
2. **Given** a known subject ID, **When** a client GETs the subject detail
   endpoint, **Then** the response contains the subject's core fields
   (patient reference, study reference, status, key enrollment dates, and
   subject/screening identifier).
3. **Given** subjects exist for multiple patients and studies, **When** a
   client GETs the subjects list filtered by a specific patient or a
   specific study, **Then** only subjects belonging to that patient or study
   are returned.
4. **Given** a study whose subjects have had status changes recorded,
   **When** a client GETs that study's subject-status history endpoint,
   **Then** the response returns every recorded status entry for subjects
   enrolled in that study, matching the Public CC API's schema for this
   endpoint.
5. **Given** the mock is under an active auth mode, **When** a client calls
   any CC-mirrored Subject endpoint without correct credentials, **Then**
   the mock returns the same error structure (status code and body shape)
   as the real CC API, consistent with existing Patient and Study endpoint
   behavior.

---

### User Story 2 - Manage Subject Data via the CC API (Priority: P1)

A developer's integration needs to create and update subject/enrollment
records — for example, enrolling a patient in a study, updating their status
as they progress through screening to active participation, or recording a
withdrawal. They call the create, update, and delete endpoints for subjects
exactly as they would against the Public CC API, and the mock enforces the
same "only one active enrollment per patient per study" rule CC does.

**Why this priority**: Many CC integrations are bidirectional (e.g.,
syncing enrollment status from a CTMS). Without write support the mock only
serves read-only testing scenarios, which is a smaller fraction of real
integration work.

**Independent Test**: A developer can enroll a patient in a study, update
that subject's status, attempt to create a second concurrently-Active
enrollment for the same patient/study and see it rejected, and re-enroll
the same patient in the same study after their prior enrollment is no
longer Active — all via API calls.

**Acceptance Scenarios**:

1. **Given** a valid existing patient and study, **When** a client POSTs a
   new subject referencing them, **Then** the subject is created and
   returned with a generated ID.
2. **Given** an existing subject, **When** a client PUTs a full update or
   PATCHes a partial update, **Then** the changed fields are persisted and
   reflected on the next GET.
3. **Given** a patient with no currently Active-category enrollment in a
   study, **When** a client creates or updates a subject to an
   Active-category status (e.g., "Randomized") for that patient/study pair,
   **Then** the write succeeds.
4. **Given** a patient already has a subject in an Active-category status
   (e.g., "Screened") for a specific study, **When** a client attempts to
   create or update another subject to any Active-category status for that
   same patient/study pair, **Then** the mock rejects the request with a
   clear validation error rather than allowing two concurrently
   Active-category enrollments.
5. **Given** a patient's prior enrollment in a study is now in an
   Inactive-category status (e.g., "Complete" or "Dropped"), **When** a
   client creates a new subject for the same patient/study pair, **Then**
   the write succeeds, reflecting that patients may enroll in the same
   study multiple times over time.
6. **Given** a request to create or update a subject referencing a patient
   or study that does not exist, **When** the client submits the request,
   **Then** the mock rejects it with a clear validation error rather than
   silently creating an inconsistent record.
7. **Given** an existing subject, **When** a client DELETEs it, **Then** it
   is removed and no longer returned on subsequent reads.
8. **Given** an existing subject, **When** a client creates that subject
   with an initial status, or updates it to a new status, **Then** a
   status-history entry recording that status (and when it took effect) is
   added, retrievable via that subject's study's status-history endpoint.

---

### User Story 3 - Generate, Monitor, and Manage Synthetic Subject Data for Testing (Priority: P2)

A developer starting a new test cycle needs realistic enrollment data
linking their already-generated patients and studies, without hand-crafting
each link through the write API. They open the admin interface's Test Data
Management dashboard, see subject counts and a per-study patient breakdown
on the Data Counts and Visualizations tab, generate a batch of subjects on
the Data Generation tab (drawn from existing patients and studies), look up
a specific subject on the Data Manipulation tab, and reset all subject data
from the Information and Destruction tab — the same convenience the Patient
and Study domains already provide, integrated into the same four tabs
rather than a separate section.

**Why this priority**: Manual creation of subjects through the write API is
slow; automated generation is what makes the Subject domain immediately
useful for testing. This depends on Patients and Studies already existing
(User Stories 1/2 of those prior features), which is why it is
lower-priority than being able to read and write subjects at all.

**Independent Test**: With existing generated patients and studies present,
a developer can generate a batch of subjects linking them, see the subject
count and per-study patient breakdown update on the Counts tab, look up a
specific subject by identifier, and reset all subject data — all from the
admin interface without touching the API or database directly. Separately,
resetting Patient data or Study data alone (without an explicit subject
reset) also results in zero remaining subject records.

**Acceptance Scenarios**:

1. **Given** existing generated patients and studies, **When** a developer
   requests generation of N subjects (within configured limits) on the Data
   Generation tab, **Then** N subjects linking existing patients and
   studies are created (respecting the one-Active-category-status-per
   -patient-per-study rule) and are immediately queryable via the CC
   Subject API endpoints.
2. **Given** no patients or studies exist yet, **When** a developer
   requests subject generation, **Then** the request is rejected with a
   clear error explaining that patients and studies must exist first.
3. **Given** generated subjects exist, **When** the developer views the
   Data Counts and Visualizations tab, **Then** they see the total subject
   count and a visual breakdown of (distinct) patients enrolled per study.
4. **Given** generated subjects exist, **When** the developer looks up a
   subject by its identifier (or by patient/study) on the Data Manipulation
   tab, **Then** matching results are returned, or a not-found message
   appears if there is no match.
5. **Given** synthetic subject data exists, **When** the developer triggers
   a subject data reset from the Information and Destruction tab's danger
   zone, **Then** all synthetic subject records are removed without
   affecting Patient or Study data.
6. **Given** synthetic subject data exists that references existing
   patients and/or studies, **When** the developer resets Patient data or
   Study data (independently, without also explicitly resetting subjects),
   **Then** all subject records that reference the removed patients or
   studies are also removed, leaving no orphaned subject data.
7. **Given** a batch size above the configured maximum, **When** the
   developer requests generation, **Then** the request is rejected with a
   clear error describing the limit.

### Edge Cases

- What happens when a client requests a subject by an ID that doesn't
  exist?
- What happens when subject generation is requested but there are fewer
  valid (patient, study) combinations available than the requested count
  without violating the one-Active-enrollment rule (e.g., every patient is
  already Active in the only study that exists)? The system generates as
  many valid subjects as it can and reports the actual count created,
  consistent with how Patient generation reports base vs. duplicate counts
  requested vs. inserted.
- What happens when a subject data reset is triggered while no subject data
  exists yet? The operation completes without error, consistent with
  existing reset behavior for Patient and Study data.
- What happens to a subject's status-history entries when the subject
  itself is deleted, or when Subject data is reset? They are removed along
  with the subject, consistent with how Study sub-resources are cascaded on
  their parent's removal.
- What happens when a study with no subjects (or no recorded status
  changes) is queried via the subject-status history endpoint? An empty
  result set is returned, not an error.
- What happens when list/query endpoints receive invalid pagination or
  filter parameters?
- What happens when a subject references a study arm that doesn't belong to
  the subject's own study?
- What happens when a subject is created or updated with a status value
  outside the nine CC-defined values? The request is rejected with a clear
  validation error (FR-006).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose CC Subject API endpoints for core
  Subject operations: list with pagination and filtering by patient and by
  study, retrieve by ID, create, full update, partial update, and delete.
- **FR-002**: A Subject record MUST reference exactly one Patient and
  exactly one Study, and MUST carry a status — one of CC's nine defined
  Subject status values, grouped into an Active category ("Prescreened",
  "Screened", "Randomized", "Run-in") and an Inactive category ("Screen
  Failed", "Non Qualified", "Dropped", "Run-in Failed", "Complete") — and
  key enrollment-episode dates (at minimum an enrollment date). A Subject
  MAY optionally carry a subject/screening identifier (not every subject
  has one assigned, e.g. before screening is complete) and MAY optionally
  reference a specific Study Arm belonging to its own Study, once assigned.
- **FR-003**: The system MUST allow the same Patient to have multiple
  Subject records against the same Study over time (representing separate
  enrollment episodes), but MUST reject any create or update that would
  result in more than one Subject record whose status is in the Active
  category ("Prescreened", "Screened", "Randomized", "Run-in") for the same
  Patient and Study at the same time, returning a clear validation error
  instead of silently mutating another record.
- **FR-004**: The system MUST record a status-history entry (the status
  value and when it took effect) each time a Subject is created with an
  initial status or updated to a new status, preserving prior entries
  rather than overwriting them.
- **FR-005**: The system MUST expose a CC-mirrored, study-scoped endpoint —
  `GET /api/v1/studies/{studyUid}/subject-statuses/odata` — that returns
  the subject status-history entries for subjects enrolled in the specified
  study (identified by the study's UID), matching the Public CC API's
  schema and simple-list (no additional query options) convention already
  used by this project's other `/odata`-suffixed endpoints.
- **FR-006**: The system MUST reject Subject create/update requests that
  reference a Patient, Study, or Study Arm that does not exist, a Study Arm
  that does not belong to the referenced Study, or a status value outside
  CC's nine defined Subject status values (FR-002).
- **FR-007**: All implemented Subject endpoints (including the
  subject-status history endpoint) MUST return structurally valid,
  non-placeholder responses that match the field names and shapes of the
  Public CC API; no endpoint may return stub data when called with valid
  inputs and credentials.
- **FR-008**: The admin interface's Data Generation tab MUST allow
  generating synthetic Subject records that link existing Patients and
  Studies, in configurable batch sizes up to a defined maximum, following
  the same generation pattern as the Patient and Study domains. Subject
  generation MUST NOT create new Patient or Study records; it MUST fail
  with a clear error if no Patients or Studies exist yet. Each generated
  Subject MUST have at least an initial status-history entry recorded.
- **FR-009**: The admin interface's Data Counts and Visualizations tab MUST
  display the total count of synthetic Subject records and a visual
  breakdown of the count of distinct patients enrolled per study, alongside
  the existing Patient and Study counts already shown there.
- **FR-010**: The admin interface's Data Manipulation tab MUST allow
  looking up a generated Subject record and viewing its full detail.
- **FR-011**: The admin interface's Information and Destruction tab's
  danger zone MUST allow resetting all synthetic Subject data — including
  subject status-history entries — in a single, confirmation-gated
  operation, independent of Patient and Study data.
- **FR-012**: Resetting Patient data MUST also remove all Subject records
  (and their status-history entries) that reference the removed patients,
  and resetting Study data MUST also remove all Subject records (and their
  status-history entries) that reference the removed studies — in both
  cases without requiring a separate, explicit Subject reset action.
- **FR-013**: Subject API endpoints MUST honor the auth mode and
  admin-session protections already enforced elsewhere in the system; no
  separate authentication mechanism is introduced for the Subject domain.
- **FR-014**: Every Subject API request MUST be captured by the existing
  request logging/monitoring infrastructure with no Subject-specific
  exemptions.

### Key Entities

- **Subject**: The enrollment-episode record linking one Patient to one
  Study — a status drawn from CC's nine defined values (Active category:
  "Prescreened", "Screened", "Randomized", "Run-in"; Inactive category:
  "Screen Failed", "Non Qualified", "Dropped", "Run-in Failed", "Complete"),
  key enrollment dates, an optional subject/screening identifier, and an
  optional reference to the specific Study Arm the patient has been
  assigned to. Multiple Subject records may exist for the same
  Patient/Study pair (representing separate enrollment episodes over
  time), but at most one per Patient/Study pair may be in an
  Active-category status at any given time.
- **SubjectStatus**: An append-only status-history entry recording one
  status change for a Subject — the status value and when it took effect,
  plus any additional descriptive fields the Public CC API's
  `subject-statuses/odata` schema exposes. Every Subject accumulates one or
  more SubjectStatus entries over its lifetime (at minimum, one at
  creation); entries are retrieved per-study via the CC-mirrored history
  endpoint and are removed whenever their owning Subject is removed
  (directly, or via a cascading Patient/Study reset).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can retrieve subject/enrollment records —
  including filtering by a specific patient or study — using only the
  mock's Subject API, with no real CC system involved.
- **SC-002**: Every implemented Subject endpoint returns a structurally
  valid, non-placeholder response for valid inputs and credentials.
- **SC-003**: Attempting to create two concurrently-Active enrollments for
  the same patient and study is rejected 100% of the time, while
  re-enrolling a patient in the same study after a prior enrollment is no
  longer Active succeeds 100% of the time.
- **SC-004**: Synthetic data for a batch of subjects linking existing
  patients and studies can be generated in under 30 seconds.
- **SC-005**: Resetting synthetic Patient data or Study data leaves zero
  orphaned Subject records, without an explicit Subject reset having been
  triggered.
- **SC-006**: An admin can see the total subject count and per-study
  patient breakdown from the Data Counts and Visualizations tab within 5
  seconds of opening it.
- **SC-007**: A developer unfamiliar with the Subject domain can generate
  synthetic subjects, retrieve one via the API, locate it again in the
  admin interface, and reset subject data within 10 minutes of first use.

## Assumptions

- The Subject status vocabulary is confirmed directly by the user (not a
  guess): nine values — Active category ("Prescreened", "Screened",
  "Randomized", "Run-in") and Inactive category ("Screen Failed", "Non
  Qualified", "Dropped", "Run-in Failed", "Complete"). Other field-level
  shape details (date fields beyond enrollment date, the subject/screening
  identifier's exact format) remain based on standard CC/CTMS enrollment
  concepts and this project's own established Study-domain conventions,
  carrying the same lower-confidence research risk noted during
  specification — a live fetch of the CC OpenAPI document did not surface
  Subject-tagged endpoints across repeated attempts, so any remaining
  field-shape details beyond what the user has directly confirmed should
  still be treated as best-effort.
- Unlike `Study.Status` (which references an admin-configurable
  `StudyStatusType` lookup table), Subject status values are a fixed,
  CC-defined closed set — not admin-configurable — since CC itself does
  not allow customizing this vocabulary; the system validates against an
  in-code list rather than a database lookup table.
- Subject sub-resources beyond the core record and its status-history are
  explicitly out of scope for this phase (e.g., subject-level visit
  completion tracking or subject notes), consistent with how the Study
  domain itself deferred the entire Subject surface in its first phase.
  Status-history is the one sub-resource pulled into this phase's scope,
  because the user identified it as needed alongside the core record and
  supplied its real CC endpoint
  (`/api/v1/studies/{studyUid}/subject-statuses/odata`); it follows the
  same current-status-plus-history-table pattern already established by
  the Study domain's `StudyDocument`/`StudyDocumentStatusHistory` pair.
- The one-Active-category-status-per-patient-per-study rule is enforced by
  rejecting the conflicting write with a validation error, not by silently
  deactivating the prior enrollment — consistent with how the Study domain
  handles other invalid/conflicting reference writes.
- The "never Active at the same time" constraint applies per Patient/Study
  pair; a patient may simultaneously hold an Active-category status in
  different studies.
- Subject generation strictly reuses existing Patient and Study records; it
  never creates new Patients or Studies as a side effect, and requires at
  least one of each to already exist.
- The existing `StudyArm` entity is reused for a subject's optional arm
  assignment rather than introducing a new parallel entity.
- Synthetic data quality need only be structurally and schema-valid, not
  clinically or operationally coherent — consistent with the standard
  already set for synthetic Patient and Study data.
- The Subject domain's admin test-data tooling (generate, counts, lookup,
  reset) is integrated into the four existing Test Data Management tabs
  (Data Counts and Visualizations, Data Generation, Data Manipulation,
  Information and Destruction) rather than introducing a new tab or
  section.
- The Subject API and admin generation endpoints reuse the platform's
  existing auth modes, admin-session protection, and request logging — no
  new cross-cutting infrastructure is introduced by this feature.
- Concurrent `test-data/subjects/generate` requests are not synchronized
  against each other beyond the one-Active-per-patient-per-study
  constraint itself, which is enforced per write; concurrent updates to the
  same subject follow last-write-wins (no optimistic concurrency token),
  consistent with how the rest of the system handles concurrent writes.
