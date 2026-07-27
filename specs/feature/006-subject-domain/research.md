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

**Superseded by Decision 12**: after implementation, the user compared this
shape directly against a real CC Subject response and found it diverged
significantly (flat FK ids instead of nested previews, a missing
Site/ProtocolVersion relationship, several missing CC fields, three invented
fields not in CC's real schema). Decision 12 documents the correction; this
decision's rationale is kept for historical context but the field list above
is no longer current — see `data-model.md` for the authoritative shape.

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

## Decision 12: Correcting Subject's shape against a real CC response

**Decision**: After the Subject domain was implemented per Decision 1's
best-effort field list, the user pasted the actual response returned by a
real CC Subject GET endpoint. It differs substantially from what was built.
This response is now the authoritative source, superseding Decision 1's
assumptions. Concrete corrections:

- **Nested previews, not flat FK ids.** CC returns `study`, `site`, `patient`,
  `protocolVersion`, and `arm` as nested `{id, uid, name}`-shaped (or richer,
  for `patient`) objects, not flat `studyId`/`siteId`/`patientId`/
  `protocolVersionId`/`studyArmId` scalars. `SubjectViewModel` now mirrors
  `StudyViewModel`'s existing convention exactly (`ManagingSite`,
  `SponsorTeam` are nested previews there too) — reusing
  `StudyPreviewModel`, `SitePreviewModel`, `StudyArmPreviewModel`,
  `ProtocolVersionPreviewModel` (all pre-existing) plus a new
  `SubjectPatientPreviewModel` for the richer patient object CC embeds
  (first/middle/last name, title, gender/race/ethnicity, date of birth —
  fields `Patient` already carries).
- **Two relationships were missing entirely.** CC's Subject has its own
  `site` and `protocolVersion` references, independent of `Patient.PrimarySite`
  and `Study`. Added `Subject.SiteId` (optional FK → `Sites`,
  `DeleteBehavior.SetNull` — matches `Study.ManagingSite`'s precedent) and
  `Subject.ProtocolVersionId` (optional FK → `ProtocolVersions`, must belong
  to the same Study, `DeleteBehavior.Restrict` — matches
  `StudyArm.ProtocolVersion`/`StudyVisit.ProtocolVersion`'s precedent).
- **CC fields that were missing**: `importId`, `tag`, `facilityCode`,
  `enrollmentLocation`, `randomizationNumber` (distinct from
  `screeningNumber`), `treatmentStatus` (distinct from the top-level
  `status` — free text, CC's real vocabulary for it is unknown), `treatmentStart`,
  `narrative`, and top-level `genderCode`/`race`/`ethnicity` (a subject-level
  demographic snapshot, real stored columns — CC duplicates these outside the
  nested `patient` object, consistent with how CTMS systems lock enrollment
  -time demographics for regulatory reporting even as the patient's live
  record changes). All added as nullable columns/fields; the faker
  independently fakes plausible values for `genderCode`/`race`/`ethnicity`
  rather than copying the referenced patient's actual values, to avoid an
  extra patient lookup in `SubjectFakerService` for a mock-data field where
  exact correlation isn't load-bearing.
- **Field renamed**: `SubjectIdentifier` → `ScreeningNumber`, matching CC's
  actual field name (`screeningNumber`).
- **Three invented fields removed**: `ScreeningDate`, `WithdrawalDate`,
  `WithdrawalReason` don't exist in CC's real schema — they were guesses made
  when the live OpenAPI fetch failed during specification (Decision 1).
- **`arm`'s casing in CC's own example** (`{"Id":0,"Uid":"...","Name":"..."}`,
  PascalCase, unlike every other field in the same response) is judged to be
  a Swagger example-generation artifact on CC's side, not a real
  serialization difference — not replicated. `Arm` is exposed camelCase like
  the rest of this API and the rest of CC's own response.
- **Kept, not in CC's example**: `createdOn`/`lastUpdatedOn` — additive-only,
  matches this project's own established convention on every other
  ViewModel (`StudyViewModel`, etc.); CC's Swagger example likely just omits
  audit/meta fields from its documented example rather than not having them.

**Write path unchanged in shape**: `SubjectEditModel`/`SubjectPatchModel`
still take flat scalar FK ids (`PatientId`, `StudyId`, `SiteId?`,
`StudyArmId?`, `ProtocolVersionId?`) — this was already correct and mirrors
`StudyEditModel.ManagingSiteId`'s established nested-on-read/flat-on-write
split; CC's response shape only concerns the read side.

**Alternatives considered**:
- Leave the flat-id shape and just add the missing fields — rejected: the
  user's explicit ask was to match CC "as closely as possible," and the
  flat-vs-nested divergence was the single largest structural gap, not a
  minor detail.
- Auto-populate `Subject.GenderCode`/`Race`/`Ethnicity` from the referenced
  `Patient`'s current values at create/generate time — rejected for the
  write path (adds a patient-lookup dependency to `SubjectsController` for
  a non-load-bearing convenience) but reconsidered per-caller: clients can
  already set these explicitly via `SubjectEditModel`, and the faker fakes
  independently. If real CC semantics turn out to require exact
  patient-value snapshotting, this can be revisited without a further shape
  change (the columns already exist).

## Decision 13: Narrowing the Subject POST body to CC's real create shape

**Decision**: The user compared this app's POST `/subjects` request body
against CC's real Subject creation payload and found this app accepted six
fields CC's create endpoint does not: `studyArmId`, `protocolVersionId`,
`status`, `genderCode`, `race`, `ethnicity`. CC's real POST body accepts only
13 fields: `patientId`, `studyId`, `siteId`, `importId`, `tag`,
`facilityCode`, `enrollmentDate`, `enrollmentLocation`, `screeningNumber`,
`randomizationNumber`, `treatmentStatus`, `treatmentStart`, `narrative`. Those
six extra fields are set later, via `PUT`/`PATCH`, not at creation.

- **New `SubjectCreateModel`**, used only by `POST /subjects`, contains
  exactly the 13 CC-accepted fields. `SubjectEditModel` (now documented as
  PUT-only) keeps the full field set including `StudyArmId`/
  `ProtocolVersionId`/`Status`/`GenderCode`/`Race`/`Ethnicity` — those remain
  settable via `PUT`/`PATCH`, just not at creation. `SubjectPatchModel` is
  unaffected.
- **Server-assigned initial status**: since `status` isn't client-settable at
  creation, every new Subject starts at `SubjectStatusCatalog.InitialStatus =
  "Prescreened"` (the first status in CC's chronological vocabulary, and
  already an Active-category status, so the existing
  one-Active-category-per-pair conflict check still applies meaningfully to
  creates).
- **`SubjectMappingService.ApplyCreateModel`** is a new method distinct from
  `ApplyEditModel` — it sets the 13 create fields plus the server-assigned
  status, and deliberately leaves `StudyArmId`/`ProtocolVersionId`/
  `GenderCode`/`Race`/`Ethnicity` at the entity's defaults (null).
- **`SubjectsController.CreateSubject`** validates with `StudyArmId: null`,
  `ProtocolVersionId: null`, `Status: SubjectStatusCatalog.InitialStatus` —
  `ValidateEditModelAsync`'s per-field checks for those three now only ever
  fire from `PUT`/`PATCH`, since `POST` can no longer supply values for them.
- **`SubjectFakerService` is unaffected.** It constructs `Subject` entities
  directly rather than going through `SubjectCreateModel`/`SubjectEditModel`,
  so it still sets `StudyArmId`/`ProtocolVersionId`/a random status (from
  `SubjectStatusCatalog.AllStatuses`, not just `InitialStatus`) directly on
  generated entities — this mirrors how test-data generation already bypasses
  API-level constraints elsewhere in the codebase to produce varied data.
- **Test impact**: the three integration tests that POSTed an out-of-scope
  field to trigger a 400 (invalid status, protocol-version/study mismatch,
  study-arm/study mismatch) no longer exercise reachable behavior on
  `POST` — those fields are silently dropped by model binding rather than
  validated. Relocated as `UpdateSubject_Returns400_When...` tests against
  `PUT`, which still accepts and validates all three.

**Alternatives considered**:
- Keep one `SubjectEditModel` for both POST and PUT, and just ignore the
  extra fields in `ApplyCreateModel`-equivalent logic within the same
  controller action — rejected: the six fields would still be present (and
  silently no-op) in the OpenAPI-visible request schema for POST, which is
  exactly the mismatch-with-CC's-real-schema the user flagged. A distinct
  `SubjectCreateModel` makes the accepted-field set self-documenting via the
  type itself.
- Reject unknown JSON properties on POST (e.g. via strict model binding) so
  sending `status`/`studyArmId`/etc. at creation is a 400 instead of a silent
  no-op — rejected: this project doesn't use strict/unknown-property
  rejection anywhere else, and CC's own real API doesn't error on extra POST
  fields either (based on the user's comparison); silent-ignore matches real
  CC behavior most closely.

## Decision 14: Wrapping `GET /subjects/odata` in CC's paged result shape

**Decision**: The user compared `GET /subjects/odata`'s response against
CC's documented shape and found this app returns a bare JSON array where CC
wraps results in `{ "Items": [...], "NextPageLink": "string", "Count": 0 }`,
and accepts a `queryOptions` header plus a `studyId` query parameter this
app's endpoint lacked entirely. Rather than invent a new paging shape, this
adopts an existing in-repo precedent directly: `SystemController.cs`'s
`conditions/odata`/`medications/odata`/`allergies/odata` endpoints already
return `MockHealthSystem.Api.Models.System.ODataPageResult<T>` and already
accept the identical `queryOptions`/`skip`/`top` parameters.

- **`ODataPageResult<SubjectViewModel>`** (reusing the existing generic type
  from `Models/System/`, not a Subject-local duplicate) replaces the bare
  `IEnumerable<SubjectViewModel>` return shape. `Items` holds the page,
  `Count` is the total matching row count computed before paging, and
  `NextPageLink` is always `null` — matching every existing usage of this
  type; no endpoint in this codebase computes a real next-page link.
- **`queryOptions`** (`[FromHeader(Name = "queryOptions")] string?`) is
  accepted but never parsed — an unused pass-through kept only for wire
  compatibility with real CC clients, exactly matching `SystemController`'s
  own documented rationale for the same parameter.
- **`studyId`** (`[FromQuery] int?`) has no existing `/odata` precedent
  elsewhere in this codebase, because every other `/odata` action belongs to
  a route already scoped under its parent (e.g.
  `/studies/{studyId}/milestones/odata`), so the parent id comes from the
  route, not the query string. `Subject` is a top-level resource
  (`/subjects`, no parent segment), so `studyId` is a query filter instead —
  mirroring how the sibling `GetSubjects` action (non-odata) already filters
  by `studyId` the same way.
- **`top` is clamped via the existing `SubjectSearchLimits.ClampLimit`**
  (already used by `GetSubjects`, caps at `MaxLimit = 5000`) rather than
  duplicating `SystemController`'s inline clamp (which has no upper bound) —
  this keeps pagination behavior consistent across both of `SubjectsController`'s
  list actions rather than introducing a second, less-safe paging rule.
- **`GET /subjects` (non-odata) is unchanged** — it keeps its own
  `skip`/`limit` naming and bare-array response; the user's request was
  scoped to `/subjects/odata` specifically, and CC's own real API keeps
  these as two distinct endpoints with two distinct shapes.

**Alternatives considered**:
- Define a Subject-local paging wrapper instead of reusing
  `Models.System.ODataPageResult<T>` — rejected: the type is already
  generic and shape-identical to what CC's Subject odata endpoint needs; a
  duplicate would just be the same three properties under a different name,
  with no Subject-specific behavior to justify it.
- Actually apply/parse `queryOptions` (e.g., real OData `$filter`/`$orderby`
  parsing) — rejected: out of scope for this change and inconsistent with
  every existing `queryOptions` usage in this codebase, which all treat it
  as accepted-but-ignored.
