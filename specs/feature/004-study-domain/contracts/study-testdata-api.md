# API Contracts: Study Test Data (Admin-Facing Endpoints)

**Base URL**: `http://<host>/api/v1/test-data`
**Authentication**: Admin-protected — `IAdminRequestValidator.IsAdminRequest(HttpContext, bypassAdminChecksInDevelopment: true)`, identical to the existing `patients/*` actions on `TestDataController`. Open in local dev (`ASPNETCORE_ENVIRONMENT=Development`); requires `X-Admin-Key` or a valid `X-Admin-Session` JWT otherwise.

All new actions are added to the existing `TestDataController` (same file as the
patient test-data actions), not a new controller — consistent with that
controller's existing role as the single home for all synthetic-data tooling.

---

### `POST /test-data/studies/generate`

Generates synthetic studies with populated structural sub-resources. Auto-seeds
prerequisite lookup rows (Sponsor/Division/Team, StudyCategory/Subcategory,
StudyStatusType, StudyType, StudyGroup) if none exist yet (see research.md).

**Request body**:
```json
{
  "totalCount": 25,
  "seed": null
}
```
Defaults: `totalCount = 25` (smaller than Patient's 5000 default, since each study
carries proportionally more sub-resource rows). `totalCount` must be > 0 and ≤ the
configured maximum (mirrors `GeneratePatientsRequest` validation).

**Response** `200 OK` (`GenerateStudiesResponse` — named DTO, not an anonymous
object, per constitution VI):
```json
{
  "totalRequested": 25,
  "totalInserted": 25,
  "armsInserted": 61,
  "visitsInserted": 143,
  "milestonesInserted": 98,
  "documentsInserted": 52,
  "notesInserted": 40,
  "totalAfter": 25
}
```

**Concurrency**: Not synchronized. Overlapping `generate` calls each produce their
own valid, non-colliding batch since study/sub-resource IDs are
database-assigned — the only effect of an overlap is more studies existing
afterward than a single call requested (see research.md). This mirrors how
`GeneratePatientsAsync` already behaves; no new locking is introduced.

---

### `POST /test-data/studies/reset`

Truncates all Study-domain tables (in FK-safe order) via `TRUNCATE ... RESTART
IDENTITY CASCADE`, mirroring `ResetPatientsAsync`. Does **not** truncate `Sites`
or `Staff` (shared with the Patient domain) or the new `Sponsor`/`SponsorDivision`/
`SponsorTeam` lookup tables by default — a `?includeLookups=true` query flag
additionally clears Sponsor/Division/Team and the Study category/subcategory/
type/status/group lookups, for a full clean-slate reset.

**Response** `200 OK`: empty body, `200` on success (matches `ResetPatientsAsync`).

---

### `GET /test-data/studies/lookup`

Looks up a single study by `id`, `uid`, `name` fragment, `identifier` fragment, or
`protocolNumber` fragment (first match wins) — mirrors
`LookupPatientAsync`'s query-parameter contract.

**Query parameters**: `id` (int) | `uid` (Guid) | `name` (string) | `identifier`
(string) | `protocolNumber` (string). At least one required (400 otherwise).

**Response** `200 OK`: `StudyViewModel`. `404` if no match.

---

### `GET /test-data/studies/random`

Returns a single random study — mirrors `GetRandomPatientAsync`.

**Response** `200 OK`: `StudyViewModel`. `404` if no studies exist.

---

### `GET /test-data/studies/stats`

Summary statistics for Study test data — mirrors `GetPatientStatsAsync`.

**Response** `200 OK` (`StudyTestDataStatsDto` — named DTO, not an anonymous
object, per constitution VI):
```json
{
  "studyCount": 25,
  "armCount": 61,
  "visitCount": 143,
  "milestoneCount": 98,
  "documentCount": 52,
  "studiesByStatus": [
    { "statusName": "Enrolling", "count": 14 },
    { "statusName": "Closed", "count": 6 }
  ],
  "studiesBySponsor": [
    { "sponsorName": "Acme Therapeutics", "count": 9 }
  ]
}
```

---

### Reference / Lookup Data (`StudyLookupController`)

**Route prefix**: `/api/v1/system/...` — note this deviates from this document's
`/test-data` base URL, but the auth model is identical to every action above:
admin-gated (`IAdminRequestValidator`, open in Development). These are
Mock-Health-System admin configuration endpoints, not part of the CC-mirrored
surface in `study-api.md` — see research.md's "`StudyLookupController` uses admin
authentication" decision for why.

| Method | Route | Notes |
|--------|-------|-------|
| GET / POST | `/system/study-categories` | |
| GET / PUT / DELETE | `/system/study-categories/{id:int}` | |
| GET / POST | `/system/study-subcategories` | |
| GET / PUT / DELETE | `/system/study-subcategories/{id:int}` | |
| GET | `/system/study-types` | Read-only in this phase (no create/update mirrored beyond the study-type association endpoints on `StudiesController`). |
| GET | `/system/study-statuses` | Read-only. |
| GET | `/system/study-groups` | Read-only. |

---

## Frontend admin UI

`TestDataPage.tsx` gains a "Studies" section alongside the existing "Patients"
section: generate (count + optional seed), reset, and lookup-by-fragment forms,
using the same granular loading/error state pattern (constitution VIII) and
calling new typed functions in `api.ts` (`generateTestStudies`,
`resetTestStudies`, `lookupTestStudy`, `getRandomTestStudy`,
`getStudyTestDataStats`).
