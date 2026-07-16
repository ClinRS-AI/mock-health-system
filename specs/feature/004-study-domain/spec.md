# Feature Specification: Study Domain

**Feature Branch**: `feature/004-study-domain`

**Created**: 2026-07-10

**Status**: Draft — Ready for planning

**Input**: User description: "I'd like to create a new feature / branch for the next set of functionality that will need to be included in the mock health system. The next domain is the Study. Similar to the Patient domain that exists today I'd like to use the CC API endpoints for what to include in the Study domain. This will need new backend database tables for the study to hold the data that is stored in the model as well as API endpoints that mirror what is in the CC API. I will want to create some data faking endpoints similar to the patient data as well."

## Overview

The mock system today exposes the Patient domain (patients, conditions, medications,
procedures) against the Clinical Conductor (CC) Public API surface. This feature adds
the second major CC domain: **Study** — the clinical trial/protocol record that
patients are ultimately screened and enrolled against in CC.

This phase covers the Study record itself — including its contact info — and the
structural sub-resources that describe how a study is organized and run: arms,
visits, milestones, documents, notes, personnel/roles, and protocol versions. It
also adds Mock-Health-System admin configuration for the reference/lookup values
(categories, subcategories, types, statuses, groups) used to populate those
records. It mirrors the same shape of work already done for Patients:
CC-equivalent REST endpoints backed by new database tables, plus admin-driven
synthetic data generation.

**Why this exists**: Developers building CC integrations that touch study setup,
protocol structure, or study-level reporting need a realistic Study surface to test
against, the same way the Patient domain already lets them test patient-record
integrations without a real CC environment.

**Out of scope for this phase** (see Assumptions): linking studies to patients
(CC's Subjects/enrollment — screening number, randomization, treatment status), and
the monitoring/EDC workflow surface (study actions, protocol elements, monitor
queries, engagements). These are deferred to a future feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Retrieve Study Data via the CC API (Priority: P1)

A developer building a CC integration wants to verify that their code correctly
parses study records and their structural detail. They point their client at the
mock and call the same endpoint paths as the Public CC API: the studies list, a
single study's detail (including its embedded contact info), and each structural
sub-resource (arms, visits, milestones, documents, notes, personnel/roles,
protocol versions). Separately, a mock administrator can browse the
Mock-Health-System admin configuration endpoints for reference/lookup values
(categories, subcategories, types, statuses, groups) that populate those records.

**Why this priority**: Read access to realistic study data is the foundation for
every other integration behavior a developer needs to validate — without it,
nothing else in this feature has value.

**Independent Test**: A developer can retrieve a paginated list of studies, fetch a
single study by ID, and retrieve its arms, visits, milestones, and documents — all
without accessing any real CC system.

**Acceptance Scenarios**:

1. **Given** synthetic studies exist, **When** a client GETs the studies list
   endpoint, **Then** a paginated response is returned with the same schema and
   query/filter conventions as the Public CC API.
2. **Given** a known study ID, **When** a client GETs the study detail endpoint,
   **Then** the response contains the study's core fields (identifier, protocol
   number, phase, status, category, sponsor references) and its finance and
   opportunity detail.
3. **Given** a study with arms, visits, and milestones defined, **When** a client
   GETs each corresponding sub-resource endpoint scoped to that study, **Then** only
   records belonging to that study are returned.
4. **Given** the reference/lookup endpoints (study categories, subcategories,
   types, statuses, groups), **When** a client GETs them, **Then** the full set of
   configured values is returned, matching the shape used elsewhere in study
   records (e.g., a study's `category` value corresponds to a name in the
   categories lookup).
5. **Given** the mock is under an active auth mode, **When** a client calls any
   CC-mirrored Study endpoint (the studies list/detail or any structural
   sub-resource) without correct credentials, **Then** the mock returns the same
   error structure (status code and body shape) as the real CC API, consistent with
   existing Patient endpoint behavior.
6. **Given** an admin key is configured, **When** a client calls a Study
   reference/lookup endpoint (categories, subcategories, types, statuses, groups)
   without a valid admin session, **Then** the request is rejected the same way
   the existing `test-data/patients/lookup` admin endpoint rejects one — this is
   Mock-Health-System admin configuration, not part of the CC auth-mode-gated
   surface.

---

### User Story 2 — Manage Study Data via the CC API (Priority: P1)

A developer's integration needs to create and update study records and their
structural sub-resources — for example, standing up a new study with arms and
visits, or updating a study's status and milestones as it progresses. They call the
create, update, partial-update, and delete endpoints for the study and each
structural sub-resource, exactly as they would against the Public CC API.

**Why this priority**: Many CC integrations are bidirectional (e.g., syncing study
setup from a CTMS). Without write support the mock only serves read-only testing
scenarios, which is a smaller fraction of real integration work.

**Independent Test**: A developer can create a new study referencing a sponsor team
and managing site, add an arm and a visit to it, update the study's status, and
delete a milestone — all via API calls, with each change immediately visible on
subsequent reads.

**Acceptance Scenarios**:

1. **Given** valid required references (sponsor team, managing site), **When** a
   client POSTs a new study, **Then** the study is created and returned with a
   generated ID and UID.
2. **Given** an existing study, **When** a client PUTs a full update or PATCHes a
   partial update, **Then** the changed fields are persisted and reflected on the
   next GET.
3. **Given** an existing study, **When** a client POSTs a new arm, visit, milestone,
   document, note, personnel/role assignment, or protocol version scoped to that
   study, **Then** the sub-resource is created and linked to the parent study.
4. **Given** a sub-resource that belongs to a study, **When** a client PUTs an
   update or DELETEs it, **Then** the change is persisted and no longer (or
   correctly) returned on subsequent reads.
5. **Given** an existing study, **When** a client PUTs or PATCHes the study with a
   `contacts` array (IRB, CRO, lab, monitor, or vendor entries), **Then** the
   contact rows are upserted by `(type, slot)` and reflected on the next GET —
   contact info has no separate create endpoint; it is set only as part of the
   Study record's own update payload.
6. **Given** a request to create or update a sub-resource with a reference to a
   study, arm, or visit that does not exist (or belongs to a different study),
   **When** the client submits the request, **Then** the mock rejects it with a
   clear validation error rather than silently creating an inconsistent record.

---

### User Story 3 — Generate Synthetic Study Data for Testing (Priority: P2)

A developer starting a new test cycle needs a realistic set of study records to
work against, without hand-crafting each one. They open the admin interface, choose
how many studies to generate, and click generate. The system creates studies with
realistic (but entirely fictitious) sponsor, protocol, phase, and status data, along
with a populated set of arms, visits, milestones, documents, and notes for each
(including contact info embedded on the study record itself) — the same
convenience the Patient domain already provides.

**Why this priority**: Manual creation of a study plus its structural sub-resources
through the write API is slow; automated generation is what makes the Study domain
immediately useful for testing, the same way it does for Patients.

**Independent Test**: A developer can generate a batch of studies with populated
arms, visits, and milestones, look up a specific one, and reset all synthetic study
data — all from the admin interface without touching the API or database directly.

**Acceptance Scenarios**:

1. **Given** the admin interface, **When** a developer requests generation of N
   studies (within configured limits), **Then** N studies with populated structural
   sub-resources appear and are immediately queryable via the CC Study API
   endpoints.
2. **Given** generated studies exist, **When** the developer looks up a study by a
   name, identifier, or protocol number fragment, **Then** matching results are
   returned.
3. **Given** synthetic study data exists, **When** the developer triggers a study
   data reset, **Then** all synthetic study records (and their sub-resources) are
   removed and the Study API returns empty collections, without affecting existing
   Patient data.
4. **Given** a batch size above the configured maximum, **When** the developer
   requests generation, **Then** the request is rejected with a clear error
   describing the limit.

---

### Edge Cases

- What happens when a client requests a sub-resource (arm, visit, milestone,
  document, etc.) using a study ID that doesn't exist?
- How does the system handle deleting a study that still has arms, visits,
  milestones, documents, or other sub-resources attached — is the delete blocked,
  or does it cascade?
- What happens when a visit-to-arm association is requested for a visit and arm
  that belong to different studies?
- What happens when generation is requested while a previous Study generation batch
  is still running?
- What happens when a study data reset is triggered while Study and Patient data
  both exist — does only Study data get removed?
- How does the system respond when a study references a sponsor team, managing
  site, or lookup value (category, status, type) that doesn't exist?
- What happens when list/query endpoints receive invalid pagination or filter
  parameters?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose CC Study API endpoints for core Study
  operations: list with pagination/filtering, retrieve by ID, create, full update,
  partial update, and delete.
- **FR-002**: The system MUST expose CC Study API endpoints for the following
  structural sub-resources, scoped to their parent study, matching the operations
  (list, detail, create, update, delete) the Public CC API exposes for each: Arms,
  Visits (including visit-to-arm association), Milestones, Documents (including
  status history), Notes, Personnel/Roles, and Protocol Versions. Study contact
  information (IRB, CRO, lab, monitor, vendor) MUST be persisted as
  relationally-modeled records associated with the parent study, but is read and
  written through the Study record's own endpoints rather than a separate
  contacts endpoint, matching how the Public CC API embeds it.
- **FR-003**: The system MUST expose Mock-Health-System admin endpoints for
  managing the Study reference/lookup values (Categories, Subcategories, Types,
  Statuses, and Groups) used to populate and validate study records. These are
  admin configuration surfaces, not part of the CC-mirrored integration surface,
  and MUST use the same admin authentication as other Mock-Health-System admin
  functionality (e.g., the synthetic-data lookup endpoints), not the active CC
  auth mode.
- **FR-004**: All implemented Study endpoints MUST return structurally valid,
  non-placeholder responses that match the field names and shapes of the Public CC
  API; no endpoint may return stub data when called with valid inputs and
  credentials.
- **FR-005**: The system MUST persist Study and sub-resource data relationally,
  rejecting writes that reference a parent study, arm, or visit that does not exist
  or does not belong to the referenced parent.
- **FR-006**: The admin interface MUST allow generating synthetic Study records,
  with their structural sub-resources populated, in configurable batch sizes up to
  a defined maximum, following the same generation pattern as the Patient domain.
- **FR-007**: The admin interface MUST allow looking up generated studies by name,
  identifier, or protocol number fragment.
- **FR-008**: The admin interface MUST allow resetting all synthetic Study data in a
  single operation, independent of Patient data, without restarting the service.
- **FR-009**: Study API endpoints MUST honor the auth mode and admin-session
  protections already enforced elsewhere in the system; no separate authentication
  mechanism is introduced for the Study domain.
- **FR-010**: Every Study API request MUST be captured by the existing request
  logging/monitoring infrastructure with no Study-specific exemptions.
- **FR-011**: The system MUST NOT expose Study-to-Patient enrollment linkage
  (CC's Subjects/PatientStudy surface) or the monitoring/EDC workflow surface
  (study actions, protocol elements, monitor queries, engagements) in this phase.

### Key Entities

- **Study**: The core clinical trial/protocol record — identifier, title, protocol
  number, phase, IND/IDE number, NCT number, status, category/subcategory, tags,
  description, launch year, finance detail, opportunity detail, enrollment/budget/
  regulatory/contract notes, and references to its sponsor team and managing site.
- **StudyArm**: A treatment arm within a study — name, status, patient goal/limit,
  linked protocol version.
- **StudyVisit**: A visit definition within a study, associated with one or more
  arms.
- **StudyMilestone**: A tracked milestone for a study — name, category, importance,
  status, assigned staff member, scheduling, projected and completed dates.
- **StudyDocument**: A regulatory/study document — type, status (with history),
  version, source, effective and expiration dates.
- **StudyNote**: A free-text note attached to a study.
- **StudyContact**: A relationally-modeled contact record (IRB, CRO, lab, monitor,
  or vendor; up to two per type) associated with a study. Modeled as a
  first-class entity — not flattened fields on Study — so the same shape can be
  reused by future parent entities beyond Study. Read and written through the
  Study record's own endpoints, matching the Public CC API's embedded (not
  separately routable) contact shape.
- **StudyRole**: A role defined for a study (e.g., coordinator), with staff members
  assigned to it.
- **ProtocolVersion**: A versioned protocol definition belonging to a study.
- **Study reference/lookup data** (StudyCategory, StudySubcategory, StudyType,
  StudyStatus, StudyGroup): Configured values used to populate and validate study
  fields, mirroring the CC system-level lookup endpoints.
- **Sponsor / Sponsor Division / Sponsor Team**: The organization and team a study
  is run on behalf of; a study must reference a sponsor team.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can retrieve a study (including its embedded contact
  info) and every one of its structural sub-resources (arms, visits, milestones,
  documents, notes, personnel/roles, protocol versions) using only the mock's
  Study API, with no real
  CC system involved.
- **SC-002**: Every implemented Study endpoint returns a structurally valid,
  non-placeholder response for valid inputs and credentials.
- **SC-003**: Synthetic data for a batch of studies with populated structural
  sub-resources can be generated in under 30 seconds, consistent with the
  performance already delivered for Patient generation.
- **SC-004**: Resetting synthetic Study data removes all Study records and their
  sub-resources while leaving existing Patient data untouched.
- **SC-005**: A developer unfamiliar with the Study domain can generate synthetic
  studies, retrieve one via the API, and locate it again by name/identifier/protocol
  fragment in the admin interface within 10 minutes of first use.

## Assumptions

- The Study domain is defined against the CC Public API OpenAPI specification
  (V1.0) as retrieved during specification — specifically the `Study`, `StudyArm`,
  `StudyVisit`, `StudyMilestone`, `StudyDocument`, `StudyNote`, `StudyContact`,
  `StudyRole`, `ProtocolVersion`, and study lookup (`StudyCategory`,
  `StudySubcategory`, `StudyType`, `StudyStatus`, `StudyGroup`) resource groups.
- Study-to-Patient enrollment (CC's Subjects/PatientStudy linkage — screening
  number, randomization number, enrollment date, treatment status) is explicitly
  deferred to a future feature and is not part of this specification.
- The monitoring/EDC workflow surface (study actions, protocol elements, monitor
  queries, engagements) is explicitly out of scope for this phase.
- `Sponsor`, `Sponsor Division`, and `Sponsor Team` are new minimal reference
  entities introduced only because a Study record requires a sponsor team
  reference; a full sponsor-management domain (contacts, CRM-style tracking) is out
  of scope.
- The existing `Site` and `Staff` entities are reused for a study's managing site
  and personnel/leadership/role-assignment references rather than introducing new
  parallel entities.
- Study custom fields are represented as a generic name/value list matching the
  shape returned by the CC API; a custom-field-definition management module is out
  of scope.
- Synthetic data quality need only be structurally and schema-valid, not clinically
  or operationally coherent — consistent with the standard already set for
  synthetic Patient data.
- Batch generation limits and admin-interface conventions (lookup, reset) follow
  the same pattern already established by the Patient domain's test-data tooling.
- The Study API and admin generation endpoints reuse the platform's existing auth
  modes, admin-session protection, and request logging — no new cross-cutting
  infrastructure is introduced by this feature.
- Study reference/lookup management (Categories, Subcategories, Types, Statuses,
  Groups) is treated as Mock-Health-System admin configuration rather than CC
  integration traffic, and is protected the same way the existing
  `test-data/patients/lookup` admin endpoint is — not gated by the active CC auth
  mode.
- Concurrent `test-data/studies/generate` requests are not synchronized against
  each other: because study and sub-resource IDs are database-assigned
  (auto-increment), overlapping generation calls each produce their own valid,
  non-colliding batch rather than corrupting data — the only effect is more
  studies existing afterward than a single call requested. Concurrent updates to
  the same study or sub-resource follow last-write-wins (no optimistic
  concurrency token), consistent with how the rest of the system, including the
  Patient domain, already handles concurrent writes.
