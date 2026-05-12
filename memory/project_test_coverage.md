---
name: Test coverage state (May 2026)
description: Current test suite counts and which areas have coverage after the May 2026 expansion and admin session work
type: project
---

As of **2026-05-11**, the test suite covers:

**Backend (.NET 10 / xUnit): 327 tests — 0 failures**
- Unit: AuthSettingsService, ExceptionHandlingMiddleware, ModelValidationActionFilter, PatientFakerService, PatientMappingService, ReportExecutionService, ApiErrorResponse, **AdminSessionJwtService** (JWT mint/validate with fake clock)
- Integration: HealthEndpoint, MonitoringEndpoints, AuthenticationModes, AuthController, AuthSettingsEndpoints, PatientEndpoints, SoapReportEndpoints, TestDataManagementEndpointTests (generate staff, audit events, **GetSoapReportPkeys** admin + sorted pkeys), SystemController OData (conditions/medications/allergies), TestDataExtended (AddTestPatient, LookupPatient, GetRandomPatient, UpdateTestPatient, GetPatientStats, GeneratePatients validation), PatientSubResourceEndpoints (devices/allergies/providers/conditions/procedures/medications/immunizations/family-history/social-history + search filters), MonitoringExtended (filter/paging/stats), **AdminSessionEndpointTests** (mint success/failure, admin routes with `X-Admin-Session`, forged/expired JWT → 403)

**Frontend (Vitest / RTL): 53 tests**
- `api.ts`: exported API helpers including **`exchangeAdminSession`**, **`getSoapReportPkeys`**, and `X-Admin-Session` interceptor behavior (via `adminSessionStore` in tests)
- Components: App, AuthSettingsPage, MonitoringPage, TestDataPage (wrapped with `AdminSessionProvider` in page tests)

**Key test infrastructure:**
- `IsolatedWebApplicationFactory`: in-memory EF, one DB per class — use for new endpoint tests
- `MockHealthSystemWebApplicationFactory`: SQLite, one file per factory — use when raw SQL is needed
- Parallelization disabled globally (`AssemblyInfo.cs`)
- Tests that seed shared DB must not assert "empty" — use >= comparisons or seed-then-assert pattern
- Vitest `setup.ts` clears `adminSessionStore` / session between tests so API header tests stay isolated

**Critical EF in-memory gotcha:**
- Non-nullable navigation properties (`= null!`) cause INNER JOIN behavior with `Include()` — rows are filtered out when the FK entity doesn't exist in the in-memory DB
- Fix: always seed the FK entity before testing sub-resource POSTs that use such navigations
- Nullable navigations (`?` suffix) are safe — behave as LEFT JOIN
- Affected: PatientMedicalDevice.Device, PatientMedication.Medication (and any other non-nullable nav props)

**Still not covered:**
- RequestLoggingMiddleware unit tests (covered functionally by MonitoringEndpointTests)
- MockAuthHandler unit tests (covered by AuthenticationModeTests integration)
- ResetPatients endpoint (uses TRUNCATE; not compatible with SQLite/in-memory test providers)
- OData paging integration tests rely on seeded data from prior tests in the same class

**Why:** Expansion done 2026-05-02 to improve regression detection; admin session flow added 2026-05-11.
