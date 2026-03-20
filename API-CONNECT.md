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
| **OAuth** | Client credentials flow. Obtain an access token from the token endpoint, then send `Authorization: Bearer <access_token>` on every request. Use the refresh endpoint to get a new access token when it expires. |

To discover the current mode (and, in Bearer mode, to obtain the token), an administrator can call the auth-settings endpoint with the admin key (see [Admin endpoints](#admin-endpoints) below). In development, if no admin key is set, auth-settings may be readable without a key.

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

To confirm that your credentials are accepted (e.g. after obtaining an OAuth access token or when using a Bearer token), call:

**Endpoint:** `GET /api/v1/auth/verify`

Send the same `Authorization: Bearer <token>` header you use for other protected endpoints. A **200 OK** with an empty body means authentication succeeded; **401 Unauthorized** means the token is missing or invalid.

---

## Endpoints overview

| Area        | Path prefix            | Auth required | Notes |
|------------|------------------------|----------------|--------|
| Health     | `/api/v1/health`       | No             | GET; no auth. |
| Auth       | `/api/v1/auth`         | No / Yes       | POST `/token`, POST `/refresh` (no auth). GET `/verify` requires auth; returns 200 if credentials are valid (useful to confirm Bearer or OAuth token). |
| Patients   | `/api/v1/patients`     | Yes*           | CRUD and sub-resources (devices, allergies, providers, etc.). *When mode is None, no auth. |
| Auth settings | `/api/v1/auth-settings` | Admin key   | GET/PUT; requires `X-Admin-Key` if `AUTH_SETTINGS_ADMIN_KEY` is set. |
| Monitoring | `/api/v1/monitoring`    | Admin key     | GET `/requests`, GET `/requests/{id}`, GET `/stats`; requires `X-Admin-Key` if `AUTH_SETTINGS_ADMIN_KEY` is set. |

---

## Admin endpoints

These endpoints are for administration and monitoring. Access can be restricted with an admin key.

- **Auth settings**  
  - `GET /api/v1/auth-settings` – current mode, token info (masked), OAuth client id (masked).  
  - `PUT /api/v1/auth-settings` – update mode, bearer token, or OAuth client credentials.  
  - If the server has `AUTH_SETTINGS_ADMIN_KEY` set, send: `X-Admin-Key: <admin-key>`.

- **Monitoring**  
  - `GET /api/v1/monitoring/requests` – list recent API request logs.  
  - `GET /api/v1/monitoring/requests/{id}` – request log detail.  
  - `GET /api/v1/monitoring/stats` – aggregated stats (last 200 requests).  
  - Same `X-Admin-Key` header when the admin key is configured.

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

- **401 Unauthorized** – Missing or invalid credentials (e.g. no/invalid Bearer token or OAuth access token).
- **403 Forbidden** – Valid auth but not allowed (e.g. admin endpoint without correct `X-Admin-Key`).
- **400 Bad Request** – Invalid request body or parameters (e.g. token/refresh requests with missing or wrong fields).

When in doubt, check the response body for details. The API also exposes Swagger UI in development at `{baseUrl}/swagger` for interactive exploration.
