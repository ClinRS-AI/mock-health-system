# Research: Study Domain

**Phase 0 — Design Decisions**

Source of truth for CC's Study surface: the CC Public API OpenAPI (Swagger 2.0)
document at `https://sales.clinicalconductor.com/CCSWeb/api/openapi/V1.0`, fetched
2026-07-10. 209 total paths; 68 study-related; ~55 `Study*`-prefixed schema
definitions inspected directly from the raw spec (`StudyViewModel`,
`StudyEditModel`, `StudyPatchModel`, `VisitViewModel`, `ProtocolVersionViewModel`,
contact/leadership/target-date sub-models, etc.).

No `[NEEDS CLARIFICATION]` markers remain in the spec — scope, patient-enrollment
linkage, and data-faking behavior were resolved via `AskUserQuestion` before the
spec was drafted. This document instead records the design decisions made while
turning that scope into a concrete entity/endpoint model.

---

## Decision: Controller split by CC tag, not one `StudiesController`

**Decision**: Split the Study surface across ~8 controllers (`StudiesController`,
`StudyArmsController`, `StudyVisitsController`, `StudyMilestonesController`,
`StudyDocumentsController`, `StudyNotesController`, `StudyRolesController`,
`ProtocolVersionsController`, `StudyLookupController`), one per CC Swagger tag
grouping (or a close approximation).

**Rationale**: `PatientsController` handles its entire surface (demographics +
9 clinical sub-resource types) in a single ~430-line file, which is already dense.
Study has a comparable or greater number of sub-resource groups (8 structural
groups + 5 lookup groups vs. Patient's 9), so following the same single-controller
pattern would produce an unwieldy 900+ line file. Splitting by CC's own tag
boundary keeps each controller focused and matches how the Public CC API itself
documents the surface (developers cross-referencing CC docs will find a 1:1
naming match).

**Alternatives considered**:
- *Single `StudiesController`* (Patient's pattern) — rejected due to file size/
  cognitive load at this entity count.
- *One controller per individual endpoint group with no CC-tag mapping* (e.g.
  purely REST-resource-driven) — rejected; CC-tag alignment gives external
  developers a predictable mapping from CC docs to mock routes.

---

## Decision: `StudyContact` is a first-class entity, written through the Study endpoint (no dedicated route) — mirrors the `PatientPhone` precedent

**Decision**: CC's `StudyContactViewModel` references up to two entries each of
IRB, CRO, Lab, Monitor, and Vendor "preview" models (`IRBSitePreviewModel`,
`CroPreviewModel`, `LabPreviewModel`, `MonitorPreviewModel`, `VendorPreviewModel`)
— effectively five separate CRM-style directories a study can point into. Rather
than either flattening this into raw columns or building five new directory
entities with their own CRUD surface, this plan models `StudyContact` as its own
table (`Id`, `StudyId` FK, `ContactType` [Irb/Cro/Lab/Monitor/Vendor], `Slot`
[1–2], `Name`, `Reference`, `Comment`). It has no dedicated controller or route —
like `PatientPhone`, it's read as a nested array on the parent's view model and
written via the parent's create/update endpoints: `StudyMappingService` upserts
`StudyContact` rows by `(ContactType, Slot)` the same way
`PatientMappingService`/`PatientsController.SyncPhonesFromEdit`/
`UpsertPatientPhone` upsert `PatientPhone` rows by `Slot`.

**Rationale**: The Public CC API itself has no separate contacts endpoint —
contact detail is embedded in `StudyViewModel`/`StudyEditModel` — so inventing a
dedicated route would violate FR-004's CC-shape-parity requirement. But
flattening into raw columns makes the shape a dead end for reuse. A real table
with a normal FK is exactly as much relational structure as `PatientPhone`
already uses for an analogous "several typed slots per parent" shape, and — per
explicit product direction — `StudyContact` needs to be reusable by future parent
entities beyond Study without a schema rework.

**Alternatives considered**:
- *Flat columns on `Study`* (this plan's original approach) — rejected as not
  reusable and awkward for a 5-type × 2-slot matrix.
- *Full directory entities per contact type* (real IRB/CRO/lab/monitor/vendor
  registries) — rejected as scope creep beyond "structural sub-resources"; can be
  revisited if a future feature needs real registry management.
- *A fully generic polymorphic `Contact` table* addressable by any parent type via
  a discriminator — rejected as premature: nothing beyond Study needs it yet, and
  a discriminator-based polymorphic FK loses referential integrity (needs
  app-level enforcement) for a reuse case that doesn't exist yet. `StudyContact`
  can be trivially copied into a same-shaped `XContact` table for a future parent
  once that need is concrete.

---

## Decision: `StudyLookupController` uses admin authentication, not the active CC auth mode

**Decision**: `StudyLookupController` (Categories, Subcategories, Types, Statuses,
Groups) is gated by `IAdminRequestValidator.IsAdminRequest(HttpContext,
bypassAdminChecksInDevelopment: true)` — the same pattern as `TestDataController`'s
`patients/lookup` and friends — not `[Authorize]`/the active CC auth mode, even
though it sits at the CC-shaped `/system/...` route prefix alongside the
(unauthenticated, GET-only) `SystemController`.

**Rationale** (per explicit product direction): these lookup values are
Mock-Health-System admin configuration — they define what categories/statuses/
types exist for synthetic studies — rather than CC integration traffic a real
client would call. Keeping the URL shape CC-like (`/system/study-categories`, etc.)
preserves a predictable mapping to CC's docs, but the auth model follows the admin
side of this codebase, not the CC-integration side.

**Alternatives considered**:
- *Open/anonymous*, matching the existing GET-only `SystemController` exactly —
  rejected because these lookup endpoints are writable in this feature
  (categories/subcategories get POST/PUT/DELETE), and open unauthenticated writes
  to configuration referenced by every study is an unnecessary risk with no
  offsetting benefit.
- `[Authorize]`/CC auth mode like `StudiesController` — rejected per explicit
  product direction that this is admin, not CC, functionality.

---

## Decision: named response DTOs for all new `test-data/studies/*` actions

**Decision**: `GenerateStudiesAsync` and the stats/lookup/random actions on
`TestDataController` return explicitly named DTOs (`GenerateStudiesResponse`,
`StudyTestDataStatsDto`, etc.) — never anonymous objects.

**Rationale**: Constitution VI prohibits anonymous-object response payloads.
Existing precedent in `TestDataController` is mixed: `GenerateStaffResponse`/
`PatientTestDataStatsDto` are named (compliant); `GeneratePatientsAsync`/
`ResetPatientsAsync` return anonymous objects (pre-existing drift on the Patient
side, not this feature's to fix). New Study actions follow the compliant half of
the existing precedent rather than repeating the drift.

---

## Decision: no concurrency control on Study generation or updates

**Decision**: Concurrent calls to `POST /test-data/studies/generate` are not
synchronized against each other, and concurrent updates to the same study or
sub-resource use no optimistic concurrency token — last write wins.

**Rationale** (per explicit product direction): Study and sub-resource IDs are
database-assigned (auto-increment), so two overlapping `generate` calls each
produce their own valid, non-colliding batch of rows — the only externally
visible effect is that more studies exist afterward than either individual call
requested, not corrupted data. This matches how the rest of the system already
behaves — no existing generation endpoint (including `GeneratePatientsAsync`)
synchronizes concurrent calls either. Concurrent updates following last-write-wins
is standard EF Core `SaveChangesAsync` behavior with no `[Timestamp]`/rowversion
column configured anywhere in this codebase; adding one only for Study would be
inconsistent with every other entity.

**Alternatives considered**: A distributed or DB advisory lock around generation
— rejected as unnecessary complexity for a single-developer local dev tool with no
realistic concurrent-caller scenario. Optimistic concurrency tokens on Study/
sub-resources — rejected for the same reason, and for consistency with every
other entity in the codebase.

---

## Decision: Generic name/value custom fields; no field-designer

**Decision**: `StudyCustomFieldValue(StudyId, FieldName, FieldValue)` — a flat
list, matching the shape CC's `customFields` array returns on `Study`. No
`StudyCustomFieldDefinition` (type, validation, options) entity is introduced.

**Rationale**: Already recorded as an explicit assumption in spec.md. CC's
system-level custom-field *designer* is a separate admin capability from Study
records themselves; faithfully mirroring the value shape is sufficient for
integration testing.

---

## Decision: `StudyDocument` is metadata-only

**Decision**: `StudyDocument` stores type/status/version/dates but no binary
content — `storedDocumentId` in CC's model is dropped; there is no file upload/
download endpoint in this phase.

**Rationale**: The mock system has no existing document storage subsystem for any
domain. Adding one is a cross-cutting capability outside a single-domain feature's
scope. Document *records* (what a developer's integration parses) are still fully
represented; only the binary itself is out of scope.

---

## Decision: New minimal `Sponsor` / `SponsorDivision` / `SponsorTeam` entities; reuse existing `Site` and `Staff`

**Decision**: Add three new lookup entities (`Sponsor`, `SponsorDivision`,
`SponsorTeam` — each just `Id`, `Uid`, `Name`, plus the obvious parent FK) because
`StudyEditModel.sponsorTeamId` is a required reference with no existing analog in
this codebase. Reuse the existing `Site` entity for `managingSiteId` and the
existing `Staff` entity for every staff reference (milestone assignee, note
author, role staff, leadership, document/status change actor).

**Rationale**: `Site` and `Staff` already exist as minimal lookup entities
(`backend/MockHealthSystem.Infrastructure/Data/Entities/Site.cs`,
`Staff.cs`) and are structurally identical to what CC's `SitePreviewModel` /
`StaffPreviewModel` need. No reason to duplicate them. Sponsor/Division/Team have
no existing analog and are required for a Study to exist at all, so they're the
one genuinely new "foundational" addition — kept minimal (name-only) since a full
sponsor-management domain is explicitly out of scope (spec Assumptions).

**Note**: `StaffPreviewModel` in CC includes `login` and `displayName` that the
existing `Staff` entity doesn't store. `StudyMappingService` synthesizes these
(`login` from `firstname.lastname`, `displayName` from `"{Last}, {First}"`) rather
than extending the shared `Staff` entity — consistent with how
`TestDataController` already derives `createdByUser` the same way for audit logs.

---

## Decision: `StudyGroup` and `StudyType` kept as simple lookups; `Study↔StudyType` is many-to-many, `Study↔StudyGroup` is a single string reference

**Decision**: `StudyType` gets a join table (`StudyStudyType`) because CC exposes
dedicated `POST /studies/{id}/types/add` and `DELETE /studies/{id}/types/{id}`
endpoints implying a many-to-many relationship. `StudyGroup` is modeled as a
lookup table but `Study.StudyGroup` remains a single string column (matching the
flat `studyGroup: string` field CC returns on `StudyViewModel`, distinct from the
plural `studyGroups: array` field), since CC exposes no group assignment endpoints
for the mock to mirror.

**Rationale**: Endpoint shape drives data-model shape here — where CC gives us a
dedicated association endpoint (types), the mock models a real relationship;
where it only returns a denormalized display field (group), a single column is
sufficient and avoids inventing unexercised write endpoints.

---

## Decision: Rate limiting, request logging, and auth apply with zero new code

**Decision**: No new middleware or auth wiring for this feature.

**Rationale**: `RateLimitingMiddleware` (feature 003, fully implemented),
`RequestLoggingMiddleware`, `ExceptionHandlingMiddleware`, and `MockAuthHandler`
are all pipeline-global. New controllers automatically inherit rate limiting,
request logging, and the active auth mode purely by being registered — matching
spec FR-009 and FR-010 exactly. `TestDataController`'s new `studies/*` actions
reuse the existing `IAdminRequestValidator` and `[RequiresAdminAuth]` /
`bypassAdminChecksInDevelopment: true` pattern unchanged.

---

## Decision: `StudyFakerService` mirrors `PatientFakerService`'s shape

**Decision**: A new `StudyFakerService` follows the same constructor pattern as
`PatientFakerService(int? seed, List<int> siteIds)` — accepting a seed and the IDs
of prerequisite lookups (sponsor teams, sites, staff, categories/statuses/types)
resolved by the caller (`TestDataController`) before construction — and exposes a
`CreateStudies(int count)`-style entry point using Bogus for realistic sponsor
names, protocol number formats (e.g., `PROTO-XXXX-###`), NCT numbers
(`NCTXXXXXXXX`), phases (`Phase I`–`Phase IV`), and status/category values drawn
from the lookup tables generated in the same batch.

**Rationale**: Consistency with the established pattern (constitution I — Bogus
required; existing `PatientFakerService` is the only precedent in the codebase)
minimizes review friction and keeps generation deterministic when a seed is
supplied, matching `GeneratePatientsRequest.Seed` semantics.

**Prerequisite-generation ordering** (new — Patient generation has no analogous
dependency chain): `StudyFakerService`/`TestDataController.GenerateStudiesAsync`
must ensure lookup rows (Sponsor→Division→Team, Site, StudyCategory/Subcategory,
StudyStatusType, StudyType, StudyGroup) exist before generating Study rows, since
`SponsorTeamId` is a required FK. If none exist, the endpoint auto-seeds a small
default set (mirrors how `GenerateRecentAuditEventsAsync` requires staff/audit
types to pre-exist, but auto-creates rather than 400s, since these are the mock's
own reference data rather than caller-supplied prerequisites).
