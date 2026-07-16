# Data Model: Study Domain

**Phase 1 — Entity Model**

All entities are managed by EF Core via `AppDbContext` and persisted to PostgreSQL.
Study data is always synthetic (Bogus-generated via `StudyFakerService`), never
seeded with real sponsor/protocol information, per constitution I.

---

## Core Domain: Study Aggregate

**Study** is the root aggregate for this feature, parallel in role to `Patient`.

```
Study
├── Id: int (PK, auto-increment)
├── Uid: Guid (external identifier, unique)
├── Identity & Classification
│   ├── Name: string
│   ├── Title: string?
│   ├── Identifier: string?
│   ├── ProtocolNumber: string?
│   ├── IndIdeNumber: string?
│   ├── NctNumber: string?
│   ├── Phase: string?
│   ├── Status: string            (references StudyStatusType.Name)
│   ├── Category: string?         (references StudyCategory.Name)
│   ├── Subcategory: string?      (references StudySubcategory.Name)
│   ├── StudyGroup: string?       (references StudyGroup.Name; denormalized display field)
│   ├── Tag1 / Tag2 / Tag3 / Tag4: string?
│   ├── Comment / Description: string?
│   ├── LaunchYear: int?
│   └── StudyCurrency: string?
├── References
│   ├── SponsorTeamId: int (FK → SponsorTeam, required)
│   └── ManagingSiteId: int? (FK → Site)
├── Finance (flattened StudyFinanceModel)
│   ├── FinanceType: string?
│   └── AccountingCode1 / 2 / 3 / 4: string?
├── Opportunity (flattened StudyOpportunityModel)
│   ├── OpportunityLevel: string?
│   ├── OpportunityProbability: double?
│   ├── OpportunityExpectedDate: DateTime?
│   ├── OpportunityExpectedNumberOfSites: int?
│   └── OpportunityComment: string?
├── Notes
│   ├── EnrollmentNote / BudgetNote / RegulatoryNote / ContractNote: string?
├── Lead Source (flattened StudyLeadSourceViewModel)
│   ├── LeadSourceStaffId: int? (FK → Staff)
│   ├── LeadSource: string?
│   ├── LeadDate: DateTime?
│   └── LeadComment: string?
├── System
│   ├── CreatedOn: DateTime (UTC, set on insert)
│   └── LastUpdatedOn: DateTime (UTC, set on insert and every update)
└── Collections (navigation properties)
    ├── Arms: ICollection<StudyArm>
    ├── Visits: ICollection<StudyVisit>
    ├── Milestones: ICollection<StudyMilestone>
    ├── Documents: ICollection<StudyDocument>
    ├── Notes: ICollection<StudyNote>
    ├── Roles: ICollection<StudyRole>
    ├── ProtocolVersions: ICollection<ProtocolVersion>
    ├── TargetDates: ICollection<StudyTargetDate>
    ├── Leadership: ICollection<StudyLeadership>
    ├── CustomFieldValues: ICollection<StudyCustomFieldValue>
    ├── Contacts: ICollection<StudyContact>
    └── StudyTypes: ICollection<StudyStudyType>  (many-to-many join)
```

### Structural Sub-Resources

```
StudyArm
├── Id: int (PK)
├── Uid: Guid (unique)
├── StudyId: int (FK → Study)
├── ProtocolVersionId: int? (FK → ProtocolVersion)
├── Name: string
├── Status: string?
├── PatientGoal / PatientLimit: int?
├── Comment: string?
└── ImportId / ImportType: string?

StudyVisit
├── Id: int (PK)
├── Uid: Guid (unique)
├── StudyId: int (FK → Study)
├── ProtocolVersionId: int? (FK → ProtocolVersion)
├── Name: string
├── Type / Reference / OptionalProcedure / Description: string?
├── StandardMinutes: int?
├── Budget / Cost / PatientStipend / CaregiverStipend: decimal?
├── IsBudgetAutoRecomputed / IsCostAutoRecomputed: bool
├── IsActive: bool
├── AutoRepeat / RepeatOnDemand: bool
└── ImportId / ImportType: string?

StudyVisitArm  (join — mirrors /arms/{armId}/visits and /visits/{visitId}/arms)
├── VisitId: int (FK → StudyVisit)
└── ArmId: int (FK → StudyArm)
    composite PK (VisitId, ArmId)

StudyMilestone
├── Id: int (PK)
├── StudyId: int (FK → Study)
├── Name / Category / Importance / Status: string
├── Comment: string?
├── AssignedToStaffId: int? (FK → Staff)
├── AssignedOn / ProjectedDate / CompletedOn: DateTime?
├── HasAutoExpenditure: bool
└── Scheduling (flattened MilestoneScheduleViewModel)
    ├── SchedulingMode: string?
    ├── DueDate: DateTime?
    ├── Offset: int? / OffsetUnits: string?
    └── WindowMin / WindowMax: int? / WindowUnits: string?

StudyDocument
├── Id: int (PK)
├── Uid: Guid (unique)
├── StudyId: int (FK → Study)
├── TypeName / TypeCategory: string?   (flattened DocumentTypePreviewModel)
├── StatusName: string                 (current status; history in StudyDocumentStatusHistory)
├── Description / Version / Source: string?
└── EffectiveDate / ExpirationDate: DateTime?

StudyDocumentStatusHistory
├── Id: int (PK)
├── StudyDocumentId: int (FK → StudyDocument)
├── StatusName: string
├── ChangedOn: DateTime (UTC)
├── ChangedByStaffId: int? (FK → Staff)
└── Comment: string?

StudyContact  (own table, no dedicated route — read/written via the Study record's
                own endpoints, mirroring how PatientPhone works for Patient; see
                research.md)
├── Id: int (PK)
├── StudyId: int (FK → Study)
├── ContactType: string  (Irb | Cro | Lab | Monitor | Vendor)
├── Slot: int  (1–2, matches CC's two-entries-per-type shape)
├── Name: string?
├── Reference: string?
└── Comment: string?
    unique index (StudyId, ContactType, Slot)

StudyNote
├── Id: int (PK)
├── StudyId: int (FK → Study)
├── StaffId: int? (FK → Staff, author)
├── LastUpdatedStaffId: int? (FK → Staff)
├── NoteDate: DateTime
├── Note: string
├── Locked: bool
└── Shared: bool

StudyRole
├── Id: int (PK)
├── StudyId: int (FK → Study)
├── Name: string
├── IsCoordinator: bool
├── AllowRoleSharing: bool
└── RestrictReassignment: bool

StudyRoleStaff  (join — staff assigned to a study role)
├── StudyRoleId: int (FK → StudyRole)
├── StaffId: int (FK → Staff)
└── Priority: string?
    composite PK (StudyRoleId, StaffId)

ProtocolVersion
├── Id: int (PK)
├── Uid: Guid (unique)
├── StudyId: int (FK → Study)
├── Name: string
├── VersionDate: DateTime?
├── TreatmentStatus / Status: string?
├── ProtocolNumber: string?
├── Comment: string?
├── IrbApprovalDate: DateTime?
├── IsPatientReconsentRequired: bool
└── ImportId / ImportType: string?

StudyTargetDate  (embedded array on Study create/update, not separately addressable)
├── Id: int (PK)
├── StudyId: int (FK → Study)
├── Name: string
├── Tooltip: string?
├── Required: bool
└── TargetDate: DateTime?

StudyLeadership  (embedded array on Study create/update)
├── Id: int (PK)
├── StudyId: int (FK → Study)
├── Name: string          (role title, e.g. "Principal Investigator")
├── Required: bool
└── StaffId: int? (FK → Staff)

StudyCustomFieldValue  (embedded array on Study create/update — see research.md)
├── Id: int (PK)
├── StudyId: int (FK → Study)
├── FieldName: string
└── FieldValue: string?
```

### Reference / Lookup Data

```
StudyCategory
├── Id: int (PK)
├── Name: string (unique)
└── Description: string?

StudySubcategory
├── Id: int (PK)
├── StudyCategoryId: int? (FK → StudyCategory)
├── Name: string
└── Description: string?

StudyType
├── Id: int (PK)
├── Name: string (unique)
├── Description: string?
├── ForeColor / BackColor: string?

StudyStudyType  (many-to-many join — mirrors /studies/{id}/types/add and /types/{id})
├── StudyId: int (FK → Study)
└── StudyTypeId: int (FK → StudyType)
    composite PK (StudyId, StudyTypeId)

StudyStatusType
├── Id: int (PK)
├── Name: string (unique)
├── Description: string?
├── BackColor: string?
├── IsActive: bool
├── IsEnrollmentPermitted: bool
└── StudyPhase: string?

StudyGroup
├── Id: int (PK)
└── Name: string (unique)
```

### New Minimal Reference Entities (not previously present in this codebase)

```
Sponsor
├── Id: int (PK)
├── Uid: Guid (unique)
└── Name: string

SponsorDivision
├── Id: int (PK)
├── SponsorId: int (FK → Sponsor)
└── Name: string

SponsorTeam
├── Id: int (PK)
├── SponsorDivisionId: int (FK → SponsorDivision)
└── Name: string
```

### Reused Existing Entities (no changes)

- **Site** (`backend/MockHealthSystem.Infrastructure/Data/Entities/Site.cs`) — used
  for `Study.ManagingSiteId`.
- **Staff** (`.../Staff.cs`) — used for every staff reference (milestone assignee,
  note author, role staff, leadership, lead source, document status change actor).
  `login`/`displayName` are synthesized in `StudyMappingService`, not stored.

---

## Relationships Summary

- `Study` 1—* `StudyArm`, `StudyVisit`, `StudyMilestone`, `StudyDocument`,
  `StudyNote`, `StudyRole`, `ProtocolVersion`, `StudyTargetDate`,
  `StudyLeadership`, `StudyCustomFieldValue`, `StudyContact` — cascade delete on
  parent `Study` delete (spec Edge Cases: deleting a study cascades to its
  sub-resources; there is no "study in use" concept in this mock, unlike Patient
  which can conflict on delete due to FK references from other domains).
- `StudyContact` has no dedicated controller — it's synced by
  `StudyMappingService` from the `Contacts` array on `StudyEditModel`/
  `StudyPatchModel`, upserted by `(ContactType, Slot)`, exactly like
  `PatientPhone` is synced from `Phone1`–`Phone4` (see research.md).
- `StudyVisit` *—* `StudyArm` via `StudyVisitArm`, scoped to the same `StudyId` on
  both sides — enforced at the application layer (FR-005: reject associations
  across studies), not just the FK.
- `StudyDocument` 1—* `StudyDocumentStatusHistory` — cascade delete.
- `StudyRole` *—* `Staff` via `StudyRoleStaff`.
- `Study` *—* `StudyType` via `StudyStudyType`.
- `Sponsor` 1—* `SponsorDivision` 1—* `SponsorTeam` 1—* `Study` (required).
- `StudySubcategory` *optionally* scoped to a `StudyCategory` (mirrors CC's
  separate-but-related category/subcategory lookup endpoints).

## Validation Rules (from Functional Requirements)

- `Study.SponsorTeamId` is required on create (FR-001; CC's `StudyEditModel`
  requires `sponsorTeamId`).
- Writes to any sub-resource referencing a `StudyId` that doesn't exist return
  404/400 rather than creating an orphaned row (FR-005).
- `StudyVisitArm` writes are rejected (400) when the visit and arm belong to
  different studies (FR-005, Edge Cases).
- `StudyRoleStaff` writes are rejected (400) when the role's `StudyId` doesn't
  match the request's study context.
- Generation batch size for `test-data/studies/generate` is bounded by a
  configured maximum, mirroring `GeneratePatientsRequest` (FR-006).
