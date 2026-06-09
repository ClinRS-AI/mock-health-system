import type {
  AuthSettings,
  MonitoredRequestDetail,
  MonitoredRequestSummary,
  MonitoringStats,
  PatientTestDataStats
} from "./api";

export const DEMO_AUTH_SETTINGS: AuthSettings = {
  mode: "CCAPIKey",
  bearerToken: null,
  oAuthClientId: null,
  oAuthClientSecret: null,
  accessTokenLifetimeMinutes: 60,
  refreshTokenLifetimeDays: 30,
  hasAnyTokens: true,
  rateLimitEnabled: true,
  rateLimitPerSecond: 10,
  rateLimitPerMinute: 300
};

// 25 realistic request log entries spanning the past 24 hours
const now = new Date();
function minsAgo(minutes: number): string {
  return new Date(now.getTime() - minutes * 60_000).toISOString();
}

export const DEMO_MONITORING_SUMMARIES: MonitoredRequestSummary[] = [
  { id: 1,  createdAtUtc: minsAgo(3),   method: "GET",  path: "/api/v1/patients",                          statusCode: 200, durationMs: 38,  origin: "http://localhost:5176" },
  { id: 2,  createdAtUtc: minsAgo(7),   method: "GET",  path: "/api/v1/patients/142",                      statusCode: 200, durationMs: 24,  origin: "http://localhost:5176" },
  { id: 3,  createdAtUtc: minsAgo(11),  method: "GET",  path: "/api/v1/patients/142/conditions",           statusCode: 200, durationMs: 51,  origin: "http://localhost:5176" },
  { id: 4,  createdAtUtc: minsAgo(14),  method: "GET",  path: "/api/v1/health",                            statusCode: 200, durationMs: 8,   origin: "http://localhost:5176" },
  { id: 5,  createdAtUtc: minsAgo(22),  method: "POST", path: "/api/v1/test-data/patients/generate",       statusCode: 204, durationMs: 287, origin: "http://localhost:5176" },
  { id: 6,  createdAtUtc: minsAgo(31),  method: "GET",  path: "/api/v1/patients",                          statusCode: 200, durationMs: 42,  origin: "http://localhost:5176" },
  { id: 7,  createdAtUtc: minsAgo(45),  method: "GET",  path: "/api/v1/patients/88",                       statusCode: 200, durationMs: 29,  origin: "http://localhost:5176" },
  { id: 8,  createdAtUtc: minsAgo(52),  method: "GET",  path: "/api/v1/patients/88/medications",           statusCode: 200, durationMs: 44,  origin: "http://localhost:5176" },
  { id: 9,  createdAtUtc: minsAgo(67),  method: "GET",  path: "/api/v1/patients/999",                      statusCode: 404, durationMs: 12,  origin: "http://localhost:5176" },
  { id: 10, createdAtUtc: minsAgo(78),  method: "GET",  path: "/api/v1/auth-settings",                     statusCode: 401, durationMs: 9,   origin: "http://localhost:5176" },
  { id: 11, createdAtUtc: minsAgo(93),  method: "POST", path: "/api/v1/admin/sessions",                    statusCode: 200, durationMs: 18,  origin: "http://localhost:5176" },
  { id: 12, createdAtUtc: minsAgo(104), method: "GET",  path: "/api/v1/auth-settings",                     statusCode: 200, durationMs: 15,  origin: "http://localhost:5176" },
  { id: 13, createdAtUtc: minsAgo(118), method: "GET",  path: "/api/v1/patients",                          statusCode: 200, durationMs: 37,  origin: "http://localhost:5176" },
  { id: 14, createdAtUtc: minsAgo(135), method: "GET",  path: "/api/v1/patients/33/procedures",            statusCode: 200, durationMs: 61,  origin: "http://localhost:5176" },
  { id: 15, createdAtUtc: minsAgo(147), method: "GET",  path: "/api/v1/patients/33/encounters",            statusCode: 200, durationMs: 55,  origin: "http://localhost:5176" },
  { id: 16, createdAtUtc: minsAgo(162), method: "POST", path: "/api/v1/test-data/patients/reset",          statusCode: 204, durationMs: 112, origin: "http://localhost:5176" },
  { id: 17, createdAtUtc: minsAgo(178), method: "GET",  path: "/api/v1/monitoring/requests",               statusCode: 200, durationMs: 33,  origin: "http://localhost:5176" },
  { id: 18, createdAtUtc: minsAgo(195), method: "GET",  path: "/api/v1/monitoring/stats",                  statusCode: 200, durationMs: 27,  origin: "http://localhost:5176" },
  { id: 19, createdAtUtc: minsAgo(223), method: "GET",  path: "/api/v1/patients/200",                      statusCode: 404, durationMs: 11,  origin: "http://localhost:5176" },
  { id: 20, createdAtUtc: minsAgo(241), method: "GET",  path: "/api/v1/patients",                          statusCode: 200, durationMs: 40,  origin: "http://localhost:5176" },
  { id: 21, createdAtUtc: minsAgo(268), method: "GET",  path: "/api/v1/patients/57/conditions",            statusCode: 200, durationMs: 48,  origin: "http://localhost:5176" },
  { id: 22, createdAtUtc: minsAgo(302), method: "GET",  path: "/api/v1/health",                            statusCode: 200, durationMs: 7,   origin: "http://localhost:5176" },
  { id: 23, createdAtUtc: minsAgo(355), method: "GET",  path: "/api/v1/patients/57",                       statusCode: 200, durationMs: 22,  origin: "http://localhost:5176" },
  { id: 24, createdAtUtc: minsAgo(412), method: "POST", path: "/api/v1/test-data/staff/generate",          statusCode: 200, durationMs: 95,  origin: "http://localhost:5176" },
  { id: 25, createdAtUtc: minsAgo(478), method: "GET",  path: "/api/v1/test-data/patients/stats",          statusCode: 200, durationMs: 31,  origin: "http://localhost:5176" }
];

export const DEMO_MONITORING_STATS: MonitoringStats = {
  requestCount: 147,
  averageDurationMs: 43,
  percentile95DurationMs: 112,
  maxDurationMs: 287,
  statusBreakdown: [
    { statusCode: 200, count: 130 },
    { statusCode: 204, count: 7 },
    { statusCode: 401, count: 6 },
    { statusCode: 404, count: 3 },
    { statusCode: 500, count: 1 }
  ]
};

function baseDetail(
  s: MonitoredRequestSummary,
  extra: Partial<MonitoredRequestDetail> = {}
): MonitoredRequestDetail {
  return {
    queryString: null,
    referer: null,
    userAgent: "ClinRSIntegration/2.1.0 (.NET/8.0)",
    remoteIp: "10.10.5.12",
    requestBody: null,
    responseBody: null,
    correlationId: `demo-corr-${String(s.id).padStart(3, "0")}`,
    ...s,
    ...extra
  };
}

const PATIENT_142 = JSON.stringify({
  id: 142, uid: "PAT-2024-0142", firstName: "Sarah", lastName: "Chen",
  dateOfBirth: "1985-03-12", gender: "Female", email: "sarah.chen@example.com",
  siteCode: "SITE-01", enrollmentStatus: "Active"
}, null, 2);

const PATIENT_88 = JSON.stringify({
  id: 88, uid: "PAT-2024-0088", firstName: "Marcus", lastName: "Okafor",
  dateOfBirth: "1972-07-28", gender: "Male", email: "marcus.okafor@example.com",
  siteCode: "SITE-02", enrollmentStatus: "Active"
}, null, 2);

const PATIENT_57 = JSON.stringify({
  id: 57, uid: "PAT-2023-0057", firstName: "Elena", lastName: "Vasquez",
  dateOfBirth: "1990-11-05", gender: "Female", email: "elena.vasquez@example.com",
  siteCode: "SITE-01", enrollmentStatus: "Screening"
}, null, 2);

const PATIENTS_LIST = JSON.stringify([
  { id: 142, uid: "PAT-2024-0142", firstName: "Sarah", lastName: "Chen", siteCode: "SITE-01", enrollmentStatus: "Active" },
  { id: 88,  uid: "PAT-2024-0088", firstName: "Marcus", lastName: "Okafor", siteCode: "SITE-02", enrollmentStatus: "Active" },
  { id: 57,  uid: "PAT-2023-0057", firstName: "Elena", lastName: "Vasquez", siteCode: "SITE-01", enrollmentStatus: "Screening" },
  { id: 33,  uid: "PAT-2023-0033", firstName: "David", lastName: "Nguyen", siteCode: "SITE-03", enrollmentStatus: "Completed" }
], null, 2);

const detailOverrides: Record<number, Partial<MonitoredRequestDetail>> = {
  1: {  // GET /api/v1/patients
    userAgent: "PostmanRuntime/7.36.0",
    responseBody: PATIENTS_LIST
  },
  2: {  // GET /api/v1/patients/142
    userAgent: "PostmanRuntime/7.36.0",
    responseBody: PATIENT_142
  },
  3: {  // GET /api/v1/patients/142/conditions
    responseBody: JSON.stringify([
      { id: 1, code: "E11.9", display: "Type 2 diabetes mellitus without complications", system: "ICD-10-CM", onsetDate: "2018-06-01", status: "Active" },
      { id: 2, code: "I10",   display: "Essential (primary) hypertension", system: "ICD-10-CM", onsetDate: "2020-03-15", status: "Active" }
    ], null, 2)
  },
  4: {  // GET /api/v1/health
    userAgent: "curl/8.6.0",
    remoteIp: "192.168.1.50",
    responseBody: JSON.stringify({ status: "Healthy", database: "Connected", version: "1.0.0" }, null, 2)
  },
  5: {  // POST /api/v1/test-data/patients/generate
    userAgent: "curl/8.6.0",
    remoteIp: "192.168.1.50",
    requestBody: JSON.stringify({ totalCount: 5000, duplicatePercentage: 3 }, null, 2),
    responseBody: JSON.stringify({
      totalRequested: 5000, totalBaseInserted: 5000,
      duplicateRequested: 150, duplicateInserted: 150, totalAfter: 5150
    }, null, 2)
  },
  8: {  // GET /api/v1/patients/88/medications
    responseBody: JSON.stringify([
      { id: 1, rxNorm: "860975", display: "Metformin 500 MG Oral Tablet", frequency: "BID", startDate: "2018-07-01", status: "Active" },
      { id: 2, rxNorm: "308460", display: "Lisinopril 10 MG Oral Tablet",  frequency: "QD",  startDate: "2020-04-01", status: "Active" }
    ], null, 2)
  },
  9: {  // GET /api/v1/patients/999 - 404
    responseBody: JSON.stringify({ type: "https://tools.ietf.org/html/rfc9110#section-15.5.5", title: "Not Found", status: 404 }, null, 2)
  },
  11: { // POST /api/v1/admin/sessions - minted JWT
    requestBody: JSON.stringify({ adminKey: "••••••••" }, null, 2),
    responseBody: JSON.stringify({ accessToken: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9…[truncated]", expiresAtUtc: "2024-01-01T11:00:00Z" }, null, 2)
  },
  12: { // GET /api/v1/auth-settings - 200
    responseBody: JSON.stringify({
      mode: "CCAPIKey", hasAnyTokens: true,
      accessTokenLifetimeMinutes: 60, refreshTokenLifetimeDays: 30
    }, null, 2)
  },
  13: { // GET /api/v1/patients
    responseBody: PATIENTS_LIST
  },
  14: { // GET /api/v1/patients/33/procedures
    responseBody: JSON.stringify([
      { id: 1, code: "71046", display: "Radiologic examination, chest; 2 views", system: "CPT", performedDate: "2023-09-10", status: "Completed" }
    ], null, 2)
  },
  15: { // GET /api/v1/patients/33/encounters
    responseBody: JSON.stringify([
      { id: 1, type: "Outpatient", date: "2023-09-10", providerId: "PROV-004", facilityCode: "SITE-03", notes: "Routine follow-up" }
    ], null, 2)
  },
  16: { // POST /api/v1/test-data/patients/reset - 204
    userAgent: "curl/8.6.0",
    remoteIp: "192.168.1.50",
    requestBody: null,
    responseBody: null
  },
  17: { // GET /api/v1/monitoring/requests
    queryString: "?limit=50&offset=0",
    responseBody: JSON.stringify([
      { id: 1, method: "GET", path: "/api/v1/patients", statusCode: 200, durationMs: 38 }
    ], null, 2)
  },
  18: { // GET /api/v1/monitoring/stats
    responseBody: JSON.stringify({
      requestCount: 147, averageDurationMs: 43, percentile95DurationMs: 112, maxDurationMs: 287,
      statusBreakdown: [{ statusCode: 200, count: 130 }, { statusCode: 204, count: 7 }]
    }, null, 2)
  },
  19: { // GET /api/v1/patients/200 - 404
    responseBody: JSON.stringify({ type: "https://tools.ietf.org/html/rfc9110#section-15.5.5", title: "Not Found", status: 404 }, null, 2)
  },
  20: { // GET /api/v1/patients
    responseBody: PATIENTS_LIST
  },
  21: { // GET /api/v1/patients/57/conditions
    responseBody: JSON.stringify([
      { id: 3, code: "J45.20", display: "Mild intermittent asthma, uncomplicated", system: "ICD-10-CM", onsetDate: "2005-02-14", status: "Active" }
    ], null, 2)
  },
  22: { // GET /api/v1/health
    userAgent: "curl/8.6.0",
    remoteIp: "192.168.1.50",
    responseBody: JSON.stringify({ status: "Healthy", database: "Connected", version: "1.0.0" }, null, 2)
  },
  23: { // GET /api/v1/patients/57
    responseBody: PATIENT_57
  },
  24: { // POST /api/v1/test-data/staff/generate
    userAgent: "curl/8.6.0",
    remoteIp: "192.168.1.50",
    requestBody: JSON.stringify({ count: 10 }, null, 2),
    responseBody: JSON.stringify({ inserted: 10, totalAfter: 18 }, null, 2)
  },
  25: { // GET /api/v1/test-data/patients/stats
    responseBody: JSON.stringify({
      patientCount: 47, duplicatePatientCount: 12, recentAuditEventCount: 234,
      totalStaffCount: 8, patientsBySite: [{ siteName: "General Hospital", count: 18 }]
    }, null, 2)
  },
  6:  { responseBody: PATIENTS_LIST },
  7:  { responseBody: PATIENT_88 },
  10: { responseBody: null }  // 401 — no body
};

// Pre-built detail records keyed by request ID, used in demo mode row expansion
export const DEMO_MONITORING_DETAILS: Record<number, MonitoredRequestDetail> =
  Object.fromEntries(
    DEMO_MONITORING_SUMMARIES.map((s) => [s.id, baseDetail(s, detailOverrides[s.id] ?? {})])
  );

export const DEMO_TEST_DATA_STATS: PatientTestDataStats = {
  patientCount: 47,
  duplicatePatientCount: 12,
  recentAuditEventCount: 234,
  totalStaffCount: 8,
  patientsBySite: [
    { siteName: "General Hospital", count: 18 },
    { siteName: "North Clinic", count: 14 },
    { siteName: "East Medical Center", count: 10 },
    { siteName: "West Branch", count: 5 }
  ]
};
