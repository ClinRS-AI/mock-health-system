# Data Model: Clinical Conductor API Mock System

**Phase 1 — As-Built Entity Model**

All entities are managed by EF Core via `AppDbContext` and persisted to PostgreSQL
in production. The database is seeded only with reference data; patient records are
always synthetic (Bogus-generated).

---

## Core Domain: Patient Aggregate

**Patient** is the root aggregate. All clinical records are owned by a patient.

```
Patient
├── Id: int (PK, auto-increment)
├── Identity
│   ├── Uid: Guid (external identifier, unique)
│   ├── Mrn: string? (medical record number)
│   ├── ImportId / ImportSourceId / ImportPatientId: string? (external import refs)
│   ├── DisplayName: string
│   ├── FirstName / MiddleName / LastName: string
│   ├── PhoneticName / PreferredName: string?
│   └── PrimarySiteId: int (FK → Site)
├── Demographics
│   ├── DateOfBirth: DateTime?
│   ├── DateOfDeath: DateTime?
│   ├── GenderCode: string?
│   ├── Race / Ethnicity / NativeLanguage: string?
│   └── MaritalStatus: string?
├── Contact
│   ├── Address1 / Address2 / Address3: string?
│   ├── City / State / Zip / Country: string?
│   ├── PrimaryEmailAddress / SecondaryEmailAddress: string?
│   ├── PrimaryDoNotEmail / SecondaryDoNotEmail: bool
│   ├── DoNotMail: bool
│   ├── Fax: string?
│   └── PhoneTypeToText: string?
├── Clinical
│   ├── Status: string (Active/Inactive/etc.)
│   ├── StatusReason: string?
│   ├── WeightValue / WeightUnit: decimal? / string?
│   └── HeightValue / HeightUnit: decimal? / string?
├── Insurance & Care
│   ├── PrimaryInsuranceJson / SecondaryInsuranceJson: string? (JSON)
│   ├── GuardianJson: string? (JSON)
│   ├── ManagedMedicare: bool
│   ├── CaregiverId: int?
│   └── Caregiver: Patient? (self-reference)
├── Other
│   ├── Ssn: string?
│   ├── CustomFieldsJson: string? (JSON)
│   └── RecruitmentTextOptIn: bool
└── Collections (navigation properties)
    ├── Phones: ICollection<PatientPhone>
    ├── Allergies: ICollection<PatientAllergy>
    ├── Conditions: ICollection<PatientCondition>
    ├── Medications: ICollection<PatientMedication>
    ├── Immunizations: ICollection<PatientImmunization>
    ├── Procedures: ICollection<PatientProcedure>
    ├── Providers: ICollection<PatientProvider>
    ├── MedicalDevices: ICollection<PatientMedicalDevice>
    ├── FamilyHistory: ICollection<PatientFamilyHistory>
    └── SocialHistoryEntries: ICollection<PatientSocialHistoryEntry>
```

### Patient Sub-Resources

```
PatientPhone
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── Slot: int (1–4, identifies which phone slot)
├── Number: string
└── RawNumber / OutOfService: string? / bool

PatientAllergy
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── AllergyId: int (FK → Allergy)
├── Reaction / Comment: string?
└── StartDate / EndDate: DateTime?

PatientCondition
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── ConditionId: int (FK → Condition)
├── StartDate / EndDate: DateTime?
├── AgeAtOnset: int?
└── Comment: string?

PatientMedication
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── MedicationId: int (FK → Medication)
├── RouteId: int? (FK → MedicationRoute)
├── Dosage: string?
├── StartDate / EndDate: DateTime?
├── Comment: string?
└── LinkedConditions: ICollection<PatientMedicationCondition>

PatientMedicationCondition  [junction table]
├── PatientMedicationId: int (FK → PatientMedication, part of composite PK)
└── PatientConditionId: int (FK → PatientCondition, part of composite PK)

PatientImmunization
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── ImmunizationId: int (FK → Immunization)
├── ImmunizationTypeId: int? (FK → ImmunizationType)
├── Name / Comment / Location: string?
└── Date: DateTime?

PatientProcedure
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── ProcedureId: int (FK → Procedure)
├── Name / Comment / CptCode: string?
├── ProcedureBy: string?
└── Date: DateTime?

PatientProvider
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── ProviderId: int (FK → Provider)
├── Comment: string?
└── StartDate / EndDate: DateTime?

PatientMedicalDevice
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── DeviceId: int (FK → Device)
└── Comment: string?

PatientFamilyHistory
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── ConditionId: int (FK → Condition)
├── FamilyMemberId: int? (FK → Relation)
├── RelationName / Comment: string?
├── AgeAtOnset: int?
└── StartDate / EndDate: DateTime?

PatientSocialHistoryEntry
├── Id: int (PK)
├── PatientId: int (FK → Patient)
├── SocialHistoryId: int (FK → SocialHistory)
└── Comment: string?
```

---

## Reference Data (Lookup Tables)

Reference data is pre-seeded and read-only via the API. Patients reference these
by foreign key.

```
Condition
├── Id: int (PK)
├── Name / Description: string
├── Icd10Code / Icd9Code: string?
├── GenderCode: string? (gender restriction)
├── ChildBearing: bool
└── ConditionTypeId: int (FK → ConditionType)

ConditionType
├── Id: int (PK)
└── Name / Description: string

Medication
├── Id: int (PK)
├── Name / Description: string
├── ChildBearing: bool
├── MedicationTypeId: int (FK → MedicationType)
├── GenderId: int? (FK → Gender)
├── DefaultRouteId: int? (FK → MedicationRoute)
└── DefaultScheduleId: int? (FK → MedicationSchedule)

MedicationType  |  MedicationRoute  |  MedicationSchedule
└── Id / Name / Description

Allergy
├── Id: int (PK)
├── Name / Description: string
└── AllergenTypeId: int (FK → AllergenType)

AllergenType
├── Id: int (PK)
├── AllergenTypeId: string (code)
├── Description: string
└── IsDefault: bool

Immunization
├── Id: int (PK)
└── Name: string

ImmunizationType
├── Id: int (PK)
└── Name: string

Procedure
├── Id: int (PK)
├── Name: string
└── CptCode: string?

Device  |  Relation  |  Gender
└── Id / Name (+ GenderCode on Gender)

Provider
├── Id: int (PK)
├── ProviderName / Title: string?
├── FirstName / MiddleName / LastName: string?
└── ProviderTypeId: int (FK → ProviderType)

ProviderType
├── Id: int (PK)
└── Name: string

SocialHistory
├── Id: int (PK)
├── Name: string
└── CategoryId: int? (FK → ConditionType — reused as category)

Site
├── Id: int (PK)
├── Uid: Guid
└── Name: string
```

---

## System & Configuration Entities

```
AuthSettings  [singleton — Id always = 1]
├── Id: int (PK)
├── Mode: string (None | Bearer | CCAPIKey | OAuth)
├── BearerToken: string?
├── CCApiKey: string?
├── OAuthClientId / OAuthClientSecret: string?
└── AccessTokenLifetimeMinutes / RefreshTokenLifetimeDays: int

AuthToken  [issued OAuth tokens]
├── Id: int (PK)
├── Token: string (the raw token value, hashed or plain)
├── TokenType: string (access | refresh)
├── ClientId / Subject: string?
├── ExpiresAt: DateTime
├── CreatedAt: DateTime
└── RevokedAt: DateTime?

AdminSession  [not a DB entity — short-lived HS256 JWT, not persisted]
└── Claims: sub, exp, iat (signed with ADMIN_SESSION_SIGNING_KEY)

ApiRequestLog
├── Id: int (PK)
├── CreatedAtUtc: DateTime
├── Method / Path / QueryString: string
├── StatusCode: int
├── DurationMs: long
├── Origin / Referer / UserAgent: string?
├── RemoteIp: string?
├── RequestBody / ResponseBody: string? (capped at 4 KB each)
└── CorrelationId: string?

ReportQueryDefinition  [SOAP report registry]
├── Id: int (PK)
├── PKey: string (named identifier used in SOAP requests)
├── SqlQuery: string
└── CreatedAtUtc / UpdatedAtUtc: DateTime
```

---

## Audit Entities

```
AuditLog
├── Id: int (PK)
├── StaffPKey: int? (FK → Staff)
├── PatientPKey: int? (FK → Patient)
├── StudyPKey: int?
├── CreatedTimeUtc: DateTime
├── CreatedByUser: string?
├── AuditEntryTypeId: int (FK → AuditEntryType)
├── Details: string? (JSON or free text)
└── SourceSystem: string?

AuditEntryType
├── Id: int (PK)
├── Code: string (e.g., PATIENT_CREATED, PATIENT_VIEWED, USER_LOGIN)
├── DisplayName: string
└── Description: string?

Staff  [used as audit actors]
├── Id: int (PK)
├── StaffUid: Guid
├── FirstName / LastName: string
└── IsActive: bool
```

---

## Entity Relationship Summary

```
Site ──────────────────────── Patient (many per site)
                                │
        ┌───────────────────────┼───────────────────────────┐
        │                       │                           │
   PatientPhone           PatientAllergy           PatientCondition
                               │                           │
                             Allergy             ←──── PatientMedication ──→ PatientMedicationCondition
                           AllergenType          │          MedicationRoute
                                              Medication
                                           MedicationType

Patient ─── PatientImmunization ─── Immunization / ImmunizationType
Patient ─── PatientProcedure ─── Procedure
Patient ─── PatientProvider ─── Provider / ProviderType
Patient ─── PatientMedicalDevice ─── Device
Patient ─── PatientFamilyHistory ─── Condition / Relation
Patient ─── PatientSocialHistoryEntry ─── SocialHistory / ConditionType (as category)
Patient ─── AuditLog ─── AuditEntryType / Staff
```

---

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| Patient | FirstName, LastName | Required; max length enforced |
| Patient | DateOfBirth | Must be in the past if provided |
| Patient | GenderCode | Must match a known Gender code if provided |
| PatientMedication | RouteId | Must reference a valid MedicationRoute if provided |
| AuthSettings | Mode | Must be one of: None, Bearer, CCAPIKey, OAuth |
| AuthToken | ExpiresAt | Must be in the future when created |
| GeneratePatientsRequest | totalCount | Must be > 0 and ≤ configured maximum |
| ReportQueryDefinition | SqlQuery | Must be a SELECT statement; no DDL/DML permitted |
