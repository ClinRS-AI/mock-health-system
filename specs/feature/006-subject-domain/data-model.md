# Data Model: Subject Domain

Two new entities, both new EF Core `DbSet`s in `AppDbContext`. Field-shape
rationale and CC-fidelity caveats are in [research.md](research.md) (Decision 1).

## Subject

The enrollment-episode record linking one Patient to one Study.

| Field | Type | Notes |
|---|---|---|
| `Id` | `int` (PK, identity) | |
| `Uid` | `Guid` | Generated on create; unique index (`HasIndex(x => x.Uid).IsUnique()`), matching every other `Uid`-bearing entity in this codebase (`Study`, `StudyArm`, `StudyVisit`, `StudyDocument`, `ProtocolVersion`, `Sponsor`) |
| `PatientId` | `int` (FK → `Patients.Id`) | Required |
| `StudyId` | `int` (FK → `Studies.Id`) | Required |
| `StudyArmId` | `int?` (FK → `StudyArms.Id`) | Optional; MUST belong to `StudyId`'s study (FR-006) |
| `Status` | `string` | Required. One of CC's nine defined values (`SubjectStatusCatalog.AllStatuses`, research.md Decision 11): Active category — "Prescreened", "Screened", "Randomized", "Run-in"; Inactive category — "Screen Failed", "Non Qualified", "Dropped", "Run-in Failed", "Complete" |
| `SubjectIdentifier` | `string?` | Optional. CC's screening/subject number — not every subject has one assigned (e.g. before screening completes) |
| `EnrollmentDate` | `DateTime` | Required (FR-002) |
| `ScreeningDate` | `DateTime?` | Optional |
| `WithdrawalDate` | `DateTime?` | Optional |
| `WithdrawalReason` | `string?` | Optional |
| `CreatedOn` | `DateTime` | Set on create |
| `LastUpdatedOn` | `DateTime` | Set on create and every update |

**Navigation**: `Patient`, `Study`, `StudyArm?`, `StatusHistory: ICollection<SubjectStatus>`.

**Validation rules** (enforced in `SubjectsController`, not at the DB layer,
matching `StudiesController`'s established pattern):
- `PatientId` MUST reference an existing `Patient` (FR-006).
- `StudyId` MUST reference an existing `Study` (FR-006).
- `StudyArmId`, if provided, MUST reference a `StudyArm` whose `StudyId`
  equals this subject's `StudyId` (FR-006, edge case).
- `Status` MUST be one of the nine CC-defined values (FR-002, FR-006) —
  checked against `SubjectStatusCatalog.AllStatuses`.
- At most one `Subject` per `(PatientId, StudyId)` pair may have a `Status`
  in the Active category ("Prescreened", "Screened", "Randomized",
  "Run-in") at a time (FR-003) — checked via existence query excluding the
  current record's `Id` on update.

**Delete/cascade behavior** (`OnModelCreating`):
- `Patient` → `DeleteBehavior.Cascade`
- `Study` → `DeleteBehavior.Cascade`
- `StudyArm` → `DeleteBehavior.SetNull`

## SubjectStatus

Append-only status-change history for a Subject. Structural mirror of
`StudyDocumentStatusHistory`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `int` (PK, identity) | |
| `SubjectId` | `int` (FK → `Subjects.Id`) | Required |
| `StatusName` | `string` | The status value at this point in time |
| `ChangedOn` | `DateTime` | When the status took effect (UTC) |
| `ChangedByStaffId` | `int?` (FK → `Staff.Id`) | Optional |
| `Comment` | `string?` | Optional |

**Navigation**: `Subject`, `ChangedByStaff?`.

**Write rules**:
- One `SubjectStatus` row is created whenever a `Subject` is created (its
  initial status) and whenever a `Subject`'s `Status` changes on update
  (FR-004, User Story 2 scenario 8) — never on writes that don't change
  `Status`.
- Entries are immutable once written; no update/delete endpoint is exposed
  for `SubjectStatus` directly. They are only ever removed as a side effect
  of their owning `Subject` being removed (FR-011, FR-012, edge case).

**Delete/cascade behavior**:
- `Subject` → `DeleteBehavior.Cascade`
- `ChangedByStaff` → `DeleteBehavior.SetNull`

## Relationships summary

```
Patient (existing) ──< Subject >── Study (existing)
                          │              │
                          │              └──< StudyArm (existing, optional FK)
                          │
                          └──< SubjectStatus >── Staff (existing, optional FK)
```

## Query patterns this model must support

- List Subjects filtered by `patientId` and/or `studyId`, paginated
  (FR-001, `SubjectsController.GetSubjects`).
- Retrieve all `SubjectStatus` rows for subjects belonging to a given
  `Study` (by `Uid`), matching CC's `/studies/{studyUid}/subject-statuses/odata`
  shape (FR-005, `SubjectStatusesController.GetSubjectStatusesOData`) — a
  join `SubjectStatuses` → `Subjects` → filter `Subjects.StudyId ==
  study.Id`.
- Count total Subjects and, per Study, the count of *distinct* `PatientId`
  values among that study's Subjects (FR-009 — "patients by study" counts
  distinct patients, not subject rows, since one patient can have multiple
  enrollment episodes in the same study).
