# API Contracts: Subject Domain (CC-Mirrored Endpoints)

**Base URL**: `http://<host>/api/v1`
**Authentication**: `[Authorize]` — subject to the active `AuthSettings.Mode`
(`None`/`Bearer`/`CCAPIKey`/`OAuth`), identical to `StudiesController`/
`PatientsController`. Not admin-gated.

Two new controllers: `SubjectsController` (top-level Subject CRUD, route
`api/v{version:apiVersion}/subjects`) and `SubjectStatusesController`
(study-scoped status-history read, route
`api/v{version:apiVersion}/studies/{studyUid:guid}/subject-statuses`). See
[research.md](research.md) Decisions 2–5 for why these are separate
controllers with different route-key types.

---

### `GET /subjects`

List subjects with optional filtering and pagination — mirrors
`StudiesController.GetStudies`.

**Query parameters**: `patientId` (int, optional), `studyId` (int, optional),
`status` (string, optional), `skip` (int, default 0), `limit` (int, default
100, max per `SubjectSearchLimits`).

**Response** `200 OK`: `IEnumerable<SubjectViewModel>`.

---

### `GET /subjects/odata`

OData-style endpoint; simple list without query options, capped at 100 rows —
mirrors `StudiesController.GetStudiesOData`.

**Response** `200 OK`: `IEnumerable<SubjectViewModel>`.

---

### `GET /subjects/{id}`

**Response** `200 OK`: `SubjectViewModel`. `404` if no subject with that ID.

---

### `POST /subjects`

Creates a subject. Also creates its initial `SubjectStatus` row (research.md
Decision 5).

**Request body** (`SubjectEditModel`):
```json
{
  "patientId": 42,
  "studyId": 7,
  "studyArmId": null,
  "status": "Prescreened",
  "subjectIdentifier": "SCR-0042",
  "enrollmentDate": "2026-07-20T00:00:00Z",
  "screeningDate": "2026-07-18T00:00:00Z",
  "withdrawalDate": null,
  "withdrawalReason": null
}
```
`status` is one of CC's nine defined values (see `SubjectStatusViewModel`
note below); `subjectIdentifier` is optional — not every subject has one
assigned (e.g. before screening completes).

**Validation** (400 on failure, before any write):
- `patientId` MUST reference an existing Patient.
- `studyId` MUST reference an existing Study.
- `studyArmId`, if present, MUST reference a `StudyArm` belonging to `studyId`.
- `status` MUST be one of the nine defined values (research.md Decision 11).
- If `status` is in the Active category ("Prescreened", "Screened",
  "Randomized", "Run-in"), no other Subject for the same `(patientId,
  studyId)` may currently have a `status` in the Active category.

**Response** `201 Created`: `SubjectViewModel`.

---

### `PUT /subjects/{id}` / `PATCH /subjects/{id}`

Full/partial update. Same validation as create (re-run against the merged
result). If `status` changes, appends a new `SubjectStatus` row (research.md
Decision 5).

**Response** `200 OK`: `SubjectViewModel`. `404` if no subject with that ID.
`400` on validation failure (including the one-Active-category-per-pair rule).

---

### `DELETE /subjects/{id}`

Deletes the subject and (via cascade) its `SubjectStatus` history.

**Response** `204 No Content`. `404` if no subject with that ID.

---

### `GET /studies/{studyUid}/subject-statuses/odata`

CC-mirrored, study-scoped status-history list. Simple list without query
options, capped at 100 rows, ordered by `ChangedOn` descending — mirrors
`StudyDocumentsController.GetDocumentHistory`'s ordering and
`StudiesController.GetStudiesOData`'s "simple odata list" shape combined.
Resolves the study by `Uid` (not the numeric `Id` every other Study
sub-resource route uses — see research.md Decision 2).

**Path parameter**: `studyUid` (Guid) — the target `Study.Uid`.

**Response** `200 OK`: `IEnumerable<SubjectStatusViewModel>` — every recorded
status entry for subjects enrolled in that study. Empty array (not 404) if the
study has no subjects or no recorded status changes (spec.md edge case).
`404` if no study has that UID.

```json
[
  {
    "id": 501,
    "subjectId": 42,
    "statusName": "Randomized",
    "changedOn": "2026-07-19T14:02:00Z",
    "changedBy": { "id": 3, "displayName": "Dr. Patel" },
    "comment": null
  }
]
```

---

### View Model shapes

`SubjectViewModel`: `id`, `uid`, `patientId`, `studyId`, `studyArmId?`,
`status`, `subjectIdentifier?`, `enrollmentDate`, `screeningDate?`,
`withdrawalDate?`, `withdrawalReason?`, `createdOn`, `lastUpdatedOn`.
`status` is always one of CC's nine defined values — Active category:
`Prescreened`, `Screened`, `Randomized`, `Run-in`; Inactive category:
`Screen Failed`, `Non Qualified`, `Dropped`, `Run-in Failed`, `Complete`
(research.md Decision 11). "Active" itself is never a literal value — it is
shorthand for "any Active-category status."

`SubjectStatusViewModel`: `id`, `subjectId`, `statusName`, `changedOn`,
`changedBy?` (`StaffPreviewModel`, reused from the Study domain), `comment?`.
`statusName` uses the same nine-value vocabulary as `SubjectViewModel.status`.

### Auth-matrix coverage

At least one representative Subject route (`GET /subjects/{id}` — read) and
the CC-mirrored status-history route (`GET
/studies/{studyUid}/subject-statuses/odata`) MUST each have an auth-matrix
test class covering all four auth modes, matching
`StudyEndpointAuthMatrixTests`'s pattern (constitution Principle III).
