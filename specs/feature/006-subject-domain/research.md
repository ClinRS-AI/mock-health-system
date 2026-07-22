# Research: Subject Domain

No blocking `NEEDS CLARIFICATION` markers remain in the Technical Context. This
document records the implementation-level decisions made while reconciling the
spec with the existing codebase, and resolves the one research risk the spec
flagged explicitly (Subject field shapes / CC endpoint surface).

## Decision 1: Subject and SubjectStatus field shapes

**Decision**: Model `Subject` with the fields CC's Public API exposes for an
enrollment record: a generated `Id`/`Uid` pair (matching the `Study`/`StudyArm`
convention), `PatientId` (FK), `StudyId` (FK), `StudyArmId` (optional FK),
`Status` (string, validated against the fixed 9-value CC vocabulary — see
Decision 11), `SubjectIdentifier` (optional — the screening/subject number CC
calls this field; not every subject has one assigned, e.g. before screening
completes), `EnrollmentDate`, `ScreeningDate` (optional), `WithdrawalDate`
(optional), `WithdrawalReason` (optional), `CreatedOn`/`LastUpdatedOn`. Model
`SubjectStatus` as a lean history row: `Id`, `SubjectId` (FK), `StatusName`,
`ChangedOn`, `ChangedByStaffId` (optional FK), `Comment` (optional) — an exact
structural mirror of `StudyDocumentStatusHistory`.

**Rationale**: A live fetch of the CC OpenAPI document during specification
(and again attempted during planning) did not surface Subject- or
SubjectStatus-tagged endpoints — 4 separate fetch attempts against
`https://sales.clinicalconductor.com/CCSWeb/api/openapi/V1.0` returned only 8
unrelated tags across every attempt this session, despite this same source
having previously produced the Study domain's detailed field list. This is a
tooling reliability problem, not evidence the endpoints don't exist — the user
independently confirmed the real endpoint path
(`/api/v1/studies/{studyUid}/subject-statuses/odata`) unprompted, and later
directly supplied the real 9-value status vocabulary (see Decision 11),
resolving what was the single largest remaining unknown in this field list.
CTMS systems universally expose the rest of this field set for enrollment
tracking, so the plan proceeds on standard CTMS domain knowledge plus this
project's own established `StudyDocument`/`StudyDocumentStatusHistory` shape
for anything not directly confirmed, rather than blocking on a live fetch
that has not worked in 5 attempts. Field names use this project's existing
PascalCase-mirrors-CC-camelCase convention (`SubjectIdentifier`,
`EnrollmentDate`, etc.) consistent with how `Study`/`StudyDocument` name
theirs.

**Alternatives considered**:
- Block planning on a working CC OpenAPI fetch — rejected: the tool has failed
  5 times across two sessions; there's no reason to expect a 6th attempt
  succeeds, and the user has already supplied the one concrete fact (the
  status-history endpoint path) that most needed external confirmation.
- Model Subject with a minimal field set (just PatientId/StudyId/Status) —
  rejected: the spec's FR-002 explicitly requires enrollment dates and a
  subject/screening identifier; CC's real Subject surface always carries
  these, and the Study domain's own precedent is to model the realistic field
  set even when exact CC names could not be verified live (see
  `specs/feature/004-study-domain/research.md`'s equivalent risk note).

## Decision 2: `studyUid`-keyed route for the subject-status history endpoint

**Decision**: Implement `GET /api/v1/studies/{studyUid:guid}/subject-statuses/odata`
in a new `SubjectStatusesController` with route
`api/v{version:apiVersion}/studies/{studyUid:guid}/subject-statuses`, resolving
the study via `_db.Studies.FirstOrDefaultAsync(s => s.Uid == studyUid)` before
querying status history for subjects belonging to that study (a join through
`Subject.StudyId`). Return 404 if no study has that UID.

**Rationale**: Every other Study sub-resource route in this codebase
(`/studies/{studyId:int}/documents`, `/visits`, `/arms`, `/milestones`,
`/notes`) is keyed by the integer `Id`, not `Uid`. The user explicitly
specified `studyUid` for this one endpoint, matching CC's real API — CC's
OData-style endpoints are commonly UID-keyed even where other CC endpoints for
the same parent use numeric IDs, so this is treated as an authentic
CC-fidelity requirement rather than an inconsistency to "fix." A route
constraint of `{studyUid:guid}` disambiguates this route from the existing
`{studyId:int}` sibling routes at the routing level with no conflict, since
ASP.NET Core route constraints are type-checked before dispatch.

**Alternatives considered**:
- Use `{studyId:int}` for consistency with sibling sub-resources — rejected:
  contradicts the concrete endpoint path the user supplied, which is meant to
  mirror the real CC API exactly (FR-005, FR-007).
- Fold this endpoint into `StudiesController` instead of a new controller —
  rejected: `StudiesController` is scoped to `{studyId:int}`-keyed routes
  throughout; mixing a `{studyUid:guid}` route into the same controller class
  is unusual for this codebase's convention of matching route/controller
  scope 1:1, and `StudyDocumentsController`'s existing precedent is a
  dedicated controller per sub-resource anyway.

## Decision 3: Subject controller and route shape

**Decision**: Add a top-level `SubjectsController` at
`api/v{version:apiVersion}/subjects` (not nested under `/studies` or
`/patients`) with `GET` (list, filterable by `patientId`/`studyId`, paginated),
`GET /odata`, `GET /{id:int}`, `POST`, `PUT /{id:int}`, `PATCH /{id:int}`,
`DELETE /{id:int}` — mirroring `StudiesController`'s top-level CRUD shape
exactly, including its `IncludeAll`/`ValidateReferencesAsync` pattern.

**Rationale**: A Subject is fundamentally a many-to-one-to-many join record
(Patient × Study), not an owned child of either — CC itself exposes Subjects
as a top-level resource filterable by patient/study query parameters, not as
a nested path under either. This matches FR-001's "list with pagination and
filtering by patient and by study" requirement directly (query params, not
path nesting) and follows the same top-level-resource-with-filters pattern
`StudiesController.GetStudies` already uses for `name`/`status`/`category`.

**Alternatives considered**:
- Nest under `/patients/{patientId}/subjects` or `/studies/{studyId}/subjects`
  — rejected: a Subject belongs equally to both parents, so nesting under
  either is arbitrary, and CC's own filterable top-level pattern (mirrored by
  this project's `GET /studies?category=...`) is the better fit.

## Decision 4: One-Active-category-status-per-Patient-per-Study enforcement

**Decision**: In `SubjectsController`, before create/update, run a tracking
query: if `editModel.Status` is one of the four Active-category values (see
Decision 11's `SubjectStatusCatalog.ActiveStatuses`), check
`_db.Subjects.Where(s => s.PatientId == patientId && s.StudyId == studyId && SubjectStatusCatalog.ActiveStatuses.Contains(s.Status) && s.Id != currentId).AnyAsync(...)`;
if true, return 400 with a clear validation message. This runs inside the
same `ValidateEditModelAsync`-style helper `StudiesController` already
establishes, called before `SaveChangesAsync`. Note this is deliberately
**not** a literal `Status == "Active"` check — "Active" is not itself one of
CC's nine real status values, it's a category covering "Prescreened",
"Screened", "Randomized", and "Run-in" (the user corrected an earlier draft
of this spec that had wrongly modeled "Active" as a literal status string).

**Rationale**: Directly implements FR-003. Using a tracking existence check
(not a DB unique constraint) matches this codebase's established pattern of
enforcing business-rule constraints in application code with a `BadRequest`
response (e.g., `StudiesController.ValidateReferencesAsync`,
`StudyRolesController`'s duplicate-staff check) rather than relying on a
database constraint that would surface as an opaque 500 via
`ExceptionHandlingMiddleware`. A partial unique index
(`WHERE "Status" = 'Active'`) was considered as a belt-and-suspenders backstop
but is unnecessary complexity for a mock system with no concurrent-write SLA
beyond what the rest of the system already provides (see spec Assumptions on
last-write-wins concurrency handling).

**Alternatives considered**:
- PostgreSQL partial unique index on `(PatientId, StudyId) WHERE Status IN
  ('Prescreened', 'Screened', 'Randomized', 'Run-in')` — rejected as the
  primary mechanism: EF Core's InMemory provider
  (used for all tests per constitution Principle III) does not enforce
  Postgres-specific partial indexes, so a test suite relying on it would pass
  against InMemory while a real Postgres deployment might behave differently
  on a race — the application-level check is the one mechanism verifiable in
  both environments identically.

## Decision 5: Status-history recording on create/update

**Decision**: `SubjectsController.CreateSubject` and `UpdateSubject`/
`PatchSubject` append a `SubjectStatus` row whenever the subject is created
(always, using its initial status) or whenever `Status` changes on update —
an exact structural mirror of `StudyDocumentsController.CreateDocument`/
`UpdateDocument`'s `statusChanged` check and two-`SaveChangesAsync` pattern
(insert the parent first to get its generated `Id`, then insert the history
row referencing that `Id`).

**Rationale**: Directly implements FR-004 and User Story 2's acceptance
scenario 8, reusing a pattern already proven correct and tested in this
codebase rather than inventing a new one.

## Decision 6: Reset cascade — Patient/Study resets clearing Subject data for free

**Decision**: No new logic is required in `ResetPatientsAsync` or
`ResetStudiesAsync` to satisfy FR-012. Both existing reset actions already use
`TRUNCATE TABLE ... RESTART IDENTITY CASCADE` — PostgreSQL's `TRUNCATE ...
CASCADE` truncates *every* table with a foreign key referencing any table in
the explicit list, even tables not named in the statement. Once `Subjects` has
FK columns to `Patients` and `Studies` (and `SubjectStatuses` has an FK to
`Subjects`), truncating `Patients` or `Studies` automatically empties
`Subjects` and `SubjectStatuses` too, with no changes to the existing
truncate SQL strings.

**Rationale**: This is a direct, verifiable Postgres behavior (confirmed
against the `TRUNCATE` documentation's cascade semantics) that fully satisfies
FR-012 and acceptance scenario 6 without duplicating table names across three
different reset endpoints — the exact kind of "generalize the underlying
mechanism instead of adding a special case" outcome the constitution's
altitude expectations favor. It must be verified empirically during
implementation (integration test: reset patients while subjects referencing
those patients exist → subject count drops to 0) since InMemory EF (used in
most tests) does not exercise real Postgres TRUNCATE CASCADE — this specific
test needs the live-Postgres verification step already used for prior
features in this session, not just the InMemory suite.

**Alternatives considered**:
- Explicitly add `"Subjects"`/`"SubjectStatuses"` to both existing TRUNCATE
  lists anyway, as defense-in-depth — rejected: redundant given CASCADE's
  documented behavior, and adding table names that duplicate what CASCADE
  already guarantees increases maintenance surface (a third place to update
  if the Subject schema changes) for no behavioral benefit. The Subject
  domain's own `subjects/reset` action still explicitly lists `"Subjects"`
  and `"SubjectStatuses"` since that reset has no other table to cascade from.

## Decision 7: SubjectFakerService and generation reuse-only constraint

**Decision**: New `SubjectFakerService` (Bogus-based, mirrors
`StudyFakerService`'s constructor-takes-prerequisite-ID-lists shape) takes the
full list of existing `PatientId`s and `(StudyId, [StudyArmId])` pairs,
builds the cross product, filters out any (patient, study) pair that would
create a second Active-category subject (respecting whatever Active-category
subjects already exist for a pair, per `SubjectStatusCatalog` from Decision
11, mirroring `StudyFakerService`'s `PickRandom`-without-replacement approach
for arms/contacts), and picks up to `totalCount` combinations without
replacement. Each generated `Subject` is assigned a status drawn from the
full 9-value vocabulary via `Faker.PickRandom(SubjectStatusCatalog.AllStatuses)`
and gets exactly one `SubjectStatus` row at that initial status (mirrors
`CreateDocuments`'s `document.StatusHistory.Add(...)` pattern). If fewer valid
combinations exist than requested, the service returns as many as it can
(edge case already documented in spec.md), and `TestDataController` reports
actual vs. requested counts the same way `GenerateStudiesResponse` does.

**Rationale**: Directly implements FR-008 and spec.md's edge case for
insufficient valid combinations. Reuses the exact `PickRandom`-based
without-replacement idiom the Study domain's second code-review pass already
established as the simplification target for "pick N distinct items from a
candidate set," avoiding reintroducing the `slotByType`/`continue`-loop
pattern that review flagged as unnecessarily complex.

**Alternatives considered**:
- Allow generation to create a concurrently-Active duplicate and rely on the
  controller's validation to reject it mid-batch — rejected: silently
  dropping requested count without a clear "why" in the response would
  contradict FR-008's requirement to fail clearly / report actual counts, and
  filtering the candidate set up front is simpler than catching per-item
  validation failures during a bulk insert.

## Decision 8: TestDataController additions

**Decision**: Add `subjects/generate`, `subjects/reset`, `subjects/lookup`,
`subjects/random`, `subjects/stats` actions to the existing
`TestDataController`, following the exact same admin-gate
(`_adminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)`),
batch-size-cap (`maxCount = 500`, matching Study), and DTO-per-action pattern
as the `studies/*` actions. `subjects/stats` returns total Subject count plus
a per-study breakdown of distinct enrolled patients (`GroupBy(s => s.StudyId)`
→ `Select(g => new { StudyId, StudyName, PatientCount = g.Select(x =>
x.PatientId).Distinct().Count() })`), satisfying FR-009 directly.

**Rationale**: No new pattern needed — this is the third domain
(Patient, Study, Subject) to follow the identical generate/reset/lookup/
random/stats action shape in the same controller, reinforcing rather than
diverging from established convention.

## Decision 9: Frontend integration into the existing four tabs

**Decision**: Extend the four existing section components exactly where their
Study equivalents already live, with no new components or tabs:
- `TestDataCountsSection.tsx`: add a Subject total-count stat card and a
  `CategoryPieChart` (the shared local component already used for
  patients-by-site/studies-by-status) for patients-by-study, sourced from a
  new `getSubjectTestDataStats()` call run in the same `Promise.all()` as the
  existing stats fetches.
- `TestDataGenerationSection.tsx`: add a "Generate Subjects" batch-size form
  calling a new `generateTestSubjects()`, following the existing
  generate-studies button/result-summary pattern.
- `TestDataManipulationSection.tsx`: add a Subject lookup form (by ID, or by
  patient + study) calling a new `lookupTestSubject()`, following the
  existing study-lookup form pattern (multiple optional query params).
- `TestDataInfoDestructionSection.tsx`: add a `ConfirmableResetButton` for
  Subject data (the shared local component already used for patient/study
  resets) calling a new `resetTestSubjects()`.
- `demoData.ts`: add `DEMO_SUBJECT_TEST_DATA_STATS`, following the exact
  `DEMO_STUDY_TEST_DATA_STATS` precedent (including the demo-mode bug fix
  already applied there — both admin-session and demo-mode code paths must
  set it).

**Rationale**: FR-006–FR-009 (renumbered FR-008–FR-011 relative to Study's own
FRs) explicitly require integration into the existing four tabs, not a new
tab — this is a hard constraint from both this feature's spec and the just
-completed 005 dashboard reorg, whose entire purpose was consolidating
scattered functionality into exactly these four areas. Reusing
`CategoryPieChart` and `ConfirmableResetButton` (both already generalized,
reusable local components as of 005's code-review fix pass) means zero new UI
components are needed.

**Alternatives considered**: None seriously — the four-tab structure and its
shared components are settled precedent from the immediately prior feature;
introducing anything new here would contradict that work's stated purpose.

## Decision 10: Migration

**Decision**: A single new EF Core migration adds `Subjects` and
`SubjectStatuses` tables via `backend/scripts/run-ef.sh migrations add
AddSubjectDomain`, following FK/cascade configuration in `OnModelCreating`:
`Subject.Patient` → `DeleteBehavior.Cascade` (removing a patient removes their
subjects, consistent with `TRUNCATE CASCADE` and FR-012's intent even for
non-truncate deletes), `Subject.Study` → `DeleteBehavior.Cascade`,
`Subject.StudyArm` → `DeleteBehavior.SetNull` (matches
`StudyArmsController`/other optional-FK conventions — losing an arm shouldn't
delete the subject), `SubjectStatus.Subject` → `DeleteBehavior.Cascade`,
`SubjectStatus.ChangedByStaff` → `DeleteBehavior.SetNull`. Also add a unique
index on `Subject.Uid` (`HasIndex(x => x.Uid).IsUnique()`), matching the
convention already applied to every other `Uid`-bearing entity in this
codebase (`Study`, `StudyArm`, `StudyVisit`, `StudyDocument`,
`ProtocolVersion`, `Sponsor`).

**Rationale**: Matches the exact `DeleteBehavior` choices already used for the
structurally identical `StudyDocument`/`StudyDocumentStatusHistory` pair and
`StudyArm.ProtocolVersion` (optional FK → SetNull). Configuring `Cascade` at
the EF/Postgres FK level (not just relying on the reset endpoints' explicit
`TRUNCATE`) also means a plain `DELETE FROM "Patients" WHERE ...` or a future
non-reset deletion path still correctly removes dependent Subject data,
consistent with FR-012's intent beyond just the reset buttons.

## Decision 11: Subject status vocabulary and validation

**Decision**: The user supplied CC's real, closed Subject status vocabulary
directly (correcting an earlier draft of this spec that had modeled a
literal `"Active"` status string, which does not exist in CC's real vocabulary
— "Active" is a category, not a value). There are nine values, split into two
categories relevant to FR-003's one-Active-per-pair rule:

- **Active category** (4): `Prescreened`, `Screened`, `Randomized`, `Run-in`
- **Inactive category** (5): `Screen Failed`, `Non Qualified`, `Dropped`,
  `Run-in Failed`, `Complete`

Add a small static `SubjectStatusCatalog` (mirrors `StudyFakerService`'s
`private static readonly string[]` constant-list pattern, but public and
shared) exposing `ActiveStatuses`, `InactiveStatuses`, `AllStatuses`, and an
`IsActiveCategory(string status)` helper, in
`backend/MockHealthSystem.Api/Models/Subjects/SubjectModels.cs` (alongside
`SubjectViewModel`/`SubjectEditModel`, avoiding a dedicated extra file for
what is a small constant list). `SubjectsController`'s write validation
(Decision 4) rejects any `Status` not in `AllStatuses` (FR-006) and enforces
the one-Active-category-per-pair rule via `IsActiveCategory`.
`SubjectFakerService` (Decision 7) uses the same catalog for both its status
assignment and its Active-category candidate filtering, so the vocabulary is
defined in exactly one place.

**Rationale**: Directly implements the corrected FR-002/FR-003/FR-006. A
single shared catalog (rather than duplicating the string lists in the
controller and the faker service, which was the risk of leaving this
implicit) guarantees the validation logic and the generation logic can never
drift out of sync on what counts as "Active."

**Why an in-code list, not a `SubjectStatusType` lookup table**: `Study.Status`
references an admin-configurable `StudyStatusType` database table, because
Study statuses in this mock's domain are project-configurable. Subject
statuses are different: CC does not allow customizing this vocabulary — it is
a fixed part of CC's own Subject workflow, not a per-deployment configuration
value. Modeling it as an admin-editable lookup table would let synthetic data
drift into non-CC-shaped values, undermining the whole point of CC fidelity
(FR-007). A hardcoded, single-source-of-truth constant list is the more
CC-faithful choice.

**Alternatives considered**:
- A C# `enum SubjectStatusValue` instead of string constants — rejected:
  every other status-like field in this codebase (`Study.Status`,
  `StudyDocument.StatusName`, `StudyArm.Status`) is a plain `string` column,
  not a C# enum backed by an integer column; matching that convention keeps
  `SubjectStatus.StatusName`/`Subject.Status` consistent with the rest of the
  schema and avoids an EF enum-to-string conversion configuration this
  codebase doesn't otherwise use.
- A `SubjectStatusType` lookup table mirroring `StudyStatusType` — rejected
  per the CC-fidelity rationale above.
