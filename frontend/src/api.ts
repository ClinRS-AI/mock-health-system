import axios from "axios";
import { getAdminSessionToken } from "./adminSessionStore";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL
});

/** Dedicated client for minting admin sessions (no X-Admin-Session interceptor). */
const mintApi = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL
});

/** Normalized API origin from env (no trailing slash), for display and composing paths like `/soap/report`. */
export function getConfiguredApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL as string | undefined;
  return (raw ?? "").trim().replace(/\/+$/, "");
}

api.interceptors.request.use((config) => {
  const token = getAdminSessionToken();
  if (token) {
    config.headers.set("X-Admin-Session", token);
  }
  return config;
});

/** Calls the versioned health endpoint (GET /api/v1/health). */
export async function getApiStatus(): Promise<string> {
  const response = await api.get<string>("/api/v1/health");
  return typeof response.data === "string" ? response.data : JSON.stringify(response.data);
}

export interface CreateAdminSessionResponse {
  accessToken: string;
  expiresAtUtc: string;
}

export async function exchangeAdminSession(adminKey: string): Promise<CreateAdminSessionResponse> {
  const response = await mintApi.post<CreateAdminSessionResponse>("/api/v1/admin/sessions", {
    adminKey
  });
  return response.data;
}

/**
 * Probes whether the server requires an admin key by calling HEAD /api/v1/auth-settings
 * without a session header. Returns true when a key is required (401/403 or network error),
 * false in open/local-dev mode (200).
 */
export async function probeAdminKeyRequired(): Promise<boolean> {
  try {
    const response = await mintApi.head("/api/v1/auth-settings", { validateStatus: () => true });
    return response.status === 401 || response.status === 403;
  } catch {
    return true;
  }
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
  rateLimitEnabled: boolean;
  rateLimitPerSecond: number;
  rateLimitPerMinute: number;
}

export interface UpdateAuthSettingsRequest {
  mode: AuthMode;
  bearerToken?: string | null;
  oAuthClientId?: string | null;
  oAuthClientSecret?: string | null;
  accessTokenLifetimeMinutes?: number;
  refreshTokenLifetimeDays?: number;
  rateLimitEnabled?: boolean;
  rateLimitPerSecond?: number;
  rateLimitPerMinute?: number;
}

export async function getAuthSettings(): Promise<AuthSettings> {
  const response = await api.get<AuthSettings>("/api/v1/auth-settings");
  return response.data;
}

export async function updateAuthSettings(payload: UpdateAuthSettingsRequest): Promise<AuthSettings> {
  const response = await api.put<AuthSettings>("/api/v1/auth-settings", payload);
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
  params?: GetMonitoredRequestsParams
): Promise<MonitoredRequestSummary[]> {
  const searchParams = new URLSearchParams();
  if (params?.take != null) searchParams.set("take", String(params.take));
  if (params?.pathPrefix) searchParams.set("pathPrefix", params.pathPrefix);
  if (params?.statusCode != null) searchParams.set("statusCode", String(params.statusCode));
  if (params?.sinceUtc) searchParams.set("sinceUtc", params.sinceUtc);

  const query = searchParams.toString();
  const url = `/api/v1/monitoring/requests${query ? `?${query}` : ""}`;

  const response = await api.get<MonitoredRequestSummary[]>(url);
  return response.data;
}

export async function getMonitoredRequest(id: number): Promise<MonitoredRequestDetail> {
  const response = await api.get<MonitoredRequestDetail>(`/api/v1/monitoring/requests/${id}`);
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

export async function getMonitoringStats(): Promise<MonitoringStats> {
  const response = await api.get<MonitoringStats>("/api/v1/monitoring/stats");
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
  options: GeneratePatientsOptions
): Promise<GeneratePatientsResult> {
  const response = await api.post<GeneratePatientsResult>(
    "/api/v1/test-data/patients/generate",
    options
  );
  return response.data;
}

export async function getPatientTestDataStats(): Promise<PatientTestDataStats> {
  const response = await api.get<PatientTestDataStats>("/api/v1/test-data/patients/stats");
  return response.data;
}

export interface SoapReportPkeysResponse {
  pkeys: string[];
}

export async function getSoapReportPkeys(): Promise<SoapReportPkeysResponse> {
  const response = await api.get<SoapReportPkeysResponse>("/api/v1/test-data/soap/report-pkeys");
  return response.data;
}

export async function generateTestStaff(options: GenerateStaffOptions): Promise<GenerateStaffResult> {
  const response = await api.post<GenerateStaffResult>("/api/v1/test-data/staff/generate", options);
  return response.data;
}

export async function generateRecentAuditEvents(
  options: GenerateRecentAuditEventsOptions
): Promise<GenerateRecentAuditEventsResult> {
  const response = await api.post<GenerateRecentAuditEventsResult>(
    "/api/v1/test-data/audit-events/generate",
    options
  );
  return response.data;
}

export async function resetTestPatients(): Promise<void> {
  await api.post("/api/v1/test-data/patients/reset", {});
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

export async function addTestPatient(body: AddTestPatientRequest): Promise<AddTestPatientResponse> {
  const response = await api.post<AddTestPatientResponse>("/api/v1/test-data/patients/add", body);
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

export async function lookupTestPatient(params: {
  id?: number;
  uid?: string;
  email?: string;
}): Promise<LookupPatientResponse> {
  const searchParams = new URLSearchParams();
  if (params.id != null) searchParams.set("id", String(params.id));
  if (params.uid) searchParams.set("uid", params.uid);
  if (params.email) searchParams.set("email", params.email);

  const response = await api.get<LookupPatientResponse>(
    `/api/v1/test-data/patients/lookup?${searchParams.toString()}`
  );
  return response.data;
}

export async function getRandomTestPatient(): Promise<LookupPatientResponse> {
  const response = await api.get<LookupPatientResponse>("/api/v1/test-data/patients/random");
  return response.data;
}

export async function updateTestPatient(
  id: number,
  body: LookupPatientResponse,
  saveWithAudit?: boolean
): Promise<LookupPatientResponse> {
  const suffix = saveWithAudit ? "?saveWithAudit=true" : "";
  const response = await api.put<LookupPatientResponse>(
    `/api/v1/test-data/patients/${id}${suffix}`,
    body
  );
  return response.data;
}
