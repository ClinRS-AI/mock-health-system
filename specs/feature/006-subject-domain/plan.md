# Implementation Plan: Subject Domain

**Branch**: `feature/006-subject-domain` | **Date**: 2026-07-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/feature/006-subject-domain/spec.md`

## Summary

Add the CC Subject domain — the enrollment-episode link between a Patient and
a Study — as the third major CC resource alongside Patient and Study. Covers
core Subject CRUD (list/filter/create/update/delete) enforcing the
one-Active-enrollment-per-patient-per-study rule, an append-only
`SubjectStatus` history table exposed via CC's study-scoped, UID-keyed
`GET /studies/{studyUid}/subject-statuses/odata` endpoint, and the
corresponding admin test-data tooling (generate from existing patients/
studies, view counts + per-study patient breakdown, look up, and reset)
integrated into the existing four-tab Test Data Management dashboard. It is
delivered as two new EF Core entities/migration, two new versioned REST
controllers (`SubjectsController`, `SubjectStatusesController`), a
`SubjectFakerService` (Bogus-based, mirrors `StudyFakerService`), new
`TestDataController` actions, and extensions to all four existing Test Data
section components. Patient/Study reset endpoints require no code changes —
their existing `TRUNCATE ... CASCADE` SQL already cascades to Subject data
once the new FK columns exist (research.md Decision 6).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend); TypeScript 5.4 / React 19 (frontend) — unchanged, extending the existing solution.

**Primary Dependencies**: Same as the existing system — ASP.NET Core, EF Core 10, Asp.Versioning 8.1, Bogus 35.6, Swashbuckle 6.6 (backend); Axios, Tailwind, Recharts 3.7, Vitest, React Testing Library, MSW (frontend). No new packages required.

**Storage**: PostgreSQL 16 via Npgsql EF Core 10 (production); EF InMemory for tests — unchanged. New tables (`Subjects`, `SubjectStatuses`) added via a CLI-generated EF migration per `backend/scripts/run-ef.sh`.

**Testing**: xUnit + `IsolatedWebApplicationFactory` (integration, in-memory EF) for backend; Vitest + React Testing Library + MSW for the frontend section-component extensions — unchanged tooling, new test classes/cases for the Subject domain.

**Target Platform**: Linux/Windows server (self-hosted); developer localhost — unchanged.

**Project Type**: Web service (REST API) + admin SPA extension — unchanged; this feature adds no new deployable unit and no new frontend tab (integrates into the existing four).

**Performance Goals**: Synthetic generation of a batch of subjects linking existing patients/studies in < 30 seconds (SC-004), consistent with the Patient/Study generation SLA.

**Constraints**: All Subject endpoints inherit the existing global `RateLimitingMiddleware`, request logging, and auth-mode enforcement with no Subject-specific opt-outs (constitution II, V, IX; spec FR-013–FR-014). No new cross-cutting infrastructure. `SubjectStatus` entries are immutable/append-only — no direct write endpoint for them (data-model.md).

**Scale/Scope**: Single-tenant, same as the rest of the system. 2 new entities/tables; 2 new controllers (~8 endpoints); 5 new `TestDataController` actions; extensions to all 4 existing `TestData*Section.tsx` components (no new frontend files).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Plan |
|-----------|--------|------|
| I. Healthcare Domain Fidelity | ✅ Pass | Subject data is Bogus-generated via `SubjectFakerService`, reusing only already-synthetic Patient/Study IDs; no new real-world data source is introduced. |
| II. Authenticated-by-Default | ✅ Pass | New `test-data/subjects/*` actions reuse `IAdminRequestValidator.IsAdminRequest(..., bypassAdminChecksInDevelopment: true)`, identical to existing `studies/*`/`patients/*` actions. `SubjectsController`/`SubjectStatusesController` use `[Authorize]`, subject to the active `AuthSettings.Mode`, matching `StudiesController`. |
| III. Integration-First Testing | ✅ Pass | New integration tests use `IsolatedWebApplicationFactory` with in-memory EF; an auth-matrix test class covers all 4 auth modes on `GET /subjects/{id}` and `GET /studies/{studyUid}/subject-statuses/odata` (contracts/subject-api.md). The Postgres-`TRUNCATE CASCADE` cascade behavior (research.md Decision 6) additionally needs a live-Postgres spot-check, since EF InMemory doesn't exercise real cascade semantics — flagged as a manual verification step, not a substitute for the InMemory suite. |
| IV. API Versioning & Stability | ✅ Pass | Both new controllers use `[ApiVersion("1.0")]` and `api/v{version:apiVersion}/...` route templates. Migration generated via `backend/scripts/run-ef.sh`; `AppDbContextModelSnapshot.cs` updated by the generator. |
| V. Observability by Default | ✅ Pass | No new middleware; existing `RequestLoggingMiddleware`/`ExceptionHandlingMiddleware` apply automatically to all new routes (FR-014). |
| VI. Code Quality | ✅ Pass | Explicit `SubjectViewModel`/`SubjectEditModel`/`SubjectStatusViewModel` DTOs; `[ProducesResponseType]` on every action including 400 for FK/business-rule validation failures; async I/O throughout; `SubjectFakerService`/`SubjectMappingService` follow the `StudyFakerService`/`StudyMappingService` naming and structure exactly. |
| VII. Testing Standards | ✅ Pass | xUnit `[Fact]`; `ApiErrorAssertions` helpers for error-shape assertions; new test classes follow `<Subject><Context>Tests` naming (e.g., `SubjectsControllerTests`, `SubjectFakerServiceTests`). Frontend: Vitest + RTL + MSW + `userEvent.setup()` for the new form/lookup interactions added to each existing section's test file (colocated, per Principle VII). |
| VIII. User Experience Consistency | ✅ Pass | New Subject stat card and patients-by-study breakdown in `TestDataCountsSection` reuse the existing `CategoryPieChart` local component and per-operation loading/error state pattern; reset button reuses `ConfirmableResetButton`. Data Manipulation lookup form follows the existing per-field loading/error/result state shape. |
| IX. Security | ✅ Pass | No new secrets or CORS surface. |
| X. Performance | ✅ Pass | Bulk-insert pattern (`AddRangeAsync` + single `SaveChangesAsync`) mirrors `StudyFakerService`/`TestDataController.GenerateStudiesAsync`; `GET /subjects` filters use indexed FK columns; generation batch size is bounded at 500 (FR-008). Frontend fetches in `TestDataCountsSection` run inside the existing `Promise.all()`, not sequentially added. |

**Gate result**: ✅ All 10 principles satisfied by the planned design. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/feature/006-subject-domain/
├── plan.md              # This file
├── research.md          # Phase 0: design decisions (field shapes, studyUid route, cascade reuse, ...)
├── data-model.md         # Phase 1: Subject + SubjectStatus entity model
├── quickstart.md         # Phase 1: Subject-domain quickstart (generate + query + reset)
├── checklists/
│   └── requirements.md  # Spec quality checklist (all items passing)
├── contracts/
│   ├── subject-api.md          # Phase 1: Subject CRUD + subject-status-history CC-mirrored endpoints
│   └── subject-testdata-api.md # Phase 1: generate/reset/lookup/random/stats admin endpoints
└── tasks.md              # Phase 2 output (/speckit-tasks — not yet generated)
```

### Source Code (repository root)

```text
backend/
├── MockHealthSystem.Api/
│   ├── Controllers/
│   │   ├── SubjectsController.cs           # Core CRUD + odata (CC tag: Subject)
│   │   ├── SubjectStatusesController.cs    # Study-scoped, studyUid-keyed status history (CC tag: SubjectStatus)
│   │   └── TestDataController.cs           # + new subjects/generate, /reset, /lookup, /random, /stats actions
│   ├── Services/
│   │   ├── SubjectFakerService.cs          # Bogus-based synthetic Subject + initial-status generation
│   │   └── SubjectMappingService.cs        # Entity → ViewModel / apply Edit model
│   └── Models/Subjects/                    # SubjectViewModel, SubjectEditModel, SubjectStatusViewModel, SubjectSearchLimits
│
├── MockHealthSystem.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs                 # + 2 new DbSets, relationship config in OnModelCreating
│   │   └── Entities/                       # Subject.cs, SubjectStatus.cs
│   └── Migrations/                         # New CLI-generated migration via run-ef.sh (AddSubjectDomain)
│
└── MockHealthSystem.Tests/
    ├── Integration/                        # SubjectsControllerTests, SubjectsControllerWriteTests,
    │                                        #   SubjectStatusesControllerTests, SubjectEndpointAuthMatrixTests,
    │                                        #   TestDataControllerSubjectGenerateTests, TestDataControllerSubjectResetTests,
    │                                        #   TestDataControllerSubjectLookupTests, TestDataControllerSubjectCascadeTests,
    │                                        #   TestDataControllerSubjectPerformanceTests
    └── Unit/                                # SubjectFakerServiceTests, SubjectMappingServiceTests

frontend/
├── src/
│   ├── api.ts                              # + generateTestSubjects, resetTestSubjects, lookupTestSubject,
│   │                                        #   getRandomTestSubject, getSubjectTestDataStats, SubjectViewModel, ...
│   ├── demoData.ts                         # + DEMO_SUBJECT_TEST_DATA_STATS
│   ├── TestDataCountsSection.tsx           # + Subject count stat + patients-by-study CategoryPieChart
│   ├── TestDataGenerationSection.tsx       # + Generate Subjects form
│   ├── TestDataManipulationSection.tsx     # + Subject lookup form
│   └── TestDataInfoDestructionSection.tsx  # + Subject ConfirmableResetButton
```

**Structure Decision**: Extends the existing Web application layout (Option 2,
same as 001/004/005) — no new projects, deployable units, or frontend tabs.
Subject is split into two controllers by CC tag and route-key type (numeric
`Id` for `SubjectsController` vs. `Uid`-keyed for `SubjectStatusesController`)
rather than one combined controller, mirroring how the Study domain already
splits by CC tag (`StudiesController` vs. `StudyDocumentsController`) — see
[research.md](research.md) Decisions 2–3 for the routing rationale.

## Complexity Tracking

No constitution violations — this section is intentionally empty.
