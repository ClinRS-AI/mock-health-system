# API Contracts: Subject Test Data (Admin-Facing Endpoints)

**Base URL**: `http://<host>/api/v1/test-data`
**Authentication**: Admin-protected —
`IAdminRequestValidator.IsAdminRequest(HttpContext,
bypassAdminChecksInDevelopment: true)`, identical to the existing
`studies/*`/`patients/*` actions on `TestDataController`. Open in local dev
(`ASPNETCORE_ENVIRONMENT=Development`); requires `X-Admin-Key` or a valid
`X-Admin-Session` JWT otherwise.

All new actions are added to the existing `TestDataController` — consistent
with its established role as the single home for all synthetic-data tooling
(research.md Decision 8).

---

### `POST /test-data/subjects/generate`

Generates synthetic Subjects linking existing Patients and Studies (never
creates new Patients or Studies — FR-008). Fails clearly if no Patients or no
Studies exist yet.

**Request body**:
```json
{
  "totalCount": 25,
  "seed": null
}
```
Defaults: `totalCount = 25`. Must be > 0 and ≤ 500 (matches Study's cap).

**Response** `200 OK` (`GenerateSubjectsResponse`):
```json
{
  "totalRequested": 25,
  "totalInserted": 25,
  "statusHistoryInserted": 25,
  "totalAfter": 25
}
```
`totalInserted` may be less than `totalRequested` if fewer valid
`(patient, study)` combinations exist than requested without violating the
one-Active-per-pair rule (spec.md edge case) — the response always reports
the actual count, never silently under-delivers without saying so.

**Error** `400 Bad Request`: `"No patients exist yet. Generate patients before generating subjects."` or the equivalent for studies, if either prerequisite is empty.

---

### `POST /test-data/subjects/reset`

Truncates `SubjectStatuses` and `Subjects` via `TRUNCATE ... RESTART IDENTITY
CASCADE`, mirroring `ResetStudiesAsync`. Does not affect Patient or Study
data.

**Response** `200 OK`: empty body.

---

### `GET /test-data/subjects/lookup`

Looks up a single subject by `id`, `uid`, or by `patientId` + `studyId`
together (first match wins).

**Query parameters**: `id` (int) | `uid` (Guid) | `patientId` + `studyId`
(both required together). At least one lookup strategy required (400
otherwise).

**Response** `200 OK`: `SubjectViewModel`. `404` if no match.

---

### `GET /test-data/subjects/random`

Returns a single random subject — mirrors `GetRandomStudyAsync`. `404` if no
subjects exist.

---

### `GET /test-data/subjects/stats`

**Response** `200 OK` (`SubjectTestDataStatsDto`):
```json
{
  "subjectCount": 150,
  "patientsByStudy": [
    { "studyId": 7, "studyName": "Acme Cardio Adjuvant Study", "patientCount": 42 },
    { "studyId": 8, "studyName": "Beta Oncology Follow-up", "patientCount": 31 }
  ]
}
```
`patientsByStudy[].patientCount` is the count of **distinct** patients enrolled
in that study (a patient with multiple enrollment episodes in the same study
counts once) — matches FR-009 and data-model.md's query-pattern note.

---

### Concurrency

Not synchronized beyond the one-Active-per-patient-per-study constraint
itself, which `SubjectFakerService`'s candidate-filtering (research.md
Decision 7) and `SubjectsController`'s per-write validation both enforce
independently — mirrors how `GenerateStudiesAsync`/`GenerateStudiesResponse`
already behave under overlapping calls.
