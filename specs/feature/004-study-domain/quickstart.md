# Quickstart: Study Domain

Assumes the mock system is already running per the top-level project quickstart
(`specs/feature/001-cc-api-mock/quickstart.md`) — database initialized, backend on
`http://localhost:5001`, frontend on `http://localhost:5176`. This guide covers
just the Study-domain additions.

---

## 1. Apply the new migration

```bash
backend/scripts/run-ef.sh database update
```

This creates the 24 new Study-domain tables (see `data-model.md`).

---

## 2. Generate synthetic studies

Via the admin API (no admin key required in local dev):

```bash
curl -X POST http://localhost:5001/api/v1/test-data/studies/generate \
  -H "Content-Type: application/json" \
  -d '{"totalCount": 10, "seed": 42}'
```

This auto-seeds prerequisite lookups (sponsor/division/team, categories, statuses,
groups) if none exist, then creates 10 studies with populated arms, visits,
milestones, documents, and notes. (Study-type associations aren't part of
generation in this phase — `StudyType`/`StudyStatusType` rows created here are
usable via `StudyLookupController`, but the faker doesn't link generated studies
to types.)

Or from the admin UI: open `http://localhost:5176`, go to **Test Data → Studies**,
enter a count, and click **Generate**.

---

## 3. Call the CC Study API

```bash
# List studies
curl http://localhost:5001/api/v1/studies

# Fetch one study's detail
curl http://localhost:5001/api/v1/studies/1

# Fetch its arms, visits, milestones
curl http://localhost:5001/api/v1/studies/1/arms
curl http://localhost:5001/api/v1/studies/1/visits/odata
curl http://localhost:5001/api/v1/studies/1/milestones
```

If an auth mode other than `None` is active, include the same credential you'd use
for Patient endpoints (`CCAPIKey`, `Authorization: Bearer`, etc.) — Study routes
enforce the identical active auth mode.

---

## 4. Look up a generated study by fragment

```bash
curl "http://localhost:5001/api/v1/test-data/studies/lookup?protocolNumber=PROTO-2026"
```

---

## 5. Reset Study data

```bash
curl -X POST http://localhost:5001/api/v1/test-data/studies/reset
```

Clears all Study-domain rows without touching Patient data (spec SC-004). Pass
`?includeLookups=true` to also clear sponsors and Study lookup tables for a fully
clean slate.
