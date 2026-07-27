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

OData-style endpoint returning a paged wrapper — mirrors
`SystemController`'s `conditions/odata`/`medications/odata`/`allergies/odata`
endpoints (research.md Decision 14), not `StudiesController.GetStudiesOData`
(which still returns a bare array).

**Header**: `queryOptions` (string, optional) — accepted for wire
compatibility with Clinical Conductor clients but not parsed; only
`skip`/`top` below are honored.

**Query parameters**: `studyId` (int, optional filter), `skip` (int,
default 0), `top` (int, default 100, max per `SubjectSearchLimits`).

**Response** `200 OK`: `ODataPageResult<SubjectViewModel>`:
```json
{
  "Items": [ { "id": 1, "uid": "1ebbb168-...", "study": { "id": 7, "uid": "...", "name": "Acme Cardio Study" }, "...": "..." } ],
  "Count": 42,
  "NextPageLink": null
}
```
`Count` is the total matching row count (before paging); `NextPageLink` is
always `null` — no endpoint in this codebase computes a real next-page link
today.

---

### `GET /subjects/{id}`

**Response** `200 OK`: `SubjectViewModel`. `404` if no subject with that ID.

---

### `POST /subjects`

Creates a subject. Also creates its initial `SubjectStatus` row (research.md
Decision 5).

**Request body** (`SubjectCreateModel`) — CC's real Subject POST body accepts
a narrower field set than PUT does; `studyArmId`, `protocolVersionId`,
`status`, `genderCode`, `race`, `ethnicity` are not part of this model and
are silently ignored if sent — they're set later via `PUT`/`PATCH`
(research.md Decision 13):
```json
{
  "patientId": 42,
  "studyId": 7,
  "siteId": null,
  "importId": null,
  "tag": null,
  "facilityCode": null,
  "enrollmentDate": "2026-07-20T00:00:00Z",
  "enrollmentLocation": null,
  "screeningNumber": "SCR-0042",
  "randomizationNumber": null,
  "treatmentStatus": null,
  "treatmentStart": null,
  "narrative": null
}
```
`screeningNumber` is optional — not every subject has one assigned (e.g.
before screening completes). `status` is always server-assigned to
`SubjectStatusCatalog.InitialStatus` ("Prescreened") on creation — it cannot
be set via POST (research.md Decision 13).

**Validation** (400 on failure, before any write):
- `patientId` MUST reference an existing Patient.
- `studyId` MUST reference an existing Study.
- `siteId`, if present, MUST reference an existing Site.
- Since a newly created Subject's `status` is always the Active-category
  "Prescreened", no other Subject for the same `(patientId, studyId)` may
  currently have a `status` in the Active category.

**Response** `201 Created`: `SubjectViewModel`.

---

### `PUT /subjects/{id}` / `PATCH /subjects/{id}`

Full/partial update. Unlike `POST`, the request body (`SubjectEditModel` /
`SubjectPatchModel`) accepts the full field set — including `studyArmId`,
`protocolVersionId`, `status`, `genderCode`, `race`, `ethnicity` — since
those transition after creation, not at it (research.md Decision 13). Same
validation as create for the shared fields, plus:
- `studyArmId`, if present, MUST reference a `StudyArm` belonging to `studyId`.
- `protocolVersionId`, if present, MUST reference a `ProtocolVersion`
  belonging to `studyId`.
- `status` MUST be one of the nine defined values (research.md Decision 11).
- If `status` is in the Active category ("Prescreened", "Screened",
  "Randomized", "Run-in"), no other Subject for the same `(patientId,
  studyId)` may currently have a `status` in the Active category.

If `status` changes, appends a new `SubjectStatus` row (research.md
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

`SubjectViewModel` (matches a real CC Subject response — see research.md
Decision 12):
```json
{
  "id": 1,
  "uid": "1ebbb168-680d-4e68-a200-538c744bb0c0",
  "study": { "id": 7, "uid": "660e8400-...", "name": "Acme Cardio Study" },
  "site": { "id": 3, "uid": "770e8400-...", "name": "Main Site" },
  "patient": {
    "id": 42,
    "uid": "880e8400-...",
    "firstName": "Jane",
    "middleName": null,
    "lastName": "Doe",
    "title": null,
    "genderCode": "F",
    "race": "White",
    "ethnicity": "Not Hispanic or Latino",
    "dateOfBirth": "1985-03-15T00:00:00Z",
    "name": "Doe, Jane"
  },
  "status": "Randomized",
  "protocolVersion": { "id": 5, "uid": "990e8400-...", "name": "v2" },
  "genderCode": "F",
  "race": "White",
  "ethnicity": "Not Hispanic or Latino",
  "arm": { "id": 9, "uid": "aa0e8400-...", "name": "Arm A" },
  "importId": null,
  "tag": null,
  "facilityCode": null,
  "enrollmentDate": "2026-07-20T00:00:00Z",
  "enrollmentLocation": null,
  "screeningNumber": "SCR-0042",
  "randomizationNumber": "RND-1234",
  "treatmentStatus": "On Treatment",
  "treatmentStart": "2026-07-25T00:00:00Z",
  "narrative": null,
  "createdOn": "2026-07-22T17:56:55Z",
  "lastUpdatedOn": "2026-07-23T09:00:00Z"
}
```
`status` is always one of CC's nine defined values — Active category:
`Prescreened`, `Screened`, `Randomized`, `Run-in`; Inactive category:
`Screen Failed`, `Non Qualified`, `Dropped`, `Run-in Failed`, `Complete`
(research.md Decision 11). "Active" itself is never a literal value — it is
shorthand for "any Active-category status." `treatmentStatus` is a distinct,
unvalidated free-text field — not the same concept as `status`. `site`,
`protocolVersion`, and `arm` are `null` when the corresponding optional FK
isn't set; `study` and `patient` are always present.

`SubjectStatusViewModel`: `id`, `subjectId`, `statusName`, `changedOn`,
`changedBy?` (`StaffPreviewModel`, reused from the Study domain), `comment?`.
`statusName` uses the same nine-value vocabulary as `SubjectViewModel.status`.

### Auth-matrix coverage

At least one representative Subject route (`GET /subjects/{id}` — read) and
the CC-mirrored status-history route (`GET
/studies/{studyUid}/subject-statuses/odata`) MUST each have an auth-matrix
test class covering all four auth modes, matching
`StudyEndpointAuthMatrixTests`'s pattern (constitution Principle III).
