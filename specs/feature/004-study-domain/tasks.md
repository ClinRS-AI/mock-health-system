---

description: "Task list for Study Domain feature implementation"
---

# Tasks: Study Domain

**Input**: Design documents from `/specs/feature/004-study-domain/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/study-api.md, contracts/study-testdata-api.md, quickstart.md

**Tests**: Included. The project constitution (III. Integration-First Testing, VII. Testing Standards) mandates `IsolatedWebApplicationFactory`-based integration coverage and an auth-matrix test for every auth-gated route — this is a hard requirement for this codebase, not an optional add-on.

**Organization**: Tasks are grouped by user story (from spec.md: US1 = Retrieve, US2 = Manage, US3 = Generate) to enable independent implementation and testing of each.

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

**Purpose**: Confirm a clean baseline before adding new code. No new dependencies
are required (research.md — all needed packages already exist in the solution).

- [X] T001 Run `dotnet build` and `dotnet test` from `backend/` on `feature/004-study-domain` to confirm a clean baseline before adding Study-domain code
- [X] T002 Run `npm run lint` and `npm run test` from `frontend/` to confirm a clean baseline

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, `AppDbContext` wiring, the EF migration, DTOs, and the
mapping service that every user story's controllers depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Entities (all separate files — safe to parallelize)

- [X] T003 [P] Create `Sponsor` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/Sponsor.cs` (Id, Uid, Name — per data-model.md)
- [X] T004 [P] Create `SponsorDivision` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/SponsorDivision.cs` (Id, SponsorId FK, Name)
- [X] T005 [P] Create `SponsorTeam` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/SponsorTeam.cs` (Id, SponsorDivisionId FK, Name)
- [X] T006 [P] Create `StudyCategory` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyCategory.cs` (Id, Name, Description)
- [X] T007 [P] Create `StudySubcategory` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudySubcategory.cs` (Id, StudyCategoryId FK nullable, Name, Description)
- [X] T008 [P] Create `StudyType` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyType.cs` (Id, Name, Description, ForeColor, BackColor)
- [X] T009 [P] Create `StudyStatusType` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyStatusType.cs` (Id, Name, Description, BackColor, IsActive, IsEnrollmentPermitted, StudyPhase)
- [X] T010 [P] Create `StudyGroup` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyGroup.cs` (Id, Name)
- [X] T011 [P] Create `Study` entity (aggregate root, all flattened field groups from data-model.md, plus navigation collections incl. `Contacts`) in `backend/MockHealthSystem.Infrastructure/Data/Entities/Study.cs`
- [X] T012 [P] Create `StudyArm` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyArm.cs`
- [X] T013 [P] Create `StudyVisit` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyVisit.cs`
- [X] T014 [P] Create `StudyVisitArm` join entity (composite key VisitId+ArmId) in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyVisitArm.cs`
- [X] T015 [P] Create `StudyMilestone` entity (incl. flattened scheduling fields) in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyMilestone.cs`
- [X] T016 [P] Create `StudyDocument` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyDocument.cs`
- [X] T017 [P] Create `StudyDocumentStatusHistory` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyDocumentStatusHistory.cs`
- [X] T018 [P] Create `StudyContact` entity (Id, StudyId FK, ContactType [Irb/Cro/Lab/Monitor/Vendor], Slot [1-2], Name, Reference, Comment; unique index on StudyId+ContactType+Slot — own table, no dedicated controller, mirrors `PatientPhone`) in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyContact.cs`
- [X] T019 [P] Create `StudyNote` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyNote.cs`
- [X] T020 [P] Create `StudyRole` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyRole.cs`
- [X] T021 [P] Create `StudyRoleStaff` join entity (composite key StudyRoleId+StaffId) in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyRoleStaff.cs`
- [X] T022 [P] Create `ProtocolVersion` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/ProtocolVersion.cs`
- [X] T023 [P] Create `StudyTargetDate` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyTargetDate.cs`
- [X] T024 [P] Create `StudyLeadership` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyLeadership.cs`
- [X] T025 [P] Create `StudyCustomFieldValue` entity in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyCustomFieldValue.cs`
- [X] T026 [P] Create `StudyStudyType` join entity (composite key StudyId+StudyTypeId) in `backend/MockHealthSystem.Infrastructure/Data/Entities/StudyStudyType.cs`

### Schema wiring

- [X] T027 Register all 24 new `DbSet<T>` properties and configure relationships in `OnModelCreating` (composite keys for `StudyVisitArm`/`StudyRoleStaff`/`StudyStudyType`; unique index on `StudyContact(StudyId, ContactType, Slot)`; required `SponsorTeamId` FK; cascade delete from `Study` to all owned sub-resources including `StudyContact` per data-model.md's Relationships Summary; unique indexes on `Uid` columns) in `backend/MockHealthSystem.Infrastructure/Data/AppDbContext.cs` (depends on T003-T026)
- [X] T028 Generate and apply the EF Core migration: `backend/scripts/run-ef.sh migrations add AddStudyDomain` then `backend/scripts/run-ef.sh database update`; verify `AppDbContextModelSnapshot.cs` reflects the new model (depends on T027)

### DTOs (separate files — safe to parallelize)

- [X] T029 [P] Create `StudyViewModel`, `StudyEditModel`, `StudyPatchModel`, `StudyPreviewModel` — including embedded target-dates/leadership/custom-fields arrays and a `contacts` array of contact-entry DTOs (`type`, `slot`, `name`, `reference`, `comment`) backed by `StudyContact` — in `backend/MockHealthSystem.Api/Models/Studies/StudyModels.cs`
- [X] T030 [P] Create `StudyArmViewModel`, `StudyArmEditModel` in `backend/MockHealthSystem.Api/Models/Studies/StudyArmModels.cs`
- [X] T031 [P] Create `StudyVisitViewModel`, `StudyVisitEditModel` in `backend/MockHealthSystem.Api/Models/Studies/StudyVisitModels.cs`
- [X] T032 [P] Create `StudyMilestoneViewModel`, `StudyMilestoneEditModel` in `backend/MockHealthSystem.Api/Models/Studies/StudyMilestoneModels.cs`
- [X] T033 [P] Create `StudyDocumentViewModel`, `StudyDocumentEditModel`, `StudyDocumentStatusHistoryViewModel` in `backend/MockHealthSystem.Api/Models/Studies/StudyDocumentModels.cs`
- [X] T034 [P] Create `StudyNoteViewModel`, `StudyNoteEditModel` in `backend/MockHealthSystem.Api/Models/Studies/StudyNoteModels.cs`
- [X] T035 [P] Create `StudyRoleViewModel`, `StudyRoleStaffEditModel` in `backend/MockHealthSystem.Api/Models/Studies/StudyRoleModels.cs`
- [X] T036 [P] Create `ProtocolVersionViewModel`, `ProtocolVersionEditModel` in `backend/MockHealthSystem.Api/Models/Studies/ProtocolVersionModels.cs`
- [X] T037 [P] Create `StudyCategoryViewModel`/`EditModel`, `StudySubcategoryViewModel`/`EditModel`, `StudyTypeViewModel`, `StudyStatusTypeViewModel`, `StudyGroupViewModel` in `backend/MockHealthSystem.Api/Models/Studies/StudyLookupModels.cs`

### Mapping service

- [X] T038 Create `StudyMappingService` with `ToViewModel(Study)`, `ApplyEditModel`, `ApplyPatchModel` for the `Study` core record — including embedded target dates, leadership, custom fields, and `StudyContact` rows upserted by `(ContactType, Slot)` (PUT replaces the set wholesale; PATCH upserts in place leaving omitted slots untouched — mirrors `PatientMappingService`'s phone-slot semantics exactly, see research.md) — in `backend/MockHealthSystem.Api/Services/StudyMappingService.cs` (depends on T011, T018, T029)
- [X] T039 Extend `StudyMappingService` with `ToViewModel`/`ApplyEditModel` pairs for every structural sub-resource (Arms, Visits, Milestones, Documents + status history, Notes, Roles + role staff, Protocol Versions) and the lookup entities, in the same file (depends on T012-T026, T030-T037, T038)

**Checkpoint**: Schema, DTOs, and mapping are in place. User story implementation can now begin.

---

## Phase 3: User Story 1 — Retrieve Study Data via the CC API (Priority: P1) 🎯 MVP

**Goal**: A developer can GET the studies list, a study's detail, every structural
sub-resource, and every reference/lookup endpoint, with responses matching CC's
field shapes.

**Independent Test**: Seed a study (with an arm, visit, milestone, document) via
`AppDbContext` in a test, then retrieve it and each sub-resource through the API
with no writes involved.

### Tests for User Story 1

> Write these first; they should fail (404/`NotFound` route) until the matching
> controller action exists.

- [X] T040 [P] [US1] Integration test for `GET /studies`, `/studies/odata`, `/studies/{id}`, `/studies/{id}/personnel` — including that a seeded study's `contacts` array is returned correctly — in `backend/MockHealthSystem.Tests/Integration/StudiesControllerTests.cs`
- [X] T041 [P] [US1] Integration test for GET list/detail across Arms, Visits (+ arm/visit association reads), Milestones, Documents (+ history), Notes, Roles, Protocol Versions in `backend/MockHealthSystem.Tests/Integration/StudySubResourceReadTests.cs`
- [X] T042 [P] [US1] Integration test for `GET /system/study-categories`, `/study-subcategories`, `/study-types`, `/study-statuses`, `/study-groups` — asserting the admin-gated behavior (open in Development; 403 without a valid `X-Admin-Session`/`X-Admin-Key` when `AUTH_SETTINGS_ADMIN_KEY` is set), matching `TestDataController`'s pattern, not the CC auth mode — in `backend/MockHealthSystem.Tests/Integration/StudyLookupControllerTests.cs`
- [X] T043 [P] [US1] Auth-matrix test covering all 4 CC auth modes (None/Bearer/CCAPIKey/OAuth) for `GET /studies/{id}` in `backend/MockHealthSystem.Tests/Integration/StudyEndpointAuthMatrixTests.cs`

### Implementation for User Story 1

- [X] T044 [US1] Create `StudiesController` with `[Authorize][ApiVersion("1.0")][Route("api/v{version:apiVersion}/studies")]` and GET actions (list w/ pagination+filter on name/status/category/protocolNumber, `odata`, detail by id incl. the `contacts` array, `{id}/personnel`) in `backend/MockHealthSystem.Api/Controllers/StudiesController.cs` (depends on T038)
- [X] T045 [US1] Create `StudyArmsController` with GET actions (list, detail, `{armId}/visits`) in `backend/MockHealthSystem.Api/Controllers/StudyArmsController.cs` (depends on T039)
- [X] T046 [US1] Create `StudyVisitsController` with GET actions (list `odata`, detail, `{visitId}/arms`) in `backend/MockHealthSystem.Api/Controllers/StudyVisitsController.cs` (depends on T039)
- [X] T047 [US1] Create `StudyMilestonesController` with GET actions (list, `odata`, detail) in `backend/MockHealthSystem.Api/Controllers/StudyMilestonesController.cs` (depends on T039)
- [X] T048 [US1] Create `StudyDocumentsController` with GET actions (list, `odata`, detail, `{id}/history`) in `backend/MockHealthSystem.Api/Controllers/StudyDocumentsController.cs` (depends on T039)
- [X] T049 [US1] Create `StudyNotesController` with GET actions (list, `odata`, detail) in `backend/MockHealthSystem.Api/Controllers/StudyNotesController.cs` (depends on T039)
- [X] T050 [US1] Create `StudyRolesController` with GET actions (list, detail incl. assigned staff) in `backend/MockHealthSystem.Api/Controllers/StudyRolesController.cs` (depends on T039)
- [X] T051 [US1] Create `ProtocolVersionsController` with GET actions (list, detail) in `backend/MockHealthSystem.Api/Controllers/ProtocolVersionsController.cs` (depends on T039)
- [X] T052 [US1] Create `StudyLookupController` (route prefix `api/v{version:apiVersion}/system`) with GET actions for categories, subcategories, types, statuses, groups, gated by `IAdminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)` — the same admin pattern as `TestDataController.LookupPatientAsync`, **not** `[Authorize]`/the CC auth mode, per explicit product direction (research.md) — in `backend/MockHealthSystem.Api/Controllers/StudyLookupController.cs` (depends on T039)
- [X] T053 [US1] Run `dotnet build` and the T040-T043 tests to green; fix any compile/route errors across the new controllers

**Checkpoint**: User Story 1 is fully functional and independently testable — every
read endpoint in contracts/study-api.md and contracts/study-testdata-api.md
returns real, schema-correct data.

---

## Phase 4: User Story 2 — Manage Study Data via the CC API (Priority: P1)

**Goal**: A developer can create, update, partially update, and delete a study and
every structural sub-resource, with cross-study reference integrity enforced.

**Independent Test**: POST a study referencing a real sponsor team, POST an arm
and a visit onto it, PUT a status change, DELETE a milestone — verify each change
via the GET endpoints from US1.

### Tests for User Story 2

- [X] T054 [P] [US2] Integration test for `POST`/`PUT`/`PATCH`/`DELETE /studies` — including missing/invalid `sponsorTeamId` → 400, cascade delete of sub-resources (incl. `StudyContact` rows), and that PUT replaces the `contacts` set wholesale while PATCH upserts by `(type, slot)` leaving omitted slots untouched — in `backend/MockHealthSystem.Tests/Integration/StudiesControllerWriteTests.cs`
- [X] T055 [P] [US2] Integration test for study-type association `POST /studies/{id}/types/add` and `DELETE /studies/{id}/types/{id}` in `backend/MockHealthSystem.Tests/Integration/StudyTypeAssociationTests.cs`
- [X] T056 [P] [US2] Integration test for Arms `POST`/`PUT`/`DELETE` and visit-arm association `POST`/`DELETE` (incl. 400 when visit/arm belong to different studies) in `backend/MockHealthSystem.Tests/Integration/StudyArmsControllerWriteTests.cs`
- [X] T057 [P] [US2] Integration test for Visits `POST`/`PUT`/`DELETE` in `backend/MockHealthSystem.Tests/Integration/StudyVisitsControllerWriteTests.cs`
- [X] T058 [P] [US2] Integration test for Milestones `POST`/`PUT`/`DELETE` in `backend/MockHealthSystem.Tests/Integration/StudyMilestonesControllerWriteTests.cs`
- [X] T059 [P] [US2] Integration test for Documents `POST`/`PUT`/`DELETE` incl. status-change appends a `StudyDocumentStatusHistory` row in `backend/MockHealthSystem.Tests/Integration/StudyDocumentsControllerWriteTests.cs`
- [X] T060 [P] [US2] Integration test for Notes `POST`/`PUT`/`DELETE` incl. 409 when `Locked = true` in `backend/MockHealthSystem.Tests/Integration/StudyNotesControllerWriteTests.cs`
- [X] T061 [P] [US2] Integration test for `PUT /studies/{studyId}/roles/{roleId}` staff-assignment replace in `backend/MockHealthSystem.Tests/Integration/StudyRolesControllerWriteTests.cs`
- [X] T062 [P] [US2] Integration test for Protocol Versions `POST`/`PUT`/`DELETE` incl. 409 when referenced by an Arm/Visit in `backend/MockHealthSystem.Tests/Integration/ProtocolVersionsControllerWriteTests.cs`
- [X] T063 [P] [US2] Integration test for a sub-resource write referencing a non-existent or wrong-parent `studyId` returning 404/400 in `backend/MockHealthSystem.Tests/Integration/StudySubResourceValidationTests.cs`

### Implementation for User Story 2

- [X] T064 [US2] Add `POST`/`PUT`/`PATCH`/`DELETE` actions to `StudiesController` — validate `SponsorTeamId` exists; cascade delete; sync `StudyContact` rows via `StudyMappingService` (PUT replaces wholesale, PATCH upserts by slot) — in `backend/MockHealthSystem.Api/Controllers/StudiesController.cs` (depends on T044)
- [X] T065 [US2] Add `POST /studies/{studyId}/types/add` and `DELETE /studies/{studyId}/types/{id}` actions to `StudiesController` (depends on T064)
- [X] T066 [US2] Add `POST`/`PUT`/`DELETE` arm actions and `POST`/`DELETE` visit-arm association actions (with same-study validation) to `StudyArmsController` in `backend/MockHealthSystem.Api/Controllers/StudyArmsController.cs` (depends on T045)
- [X] T067 [US2] Add `POST`/`PUT`/`DELETE` visit actions to `StudyVisitsController` in `backend/MockHealthSystem.Api/Controllers/StudyVisitsController.cs` (depends on T046)
- [X] T068 [US2] Add `POST`/`PUT`/`DELETE` milestone actions to `StudyMilestonesController` in `backend/MockHealthSystem.Api/Controllers/StudyMilestonesController.cs` (depends on T047)
- [X] T069 [US2] Add `POST`/`PUT`/`DELETE` document actions (incl. status-history append on status change) to `StudyDocumentsController` in `backend/MockHealthSystem.Api/Controllers/StudyDocumentsController.cs` (depends on T048)
- [X] T070 [US2] Add `POST`/`PUT`/`DELETE` note actions (incl. 409 on locked notes) to `StudyNotesController` in `backend/MockHealthSystem.Api/Controllers/StudyNotesController.cs` (depends on T049)
- [X] T071 [US2] Add `PUT /studies/{studyId}/roles/{roleId}` staff-assignment action to `StudyRolesController` in `backend/MockHealthSystem.Api/Controllers/StudyRolesController.cs` (depends on T050)
- [X] T072 [US2] Add `POST`/`PUT`/`DELETE` protocol-version actions (incl. 409 when referenced) to `ProtocolVersionsController` in `backend/MockHealthSystem.Api/Controllers/ProtocolVersionsController.cs` (depends on T051)
- [X] T073 [US2] Add `POST`/`PUT`/`DELETE` actions for categories and subcategories to `StudyLookupController`, gated by the same `IAdminRequestValidator` admin check as its GET actions (types/statuses/groups remain read-only per contracts/study-testdata-api.md) in `backend/MockHealthSystem.Api/Controllers/StudyLookupController.cs` (depends on T052)
- [X] T074 [US2] Run `dotnet build` and the T054-T063 tests to green; fix any validation/cascade edge cases surfaced

**Checkpoint**: User Stories 1 AND 2 both work independently — the full CC Study
CRUD surface from contracts/study-api.md is live, and Study reference/lookup
admin configuration from contracts/study-testdata-api.md is live.

---

## Phase 5: User Story 3 — Generate Synthetic Study Data for Testing (Priority: P2)

**Goal**: A developer can generate a realistic batch of studies with populated
sub-resources from the admin interface, look one up, and reset all Study data
independent of Patient data.

**Independent Test**: Call `POST /test-data/studies/generate`, confirm N studies
with sub-resources exist via US1's GET endpoints, look one up by protocol-number
fragment, then reset and confirm the Study API returns empty collections while
Patient data is untouched.

### Tests for User Story 3

- [X] T075 [P] [US3] Unit test for `StudyFakerService` — deterministic output given a seed, all generated FK references (sponsor team, site, category, status, type) resolve to real rows in `backend/MockHealthSystem.Tests/Unit/StudyFakerServiceTests.cs`
- [X] T076 [P] [US3] Integration test for `POST /test-data/studies/generate` incl. prerequisite lookup auto-seeding when none exist, and 400 when `totalCount` exceeds the configured maximum in `backend/MockHealthSystem.Tests/Integration/TestDataControllerStudyGenerateTests.cs`
- [X] T077 [P] [US3] Performance test asserting `POST /test-data/studies/generate` for a representative batch (the documented default of 25 studies with populated sub-resources) completes in under 30 seconds (spec SC-003), using a `Stopwatch` around the HTTP call in `backend/MockHealthSystem.Tests/Integration/TestDataControllerStudyPerformanceTests.cs`
- [X] T078 [P] [US3] Integration test for `POST /test-data/studies/reset` (Study tables cleared, `Patients` table untouched; `includeLookups=true` also clears Sponsor/lookup tables) in `backend/MockHealthSystem.Tests/Integration/TestDataControllerStudyResetTests.cs`
- [X] T079 [P] [US3] Integration test for `GET /test-data/studies/lookup`, `/random`, `/stats` in `backend/MockHealthSystem.Tests/Integration/TestDataControllerStudyLookupTests.cs`

### Implementation for User Story 3

- [X] T080 [US3] Implement `StudyFakerService` (Bogus-based; constructor takes seed + prerequisite lookup ID lists, mirroring `PatientFakerService`; generates realistic sponsor/protocol-number/NCT-number/phase/status values and populates arms, visits, milestones, documents, notes, and contacts per study) in `backend/MockHealthSystem.Api/Services/StudyFakerService.cs` (depends on T038, T039)
- [X] T081 [US3] Implement `GenerateStudiesAsync` action on `TestDataController`, returning an explicitly named `GenerateStudiesResponse` DTO (not an anonymous object, per constitution VI) — resolves or auto-seeds prerequisite lookups (Sponsor→Division→Team, Site, StudyCategory/Subcategory, StudyStatusType, StudyType, StudyGroup), then calls `StudyFakerService`, bulk-inserts via `AddRangeAsync`+`SaveChangesAsync` — in `backend/MockHealthSystem.Api/Controllers/TestDataController.cs` (depends on T080)
- [X] T082 [US3] Implement `ResetStudiesAsync` action (`TRUNCATE` all Study-domain tables `RESTART IDENTITY CASCADE`, `?includeLookups=true` flag additionally truncates Sponsor/Division/Team and Study lookup tables) on `TestDataController` (depends on T028)
- [X] T083 [US3] Implement `LookupStudyAsync` (by id/uid/name/identifier/protocolNumber fragment), `GetRandomStudyAsync`, and `GetStudyTestDataStatsAsync` — the latter returning an explicitly named `StudyTestDataStatsDto` (not an anonymous object) — actions on `TestDataController` (depends on T038)
- [X] T084 [US3] Run `dotnet build` and the T075-T079 tests to green
- [X] T085 [P] [US3] Add `generateTestStudies`, `resetTestStudies`, `lookupTestStudy`, `getRandomTestStudy`, `getStudyTestDataStats` typed functions (request/response interfaces, no `any`) in `frontend/src/api.ts`
- [X] T086 [US3] Add a "Studies" section to `TestDataPage.tsx` (generate form with count+seed, reset button, lookup-by-fragment form) using the existing granular loading-state and `text-red-700 bg-red-50` error pattern in `frontend/src/TestDataPage.tsx` (depends on T085)
- [X] T087 [P] [US3] Add MSW-backed RTL test for the new Studies section (generate/reset/lookup happy paths + error state) in `frontend/src/TestDataPage.test.tsx` (depends on T086)

**Checkpoint**: All three user stories are independently functional — the Study
domain is feature-complete per spec.md.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation sync and final verification across all three stories.

- [X] T088 [P] Update `README.md` and `API-CONNECT.md` per the constitution's Documentation Sync requirement — new endpoint groups, the admin-gated `system/study-*` lookup routes and `test-data/studies/*` routes, and the new `TestDataPage` "Studies" tab section
- [X] T089 Run `dotnet test` from `backend/` for the full suite and fix any regressions
- [X] T090 Run `npm run test` from `frontend/` for the full suite and fix any regressions
- [X] T091 Manually execute `specs/feature/004-study-domain/quickstart.md` end-to-end (migrate → generate → query core + sub-resources → lookup → reset) and confirm each documented step matches actual behavior — full live walkthrough completed against the real backend and real Postgres, restarted with user consent mid-debugging after the user's own quickstart run surfaced a real bug (see below): generate (200, 10 studies + sub-resources), list/detail/arms/visits-odata/milestones (200, real synthetic data, verified auth-mode parity with `/patients` under active CCAPIKey mode), lookup by protocol-number fragment (200, correct match), stats, reset (200, `TRUNCATE` executed successfully against real Postgres, confirmed empty via stats immediately after), and confirmed Patient data (105,679 records) completely untouched by the Study reset (SC-004). One doc inaccuracy found and fixed (auto-seed list wrongly included "types"). **Bug found and fixed during this step**: `StudyFakerService` used Bogus's `_faker.Date.Past/Future()`, which returns `DateTime` with `Kind=Local`; Npgsql rejects non-UTC `DateTime` for `timestamptz` columns, so every `POST /test-data/studies/generate` call failed against real Postgres (`System.ArgumentException: Cannot write DateTime with Kind=Local...`) despite all 444 in-memory tests passing, since the in-memory provider doesn't validate `DateTime.Kind`. Fixed by adding the same `ToUtc` normalization pattern `PatientsController` already uses: a private `Utc()` helper in `StudyFakerService` (applied to all 7 Bogus-generated dates) and a `ToUtc(DateTime?)` helper in `StudyMappingService` (applied to all 11 model-supplied date fields across `Study`/`StudyMilestone`/`StudyDocument`/`ProtocolVersion` edit/patch paths, so the same class of bug can't reappear on any other write endpoint). Verified fixed by re-running the exact failing command against the live server — 200 OK, zero `fail:` lines in the server log.
- [X] T092 Confirm `AppDbContextModelSnapshot.cs` has no pending model changes after all schema work (re-run `backend/scripts/run-ef.sh migrations add CheckNoPendingChanges --dry-run` equivalent, or attempt a migration add and confirm it's empty, then discard)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories (entities, migration, DTOs, and mapping service are shared by every story).
- **User Story 1 (Phase 3)**: Depends on Foundational only.
- **User Story 2 (Phase 4)**: Depends on Foundational; each write task extends the *same controller file* US1 created (T064 extends `StudiesController` from T044, etc.), so within a controller, the US2 task must follow its US1 counterpart. US2 does not depend on US1's tests passing, only on the controller file existing.
- **User Story 3 (Phase 5)**: Depends on Foundational (specifically T038/T039 mapping service and T028 migration). Does not depend on US1 or US2 controllers — the faker writes directly via `AppDbContext`, and `TestDataController` is a separate file — but is far more useful to a developer once US1's GET endpoints exist to verify generated data, so is sequenced last per spec priority (P2 vs. P1).
- **Polish (Phase 6)**: Depends on all three user stories being complete.

### Within Each User Story

- Tests are written first per file (T040-T043 before T044-T053; T054-T063 before T064-T074; T075-T079 before T080-T087) and should fail until the corresponding implementation task lands.
- Within Phase 2, entity tasks (T003-T026) and DTO tasks (T029-T037) are file-independent and parallelizable; the mapping service (T038-T039) and schema wiring (T027-T028) are not, since they touch shared files/depend on all entities existing.
- Within Phase 4, each controller's write task (T064-T073) depends only on that same controller's US1 read task, not on the other controllers' write tasks — they can proceed in parallel across controllers even though each is a single sequential edit to its own file.
- `StudyLookupController`'s auth model (T052, T073) is admin-gated (`IAdminRequestValidator`), not `[Authorize]` — do not copy the pattern from `StudiesController` or the open, unauthenticated `SystemController` precedent; see research.md.

### Parallel Opportunities

- All Phase 2 entity tasks (T003-T026) — 24 tasks, all different files.
- All Phase 2 DTO tasks (T029-T037) — 9 tasks, all different files.
- All Phase 3 test tasks (T040-T043) — different files.
- All Phase 3 controller-creation tasks (T045-T052) are parallelizable with each other (different files); T044 (`StudiesController`) has no cross-controller dependency either, so all of T044-T052 can run in parallel once T038/T039 land.
- All Phase 4 test tasks (T054-T063) — different files.
- All Phase 4 write-action tasks (T064-T073) — different files (each extends a distinct controller from Phase 3).
- All Phase 5 test tasks (T075-T079) — different files.
- T085 (frontend `api.ts` additions) can run in parallel with backend Phase 5 tasks; T087 (frontend test) depends on T086 (frontend UI), not on any backend task.
- T088 (docs) can run in parallel with T089-T092.

---

## Parallel Example: Phase 2 entities

```bash
# Launch all 24 entity-creation tasks together — each is an independent new file:
Task: "Create Sponsor entity in backend/MockHealthSystem.Infrastructure/Data/Entities/Sponsor.cs"
Task: "Create SponsorDivision entity in backend/MockHealthSystem.Infrastructure/Data/Entities/SponsorDivision.cs"
Task: "Create Study entity in backend/MockHealthSystem.Infrastructure/Data/Entities/Study.cs"
Task: "Create StudyContact entity in backend/MockHealthSystem.Infrastructure/Data/Entities/StudyContact.cs"
Task: "Create StudyArm entity in backend/MockHealthSystem.Infrastructure/Data/Entities/StudyArm.cs"
# ...through T026
```

## Parallel Example: User Story 1 controllers

```bash
# Once T038/T039 (mapping service) land, all read-only controllers are independent files:
Task: "Create StudiesController GET actions in backend/MockHealthSystem.Api/Controllers/StudiesController.cs"
Task: "Create StudyArmsController GET actions in backend/MockHealthSystem.Api/Controllers/StudyArmsController.cs"
Task: "Create StudyDocumentsController GET actions in backend/MockHealthSystem.Api/Controllers/StudyDocumentsController.cs"
Task: "Create StudyLookupController GET actions (admin-gated) in backend/MockHealthSystem.Api/Controllers/StudyLookupController.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

Both are P1 in spec.md — read-only alone (US1) is a valid, smaller MVP if you want
to ship faster and add write support next, but the spec's stated MVP bar is
read + write parity with the CC Study surface:

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks everything — the largest phase by task count)
3. Complete Phase 3: User Story 1 — **STOP and VALIDATE** independently (T053)
4. Complete Phase 4: User Story 2 — **STOP and VALIDATE** independently (T074)
5. Ship the MVP: full CC-mirrored Study CRUD surface

### Incremental Delivery

1. Setup + Foundational → schema and DTOs exist, nothing callable yet
2. + User Story 1 → read-only Study API usable for integration testing (demo-able)
3. + User Story 2 → full CRUD parity with CC (demo-able)
4. + User Story 3 → one-click synthetic data generation from the admin UI (demo-able)
5. + Polish → docs in sync, full suite green, quickstart verified

### Suggested Task Count Summary

- Phase 1 (Setup): 2 tasks
- Phase 2 (Foundational): 37 tasks (T003-T039)
- Phase 3 (US1): 14 tasks (T040-T053)
- Phase 4 (US2): 21 tasks (T054-T074)
- Phase 5 (US3): 13 tasks (T075-T087)
- Phase 6 (Polish): 5 tasks (T088-T092)
- **Total: 92 tasks**

---

## Notes

- [P] tasks touch different files with no ordering dependency on incomplete work.
- [Story] labels map every Phase 3+ task to US1/US2/US3 for traceability back to spec.md.
- Commit after each task or logical group (per repo convention — new commits, not amends).
- Stop at either checkpoint (end of Phase 3, end of Phase 4) to validate that story independently before continuing.
- Cross-study reference integrity (visit-arm associations, role-staff assignments) is enforced at the application layer in the controller actions, not purely via FK constraints — see data-model.md's Validation Rules.
- `StudyContact` has no dedicated controller or route — it's read/written exclusively through `StudiesController`'s own GET/POST/PUT/PATCH actions, mirroring how `PatientPhone` works today. Do not add a `StudyContactsController`.
- Concurrent `generate` calls and concurrent updates are intentionally left unsynchronized (last-write-wins for updates; auto-increment IDs make concurrent generation additive, not corrupting) — see research.md and spec.md's Assumptions. No task exists to add locking; this is a deliberate scope decision, not a gap.
