# Quickstart: Subject Domain

Assumes the mock system is already running per the top-level project
quickstart (`specs/feature/001-cc-api-mock/quickstart.md`) — database
initialized, backend on `http://localhost:5001`, frontend on
`http://localhost:5176` — and that Patient and Study data already exist
(Subject generation reuses them; it never creates its own).

---

## 1. Apply the new migration

```bash
backend/scripts/run-ef.sh database update
```

This creates the `Subjects` and `SubjectStatuses` tables (see `data-model.md`).

---

## 2. Generate prerequisite Patients and Studies (if needed)

```bash
curl -X POST http://localhost:5001/api/v1/test-data/patients/generate -d '{"totalCount": 50}' -H "Content-Type: application/json"
curl -X POST http://localhost:5001/api/v1/test-data/studies/generate -d '{"totalCount": 10}' -H "Content-Type: application/json"
```

---

## 3. Generate synthetic subjects

```bash
curl -X POST http://localhost:5001/api/v1/test-data/subjects/generate \
  -H "Content-Type: application/json" \
  -d '{"totalCount": 30, "seed": 42}'
```

Links existing patients and studies; fails with a clear error if step 2 was
skipped. Each generated subject gets an initial `SubjectStatus` entry.

Or from the admin UI: open `http://localhost:5176`, go to **Test Data
Management → Data Generation**, enter a count under Subjects, and click
**Generate**.

---

## 4. Call the CC Subject API

```bash
# List subjects
curl http://localhost:5001/api/v1/subjects

# Filter by study or patient
curl "http://localhost:5001/api/v1/subjects?studyId=1"
curl "http://localhost:5001/api/v1/subjects?patientId=5"

# Fetch one subject's detail
curl http://localhost:5001/api/v1/subjects/1

# A study's subject-status history (note: keyed by the study's UID, not its
# numeric ID — grab it from a study detail response first)
STUDY_UID=$(curl -s http://localhost:5001/api/v1/studies/1 | jq -r .uid)
curl "http://localhost:5001/api/v1/studies/$STUDY_UID/subject-statuses/odata"
```

If an auth mode other than `None` is active, include the same credential
you'd use for Patient/Study endpoints — Subject routes enforce the identical
active auth mode.

---

## 5. Enroll and update a subject via the write API

Status is one of CC's nine defined values — Active category: `Prescreened`,
`Screened`, `Randomized`, `Run-in`; Inactive category: `Screen Failed`,
`Non Qualified`, `Dropped`, `Run-in Failed`, `Complete`. "Active" is not
itself a literal status — it's shorthand for "any Active-category status."

```bash
# Enroll patient 5 in study 1
curl -X POST http://localhost:5001/api/v1/subjects \
  -H "Content-Type: application/json" \
  -d '{"patientId": 5, "studyId": 1, "status": "Prescreened", "enrollmentDate": "2026-07-20T00:00:00Z"}'

# Advance to an Active-category status (appends a SubjectStatus entry)
curl -X PUT http://localhost:5001/api/v1/subjects/1 \
  -H "Content-Type: application/json" \
  -d '{"patientId": 5, "studyId": 1, "status": "Randomized", "enrollmentDate": "2026-07-20T00:00:00Z"}'

# A second concurrently Active-category enrollment for the same pair is rejected (400)
curl -X POST http://localhost:5001/api/v1/subjects \
  -H "Content-Type: application/json" \
  -d '{"patientId": 5, "studyId": 1, "status": "Screened", "enrollmentDate": "2026-07-21T00:00:00Z"}'
```

---

## 6. Look up a generated subject by ID or by patient/study

```bash
curl "http://localhost:5001/api/v1/test-data/subjects/lookup?id=1"
curl "http://localhost:5001/api/v1/test-data/subjects/lookup?patientId=5&studyId=1"
```

Or from the admin UI: **Test Data Management → Data Manipulation**.

---

## 7. Reset Subject data

```bash
curl -X POST http://localhost:5001/api/v1/test-data/subjects/reset
```

Clears all Subject data (including status history) without touching Patient
or Study data. Conversely, resetting Patient or Study data alone also clears
any Subject data referencing what was removed — no separate Subject reset is
needed in that case (FR-012):

```bash
curl -X POST http://localhost:5001/api/v1/test-data/patients/reset
# Subjects referencing the removed patients are now gone too
curl http://localhost:5001/api/v1/test-data/subjects/stats
```
