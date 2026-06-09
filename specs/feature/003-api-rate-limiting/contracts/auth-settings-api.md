# API Contract: Authentication Settings (extended for Rate Limiting)

**Feature**: Configurable API Rate Limiting
**Existing endpoint**: `GET /api/v1/auth-settings` and `PUT /api/v1/auth-settings`
**Change type**: Additive (new fields on existing response and request bodies)

---

## GET /api/v1/auth-settings

No change to method, path, authentication requirements, or status codes.

### Response body changes (additive)

The following fields are added to the existing `AuthSettingsViewModel` response:

```json
{
  "mode": "CCAPIKey",
  "bearerToken": null,
  "oAuthClientId": null,
  "oAuthClientSecret": null,
  "accessTokenLifetimeMinutes": 60,
  "refreshTokenLifetimeDays": 30,
  "hasAnyTokens": true,
  "rateLimitEnabled": false,
  "rateLimitPerSecond": 10,
  "rateLimitPerMinute": 300
}
```

| New field | Type | Notes |
|---|---|---|
| `rateLimitEnabled` | boolean | `false` = rate limiting is off; `true` = limits are actively enforced |
| `rateLimitPerSecond` | integer | Requests per second per IP; meaningful only when `rateLimitEnabled = true` |
| `rateLimitPerMinute` | integer | Requests per minute per IP; meaningful only when `rateLimitEnabled = true` |

---

## PUT /api/v1/auth-settings

No change to method, path, authentication requirements, or 200/400 status code semantics.

### Request body changes (additive)

The following optional fields are added to `AuthSettingsUpdateModel`:

```json
{
  "mode": "CCAPIKey",
  "rateLimitEnabled": true,
  "rateLimitPerSecond": 10,
  "rateLimitPerMinute": 300
}
```

| New field | Type | Required | Notes |
|---|---|---|---|
| `rateLimitEnabled` | boolean | No | Omit to leave unchanged |
| `rateLimitPerSecond` | integer | No | Omit to leave unchanged; must be ≥ 1 if provided alongside `rateLimitEnabled: true` |
| `rateLimitPerMinute` | integer | No | Omit to leave unchanged; must be ≥ 1 if provided alongside `rateLimitEnabled: true` |

**Validation errors (400 Bad Request)**:
- `rateLimitPerSecond` ≤ 0 when rate limiting would be enabled after save
- `rateLimitPerMinute` ≤ 0 when rate limiting would be enabled after save

**Side effect on save**: All in-memory per-IP rate limit counters are reset. Clients that were accumulating requests in the current window start fresh.

### Response body

Same shape as GET, including the three new rate limit fields reflecting the persisted state after the update.

---

## New response: 429 Too Many Requests (API endpoints only)

**Applies to**: All API data endpoints. Admin endpoints (`/api/v1/auth-settings`, `/api/v1/monitoring/*`, `/api/v1/test-data/*`, `/api/v1/admin/*`) are NOT subject to the configurable limit.

**Trigger**: A caller IP address exceeds either the per-second or per-minute configured limit.

### Response headers

| Header | Example value | Notes |
|---|---|---|
| `Retry-After` | `47` | Integer seconds until the rate-limited window resets. Reflects the longer window if both are exhausted. |
| `Content-Type` | `application/json` | Standard JSON body |

### Response body

Standard `ApiErrorResponse` shape (consistent with 400/401/404/500 responses):

```json
{
  "status": 429,
  "title": "Too Many Requests",
  "detail": "Rate limit exceeded. Retry after 47 seconds.",
  "traceId": "00-abc123..."
}
```

### Example scenario

**Request** (triggers 429):
```
GET /api/v1/patients HTTP/1.1
CCAPIKey: my-shared-secret
```

**Response**:
```
HTTP/1.1 429 Too Many Requests
Retry-After: 1
Content-Type: application/json

{
  "status": 429,
  "title": "Too Many Requests",
  "detail": "Rate limit exceeded. Retry after 1 seconds.",
  "traceId": "00-9f8a7b..."
}
```

---

## No new endpoints

This feature adds no new route paths. All changes are:
1. Additive fields on an existing endpoint response/request
2. A new 429 response status that any API data endpoint can now return
