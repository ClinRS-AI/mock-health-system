# API Contracts: Admin API

**Base URL**: `http://<host>/api/v1`
**Admin Authentication**: When `AUTH_SETTINGS_ADMIN_KEY` env var is set, all admin
routes (except session minting) require `X-Admin-Session: {jwt}`. When the env var
is unset, admin routes are open (local dev mode).

---

## Admin Session Management

### `POST /admin/sessions`
Exchange the static admin key for a short-lived session JWT. This is the **only**
endpoint that accepts the raw admin key.

**Auth required**: No (anonymous — `[AllowAnonymous]`)

**Request body**:
```json
{ "adminKey": "string" }
```

**Response** `200`:
```json
{
  "accessToken": "string (HS256 JWT)",
  "expiresAtUtc": "2026-05-19T14:30:00Z"
}
```

**Errors**:
- `403 Forbidden`: Incorrect admin key
- `503 Service Unavailable`: No admin key configured (environment has no key set)

**Session usage**: Include the returned token as `X-Admin-Session: {accessToken}` on
all subsequent admin calls. Default TTL is 30 minutes (configurable via
`AdminSession:TtlMinutes`).

---

## Auth Settings

### `GET /auth-settings`
Get the current authentication mode and settings.

**Admin auth required**: Yes

**Response** `200`:
```json
{
  "mode": "CCAPIKey",
  "bearerToken": null,
  "ccApiKey": "my-secret-key",
  "oAuthClientId": null,
  "oAuthClientSecret": null,
  "accessTokenLifetimeMinutes": 60,
  "refreshTokenLifetimeDays": 30
}
```

---

### `PUT /auth-settings`
Change the active authentication mode and credentials. Changes take effect within
seconds for new requests — no service restart required.

**Admin auth required**: Yes

**Request body**:
```json
{
  "mode": "Bearer | CCAPIKey | OAuth | None",
  "bearerToken": "string (required if mode=Bearer)",
  "ccApiKey": "string (required if mode=CCAPIKey)",
  "oAuthClientId": "string (required if mode=OAuth)",
  "oAuthClientSecret": "string (required if mode=OAuth)",
  "accessTokenLifetimeMinutes": 60,
  "refreshTokenLifetimeDays": 30
}
```

**Response** `200`: Same shape as GET.

**Errors**: `400` if required credential fields missing for the selected mode.

---

## Monitoring

### `GET /monitoring/requests`
List recent API requests with optional filtering.

**Admin auth required**: Yes
**Query parameters**:
- `take` (int, default 100, max 500)
- `pathPrefix` (string, optional — filter by URL path prefix)
- `statusCode` (int, optional — filter to exact status code)
- `sinceUtc` (ISO 8601 datetime, optional)

**Response** `200`:
```json
[
  {
    "id": 1001,
    "createdAtUtc": "2026-05-19T10:00:00Z",
    "method": "GET",
    "path": "/api/v1/patients/odata",
    "statusCode": 200,
    "durationMs": 45,
    "correlationId": "abc-123"
  }
]
```

---

### `GET /monitoring/requests/{id}`
Get full detail of a single logged request including bodies.

**Admin auth required**: Yes

**Response** `200`:
```json
{
  "id": 1001,
  "createdAtUtc": "2026-05-19T10:00:00Z",
  "method": "POST",
  "path": "/api/v1/patients/search",
  "queryString": "",
  "statusCode": 200,
  "durationMs": 32,
  "origin": "http://localhost:5176",
  "userAgent": "axios/1.7.0",
  "remoteIp": "127.0.0.1",
  "requestBody": "{ \"name\": \"Smith\" }",
  "responseBody": "[ { \"id\": 42, ... } ]",
  "correlationId": "abc-123"
}
```

**Errors**: `404` if log entry not found.

---

### `GET /monitoring/stats`
Aggregated statistics from the most recent requests.

**Admin auth required**: Yes

**Response** `200`:
```json
{
  "totalRequests": 847,
  "statusBreakdown": {
    "2xx": 812,
    "4xx": 30,
    "5xx": 5
  },
  "averageDurationMs": 38,
  "p50DurationMs": 22,
  "p95DurationMs": 120,
  "p99DurationMs": 340
}
```

---

## Test Data Management

### `POST /test-data/patients/generate`
Generate synthetic patients with linked clinical records.

**Admin auth required**: Yes (bypassed in Development environment)

**Request body**:
```json
{
  "totalCount": 50,
  "duplicatePercentage": 10,
  "seed": 12345
}
```

- `totalCount`: Number of patients to generate (required, max enforced by server)
- `duplicatePercentage`: Percent of generated patients that are near-duplicates (0–100)
- `seed`: Optional integer seed for reproducible generation

**Response** `200`:
```json
{
  "generatedCount": 50,
  "duplicatesCreated": 5,
  "totalPatients": 142
}
```

**Errors**: `400` if totalCount exceeds the server-configured maximum.

---

### `POST /test-data/patients/reset`
Delete all patient records and linked clinical data. Non-reversible.

**Admin auth required**: Yes (bypassed in Development)
**Response** `200 OK`

---

### `POST /test-data/patients/add`
Add a single named patient.

**Admin auth required**: Yes (bypassed in Development)

**Request body**:
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com"
}
```

**Response** `200`:
```json
{ "id": 143, "uid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" }
```

---

### `GET /test-data/patients/lookup`
Look up a patient by any available identifier.

**Admin auth required**: Yes (bypassed in Development)
**Query parameters** (at least one required): `id`, `uid`, `email`

**Response** `200`: Full patient record including all sub-resources.
**Errors**: `404` if no matching patient.

---

### `GET /test-data/patients/random`
Get a randomly selected patient.

**Admin auth required**: Yes (bypassed in Development)
**Response** `200`: Full patient record (same as lookup).
**Errors**: `404` if no patients exist.

---

### `PUT /test-data/patients/{id}`
Update a patient record, optionally creating an audit log entry.

**Admin auth required**: Yes (bypassed in Development)
**Query parameters**: `saveWithAudit` (bool, default false)
**Request body**: Full patient view model
**Response** `200`: Updated patient record.

---

### `GET /test-data/patients/stats`
Summary statistics about the current synthetic dataset.

**Admin auth required**: Yes (bypassed in Development)

**Response** `200`:
```json
{
  "totalPatients": 142,
  "perSiteDistribution": [
    { "siteId": 1, "siteName": "Main Site", "count": 142 }
  ]
}
```

---

### `POST /test-data/staff/generate`
Generate synthetic staff records for use in audit log testing.

**Admin auth required**: Yes (bypassed in Development)

**Request body**:
```json
{ "count": 10, "seed": 42 }
```

**Response** `200`:
```json
{ "generatedCount": 10 }
```

---

### `POST /test-data/audit-events/generate`
Generate recent audit log entries using existing patients and staff.

**Admin auth required**: Yes (bypassed in Development)

**Request body**:
```json
{ "count": 20, "seed": 42 }
```

**Response** `200`:
```json
{ "generatedCount": 20 }
```

---

### `GET /test-data/soap/report-pkeys`
List available SOAP report PKeys registered in the database.

**Admin auth required**: Yes (bypassed in Development)

**Response** `200`:
```json
{
  "pkeys": [ "PatientReport", "AuditReport" ]
}
```
