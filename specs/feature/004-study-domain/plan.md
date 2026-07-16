# Implementation Plan: Study Domain

**Branch**: `feature/004-study-domain` | **Date**: 2026-07-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/feature/004-study-domain/spec.md`

## Summary

Add the CC Study domain to the mock system as the second major CC resource
alongside Patients. This covers core Study CRUD, eight structural sub-resources
(Arms, Visits + visit-arm association, Milestones, Documents + status history,
Notes, Roles/personnel, Protocol Versions, and the Study record's embedded
target-dates/leadership/custom-fields/contacts), and CC's Study reference/lookup
endpoints (Categories, Subcategories, Types, Statuses, Groups). It is delivered as
new EF Core entities/migrations, new versioned REST controllers mirroring the CC
path shapes, a `StudyFakerService` for synthetic generation (Bogus-based, following
`PatientFakerService`), and new `TestDataController` actions for generate/reset/
lookup — plus an admin-UI extension point for browsing generated studies. Patient
enrollment linkage and the monitoring/EDC surface are explicitly out of scope per
the spec's Assumptions.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (backend); TypeScript 5.4 / React 18 (frontend) — unchanged, extending the existing solution.

**Primary Dependencies**: Same as the existing system — ASP.NET Core, EF Core 10, Asp.Versioning 8.1, Bogus 35.6, Swashbuckle 6.6 (backend); Axios, Tailwind, Vitest, React Testing Library, MSW (frontend). No new packages required.

**Storage**: PostgreSQL 16 via Npgsql EF Core 10 (production); SQLite/InMemory for tests — unchanged. New tables added via CLI-generated EF migrations per `backend/scripts/run-ef.sh`.

**Testing**: xUnit + `IsolatedWebApplicationFactory` (integration, in-memory EF) for backend; Vitest + React Testing Library + MSW for any new frontend surface — unchanged tooling, new test classes for the Study domain.

**Target Platform**: Linux/Windows server (self-hosted); developer localhost — unchanged.

**Project Type**: Web service (REST API) + admin SPA extension — unchanged; this feature adds no new deployable unit.

**Performance Goals**: Synthetic generation of a batch of studies (with populated structural sub-resources) in < 30 seconds, consistent with the existing patient-generation SLA (SC-003).

**Constraints**: All Study endpoints inherit the existing global `RateLimitingMiddleware`, request logging, and auth-mode enforcement with no Study-specific opt-outs (constitution II, V, IX; spec FR-009–FR-010). No document binary storage is introduced — `StudyDocument` is metadata-only (assumption).

**Scale/Scope**: Single-tenant, same as the rest of the system. 24 new entities/tables; roughly 45 new REST endpoints across 9 new controllers.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Plan |
|-----------|--------|------|
| I. Healthcare Domain Fidelity | ✅ Pass | All Study data is Bogus-generated via `StudyFakerService`; no real sponsor/protocol data is ever seeded. Study data is not PHI, but the same synthetic-only discipline applies. |
| II. Authenticated-by-Default | ✅ Pass | New `test-data/studies/*` actions reuse `IAdminRequestValidator.IsAdminRequest(..., bypassAdminChecksInDevelopment: true)`, identical to existing `TestDataController` patient actions. Study CRUD controllers use `[Authorize]` like `PatientsController`, subject to the active `AuthSettings.Mode`. `StudyLookupController` (Categories/Subcategories/Types/Statuses/Groups) is Mock-Health-System admin configuration, not CC integration traffic, so it uses the same `IAdminRequestValidator` admin gate as `test-data/patients/lookup` instead of `[Authorize]` — see research.md. |
| III. Integration-First Testing | ✅ Pass | New integration tests use `IsolatedWebApplicationFactory` with in-memory EF; an auth-matrix test class covers all 4 auth modes on at least one representative Study route (`GET /studies/{id}`), matching the existing `PatientAndSystemEndpointAuthMatrixTests` pattern. |
| IV. API Versioning & Stability | ✅ Pass | All new controllers use `[ApiVersion("1.0")]` and `api/v{version:apiVersion}/...` route templates. Migrations generated via `backend/scripts/run-ef.sh`; `AppDbContextModelSnapshot.cs` updated by the generator, not by hand. |
| V. Observability by Default | ✅ Pass | No new middleware; existing `RequestLoggingMiddleware` and `ExceptionHandlingMiddleware` apply automatically to all new routes. |
| VI. Code Quality | ✅ Pass | Explicit `*ViewModel`/`*EditModel`/`*PatchModel` DTOs per CC naming; `[ProducesResponseType]` on every action; async I/O throughout; specific exception types where needed. New `test-data/studies/*` responses use explicitly named DTOs (`GenerateStudiesResponse`, `StudyTestDataStatsDto`, etc.) rather than anonymous objects — deliberately not repeating the pre-existing anonymous-object pattern in `GeneratePatientsAsync`/`ResetPatientsAsync`. |
| VII. Testing Standards | ✅ Pass | xUnit `[Fact]`; `ApiErrorAssertions` helpers for error-shape assertions; new test classes follow `<Subject><Context>Tests` naming (e.g., `StudiesControllerTests`, `StudyFakerServiceTests`). |
| VIII. User Experience Consistency | ✅ Pass | If a `TestDataPage` extension for Study browsing is added, it follows the same granular loading-state and `text-red-700 bg-red-50` error pattern already used for patients. |
| IX. Security | ✅ Pass | No new secrets or CORS surface. `RequestLoggingMiddleware`'s existing 4 KB body cap and credential-redaction logic apply unchanged. |
| X. Performance | ✅ Pass | Bulk-insert pattern (`AddRangeAsync` + single `SaveChangesAsync` per batch) mirrors `PatientFakerService`/`TestDataController.GeneratePatientsAsync`; sub-resource list endpoints filter by parent ID with `.Include()` to avoid N+1; generation batch size is bounded (FR-006). |

**Gate result**: ✅ All 10 principles satisfied by the planned design. No violations to justify.

## Project Structure

### Documentation (this feature)

```text
specs/feature/004-study-domain/
├── plan.md              # This file
├── research.md          # Phase 0: scope simplifications and design decisions
├── data-model.md        # Phase 1: full entity model
├── quickstart.md        # Phase 1: Study-domain quickstart (generate + query)
├── checklists/
│   └── requirements.md  # Spec quality checklist (all items passing)
├── contracts/
│   ├── study-api.md         # Phase 1: Study CRUD + structural sub-resource + lookup endpoints
│   └── study-testdata-api.md # Phase 1: generate/reset/lookup admin endpoints
└── tasks.md              # Phase 2 output (/speckit-tasks — not yet generated)
```

### Source Code (repository root)

```text
backend/
├── MockHealthSystem.Api/
│   ├── Controllers/
│   │   ├── StudiesController.cs            # Core CRUD + odata + personnel (CC tag: Study)
│   │   ├── StudyArmsController.cs           # Arms + visit-arm association (CC tag: StudyArm)
│   │   ├── StudyVisitsController.cs         # Visits + arm listing (CC tag: Visit)
│   │   ├── StudyMilestonesController.cs     # Milestones (CC tag: StudyMilestone)
│   │   ├── StudyDocumentsController.cs      # Documents + status history (CC tag: StudyDocument)
│   │   ├── StudyNotesController.cs          # Notes (CC tag: StudyNote)
│   │   ├── StudyRolesController.cs          # Roles/personnel assignment (CC tag: StudyRole)
│   │   ├── ProtocolVersionsController.cs    # Protocol versions
│   │   ├── StudyLookupController.cs         # Categories/Subcategories/Types/Statuses/Groups — admin-gated
│   │   │                                    #   config (IAdminRequestValidator), route prefix mirrors CC's
│   │   │                                    #   "system" tag but auth model mirrors TestDataController
│   │   └── TestDataController.cs            # + new studies/generate, /reset, /lookup, /random, /stats actions
│   ├── Services/
│   │   ├── StudyFakerService.cs             # Bogus-based synthetic Study + sub-resource generation
│   │   └── StudyMappingService.cs           # Entity → ViewModel / apply Edit-Patch models
│   └── Models/Studies/                      # New *ViewModel / *EditModel / *PatchModel DTOs
│
├── MockHealthSystem.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs                  # + 24 new DbSets, relationship config in OnModelCreating
│   │   └── Entities/                        # Study, StudyArm, StudyVisit, StudyVisitArm, StudyMilestone,
│   │                                         #   StudyDocument, StudyDocumentStatusHistory, StudyContact,
│   │                                         #   StudyNote, StudyRole, StudyRoleStaff, ProtocolVersion,
│   │                                         #   StudyTargetDate, StudyLeadership, StudyCustomFieldValue,
│   │                                         #   StudyCategory, StudySubcategory, StudyType, StudyStudyType,
│   │                                         #   StudyStatusType, StudyGroup, Sponsor, SponsorDivision,
│   │                                         #   SponsorTeam
│   └── Migrations/                          # New CLI-generated migration(s) via run-ef.sh
│
└── MockHealthSystem.Tests/
    ├── Integration/                         # StudiesControllerTests, StudyArmsControllerTests, ...,
    │                                         #   StudyEndpointAuthMatrixTests, TestDataControllerStudyTests
    └── Unit/                                # StudyFakerServiceTests, StudyMappingServiceTests

frontend/
├── src/
│   ├── api.ts                               # + generateTestStudies, resetTestStudies, lookupTestStudy, ...
│   └── TestDataPage.tsx                     # + a Studies section alongside the existing Patients section
```

**Structure Decision**: Extends the existing Web application layout (Option 2, same
as 001-cc-api-mock) — no new projects or deployable units. The Study surface is
split across multiple controllers by CC tag (unlike `PatientsController`, which
handles its whole surface in one file) because Study has roughly 3x the number of
distinct sub-resource groups; see [research.md](research.md) for the rationale.

## Complexity Tracking

No constitution violations — this section is intentionally empty.
