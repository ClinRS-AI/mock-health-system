# API Contracts: Health API (Patient-Facing Endpoints)

**Base URL**: `http://<host>/api/v1`
**Authentication**: Controlled by active `AuthSettings.Mode` (None / Bearer / CCAPIKey / OAuth)
**Versioning**: URL path (`v1`); header `api-version: 1.0` also accepted

---

## Authentication

All patient-facing endpoints require authentication when a non-None auth mode is
active. The mock returns identical error shapes to the real CC API on auth failure.

| Mode | Credential Location | Value |
|------|--------------------|-|
| None | — | No credential required |
| Bearer | `Authorization: Bearer {token}` | Configured bearer token value |
| CCAPIKey | `CCAPIKey: {key}` | Configured API key value |
| OAuth | `Authorization: Bearer {accessToken}` | Token obtained from `/auth/token` |

**Auth failure response** (401):
```json
{ "status": 401, "title": "Unauthorized", "detail": "..." }
```

---

## Health Check

### `GET /health`
Liveness check. Always returns 200. No authentication required.

**Response** `200 text/plain`:
```
Mock Health System API is running.
```

---

## Authentication Endpoints

### `GET /auth/verify`
Verify that the caller's current credentials are valid.

**Auth required**: Yes (active auth mode)
**Response** `200 OK`: Empty body on success; `401` on invalid credentials.

---

### `POST /auth/token`
Exchange client credentials for an OAuth access token and refresh token.
Only available when `AuthSettings.Mode = OAuth`.

**Auth required**: No (anonymous)

**Request body**:
```json
{
  "clientId": "string",
  "clientSecret": "string",
  "subject": "string (optional)"
}
```

**Response** `200`:
```json
{
  "accessToken": "string",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "refreshToken": "string"
}
```

**Errors**: `400` if OAuth mode is not active; `401` if credentials are invalid.

---

### `POST /auth/refresh`
Exchange a refresh token for a new access token.
Only available when `AuthSettings.Mode = OAuth`.

**Auth required**: No (anonymous)

**Request body**:
```json
{ "refreshToken": "string" }
```

**Response** `200`: Same shape as `/auth/token`.

**Errors**: `400` if OAuth mode not active; `401` if refresh token is expired or revoked.

---

## Patient Endpoints

### `GET /patients/odata`
Paginated list of patients. OData-style paging via query parameters.

**Auth required**: Yes
**Query parameters**: `$skip` (int), `$top` (int, max 100)

**Response** `200`:
```json
{
  "value": [ { /* PatientViewModel */ } ],
  "count": 1250,
  "nextLink": "string (URL for next page, if applicable)"
}
```

---

### `POST /patients/search`
Search patients by one or more criteria.

**Auth required**: Yes

**Request body**:
```json
{
  "name": "string (partial, optional)",
  "email": "string (optional)",
  "dateOfBirth": "YYYY-MM-DD (optional)",
  "zip": "string (optional)",
  "genderCode": "string (optional)",
  "city": "string (optional)",
  "status": "string (optional)"
}
```

**Response** `200`: Array of `PatientViewModel` (unordered).

---

### `GET /patients/{id}`
Get a single patient by integer ID.

**Auth required**: Yes

**Response** `200 PatientViewModel`:
```json
{
  "id": 42,
  "uid": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "displayName": "Jane Smith",
  "firstName": "Jane",
  "middleName": null,
  "lastName": "Smith",
  "dateOfBirth": "1985-03-15",
  "genderCode": "F",
  "status": "Active",
  "primaryEmailAddress": "jane.smith@example.com",
  "address1": "123 Main St",
  "city": "Springfield",
  "state": "IL",
  "zip": "62701",
  "mrn": "MRN-000042",
  "phones": [ { "slot": 1, "number": "555-0100" } ]
}
```

**Errors**: `404` if patient not found.

---

### `POST /patients`
Create a new patient.

**Auth required**: Yes

**Request body**: `PatientEditModel` (same fields as PatientViewModel, id/uid omitted)
**Response** `201 PatientViewModel`

---

### `PUT /patients/{id}`
Replace all patient fields.

**Auth required**: Yes
**Request body**: `PatientEditModel`
**Response** `200 PatientViewModel`
**Errors**: `404` if not found

---

### `PATCH /patients/{id}`
Partial update — only provided fields are changed.

**Auth required**: Yes
**Request body**: `PatientPatchModel` (all fields optional)
**Response** `200 PatientViewModel`

---

### `DELETE /patients/{id}`
Delete a patient and all linked records.

**Auth required**: Yes
**Response** `204 No Content`
**Errors**: `404` if not found; `409 Conflict` if cascading delete is blocked

---

### `PUT /patients/{id}/status`
Update only the patient's status and status reason.

**Auth required**: Yes
**Request body**:
```json
{ "status": "Inactive", "statusReason": "Withdrawn" }
```
**Response** `200 PatientViewModel`

---

## Patient Sub-Resource Endpoints

All sub-resource endpoints follow the same pattern:
- `GET /patients/{id}/{resource}` → array of resource view models
- `POST /patients/{id}/{resource}` → add records (returns 201 with array)
- Patient must exist (404 if not)

### Sub-resources and their models

| Path | GET response | POST body |
|------|-------------|-----------|
| `/patients/{id}/allergies` | `PatientAllergyViewModel[]` | `PatientAllergyModel[]` |
| `/patients/{id}/conditions` | `PatientConditionViewModel[]` | `PatientConditionEditModel[]` |
| `/patients/{id}/medications` | `PatientMedicationViewModel[]` | `PatientMedicationEditModel[]` |
| `/patients/{id}/immunizations` | `PatientImmunizationViewModel[]` | `PatientImmunizationEditModel[]` |
| `/patients/{id}/procedures` | `PatientProcedureViewModel[]` | `PatientProcedureModel[]` |
| `/patients/{id}/providers` | `PatientProviderViewModel[]` | `PatientProviderEditModel[]` (replaces all) |
| `/patients/{id}/devices` | `PatientMedicalDeviceViewModel[]` | `PatientMedicalDeviceEditModel[]` |
| `/patients/{id}/family-history` | `PatientFamilyHistoryViewModel[]` | `PatientFamilyHistoryEditModel[]` |
| `/patients/{id}/social-history` | `PatientLifeStylesHistoryViewModel[]` | `PatientLifeStylesHistoryEditModel[]` |

**Note on medications**: POST body may include `linkedConditionIds: int[]` on each
medication to link to existing `PatientCondition` records.

---

## System / Reference Data Endpoints

### `GET /system/conditions/odata`
### `GET /system/medications/odata`
### `GET /system/allergies/odata`

Returns paginated reference data. OData paging via `$skip` / `$top`.

**Auth required**: Yes (active auth mode)

**Response** `200`:
```json
{
  "value": [ { "id": 1, "name": "Type 2 Diabetes", "icd10Code": "E11", ... } ],
  "count": 450
}
```
