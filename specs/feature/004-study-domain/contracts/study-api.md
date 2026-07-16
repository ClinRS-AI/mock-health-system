# API Contracts: Study API (CC-Facing Endpoints)

**Base URL**: `http://<host>/api/v1`
**Authentication**: Controlled by active `AuthSettings.Mode` (None / Bearer / CCAPIKey / OAuth), identical to Patient endpoints.
**Versioning**: URL path (`v1`); header `api-version: 1.0` also accepted.

All routes below return the same error shape (`ApiErrorResponse`) as existing
endpoints on validation failure (400), not-found (404), and auth failure (401).
Cross-study reference violations (e.g., a visit-arm association spanning two
studies) return 400 with a descriptive detail message.

**Note on reference/lookup endpoints**: Study Categories, Subcategories, Types,
Statuses, and Groups are Mock-Health-System admin configuration, not part of the
CC-mirrored surface documented here — see
[study-testdata-api.md](study-testdata-api.md) for their contract and auth model.

---

## Studies — core (`StudiesController`)

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/studies` | Paginated list; supports filters on name/status/category/protocolNumber, `skip`/`limit` — mirrors `PatientsController.SearchPatients` pagination conventions. |
| GET | `/studies/odata` | Simple list (no query options), `Take(100)` cap — mirrors `PatientsController.GetPatientsOData`. |
| GET | `/studies/{id:int}` | Full `StudyViewModel` including embedded target dates, leadership, custom fields, and a `contacts` array (backed by real `StudyContact` rows, upserted by `(contactType, slot)` on write — no separate contacts endpoint exists, matching CC). |
| POST | `/studies` | Create; requires `sponsorTeamId`. Returns 201 with `Location` via `CreatedAtAction`. |
| PUT | `/studies/{id:int}` | Full update (embedded arrays replaced wholesale, matching `PatientsController.UpdatePatient`'s phone-slot replace semantics). |
| PATCH | `/studies/{id:int}` | Partial update — only supplied fields change; omitted embedded arrays are left untouched (matching `PatientMappingService.ApplyPatchModel` semantics). |
| DELETE | `/studies/{id:int}` | Cascades to all structural sub-resources (see data-model.md). 204 on success. |
| GET | `/studies/{id:int}/personnel` | Aggregates `Leadership` + `StudyRoleStaff` staff into one read-only list. |

## Study Arms (`StudyArmsController`)

| Method | Route | Notes |
|--------|-------|-------|
| GET / POST | `/studies/{studyId:int}/arms` | List / create arms scoped to the study. |
| GET / PUT / DELETE | `/studies/{studyId:int}/arms/{id:int}` | 404 if `id` doesn't belong to `studyId`. |
| GET | `/studies/{studyId:int}/arms/{armId:int}/visits` | Visits associated with the arm. |
| POST / DELETE | `/studies/{studyId:int}/arms/{armId:int}/visits/{visitId:int}` | Create/remove a `StudyVisitArm` row; 400 if the visit isn't in the same study. |

## Study Visits (`StudyVisitsController`)

| Method | Route | Notes |
|--------|-------|-------|
| POST | `/studies/{studyId:int}/visits` | Create a visit scoped to the study. |
| GET | `/studies/{studyId:int}/visits/odata` | Simple list, `Take(100)`. |
| GET / PUT / DELETE | `/studies/{studyId:int}/visits/{id:int}` | 404 if `id` doesn't belong to `studyId`. |
| GET | `/studies/{studyId:int}/visits/{visitId:int}/arms` | Arms associated with the visit. |

## Study Milestones (`StudyMilestonesController`)

| Method | Route | Notes |
|--------|-------|-------|
| GET / POST | `/studies/{studyId:int}/milestones` | |
| GET | `/studies/{studyId:int}/milestones/odata` | Simple list, `Take(100)`. |
| GET / PUT / DELETE | `/studies/{studyId:int}/milestones/{id:int}` | |

## Study Documents (`StudyDocumentsController`)

| Method | Route | Notes |
|--------|-------|-------|
| GET / POST | `/studies/{studyId:int}/documents` | POST also creates the initial `StudyDocumentStatusHistory` row. |
| GET | `/studies/{studyId:int}/documents/odata` | Simple list, `Take(100)`. |
| GET / PUT / DELETE | `/studies/{studyId:int}/documents/{id:int}` | PUT that changes `StatusName` appends a new history row. |
| GET | `/studies/{studyId:int}/documents/{id:int}/history` | Ordered by `ChangedOn` descending. |

## Study Notes (`StudyNotesController`)

| Method | Route | Notes |
|--------|-------|-------|
| GET / POST | `/studies/{studyId:int}/notes` | |
| GET | `/studies/{studyId:int}/notes/odata` | Simple list, `Take(100)`. |
| GET / PUT / DELETE | `/studies/{studyId:int}/notes/{id:int}` | PUT rejected (409) when `Locked = true`, matching CC's locked-note semantics. |

## Study Roles (`StudyRolesController`)

| Method | Route | Notes |
|--------|-------|-------|
| GET | `/studies/{studyId:int}/roles` | Includes assigned `StudyRoleStaff`. |
| GET / PUT | `/studies/{studyId:int}/roles/{roleId:int}` | PUT replaces the assigned-staff set for the role. |

## Protocol Versions (`ProtocolVersionsController`)

| Method | Route | Notes |
|--------|-------|-------|
| GET / POST | `/studies/{studyId:int}/protocol-versions` | |
| GET / PUT / DELETE | `/studies/{studyId:int}/protocol-versions/{id:int}` | Referenced by `StudyArm.ProtocolVersionId` / `StudyVisit.ProtocolVersionId`; delete blocked (409) while referenced, matching `PatientsController.DeletePatient`'s `DbUpdateException → Conflict` pattern. |

## Study Types association (`StudiesController`)

| Method | Route | Notes |
|--------|-------|-------|
| POST | `/studies/{studyId:int}/types/add` | Body: `{ "studyTypeId": int }`; creates a `StudyStudyType` row. |
| DELETE | `/studies/{studyId:int}/types/{id:int}` | `id` is the `StudyType` id being removed from the study. |

---

## Auth & error-shape parity

Every route above (i.e., everything in this document — `StudyLookupController`'s
admin-gated routes are documented separately in
[study-testdata-api.md](study-testdata-api.md)):
- Requires the active auth mode's credential exactly like Patient routes
  (`[Authorize]`, enforced by `MockAuthHandler`).
- Returns the same `ApiErrorResponse` JSON shape on 400/401/404/409 as existing
  endpoints.
- Is subject to the global `RateLimitingMiddleware` and
  `RequestLoggingMiddleware` with no Study-specific exemptions (FR-009, FR-010).
