# Connecting to the Mock Health System API

This guide describes how other systems should connect to the Mock Health System API, including base URL, versioning, and authentication.

---

## Base URL and versioning

- **Base URL**: Use the URL where the API is running (e.g. `http://localhost:5001` in development, or your deployed host).
- **Versioning**: All API routes are versioned. Use the `v1` prefix:
  - Base path: `{baseUrl}/api/v1/`
  - Example: `http://localhost:5001/api/v1/health`

---

## Authentication modes

The API supports three authentication modes, configured by the administrator. Your integration must match the mode in use.

| Mode   | Description |
|--------|--------------|
| **None** | No authentication. Send requests without an `Authorization` header. |
| **Bearer** | Single shared token. Send `Authorization: Bearer <token>` on every request. The token value is configured by the administrator. |
| **CCAPIKey** | Shared secret. Send header `CCAPIKey: <secret>` on every request (same value the administrator configured as the API key). |
| **OAuth** | Client credentials flow. Obtain an access token from the token endpoint, then send `Authorization: Bearer <access_token>` on every request. Use the refresh endpoint to get a new access token when it expires. |

To discover the current mode (and, in Bearer mode, to obtain the token), an administrator can call the auth-settings endpoint with admin credentials (see [Admin endpoints](#admin-endpoints) below). In development, if no admin key is set, auth-settings may be readable without a session or key.

---

## Authenticating when mode is **None**

No headers are required. Call any endpoint directly:

```http
GET /api/v1/health HTTP/1.1
Host: your-api-host
```

---

## Authenticating when mode is **Bearer**

1. Obtain the bearer token from the administrator (or from the auth-settings response if you have admin access).
2. Send it on every request to protected endpoints:

```http
GET /api/v1/patients/1 HTTP/1.1
Host: your-api-host
Authorization: Bearer your-configured-bearer-token
```

---

## Authenticating when mode is **CCAPIKey**

1. Obtain the shared API key from the administrator (same value stored for CCAPIKey mode).
2. Send it on every request to protected endpoints:

```http
GET /api/v1/patients/1 HTTP/1.1
Host: your-api-host
CCAPIKey: your-configured-api-key
```

---

## Authenticating when mode is **OAuth**

### 1. Obtain client credentials

The administrator configures **OAuth client ID** and **client secret** in the Mock Health System. You must have these values to get tokens.

### 2. Request an access token

**Endpoint:** `POST /api/v1/auth/token`

**Request body (JSON):**

```json
{
  "clientId": "your-client-id",
  "clientSecret": "your-client-secret"
}
```

Optional: include `"subject": "optional-subject"` to associate a subject with the token.

**Response (200 OK):**

```json
{
  "accessToken": "guid-access-token",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "refreshToken": "guid-refresh-token"
}
```

- **accessToken**: Use this in the `Authorization` header for API requests.
- **expiresIn**: Lifetime of the access token in seconds. Request a new access token before it expires using the refresh token.
- **refreshToken**: Use this to obtain a new access token without sending the client secret again (see step 4).

### 3. Call protected endpoints

Send the access token on every request:

```http
GET /api/v1/patients/1 HTTP/1.1
Host: your-api-host
Authorization: Bearer guid-access-token
```

### 4. Refresh the access token

Before the access token expires, exchange the refresh token for a new access token.

**Endpoint:** `POST /api/v1/auth/refresh`

**Request body (JSON):**

```json
{
  "refreshToken": "your-refresh-token"
}
```

**Response (200 OK):** Same shape as the token endpoint (`accessToken`, `tokenType`, `expiresIn`, `refreshToken`). The returned `refreshToken` is the same as the one you sent; use it again for the next refresh.

---

## Verifying credentials

To confirm that your credentials are accepted (e.g. after obtaining an OAuth access token, when using a Bearer token, or when using CCAPIKey mode), call:

**Endpoint:** `GET /api/v1/auth/verify`

Send the same credentials you use for other protected endpoints (e.g. `Authorization: Bearer …` for Bearer/OAuth, or `CCAPIKey` header in CCAPIKey mode). A **200 OK** with an empty body means authentication succeeded; **401 Unauthorized** means the token or key is missing or invalid.

---

## Endpoints overview

| Area        | Path prefix            | Auth required | Notes |
|------------|------------------------|----------------|--------|
| Health     | `/api/v1/health`       | No             | GET; no auth. |
| Auth       | `/api/v1/auth`         | No / Yes       | POST `/token`, POST `/refresh` (no auth). GET `/verify` requires auth; returns 200 if credentials are valid (useful to confirm Bearer, CCAPIKey, or OAuth token). |
| Patients   | `/api/v1/patients`     | Yes*           | CRUD and sub-resources (devices, allergies, providers, etc.). *When mode is None, no auth. |
| Admin session | `/api/v1/admin/sessions` | No (mint)  | POST only; exchanges static admin key for JWT (see [Admin endpoints](#admin-endpoints)). |
| Auth settings | `/api/v1/auth-settings` | Admin*   | GET/PUT; *requires `X-Admin-Key` or valid `X-Admin-Session` JWT if `AUTH_SETTINGS_ADMIN_KEY` is set. |
| Monitoring | `/api/v1/monitoring`    | Admin*     | GET `/requests`, GET `/requests/{id}`, GET `/stats`; same admin headers when `AUTH_SETTINGS_ADMIN_KEY` is set. |
| Test data  | `/api/v1/test-data`     | Admin*†    | Generate/reset/lookup test patients and related operations. *Same admin headers when key is set. †In `Development`, test-data routes skip admin checks (convenience for local workflows); use non-Development environments to enforce the key. **GET `/api/v1/test-data/soap/report-pkeys`** lists SOAP `pkey` values from `ReportQueryDefinitions` (PKeys only, no SQL). |

---

## Admin endpoints

These endpoints are for administration, monitoring, and synthetic data. When the server has **`AUTH_SETTINGS_ADMIN_KEY`** set, protected admin routes require:

- **`X-Admin-Session: <jwt>`** — short-lived HS256 JWT returned by the mint endpoint below. Use a **dedicated** header (not `Authorization`) so it does not collide with Bearer/OAuth API auth. The raw admin key is **only** accepted at the mint endpoint — it cannot be sent directly on admin routes.

If **`AUTH_SETTINGS_ADMIN_KEY`** is **not** set, admin routes are open (typical local development).

### Mint an admin session JWT

**Endpoint:** `POST /api/v1/admin/sessions`  
**Auth:** None (anonymous).  
**Body (JSON):**

```json
{
  "adminKey": "your-AUTH_SETTINGS_ADMIN_KEY-value"
}
```

**Responses:**

- **200 OK** — `{ "accessToken": "<jwt>", "expiresAtUtc": "<ISO-8601>" }`. Send `accessToken` as the value of `X-Admin-Session` on subsequent admin API calls until it expires.
- **400 Bad Request** — `AUTH_SETTINGS_ADMIN_KEY` is not configured on the server (minting is disabled; admin routes may still be open).
- **403 Forbidden** — wrong `adminKey`.

JWT signing: prefer env **`ADMIN_SESSION_SIGNING_KEY`** (or config `AdminSession:SigningKey`) as the HS256 secret. If not set, the server derives key material from `AUTH_SETTINGS_ADMIN_KEY`. Lifetime is controlled by **`AdminSession:TtlMinutes`** / env **`AdminSession__TtlMinutes`** (default 30, clamped server-side).

### Auth settings

- `GET /api/v1/auth-settings` — current mode, token info (masked), OAuth client id (masked).  
- `PUT /api/v1/auth-settings` — update mode, bearer token, CCAPIKey secret, or OAuth client credentials.  
- When admin key is required: include **`X-Admin-Key`** or **`X-Admin-Session`** as above.

### Monitoring

- `GET /api/v1/monitoring/requests` — list recent API request logs.  
- `GET /api/v1/monitoring/requests/{id}` — request log detail.  
- `GET /api/v1/monitoring/stats` — aggregated stats.  
- Same admin headers when `AUTH_SETTINGS_ADMIN_KEY` is set.

### Test data

- Prefix **`/api/v1/test-data/`** (e.g. patients generate/reset/stats, staff, audit events).  
- When `AUTH_SETTINGS_ADMIN_KEY` is set, send the same admin headers as for auth settings **unless** the server runs in **`Development`**, in which case test-data endpoints skip admin validation for local convenience. Auth settings and monitoring **always** enforce the admin key when it is set.

---

## Rate limiting

When rate limiting is enabled (configurable via **Authentication Settings** in the admin UI or directly via `PUT /api/v1/auth-settings`), API data endpoints enforce per-IP fixed-window counters. Admin interface endpoints (`/api/v1/auth-settings`, `/api/v1/monitoring`, `/api/v1/test-data`, `/api/v1/admin`) are **exempt** from the configurable limit; they are subject to a separate fixed ceiling of 120 requests/second and 5 000 requests/minute per IP, which cannot be changed via settings.

When a limit is exceeded the server returns **429 Too Many Requests** with:

- `Retry-After: <seconds>` header — the number of seconds until the client can retry. Reflects the **longer** resetting window (per-minute takes precedence over per-second) so the retry will succeed rather than immediately hitting the other limit again.
- JSON body in the standard error shape:

```json
{
  "status": 429,
  "title": "Too Many Requests",
  "detail": "Rate limit exceeded. Retry after 58 seconds.",
  "traceId": "<request-trace-id>"
}
```

Rate limiting is **off by default** (`RateLimitEnabled = false`). Default limits when enabled: 10 requests/second and 300 requests/minute per IP.

---

## Example: mint admin session then call auth-settings (curl)

```bash
# Replace host and values
MINT=$(curl -s -X POST "http://localhost:5001/api/v1/admin/sessions" \
  -H "Content-Type: application/json" \
  -d '{"adminKey":"your-admin-key"}')
SESSION=$(echo "$MINT" | jq -r .accessToken)

curl -s "http://localhost:5001/api/v1/auth-settings" \
  -H "X-Admin-Session: $SESSION" | jq .
```

---

## Example: OAuth flow (curl)

```bash
# 1. Get tokens (replace host and credentials)
curl -s -X POST "http://localhost:5001/api/v1/auth/token" \
  -H "Content-Type: application/json" \
  -d '{"clientId":"my-client","clientSecret":"my-secret"}' \
  | jq .

# 2. Call a protected endpoint with the access token
ACCESS_TOKEN="<accessToken from step 1>"
curl -s "http://localhost:5001/api/v1/patients/1" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# 3. When the token is near expiry, refresh
curl -s -X POST "http://localhost:5001/api/v1/auth/refresh" \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<refreshToken from step 1>"}' \
  | jq .
```

---

## Errors

- **401 Unauthorized** – Missing or invalid credentials (e.g. no/invalid Bearer token, OAuth access token, or CCAPIKey header).
- **403 Forbidden** – Valid API auth but not allowed (e.g. admin endpoint without correct `X-Admin-Key` / `X-Admin-Session`, wrong static admin key to mint, or expired or tampered session JWT).
- **400 Bad Request** – Invalid request body or parameters (e.g. token/refresh requests with missing or wrong fields).

When in doubt, check the response body for details. The API also exposes Swagger UI in development at `{baseUrl}/swagger` for interactive exploration.
