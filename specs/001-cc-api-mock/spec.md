# Feature Specification: Clinical Conductor API Mock System

**Feature Branch**: `feature/spec-kit-setup`

**Created**: 2026-05-19

**Status**: Draft — Ready for planning

## Overview

The Clinical Conductor (CC) API Mock System is a self-hosted development tool that
lets software developers test and build CC integrations without access to a
production CC environment. It exposes the same API surface as the Public CC API,
supports all CC authentication modes, generates synthetic clinical data on demand,
and provides a web-based admin interface for configuration, monitoring, and
data management.

The system is designed to be runnable on a developer's local machine or deployed
to a shared server (including on the public internet), with optional admin-key
protection when team access needs to be controlled.

**Why this exists**: Gaining access to a CC production environment for integration
development is slow and risky. This mock eliminates that dependency, allowing
developers to move fast against a realistic, controllable CC-equivalent surface.

## User Scenarios & Testing

### User Story 1 — Configure Auth Mode to Match Target Environment (Priority: P1)

A developer building a CC integration needs their test client to authenticate the
same way CC requires in production. The CC Public API primarily uses a CCAPIKey
header for authentication. They open the admin interface, select the appropriate
auth mode (CCAPIKey, No Auth, Bearer Token, or OAuth), supply the required
credentials, and save. Their integration client now receives the same
authentication challenges and success/failure responses they will see against the
real CC system.

**Why this priority**: Auth configuration is the prerequisite to all other
integration testing. Without it, no API call can succeed under the correct
conditions. CCAPIKey is the primary CC authentication mechanism and must work
correctly before any other integration work can proceed.

**Independent Test**: A developer can configure CCAPIKey mode with a known key,
make an authenticated request to any patient endpoint using the CCAPIKey header,
and receive a valid response — with no other features required.

**Acceptance Scenarios**:

1. **Given** CCAPIKey mode is configured with a known key value, **When** a client
   calls any CC API endpoint with the correct `CCAPIKey` header, **Then** the
   request succeeds with a valid response; calls missing the header or supplying
   a wrong key receive a 401.
2. **Given** the mock is running with No Auth configured, **When** a client calls
   any CC API endpoint without credentials, **Then** the request succeeds with a
   valid response.
3. **Given** Bearer Token mode is configured with a known token, **When** a client
   calls with the correct `Authorization: Bearer` header, **Then** the request
   succeeds; calls without it receive a 401.
4. **Given** OAuth mode is configured, **When** a client POSTs valid client
   credentials to the token endpoint, **Then** it receives an access token and
   refresh token it can use on subsequent calls.
5. **Given** an admin key is set on the server, **When** an admin attempts to change
   auth settings without a valid session token, **Then** the request is rejected
   with a 403.

---

### User Story 2 — Call CC API Endpoints to Test Integration Logic (Priority: P1)

A developer wants to verify that their CC integration code correctly parses
responses, handles pagination, and maps clinical data to their own domain model.
They point their client at the mock and exercise the same endpoint paths as the
Public CC API. The mock returns structurally valid, realistic-looking synthetic
data so the developer's parsing and mapping logic is fully exercised.

**Why this priority**: Endpoint simulation is the core value proposition of the
system. Without realistic endpoint responses, developers cannot trust their
integration code.

**Independent Test**: A developer can retrieve a paginated list of patients, fetch
a single patient by ID, and retrieve associated conditions — all without accessing
any real CC system.

**Acceptance Scenarios**:

1. **Given** synthetic patients exist, **When** a client GETs the patients list
   endpoint, **Then** a paginated response is returned with the same schema as the
   Public CC API.
2. **Given** a known patient ID, **When** a client GETs the patient detail
   endpoint, **Then** the response contains the patient's demographics and linked
   clinical records.
3. **Given** a SOAP report consumer, **When** it calls the SOAP report endpoint
   with valid credentials, **Then** it receives a well-formed SOAP response
   matching the CC report schema.
4. **Given** the mock is under an active auth mode, **When** a client calls any
   endpoint without correct credentials, **Then** the mock returns the same error
   structure (status code and body shape) as the real CC API.

---

### User Story 3 — Generate and Reset Synthetic Data (Priority: P2)

A developer starting a new test cycle needs a fresh set of realistic patient
records to work against. They open the admin interface, choose how many patients
to generate, and click generate. The system creates patients with realistic (but
entirely fictitious) demographics and linked clinical records. When a test cycle
is complete, they can reset the data without restarting the service.

**Why this priority**: Manual data creation is time-consuming and error-prone.
Automated generation dramatically reduces the time to start a meaningful test
session.

**Independent Test**: A developer can generate 50 patients with linked conditions
and medications, look up a specific patient by name or ID, and then reset all data
— all from the admin interface without touching any API or database directly.

**Acceptance Scenarios**:

1. **Given** the admin interface, **When** a developer requests generation of N
   patients (within configured limits), **Then** N patients with linked clinical
   records appear and are immediately queryable via the CC API endpoints.
2. **Given** generated patients exist, **When** the developer looks up a patient by
   a name fragment or identifier, **Then** matching results are returned.
3. **Given** test data exists, **When** the developer triggers a data reset,
   **Then** all synthetic records are removed and the API returns empty collections.
4. **Given** a batch size above the configured maximum, **When** the developer
   requests generation, **Then** the request is rejected with a clear error
   describing the limit.

---

### User Story 4 — Monitor API Requests for Debugging (Priority: P2)

A developer's integration client is behaving unexpectedly — requests are failing
or returning wrong data. They open the monitoring view in the admin interface and
see a chronological log of all requests the mock has received, including the method,
path, status code, response time, and request/response bodies (where safe to log).
They can filter by time range, status code, or path to quickly isolate the
problem.

**Why this priority**: Without visibility into what the server received, debugging
integration issues is guesswork. Monitoring is a force-multiplier for all other
user stories.

**Independent Test**: After making a series of API calls, a developer can open the
monitoring view and see each call listed with its status and timing, and can
expand any entry to view full request and response detail.

**Acceptance Scenarios**:

1. **Given** API calls have been made, **When** the developer opens the monitoring
   view, **Then** each call appears with method, path, status code, and response
   time.
2. **Given** the monitoring log, **When** the developer expands a request entry,
   **Then** the full request headers, body (where present and safe), and response
   body are shown.
3. **Given** a busy log, **When** the developer filters by status code (e.g., 4xx
   only), **Then** only matching entries are shown.
4. **Given** the monitoring view, **When** aggregate statistics are displayed,
   **Then** the developer can see total request counts, error rates, and average
   response time without exporting data.

---

### User Story 5 — Secure Admin Interface Access (Priority: P3)

When the mock is deployed to a shared or public server, an admin needs to ensure
only authorized team members can change auth settings, view request logs, or
generate/reset data. They configure an admin key via environment variable. From
that point, the admin interface prompts team members to enter the key once per
browser session; subsequent admin operations use a short-lived session token rather
than the raw key on every call.

**Why this priority**: This is essential for shared deployments but lower priority
than core mock functionality because local single-developer deployments can run
without it.

**Independent Test**: With an admin key configured, a team member can sign in via
the admin interface using the key, perform configuration and monitoring operations,
and have their session automatically expire without needing to re-enter the key for
the duration of the session.

**Acceptance Scenarios**:

1. **Given** an admin key is configured, **When** a team member enters the correct
   key in the admin interface, **Then** they receive a session that allows admin
   operations for a configurable time period.
2. **Given** an active admin session, **When** the session expires, **Then**
   subsequent admin operations are rejected and the user is prompted to re-authenticate.
3. **Given** no admin key is configured (local dev mode), **When** any request
   reaches admin routes, **Then** the request succeeds without authentication so
   local development requires zero setup.
4. **Given** an admin key is configured, **When** a direct API call arrives at an
   admin route without a valid session token, **Then** it is rejected with a 403
   regardless of origin.

---

### Edge Cases

- What happens when a developer requests data generation while a previous batch is
  still running?
- How does the system behave if the database is unavailable at startup?
- What is the experience when a request body contains credential-like values (e.g.,
  the session mint payload) — are those values excluded from request logs?
- What happens when an OAuth token expires mid-session and the client attempts to
  refresh using an already-expired refresh token?
- Can the mock be misconfigured so that No Auth mode is active while the system is
  exposed on the public internet? Should there be a warning?

## Requirements

### Functional Requirements

- **FR-001**: The system MUST support four authentication modes that can be switched
  at runtime without restarting: No Auth, Shared Bearer Token, API Key (CCAPIKey
  header), and OAuth 2.0 client credentials with access and refresh tokens.
- **FR-002**: The system MUST expose the CC API REST endpoints required to support
  a patient notification system and patient portal — specifically Patients,
  Conditions, Medications, Procedures, Encounters, and related clinical entities.
  All implemented endpoints MUST return structurally valid, non-placeholder
  responses. Additional endpoint groups are deferred to future phases.
- **FR-003**: The system MUST expose a SOAP report endpoint compatible with CC SOAP
  report consumers, protected by a configurable password.
- **FR-004**: The system MUST provide a web-based admin interface accessible from a
  standard browser requiring no additional installation.
- **FR-005**: The admin interface MUST allow generating synthetic patient and clinical
  records in configurable batch sizes up to a defined maximum.
- **FR-006**: The admin interface MUST allow looking up generated patients by name
  fragment or identifier.
- **FR-007**: The admin interface MUST allow resetting all synthetic data in a single
  operation without restarting the service.
- **FR-008**: The system MUST log every inbound API request with method, path, status
  code, response time, correlation ID, and safe-to-log request/response excerpts.
- **FR-009**: The admin interface MUST display request logs with filtering by time
  range, status code, and path, and show aggregate statistics (total count, error
  rate, average response time).
- **FR-010**: When an admin key is configured via environment variable, all admin
  routes MUST require a valid short-lived session token; the raw admin key MUST
  only be accepted at the session creation endpoint.
- **FR-011**: When no admin key is configured, admin routes MUST be open (no
  authentication required) to support zero-friction local development.
- **FR-012**: The system MUST be configurable to run on localhost or a public-facing
  server exclusively via environment variables and configuration files, with no code
  changes required between environments.
- **FR-013**: The system MUST protect against common web security threats including
  but not limited to: cross-origin request forgery, injection attacks, and
  credential exposure in logs.
- **FR-014**: The system MUST provide a health-check endpoint that returns current
  API status and active authentication mode, queryable without admin credentials.

### Key Entities

- **Patient**: A synthetic person record with demographics (name, DOB, gender,
  identifiers) and relationships to clinical records. Entirely fictitious — no real
  PHI ever stored.
- **Clinical Record** (Condition, Medication, Procedure, Encounter, etc.): Domain
  entities linked to a Patient that mirror the CC data model, used to populate
  realistic API responses.
- **AuthSettings**: The active authentication mode and associated credentials
  (token value, API key, OAuth client config). Stored in the database; one active
  configuration at a time.
- **AuthToken**: An issued OAuth access or refresh token with expiry, used to
  validate OAuth-mode requests.
- **AdminSession**: A short-lived, signed token issued after admin key verification,
  used to authorize admin API calls for the duration of the session.
- **ApiRequestLog**: A timestamped record of each inbound API call including
  method, path, status, response time, and safe excerpts of request/response.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A developer unfamiliar with the system can complete initial setup,
  configure an auth mode, and make a successful authenticated API call within
  10 minutes of first run.
- **SC-002**: Every implemented CC API endpoint returns a structurally valid,
  non-placeholder response; no endpoint returns stub data or an unimplemented
  error when called with valid inputs and credentials.
- **SC-003**: Synthetic data for up to 100 patients with linked clinical records can
  be generated in under 30 seconds.
- **SC-004**: Every API request appears in the monitoring log within 2 seconds of
  the request completing.
- **SC-005**: The admin interface is fully functional in current versions of Chrome,
  Firefox, and Safari without any browser extensions or plugins.
- **SC-006**: The system can run entirely on a developer's local machine with no
  external network dependencies beyond a local database.
- **SC-007**: Auth mode changes take effect for new requests within 5 seconds of
  being saved, without restarting the service.

## Assumptions

- The primary user is a software developer building a CC integration, not a clinical
  end user or patient.
- "Public CC API" refers to the REST API documented for CC integration partners;
  this spec does not include any internal or partner-only CC API surfaces.
- SOAP support is required as a first-class feature because some CC consumers use
  the SOAP report interface.
- A single-tenant deployment model is assumed: one team or developer per running
  instance. Multi-tenant isolation is out of scope.
- HTTPS termination is assumed to be handled at the infrastructure layer (reverse
  proxy, load balancer) in public internet deployments. The application itself is
  not responsible for TLS certificate management.
- Synthetic data quality need only be structurally and schema-valid, not clinically
  coherent or medically plausible.
- The system is not a compliance-tested substitute for a CC sandbox — it is a
  developer convenience tool. Clinical accuracy of responses is out of scope.
- The primary audience is internal ClinRS developers; however, the project is
  maintained and documented as an external-facing example project. Users are
  assumed to have CC domain knowledge. Documentation must meet a standard suitable
  for public sharing, but deep hand-holding for non-CC-familiar users is out of
  scope.
- Future extension to mock other vendor APIs (beyond CC) is a stated architectural
  intent but is explicitly out of scope for this specification.
