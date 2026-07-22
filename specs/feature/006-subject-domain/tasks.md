---

description: "Task list for Subject Domain feature implementation"
---

# Tasks: Subject Domain

**Input**: Design documents from `/specs/feature/006-subject-domain/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/subject-api.md, contracts/subject-testdata-api.md, quickstart.md

**Tests**: Included. The project constitution (III. Integration-First Testing, VII. Testing Standards) mandates `IsolatedWebApplicationFactory`-based integration coverage and an auth-matrix test for every auth-gated route — this is a hard requirement for this codebase, not an optional add-on.

**Organization**: Tasks are grouped by user story (from spec.md: US1 = Retrieve, US2 = Manage, US3 = Generate/Monitor/Manage synthetic data) to enable independent implementation and testing of each.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are exact and repo-relative

## Path Conventions

Web app layout (see plan.md): `backend/MockHealthSystem.Api/`,
`backend/MockHealthSystem.Infrastructure/`, `backend/MockHealthSystem.Tests/`,
`frontend/src/`.

---

## Phase 1: Setup

**Purpose**: Confirm a clean baseline before adding new code. No new
dependencies are required (research.md — all needed packages already exist in
the solution).

- [ ] T001 Run `dotnet build` and `dotnet test` from `backend/` on `feature/006-subject-domain` to confirm a clean baseline before adding Subject-domain code
- [ ] T002 Run `npm run lint` and `npm run test` from `frontend/` to confirm a clean baseline

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, `AppDbContext` wiring, the EF migration, DTOs, the
mapping service, and the shared test-seed helper that every user story
depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T003 [P] Create `Subject` entity (Id, Uid, PatientId, StudyId, StudyArmId?, Status, SubjectIdentifier?, EnrollmentDate, ScreeningDate?, WithdrawalDate?, WithdrawalReason?, CreatedOn, LastUpdatedOn, navigation to Patient/Study/StudyArm/StatusHistory — per data-model.md) in `backend/MockHealthSystem.Infrastructure/Data/Entities/Subject.cs`
- [ ] T004 [P] Create `SubjectStatus` entity (Id, SubjectId, StatusName, ChangedOn, ChangedByStaffId?, Comment?, navigation to Subject/ChangedByStaff — structural mirror of `StudyDocumentStatusHistory`) in `backend/MockHealthSystem.Infrastructure/Data/Entities/SubjectStatus.cs`
- [ ] T005 Register `DbSet<Subject>` and `DbSet<SubjectStatus>`, configure relationships in `OnModelCreating` — `Subject.Patient`/`Subject.Study` → `DeleteBehavior.Cascade`, `Subject.StudyArm` → `DeleteBehavior.SetNull`, `SubjectStatus.Subject` → `DeleteBehavior.Cascade`, `SubjectStatus.ChangedByStaff` → `DeleteBehavior.SetNull` — and add `modelBuilder.Entity<Subject>().HasIndex(x => x.Uid).IsUnique()`, matching the unique-`Uid`-index convention already applied to every other `Uid`-bearing entity (`Study`, `StudyArm`, `StudyVisit`, `StudyDocument`, `ProtocolVersion`, `Sponsor`) (per data-model.md and research.md Decision 10) in `backend/MockHealthSystem.Infrastructure/Data/AppDbContext.cs` (depends on T003, T004)
- [ ] T006 Generate and apply the EF Core migration: `backend/scripts/run-ef.sh migrations add AddSubjectDomain` then `backend/scripts/run-ef.sh database update`; verify `AppDbContextModelSnapshot.cs` reflects the new model (depends on T005)
- [ ] T007 [P] Create `SubjectSearchLimits` (DefaultLimit=100, MaxLimit=5000, ClampLimit — mirrors `StudySearchLimits`) in `backend/MockHealthSystem.Api/Models/Subjects/SubjectSearchLimits.cs`
- [ ] T008 [P] Create `SubjectViewModel` and `SubjectEditModel` (fields per contracts/subject-api.md) and a `SubjectStatusCatalog` static class (`ActiveStatuses` = "Prescreened"/"Screened"/"Randomized"/"Run-in", `InactiveStatuses` = "Screen Failed"/"Non Qualified"/"Dropped"/"Run-in Failed"/"Complete", `AllStatuses`, `IsActiveCategory(string)` — per research.md Decision 11; NOTE: "Active" is not itself a real CC status value, it is shorthand for "any status in `ActiveStatuses`" — do not implement a literal `Status == "Active"` check anywhere) in `backend/MockHealthSystem.Api/Models/Subjects/SubjectModels.cs`
- [ ] T009 [P] Create `SubjectStatusViewModel` (id, subjectId, statusName, changedOn, changedBy? reusing `MockHealthSystem.Api.Models.Studies.StaffPreviewModel` via a `using` directive rather than duplicating it, comment?) in `backend/MockHealthSystem.Api/Models/Subjects/SubjectStatusModels.cs`
- [ ] T010 Create `SubjectMappingService` with `ToViewModel(Subject)`, `ApplyEditModel(Subject, SubjectEditModel)`, and `ToViewModel(SubjectStatus)` (mirrors `StudyMappingService`'s method shapes) in `backend/MockHealthSystem.Api/Services/SubjectMappingService.cs` (depends on T003, T004, T008, T009)
- [ ] T011 [P] Create `SubjectSeedHelpers` with `SeedPrerequisitesAsync` (patient + study + optional arm) and `SeedSubjectAsync` test helpers (mirrors `StudySeedHelpers`) in `backend/MockHealthSystem.Tests/Integration/SubjectSeedHelpers.cs`

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 - Retrieve Subject Data via the CC API (Priority: P1) 🎯 MVP

**Goal**: A developer can retrieve a paginated/filterable list of subjects,
fetch one subject by ID, and retrieve a study's subject-status change
history — all read-only, all via the CC-mirrored API.

**Independent Test**: With seeded subjects (via `SubjectSeedHelpers`), GET the
list endpoint (with and without patient/study filters), GET a single subject
by ID (including a 404 case), and GET a study's subject-status history
endpoint by its `Uid` (including the empty-result and 404-unknown-study
cases) — all without any write endpoint existing yet.

### Tests for User Story 1

- [ ] T012 [P] [US1] Integration tests for `GET /subjects` (unfiltered, filtered by `patientId`, filtered by `studyId`, pagination `skip`/`limit`) in `backend/MockHealthSystem.Tests/Integration/SubjectsControllerTests.cs`
- [ ] T013 [P] [US1] Integration tests for `GET /subjects/odata` and `GET /subjects/{id}` (including 404 for unknown id) in `backend/MockHealthSystem.Tests/Integration/SubjectsControllerTests.cs`
- [ ] T014 [P] [US1] Integration tests for `GET /studies/{studyUid}/subject-statuses/odata` — populated result ordered by `ChangedOn` descending, empty array for a study with no subjects/status changes, 404 for an unknown `studyUid` — in `backend/MockHealthSystem.Tests/Integration/SubjectStatusesControllerTests.cs`
- [ ] T015 [P] [US1] Auth-matrix test class covering all 4 auth modes (`None`/`Bearer`/`CCAPIKey`/`OAuth`) on `GET /subjects/{id}` and `GET /studies/{studyUid}/subject-statuses/odata`, mirroring `StudyEndpointAuthMatrixTests`, in `backend/MockHealthSystem.Tests/Integration/SubjectEndpointAuthMatrixTests.cs`

### Implementation for User Story 1

- [ ] T016 [US1] Implement `SubjectsController` with `[Authorize]`/`[ApiVersion("1.0")]`/route `api/v{version:apiVersion}/subjects`, an `IncludeAll` query helper, `GetSubjects` (list with `patientId`/`studyId`/`status` filters + `skip`/`limit` pagination via `SubjectSearchLimits`), `GetSubjectsOData` (capped at 100, ordered by Id), and `GetSubject(id)` in `backend/MockHealthSystem.Api/Controllers/SubjectsController.cs` (depends on T010; must satisfy T012, T013)
- [ ] T017 [US1] Implement `SubjectStatusesController` with `[Authorize]`/`[ApiVersion("1.0")]`/route `api/v{version:apiVersion}/studies/{studyUid:guid}/subject-statuses`, resolving the study via `_db.Studies.FirstOrDefaultAsync(s => s.Uid == studyUid)` (404 if none), then `GetSubjectStatusesOData` joining `SubjectStatuses` → `Subjects` filtered to that study's `Id`, capped at 100, ordered by `ChangedOn` descending, in `backend/MockHealthSystem.Api/Controllers/SubjectStatusesController.cs` (depends on T010; must satisfy T014)

**Checkpoint**: User Story 1 is fully functional and testable independently — subjects and their status history can be read via the CC API once seeded directly through `AppDbContext`/`SubjectSeedHelpers`.

---

## Phase 4: User Story 2 - Manage Subject Data via the CC API (Priority: P1)

**Goal**: A developer can create, update, and delete subjects through the
write API, with the mock enforcing the one-Active-category-status-per-patient
-per-study rule and recording a status-history entry on every status change.

**Independent Test**: POST a new subject, PUT/PATCH updates to it, attempt a
conflicting concurrent Active-category create/update and see it rejected,
re-enroll the same patient/study pair after the prior enrollment is now in
an Inactive-category status and see it succeed, DELETE a subject and confirm
it's gone, and confirm each
status change produced a retrievable `SubjectStatus` entry — all via API
calls, independent of the admin UI (US3).

### Tests for User Story 2

- [ ] T018 [P] [US2] Integration tests for `POST /subjects` — success (201, generated Id/Uid), rejects nonexistent `patientId`/`studyId` (400), rejects `studyArmId` not belonging to `studyId` (400), rejects a `status` value outside the nine defined values (400, FR-006) — in `backend/MockHealthSystem.Tests/Integration/SubjectsControllerWriteTests.cs`
- [ ] T019 [P] [US2] Integration tests for `PUT /subjects/{id}` and `PATCH /subjects/{id}` — changed fields persisted and reflected on next GET, 404 for unknown id — in `backend/MockHealthSystem.Tests/Integration/SubjectsControllerWriteTests.cs`
- [ ] T020 [P] [US2] Integration tests for the one-Active-category-status-per-patient-per-study rule — create/update to an Active-category status (e.g. "Screened") when another subject already has an Active-category status (e.g. "Randomized") for the same pair is rejected (400); creating a new subject for the same pair after the prior one is now in an Inactive-category status (e.g. "Complete") succeeds — in `backend/MockHealthSystem.Tests/Integration/SubjectsControllerWriteTests.cs`
- [ ] T021 [P] [US2] Integration test for `DELETE /subjects/{id}` — 204, subsequent GET returns 404, and its `SubjectStatus` rows are gone too — in `backend/MockHealthSystem.Tests/Integration/SubjectsControllerWriteTests.cs`
- [ ] T022 [P] [US2] Integration test that creating a subject and later updating its status each produce a new `SubjectStatus` row, retrievable via `GET /studies/{studyUid}/subject-statuses/odata` from US1 — in `backend/MockHealthSystem.Tests/Integration/SubjectsControllerWriteTests.cs`

### Implementation for User Story 2

- [ ] T023 [US2] Implement `ValidateReferencesAsync`/`ValidateEditModelAsync` on `SubjectsController` — tracking existence checks for `PatientId`/`StudyId`/`StudyArmId` (with arm-belongs-to-study check), a `SubjectStatusCatalog.AllStatuses.Contains(status)` check (FR-006), and — only when `status` is in `SubjectStatusCatalog.ActiveStatuses` — the one-Active-category-status-per-`(PatientId, StudyId)` existence check (`ActiveStatuses.Contains(s.Status)`) excluding the current record's Id — in `backend/MockHealthSystem.Api/Controllers/SubjectsController.cs` (depends on T016, T008; must satisfy T018, T020)
- [ ] T024 [US2] Implement `CreateSubject` (`POST`) — validate, insert, then insert the initial `SubjectStatus` row in a second `SaveChangesAsync` (mirrors `StudyDocumentsController.CreateDocument`) — in `backend/MockHealthSystem.Api/Controllers/SubjectsController.cs` (depends on T023; must satisfy T018, T022)
- [ ] T025 [US2] Implement `UpdateSubject` (`PUT`) and `PatchSubject` (`PATCH`) — validate, apply changes, append a new `SubjectStatus` row only if `Status` changed (mirrors `StudyDocumentsController.UpdateDocument`'s `statusChanged` check) — in `backend/MockHealthSystem.Api/Controllers/SubjectsController.cs` (depends on T023; must satisfy T019, T020, T022)
- [ ] T026 [US2] Implement `DeleteSubject` (`DELETE`) in `backend/MockHealthSystem.Api/Controllers/SubjectsController.cs` (depends on T016; must satisfy T021)

**Checkpoint**: User Stories 1 AND 2 both work independently — the full CC-mirrored Subject API (read + write) is functional.

---

## Phase 5: User Story 3 - Generate, Monitor, and Manage Synthetic Subject Data for Testing (Priority: P2)

**Goal**: A developer can generate synthetic subjects linking existing
patients/studies, see subject counts and a per-study patient breakdown, look
up a subject, and reset subject data — all from the admin Test Data
Management dashboard's existing four tabs. Resetting Patient or Study data
alone also clears dependent Subject data.

**Independent Test**: With existing generated patients and studies, generate
a batch of subjects from the Data Generation tab, see the count/breakdown
update on the Data Counts tab, look up a specific subject on the Data
Manipulation tab, reset subject data from the Information and Destruction
tab, and — separately — reset Patient or Study data alone and confirm zero
subjects remain referencing what was removed.

### Tests for User Story 3

- [ ] T027 [P] [US3] Unit tests for `SubjectFakerService` — builds valid `(patientId, studyId)` candidates without violating the one-Active-category-status-per-pair rule, picks distinct combinations without replacement via `Faker.PickRandom`, assigns each subject a status from `SubjectStatusCatalog.AllStatuses`, each generated subject has exactly one initial `SubjectStatus` — in `backend/MockHealthSystem.Tests/Unit/SubjectFakerServiceTests.cs`
- [ ] T028 [P] [US3] Integration tests for `POST /test-data/subjects/generate` — success with actual vs. requested counts, clear 400 when no patients or no studies exist, batch-size cap (>500 rejected) — in `backend/MockHealthSystem.Tests/Integration/TestDataControllerSubjectGenerateTests.cs`
- [ ] T029 [P] [US3] Integration test for `POST /test-data/subjects/reset` — clears `Subjects`/`SubjectStatuses` without affecting Patient/Study data — in `backend/MockHealthSystem.Tests/Integration/TestDataControllerSubjectResetTests.cs`
- [ ] T030 [P] [US3] Integration tests for `GET /test-data/subjects/lookup` (by `id`, `uid`, `patientId`+`studyId`, 400 when no strategy provided, 404 on no match), `GET /test-data/subjects/random`, and `GET /test-data/subjects/stats` (total count + per-study **distinct**-patient breakdown) — in `backend/MockHealthSystem.Tests/Integration/TestDataControllerSubjectLookupTests.cs`
- [ ] T031 [P] [US3] Integration test proving the reset-cascade: seed subjects referencing existing patients/studies, call `POST /test-data/patients/reset` (and separately `POST /test-data/studies/reset`), assert `subjects/stats` reports zero remaining subjects with no explicit subject reset called — in `backend/MockHealthSystem.Tests/Integration/TestDataControllerSubjectCascadeTests.cs` (validates research.md Decision 6; note in the test that `TRUNCATE ... CASCADE` semantics require this to run against a real Postgres-backed factory if the InMemory provider doesn't honor the FK cascade the same way — flag for the live-Postgres spot-check in Polish if so)
- [ ] T032 [P] [US3] Performance test asserting `POST /test-data/subjects/generate` for a representative batch (the documented default of 25 subjects) completes in under 30 seconds (spec SC-004), using a `Stopwatch` around the HTTP call — mirrors the Study domain's `TestDataControllerStudyPerformanceTests.cs` (SC-003 equivalent) — in `backend/MockHealthSystem.Tests/Integration/TestDataControllerSubjectPerformanceTests.cs`

### Implementation for User Story 3

- [ ] T033 [US3] Create `SubjectFakerService` (constructor takes patient/study/arm ID lists, builds cross-product candidates respecting the Active-category-uniqueness rule via `SubjectStatusCatalog`, `CreateSubjects(count)` mirrors `StudyFakerService.CreateStudies`, assigns each subject a status via `Faker.PickRandom(SubjectStatusCatalog.AllStatuses)`, and gets one initial `SubjectStatus` via `subject.StatusHistory.Add(...)`) in `backend/MockHealthSystem.Api/Services/SubjectFakerService.cs` (depends on T008, T010; must satisfy T027)
- [ ] T034 [US3] Add `subjects/generate` action to `TestDataController` (admin-gated, `maxCount = 500`, fails clearly if no patients/studies exist, returns `GenerateSubjectsResponse`) in `backend/MockHealthSystem.Api/Controllers/TestDataController.cs` (depends on T033; must satisfy T028, T032)
- [ ] T035 [US3] Add `subjects/reset` action to `TestDataController` (`TRUNCATE "SubjectStatuses", "Subjects" RESTART IDENTITY CASCADE`) in `backend/MockHealthSystem.Api/Controllers/TestDataController.cs` (must satisfy T029)
- [ ] T036 [US3] Add `subjects/lookup`, `subjects/random`, and `subjects/stats` (distinct-patients-per-study via `GroupBy(s => s.StudyId)`) actions to `TestDataController` in `backend/MockHealthSystem.Api/Controllers/TestDataController.cs` (must satisfy T030)
- [ ] T037 [US3] Add `SubjectViewModel`, `GenerateSubjectsOptions`/`GenerateSubjectsResult`, `SubjectPatientsByStudyCount`, `SubjectTestDataStats` TypeScript types and `generateTestSubjects`/`resetTestSubjects`/`lookupTestSubject`/`getRandomTestSubject`/`getSubjectTestDataStats` functions (mirrors the existing `*TestStudies*`/`StudyTestDataStats` block) in `frontend/src/api.ts`
- [ ] T038 [P] [US3] Add `DEMO_SUBJECT_TEST_DATA_STATS` demo-mode fixture (mirrors `DEMO_STUDY_TEST_DATA_STATS`, including setting it on both the admin-session and demo-mode code paths per the prior demo-mode bug fix) in `frontend/src/demoData.ts` (depends on T037)
- [ ] T039 [US3] Extend `TestDataCountsSection.tsx` with a Subject total-count stat card and a patients-by-study `CategoryPieChart` (reusing the existing shared local component), fetched inside the section's existing `Promise.all()` via `getSubjectTestDataStats()` in `frontend/src/TestDataCountsSection.tsx` (depends on T037, T038)
- [ ] T040 [P] [US3] Extend `TestDataCountsSection.test.tsx` for the new Subject stat card and chart (asserting on absence-of-empty-state text for the chart per this project's established Recharts/jsdom test pattern) in `frontend/src/TestDataCountsSection.test.tsx` (depends on T039)
- [ ] T041 [US3] Extend `TestDataGenerationSection.tsx` with a "Generate Subjects" batch-size form calling `generateTestSubjects()`, following the existing generate-studies button/result-summary/`disabled:opacity-70` pattern in `frontend/src/TestDataGenerationSection.tsx` (depends on T037)
- [ ] T042 [P] [US3] Extend `TestDataGenerationSection.test.tsx` for the new Subject generation form in `frontend/src/TestDataGenerationSection.test.tsx` (depends on T041)
- [ ] T043 [US3] Extend `TestDataManipulationSection.tsx` with a Subject lookup form (by id, or by patientId+studyId) calling `lookupTestSubject()`, following the existing study-lookup form pattern in `frontend/src/TestDataManipulationSection.tsx` (depends on T037)
- [ ] T044 [P] [US3] Extend `TestDataManipulationSection.test.tsx` for the new Subject lookup form in `frontend/src/TestDataManipulationSection.test.tsx` (depends on T043)
- [ ] T045 [US3] Extend `TestDataInfoDestructionSection.tsx` with a Subject `ConfirmableResetButton` (reusing the existing shared local component) calling `resetTestSubjects()` in `frontend/src/TestDataInfoDestructionSection.tsx` (depends on T037)
- [ ] T046 [P] [US3] Extend `TestDataInfoDestructionSection.test.tsx` for the new Subject reset button in `frontend/src/TestDataInfoDestructionSection.test.tsx` (depends on T045)

**Checkpoint**: All three user stories are independently functional — the full Subject domain (CC API + admin tooling) is complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verification and documentation sync spanning all three stories.

- [ ] T047 [P] Unit tests for `SubjectMappingService` (`ToViewModel`/`ApplyEditModel` round-trip correctness) in `backend/MockHealthSystem.Tests/Unit/SubjectMappingServiceTests.cs`
- [ ] T048 Update `README.md` and `API-CONNECT.md` to document the new Subject API endpoints, the `subject-statuses/odata` endpoint's `studyUid` key, and the Test Data Management dashboard's new Subject controls, per the constitution's Documentation Sync workflow rule
- [ ] T049 Live-Postgres verification of the reset-cascade behavior from research.md Decision 6: with a real Postgres instance, seed subjects referencing patients/studies, run `patients/reset` and `studies/reset` independently, confirm `Subjects`/`SubjectStatuses` are empty each time (this is the authoritative check for `TRUNCATE ... CASCADE` semantics that EF InMemory cannot fully validate — see T031's note)
- [ ] T050 Run `specs/feature/006-subject-domain/quickstart.md` end-to-end against a running local instance to validate the full generate → query → write → reset flow
- [ ] T051 Run full backend suite: `dotnet build` (0 errors) and `dotnet test` (full suite green) from `backend/`
- [ ] T052 Run full frontend suite: `npm run lint`, `npm run test`, and `npm run build` from `frontend/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories (entities, migration, DTOs, and mapping service are shared by every story).
- **User Story 1 (Phase 3)**: Depends on Foundational only. No dependency on US2 or US3.
- **User Story 2 (Phase 4)**: Depends on Foundational (specifically T010's mapping service) and reuses `SubjectsController` from US1 (T016) as the file it extends — sequenced after US1 for that reason, though its own business logic (validation, write actions) is independent of US1's read logic.
- **User Story 3 (Phase 5)**: Depends on Foundational (specifically T006's migration and T010's mapping service). Does not depend on US2's write endpoints — the faker writes directly via `AppDbContext` — but is far more useful once US1's GET endpoints exist to verify generated data and US2 exists so the full write-cycle can be exercised end-to-end during manual verification, so is sequenced last per spec priority (P2 vs. P1).
- **Polish (Phase 6)**: Depends on all three user stories being complete.

### Within Each User Story

- Tests are written before their corresponding implementation tasks and MUST fail first.
- Entities/DTOs before controllers.
- Controller read actions (US1) before write actions (US2) in the same file — `SubjectsController.cs` is extended, not duplicated.
- Story complete before moving to the next priority phase.

### Parallel Opportunities

- T003, T004 (entities) in parallel.
- T007, T008, T009 (DTOs) in parallel once entities exist.
- T011 (test seed helper) in parallel with the DTO tasks.
- All test tasks within a phase marked [P] can run in parallel (different test files or independent test methods).
- T018–T022 (US2 tests) in parallel with each other.
- T027–T032 (US3 tests) in parallel with each other.
- T040, T042, T044, T046 (frontend test files) can run in parallel with each other once their respective component tasks land.

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Integration tests for GET /subjects in backend/MockHealthSystem.Tests/Integration/SubjectsControllerTests.cs"
Task: "Integration tests for GET /subjects/odata and GET /subjects/{id} in backend/MockHealthSystem.Tests/Integration/SubjectsControllerTests.cs"
Task: "Integration tests for GET /studies/{studyUid}/subject-statuses/odata in backend/MockHealthSystem.Tests/Integration/SubjectStatusesControllerTests.cs"
Task: "Auth-matrix test class in backend/MockHealthSystem.Tests/Integration/SubjectEndpointAuthMatrixTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories).
3. Complete Phase 3: User Story 1.
4. **STOP and VALIDATE**: Confirm subjects and status history are readable via the CC API against seeded data.
5. Demo if ready.

### Incremental Delivery

1. Setup + Foundational → Foundation ready.
2. Add User Story 1 → validate independently → demo read-only Subject API (MVP!).
3. Add User Story 2 → validate independently → demo full CC-mirrored write API.
4. Add User Story 3 → validate independently → demo the admin dashboard end-to-end.
5. Polish: documentation sync, live-Postgres cascade verification, full suite green.

### Parallel Team Strategy

With multiple developers, once Foundational is done: Developer A takes User
Story 1, Developer B starts User Story 3's `SubjectFakerService`/
`TestDataController` work (independent of US1/US2's controller code once the
mapping service exists), Developer C takes User Story 2 once US1's
`SubjectsController` file exists to extend. Frontend tasks in US3 (T037–T046)
can proceed in parallel with backend US2 work once T037's `api.ts` types are
stubbed.

---

## Notes

- [P] tasks = different files, no dependencies.
- [Story] label maps task to specific user story for traceability.
- research.md Decision 6 (reset-cascade via Postgres `TRUNCATE ... CASCADE`)
  means no code changes are needed in the existing `patients/reset` or
  `studies/reset` actions — T031/T049 exist to *verify* this, not to
  implement anything there.
- Commit after each task or logical group.
- Stop at any checkpoint to validate story independently.
