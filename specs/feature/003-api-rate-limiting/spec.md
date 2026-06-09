# Feature Specification: Configurable API Rate Limiting

**Feature Branch**: `feature/003-api-rate-limiting`

**Created**: 2026-06-08

**Status**: Draft

**Input**: User description: "I'd like to start on a new feature for rate limits. The Clinical Conductor application has rate limits per API key that are used in production and can slow down any integration. These are in place to prevent DOS attacks and clients from accidentally impacting production. I don't recall the exact limits, but it is something like 10 requests per second and 300 requests per minute max. Once an API key goes over that limit, then a 429 error is returned. Because this mock system does not have multiple API keys, I'd like to simply configure a per second and per minute limit. I'd like to prevent the API calls to the admin endpoints (accessed by the UI) from being impacted by these limits though having some form of limit for even the admin interface does seem like a good idea. I think the best place to put the configuration of these limits is on the Authentication settings page."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Configure and Enforce API Rate Limits (Priority: P1)

An integrator working against the mock system wants to verify their application handles rate limit errors gracefully before going to production. They navigate to Authentication Settings, configure per-second and per-minute limits that match the production values they know about, and then run their client to observe 429 responses at the expected thresholds.

**Why this priority**: This is the core purpose of the feature. Without the ability to configure limits and observe 429 responses, no other user story is meaningful.

**Independent Test**: Navigate to Authentication Settings, enable rate limiting, set per-second and per-minute values, save, then drive API traffic from a client. The client should receive 429 responses once either threshold is crossed.

**Acceptance Scenarios**:

1. **Given** rate limiting is disabled, **When** the admin enables it with a per-second limit of 10 and per-minute limit of 300 and saves, **Then** the new settings are persisted and applied immediately.
2. **Given** rate limiting is enabled with a per-second limit of 5, **When** a client sends 6 requests within one second from the same IP address, **Then** the 6th request receives an HTTP 429 Too Many Requests response.
3. **Given** rate limiting is enabled with a per-minute limit of 20, **When** a client sends 21 requests within one minute from the same IP address, **Then** the 21st request receives an HTTP 429 Too Many Requests response.
4. **Given** a client has hit the per-second limit, **When** the next one-second window begins, **Then** the client's counter resets and requests are accepted again (up to the configured limit).
5. **Given** two clients have different IP addresses, **When** one client hits the rate limit, **Then** the other client's requests are unaffected and continue to succeed.

---

### User Story 2 — Disable Rate Limiting for Unrestricted Testing (Priority: P1)

A developer wants to run high-volume load or smoke tests without being throttled by the mock. They disable rate limiting on the Authentication Settings page so their tooling can make unlimited requests.

**Why this priority**: Developers need to be able to turn off rate limiting entirely when testing scenarios that are not rate-limit related. Without this, the configured limits would block any performance or integration test suite.

**Independent Test**: With rate limiting enabled and a low threshold set, disable rate limiting, then send a burst of requests. No 429 responses should be returned.

**Acceptance Scenarios**:

1. **Given** rate limiting is enabled, **When** the admin disables it on Authentication Settings and saves, **Then** subsequent API requests are no longer throttled regardless of volume.
2. **Given** rate limiting is disabled, **When** a client sends 100 requests in one second, **Then** all requests receive successful responses (assuming the API itself is healthy).
3. **Given** rate limiting is disabled, **When** the admin re-enables it, **Then** the previously configured per-second and per-minute values are restored and enforced again.

---

### User Story 3 — Receive Actionable Feedback on Rate Limit Errors (Priority: P2)

A developer's client application receives a 429 response and needs to know when it is safe to retry. The response includes a standard `Retry-After` header so the client can implement intelligent backoff.

**Why this priority**: A bare 429 with no guidance forces developers to guess retry intervals. This is a secondary enhancement — rate limiting works without it, but the developer experience is poor.

**Independent Test**: Exceed the configured limit, inspect the 429 response headers. The `Retry-After` header must be present and contain the number of seconds until the window resets. Wait that duration and confirm the next request succeeds.

**Acceptance Scenarios**:

1. **Given** a client exceeds the rate limit, **When** the 429 response is returned, **Then** it includes a `Retry-After` header with a positive integer value indicating seconds until the window resets.
2. **Given** a client receives a 429 with `Retry-After: 1`, **When** the client waits 1 second and sends a single request, **Then** the request succeeds (assuming they stay within the limit for the new window).
3. **Given** only the per-second limit is reached (per-minute limit is not exhausted), **When** the 429 response is returned, **Then** the `Retry-After` value reflects the per-second window reset (≤ 1 second).
4. **Given** both the per-second and per-minute limits are exhausted, **When** the 429 response is returned, **Then** the `Retry-After` value reflects the per-minute window reset — so the client's next request will actually succeed.

---

### User Story 4 — Admin UI Is Unaffected by API Rate Limits (Priority: P1)

The admin uses Authentication Settings, Monitoring, and Test Data Management pages actively while simultaneously running load tests against the API. The admin UI never triggers a 429 even when the API is being hammered at the rate limit threshold.

**Why this priority**: If admin requests counted against the API rate limit, the UI would become unusable during the load tests it exists to support — undermining the entire tooling purpose.

**Independent Test**: Set a low API rate limit (e.g., 1 request per second). While a client saturates that limit from the same machine, navigate between admin pages and save settings. The UI must not display 429 errors.

**Acceptance Scenarios**:

1. **Given** the API rate limit is set to 1 request per second, **When** the admin actively uses the UI (page navigation, saving settings, viewing logs), **Then** no 429 errors appear in the admin interface.
2. **Given** a client from the same IP is actively exhausting the per-second API limit, **When** the admin submits a settings change, **Then** the save succeeds normally.
3. **Given** the admin UI makes many requests in rapid succession (e.g., auto-polling in Monitoring), **Then** those requests are not counted against the API rate limit counters.

---

### Edge Cases

- What if both the per-second and per-minute limits are hit simultaneously? Both are enforced concurrently — whichever is hit first causes the 429. The `Retry-After` value reflects the longer-resetting window (per-minute), because retrying after only the per-second window resets would immediately hit the per-minute limit again.
- What if the per-second limit is set higher than the per-minute limit (e.g., 100/sec, 50/min)? Both limits are enforced independently. Sustained traffic will hit the per-minute cap before the per-second cap.
- What happens to in-flight rate limit counters when limits are changed? Counters reset when new settings are saved.
- What if a limit value of 0 is entered? Saving a zero or negative value when rate limiting is enabled must be rejected with a validation error. Minimum valid value for each limit is 1.
- What happens when the server restarts? In-memory counters reset. Persisted settings (enabled state, limit values) are restored on startup.
- What if multiple developers share the same IP address (e.g., behind a corporate NAT)? Their combined traffic counts against one counter. This is a documented limitation of per-IP rate limiting in a dev mock context.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST enforce a configurable per-second request limit on all API data endpoints, scoped per caller IP address.
- **FR-002**: The system MUST enforce a configurable per-minute request limit on all API data endpoints, scoped per caller IP address.
- **FR-003**: When a caller exceeds either the per-second or per-minute limit, the system MUST return HTTP 429 Too Many Requests for each request that exceeds the threshold within that window.
- **FR-004**: HTTP 429 responses MUST include a `Retry-After` header containing a positive integer number of seconds until the relevant rate limit window resets.
- **FR-005**: Rate limiting MUST be independently togglable (enabled/disabled) without altering the stored per-second and per-minute values.
- **FR-006**: Rate limit settings (enabled flag, per-second limit, per-minute limit) MUST be exposed on the Authentication Settings page and persisted across server restarts.
- **FR-007**: Rate limit configuration changes MUST take effect within the next request after saving — no server restart required.
- **FR-008**: Rate limit counters for all IP addresses MUST reset when rate limit settings are saved.
- **FR-009**: Admin endpoints (Authentication Settings, Monitoring, Test Data Management, and session management) MUST NOT be subject to the configurable API rate limits.
- **FR-010**: Admin endpoints MUST be protected by a fixed built-in rate limit that prevents abuse while never impacting normal UI usage.
- **FR-011**: When rate limiting is disabled, the system MUST process all API requests regardless of request rate from any IP.
- **FR-012**: Saving a per-second or per-minute limit value less than 1 when rate limiting is enabled MUST be rejected with a descriptive validation error.
- **FR-013**: The per-second and per-minute limits MUST be enforced concurrently — a request must pass both checks to be accepted.
- **FR-014**: The Authentication Settings page demo mode view MUST display representative rate limit configuration data consistent with the existing demo data approach.

### Key Entities

- **Rate Limit Configuration**: Enabled/disabled flag, per-second limit (integer ≥ 1, default 10), per-minute limit (integer ≥ 1, default 300). Persisted alongside authentication settings. Default state: disabled.
- **Rate Limit Counter**: Per-IP, per-window (one-second and one-minute) request counts. In-memory only; resets on server restart or when settings are saved.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An integrator can configure rate limits and observe 429 responses from a real client within 60 seconds of enabling the feature, with no additional setup.
- **SC-002**: Admin UI operations (page navigation, form saves, log refresh) complete successfully at normal interactive speed even when the API rate limit is set to 1 request per second.
- **SC-003**: Every request that exceeds the configured per-second or per-minute limit receives a 429 response — no requests above the threshold are silently accepted.
- **SC-004**: All 429 responses include a valid `Retry-After` header; clients waiting the indicated duration can resume requests without continued throttling.
- **SC-005**: Disabling rate limiting restores unrestricted throughput immediately — no observable delay between save and enforcement change.
- **SC-006**: Rate limit settings survive a server restart unchanged.

## Assumptions

- Rate limit counters are **in-memory only** and reset on server restart. Persistent counter state across restarts is out of scope — this is a development mock, not a production rate limiter.
- The fixed built-in limit for admin endpoints is generous (120 requests per second, 5000 per minute). This value is not user-configurable and is chosen to be invisible under normal UI usage.
- The **default state** for rate limiting is **disabled**, so existing integrations are unaffected when the feature is deployed.
- A **fixed (tumbling) window** algorithm is used for both per-second and per-minute counters. Sliding window behavior is out of scope.
- The **SOAP report endpoint** is classified as an API endpoint (subject to the configurable rate limit), not an admin endpoint.
- Rate limit configuration is stored in the **database** alongside authentication settings, not in environment variables or configuration files.
- The `Retry-After` value reflects the **later-resetting** window when both limits are exceeded simultaneously. Returning the shorter window would mislead clients into retrying before the per-minute limit clears.
- The **demo mode** view of Authentication Settings will show rate limiting as enabled with representative values (10/sec, 300/min) in the existing demo data constants.
- Two limits (per-second and per-minute) are sufficient to replicate the production CC experience. Burst limits, concurrency limits, or per-endpoint limits are out of scope.
