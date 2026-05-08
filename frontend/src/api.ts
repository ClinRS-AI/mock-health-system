import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL
});

/** Calls the versioned health endpoint (GET /api/v1/health). */
export async function getApiStatus(): Promise<string> {
  const response = await api.get<string>("/api/v1/health");
  return typeof response.data === "string" ? response.data : JSON.stringify(response.data);
}

export type AuthMode = "None" | "Bearer" | "CCAPIKey" | "OAuth";

export interface AuthSettings {
  mode: AuthMode;
  bearerToken?: string | null;
  oAuthClientId?: string | null;
  oAuthClientSecret?: string | null;
  accessTokenLifetimeMinutes: number;
  refreshTokenLifetimeDays: number;
  hasAnyTokens: boolean;
}

export interface UpdateAuthSettingsRequest {
  mode: AuthMode;
  bearerToken?: string | null;
  oAuthClientId?: string | null;
  oAuthClientSecret?: string | null;
  accessTokenLifetimeMinutes?: number;
  refreshTokenLifetimeDays?: number;
}

export async function getAuthSettings(adminKey?: string): Promise<AuthSettings> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }
  const response = await api.get<AuthSettings>("/api/v1/auth-settings", { headers });
  return response.data;
}

export async function updateAuthSettings(
  payload: UpdateAuthSettingsRequest,
  adminKey?: string
): Promise<AuthSettings> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }
  const response = await api.put<AuthSettings>("/api/v1/auth-settings", payload, { headers });
  return response.data;
}

export interface MonitoredRequestSummary {
  id: number;
  createdAtUtc: string;
  method: string;
  path: string;
  statusCode: number;
  durationMs: number;
  origin?: string | null;
}

export interface MonitoredRequestDetail {
  id: number;
  createdAtUtc: string;
  method: string;
  path: string;
  queryString?: string | null;
  statusCode: number;
  durationMs: number;
  origin?: string | null;
  referer?: string | null;
  userAgent?: string | null;
  remoteIp?: string | null;
  requestBody?: string | null;
  responseBody?: string | null;
  correlationId?: string | null;
}

export interface GetMonitoredRequestsParams {
  take?: number;
  pathPrefix?: string;
  statusCode?: number;
  sinceUtc?: string;
}

export async function getMonitoredRequests(
  params?: GetMonitoredRequestsParams,
  adminKey?: string
): Promise<MonitoredRequestSummary[]> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const searchParams = new URLSearchParams();
  if (params?.take != null) searchParams.set("take", String(params.take));
  if (params?.pathPrefix) searchParams.set("pathPrefix", params.pathPrefix);
  if (params?.statusCode != null) searchParams.set("statusCode", String(params.statusCode));
  if (params?.sinceUtc) searchParams.set("sinceUtc", params.sinceUtc);

  const query = searchParams.toString();
  const url = `/api/v1/monitoring/requests${query ? `?${query}` : ""}`;

  const response = await api.get<MonitoredRequestSummary[]>(url, { headers });
  return response.data;
}

export async function getMonitoredRequest(
  id: number,
  adminKey?: string
): Promise<MonitoredRequestDetail> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const response = await api.get<MonitoredRequestDetail>(`/api/v1/monitoring/requests/${id}`, {
    headers
  });
  return response.data;
}

export interface StatusBreakdownItem {
  statusCode: number;
  count: number;
}

export interface MonitoringStats {
  statusBreakdown: StatusBreakdownItem[];
  requestCount: number;
  averageDurationMs?: number | null;
  percentile95DurationMs?: number | null;
  maxDurationMs?: number | null;
}

export async function getMonitoringStats(adminKey?: string): Promise<MonitoringStats> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }
  const response = await api.get<MonitoringStats>("/api/v1/monitoring/stats", { headers });
  return response.data;
}

// Test data management

export interface GeneratePatientsOptions {
  totalCount?: number;
  duplicatePercentage?: number;
  seed?: number;
}

export interface GeneratePatientsResult {
  totalRequested: number;
  totalBaseInserted: number;
  duplicateRequested: number;
  duplicateInserted: number;
  totalAfter: number;
}

export interface GenerateStaffOptions {
  count?: number;
  seed?: number;
}

export interface GenerateStaffResult {
  requested: number;
  inserted: number;
  totalAfter: number;
}

export interface GenerateRecentAuditEventsOptions {
  count?: number;
  seed?: number;
}

export interface AuditTypeInsertCount {
  code: string;
  displayName: string;
  count: number;
}

export interface GenerateRecentAuditEventsResult {
  requested: number;
  inserted: number;
  totalAfter: number;
  /** Present when backend supports breakdown (new API). */
  insertedByAuditType?: AuditTypeInsertCount[];
}

export interface PatientsBySite {
  siteName: string;
  count: number;
}

export interface PatientTestDataStats {
  patientCount: number;
  duplicatePatientCount: number;
  recentAuditEventCount: number;
  totalStaffCount: number;
  patientsBySite: PatientsBySite[];
}

export async function generateTestPatients(
  options: GeneratePatientsOptions,
  adminKey?: string
): Promise<GeneratePatientsResult> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const response = await api.post<GeneratePatientsResult>(
    "/api/v1/test-data/patients/generate",
    options,
    { headers }
  );
  return response.data;
}

export async function getPatientTestDataStats(
  adminKey?: string
): Promise<PatientTestDataStats> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const response = await api.get<PatientTestDataStats>("/api/v1/test-data/patients/stats", {
    headers
  });
  return response.data;
}

export async function generateTestStaff(
  options: GenerateStaffOptions,
  adminKey?: string
): Promise<GenerateStaffResult> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const response = await api.post<GenerateStaffResult>("/api/v1/test-data/staff/generate", options, {
    headers
  });
  return response.data;
}

export async function generateRecentAuditEvents(
  options: GenerateRecentAuditEventsOptions,
  adminKey?: string
): Promise<GenerateRecentAuditEventsResult> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const response = await api.post<GenerateRecentAuditEventsResult>(
    "/api/v1/test-data/audit-events/generate",
    options,
    { headers }
  );
  return response.data;
}

export async function resetTestPatients(adminKey?: string): Promise<void> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  await api.post("/api/v1/test-data/patients/reset", {}, { headers });
}

export interface AddTestPatientRequest {
  firstName: string;
  lastName: string;
  email: string;
}

export interface AddTestPatientResponse {
  id: number;
  uid: string;
}

export async function addTestPatient(
  body: AddTestPatientRequest,
  adminKey?: string
): Promise<AddTestPatientResponse> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const response = await api.post<AddTestPatientResponse>(
    "/api/v1/test-data/patients/add",
    body,
    { headers }
  );
  return response.data;
}

export interface LookupPatientResponse {
  id: number;
  primarySite?: { id: number; uid?: string | null; name?: string | null } | null;
  uid?: string | null;
  displayName?: string | null;
  status?: string | null;
  statusReason?: string | null;
  phone1?: { rawNumber?: string | null; number?: string | null; outOfService?: boolean } | null;
  phone2?: { rawNumber?: string | null; number?: string | null; outOfService?: boolean } | null;
  phone3?: { rawNumber?: string | null; number?: string | null; outOfService?: boolean } | null;
  phone4?: { rawNumber?: string | null; number?: string | null; outOfService?: boolean } | null;
  customFields?: unknown[] | null;
  firstName: string;
  middleName?: string | null;
  lastName: string;
  phoneticName?: string | null;
  preferredName?: string | null;
  title?: string | null;
  primaryEmail?: { email?: string; doNotEmail?: boolean } | null;
  secondaryEmail?: { email?: string; doNotEmail?: boolean } | null;
  country?: string | null;
  address1?: string | null;
  address2?: string | null;
  address3?: string | null;
  city?: string | null;
  state?: string | null;
  zip?: string | null;
  doNotMail?: boolean;
  recruitmentTextOptIn?: boolean;
  phoneTypeToText?: string | null;
  fax?: string | null;
  dateOfBirth?: string | null;
  dateOfDeath?: string | null;
  genderCode?: string | null;
  race?: string | null;
  ethnicity?: string | null;
  nativeLanguage?: string | null;
  maritalStatus?: string | null;
  weight?: { value?: number | null; unit?: string | null } | null;
  height?: { value?: number | null; unit?: string | null } | null;
  ssn?: string | null;
  mrn?: string | null;
  importId?: number | null;
  importSourceId?: string | null;
  importPatientId?: string | null;
  primaryInsurance?: unknown | null;
  secondaryInsurance?: unknown | null;
  managedMedicare?: boolean;
  guardian?: unknown | null;
  caregiverId?: number | null;
  caregiver?: boolean;
}

export async function lookupTestPatient(
  params: { id?: number; uid?: string; email?: string },
  adminKey?: string
): Promise<LookupPatientResponse> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const searchParams = new URLSearchParams();
  if (params.id != null) searchParams.set("id", String(params.id));
  if (params.uid) searchParams.set("uid", params.uid);
  if (params.email) searchParams.set("email", params.email);

  const response = await api.get<LookupPatientResponse>(
    `/api/v1/test-data/patients/lookup?${searchParams.toString()}`,
    { headers }
  );
  return response.data;
}

export async function getRandomTestPatient(adminKey?: string): Promise<LookupPatientResponse> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const response = await api.get<LookupPatientResponse>("/api/v1/test-data/patients/random", {
    headers
  });
  return response.data;
}

export async function updateTestPatient(
  id: number,
  body: LookupPatientResponse,
  adminKey?: string,
  saveWithAudit?: boolean
): Promise<LookupPatientResponse> {
  const headers: Record<string, string> = {};
  if (adminKey) {
    headers["X-Admin-Key"] = adminKey;
  }

  const suffix = saveWithAudit ? "?saveWithAudit=true" : "";
  const response = await api.put<LookupPatientResponse>(
    `/api/v1/test-data/patients/${id}${suffix}`,
    body,
    { headers }
  );
  return response.data;
}

