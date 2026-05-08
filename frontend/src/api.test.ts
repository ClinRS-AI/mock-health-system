import { describe, it, expect } from "vitest";
import { http, HttpResponse } from "msw";
import {
  getApiStatus,
  getAuthSettings,
  updateAuthSettings,
  getMonitoredRequests,
  getMonitoredRequest,
  getMonitoringStats,
  generateTestPatients,
  getPatientTestDataStats,
  generateTestStaff,
  generateRecentAuditEvents,
  resetTestPatients,
  addTestPatient,
  lookupTestPatient,
  getRandomTestPatient,
  updateTestPatient
} from "./api";
import { server } from "./test/server";

// ---- getApiStatus ----

describe("getApiStatus", () => {
  it("returns string response from health endpoint", async () => {
    server.use(http.get("*/api/v1/health", () => HttpResponse.text("Healthy")));

    const result = await getApiStatus();

    expect(result).toBe("Healthy");
  });

  it("stringifies object response when backend returns object", async () => {
    server.use(
      http.get("*/api/v1/health", () =>
        HttpResponse.json({ status: "ok", version: "1.0" })
      )
    );

    const result = await getApiStatus();

    expect(result).toBe('{"status":"ok","version":"1.0"}');
  });
});

// ---- getAuthSettings ----

describe("getAuthSettings", () => {
  const settings = {
    mode: "None" as const,
    accessTokenLifetimeMinutes: 60,
    refreshTokenLifetimeDays: 30,
    hasAnyTokens: false
  };

  it("calls the correct endpoint and returns settings", async () => {
    server.use(http.get("*/api/v1/auth-settings", () => HttpResponse.json(settings)));

    const result = await getAuthSettings();

    expect(result).toEqual(settings);
  });

  it("includes X-Admin-Key header when adminKey is provided", async () => {
    server.use(
      http.get("*/api/v1/auth-settings", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("my-admin-key");
        return HttpResponse.json(settings);
      })
    );

    await getAuthSettings("my-admin-key");
  });

  it("sends no X-Admin-Key header when adminKey is omitted", async () => {
    server.use(
      http.get("*/api/v1/auth-settings", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBeNull();
        return HttpResponse.json(settings);
      })
    );

    await getAuthSettings();
  });
});

// ---- updateAuthSettings ----

describe("updateAuthSettings", () => {
  const payload = { mode: "Bearer" as const, bearerToken: "tok", accessTokenLifetimeMinutes: 60, refreshTokenLifetimeDays: 30 };
  const returned = { ...payload, hasAnyTokens: true };

  it("sends PUT to correct endpoint with payload", async () => {
    server.use(
      http.put("*/api/v1/auth-settings", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBeNull();
        const body = await request.json();
        expect(body).toEqual(payload);
        return HttpResponse.json(returned);
      })
    );

    const result = await updateAuthSettings(payload);

    expect(result).toEqual(returned);
  });

  it("includes X-Admin-Key header when provided", async () => {
    server.use(
      http.put("*/api/v1/auth-settings", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("secret");
        const body = await request.json();
        expect(body).toEqual(payload);
        return HttpResponse.json(returned);
      })
    );

    await updateAuthSettings(payload, "secret");
  });
});

// ---- getMonitoredRequests ----

describe("getMonitoredRequests", () => {
  const requests = [
    {
      id: 1,
      method: "GET",
      path: "/api/v1/health",
      statusCode: 200,
      durationMs: 5,
      createdAtUtc: "2026-01-01T00:00:00Z"
    }
  ];

  it("calls the base requests URL when no params provided", async () => {
    server.use(
      http.get("*/api/v1/monitoring/requests", ({ request }) => {
        expect(new URL(request.url).search).toBe("");
        expect(request.headers.get("X-Admin-Key")).toBeNull();
        return HttpResponse.json(requests);
      })
    );

    await getMonitoredRequests();
  });

  it("appends take param to URL", async () => {
    server.use(
      http.get("*/api/v1/monitoring/requests", ({ request }) => {
        const q = new URL(request.url).searchParams;
        expect(q.get("take")).toBe("50");
        return HttpResponse.json(requests);
      })
    );

    await getMonitoredRequests({ take: 50 });
  });

  it("appends pathPrefix, statusCode, sinceUtc params to URL", async () => {
    server.use(
      http.get("*/api/v1/monitoring/requests", ({ request }) => {
        const q = new URL(request.url).searchParams;
        expect(q.get("pathPrefix")).toBe("/api");
        expect(q.get("statusCode")).toBe("200");
        expect(q.get("sinceUtc")).toBe("2026-01-01T00:00:00Z");
        return HttpResponse.json(requests);
      })
    );

    await getMonitoredRequests({
      pathPrefix: "/api",
      statusCode: 200,
      sinceUtc: "2026-01-01T00:00:00Z"
    });
  });

  it("includes X-Admin-Key header when provided", async () => {
    server.use(
      http.get("*/api/v1/monitoring/requests", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        return HttpResponse.json(requests);
      })
    );

    await getMonitoredRequests({}, "admin");
  });

  it("returns the response data array", async () => {
    server.use(http.get("*/api/v1/monitoring/requests", () => HttpResponse.json(requests)));

    const result = await getMonitoredRequests();

    expect(result).toEqual(requests);
  });
});

// ---- getMonitoredRequest ----

describe("getMonitoredRequest", () => {
  it("calls the correct detail endpoint", async () => {
    const detail = {
      id: 42,
      method: "POST",
      path: "/api/v1/patients",
      statusCode: 201,
      durationMs: 20,
      createdAtUtc: ""
    };
    server.use(
      http.get("*/api/v1/monitoring/requests/42", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBeNull();
        return HttpResponse.json(detail);
      })
    );

    const result = await getMonitoredRequest(42);

    expect(result).toEqual(detail);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.get("*/api/v1/monitoring/requests/1", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin-secret");
        return HttpResponse.json({});
      })
    );

    await getMonitoredRequest(1, "admin-secret");
  });
});

// ---- getMonitoringStats ----

describe("getMonitoringStats", () => {
  const stats = {
    statusBreakdown: [],
    requestCount: 10,
    averageDurationMs: 15
  };

  it("calls the stats endpoint and returns data", async () => {
    server.use(
      http.get("*/api/v1/monitoring/stats", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBeNull();
        return HttpResponse.json(stats);
      })
    );

    const result = await getMonitoringStats();

    expect(result).toEqual(stats);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.get("*/api/v1/monitoring/stats", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        return HttpResponse.json(stats);
      })
    );

    await getMonitoringStats("admin");
  });
});

// ---- generateTestPatients ----

describe("generateTestPatients", () => {
  const result = {
    totalRequested: 100,
    totalBaseInserted: 100,
    duplicateRequested: 3,
    duplicateInserted: 3,
    totalAfter: 103
  };

  it("posts to the correct endpoint with options", async () => {
    server.use(
      http.post("*/api/v1/test-data/patients/generate", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBeNull();
        const body = await request.json();
        expect(body).toEqual({ totalCount: 100, duplicatePercentage: 3, seed: 42 });
        return HttpResponse.json(result);
      })
    );

    const options = { totalCount: 100, duplicatePercentage: 3, seed: 42 };
    const returned = await generateTestPatients(options);

    expect(returned).toEqual(result);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.post("*/api/v1/test-data/patients/generate", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        const body = await request.json();
        expect(body).toEqual({});
        return HttpResponse.json(result);
      })
    );

    await generateTestPatients({}, "admin");
  });
});

// ---- getPatientTestDataStats ----

describe("getPatientTestDataStats", () => {
  const stats = {
    patientCount: 5000,
    duplicatePatientCount: 150,
    recentAuditEventCount: 25,
    totalStaffCount: 10,
    patientsBySite: []
  };

  it("calls the stats endpoint and returns data", async () => {
    server.use(http.get("*/api/v1/test-data/patients/stats", () => HttpResponse.json(stats)));

    const result = await getPatientTestDataStats();

    expect(result).toEqual(stats);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/stats", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        return HttpResponse.json(stats);
      })
    );

    await getPatientTestDataStats("admin");
  });
});

// ---- generateTestStaff ----

describe("generateTestStaff", () => {
  const result = { requested: 10, inserted: 10, totalAfter: 10 };

  it("posts to the staff generate endpoint with options", async () => {
    server.use(
      http.post("*/api/v1/test-data/staff/generate", async ({ request }) => {
        const body = await request.json();
        expect(body).toEqual({ count: 10, seed: 7 });
        return HttpResponse.json(result);
      })
    );

    const returned = await generateTestStaff({ count: 10, seed: 7 });

    expect(returned).toEqual(result);
  });
});

// ---- generateRecentAuditEvents ----

describe("generateRecentAuditEvents", () => {
  const result = { requested: 25, inserted: 25, totalAfter: 25, insertedByAuditType: [] };

  it("posts to the audit-events generate endpoint with options", async () => {
    server.use(
      http.post("*/api/v1/test-data/audit-events/generate", async ({ request }) => {
        const body = await request.json();
        expect(body).toEqual({ count: 25, seed: 3 });
        return HttpResponse.json(result);
      })
    );

    const returned = await generateRecentAuditEvents({ count: 25, seed: 3 });

    expect(returned).toEqual(result);
  });
});

// ---- resetTestPatients ----

describe("resetTestPatients", () => {
  it("posts to the reset endpoint", async () => {
    server.use(
      http.post("*/api/v1/test-data/patients/reset", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBeNull();
        const body = await request.json();
        expect(body).toEqual({});
        return new HttpResponse(null, { status: 204 });
      })
    );

    await resetTestPatients();
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.post("*/api/v1/test-data/patients/reset", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        const body = await request.json();
        expect(body).toEqual({});
        return new HttpResponse(null, { status: 204 });
      })
    );

    await resetTestPatients("admin");
  });
});

// ---- addTestPatient ----

describe("addTestPatient", () => {
  const response = { id: 42, uid: "00000000-0000-0000-0000-000000000001" };

  it("posts to the add endpoint with body", async () => {
    server.use(
      http.post("*/api/v1/test-data/patients/add", async ({ request }) => {
        const body = await request.json();
        expect(body).toEqual({ firstName: "Alice", lastName: "Smith", email: "alice@example.com" });
        return HttpResponse.json(response);
      })
    );

    const body = { firstName: "Alice", lastName: "Smith", email: "alice@example.com" };
    const result = await addTestPatient(body);

    expect(result).toEqual(response);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.post("*/api/v1/test-data/patients/add", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        const body = await request.json();
        expect(body).toEqual({ firstName: "A", lastName: "B", email: "a@b.com" });
        return HttpResponse.json(response);
      })
    );

    await addTestPatient({ firstName: "A", lastName: "B", email: "a@b.com" }, "admin");
  });
});

// ---- lookupTestPatient ----

describe("lookupTestPatient", () => {
  const patient = { id: 7, firstName: "Test", lastName: "Patient" };

  it("builds URL with id param when id is provided", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/lookup", ({ request }) => {
        const q = new URL(request.url).searchParams;
        expect(q.get("id")).toBe("7");
        return HttpResponse.json(patient);
      })
    );

    await lookupTestPatient({ id: 7 });
  });

  it("builds URL with uid param when uid is provided", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/lookup", ({ request }) => {
        const q = new URL(request.url).searchParams;
        expect(q.get("uid")).toBe("some-uid-string");
        return HttpResponse.json(patient);
      })
    );

    await lookupTestPatient({ uid: "some-uid-string" });
  });

  it("builds URL with email param when email is provided", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/lookup", ({ request }) => {
        const q = new URL(request.url).searchParams;
        expect(q.get("email")).toBe("test@example.com");
        return HttpResponse.json(patient);
      })
    );

    await lookupTestPatient({ email: "test@example.com" });
  });

  it("calls the lookup endpoint and returns patient data", async () => {
    server.use(http.get("*/api/v1/test-data/patients/lookup", () => HttpResponse.json(patient)));

    const result = await lookupTestPatient({ id: 7 });

    expect(result).toEqual(patient);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/lookup", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        return HttpResponse.json(patient);
      })
    );

    await lookupTestPatient({ id: 1 }, "admin");
  });
});

// ---- getRandomTestPatient ----

describe("getRandomTestPatient", () => {
  const patient = { id: 3, firstName: "Random", lastName: "Patient" };

  it("calls the random patient endpoint", async () => {
    server.use(http.get("*/api/v1/test-data/patients/random", () => HttpResponse.json(patient)));

    const result = await getRandomTestPatient();

    expect(result).toEqual(patient);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/random", ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin");
        return HttpResponse.json({});
      })
    );

    await getRandomTestPatient("admin");
  });
});

// ---- updateTestPatient ----

describe("updateTestPatient", () => {
  const patientBody = { id: 5, firstName: "Updated", lastName: "Patient" } as Parameters<
    typeof updateTestPatient
  >[1];
  const returned = { ...patientBody };

  it("puts to the correct patient endpoint", async () => {
    server.use(
      http.put("*/api/v1/test-data/patients/5", async ({ request }) => {
        expect(request.url.endsWith("/api/v1/test-data/patients/5")).toBe(true);
        expect(new URL(request.url).search).toBe("");
        const body = await request.json();
        expect(body).toEqual(patientBody);
        return HttpResponse.json(returned);
      })
    );

    const result = await updateTestPatient(5, patientBody);

    expect(result).toEqual(returned);
  });

  it("appends saveWithAudit=true query param when flag is set", async () => {
    server.use(
      http.put("*/api/v1/test-data/patients/5", ({ request }) => {
        expect(new URL(request.url).searchParams.get("saveWithAudit")).toBe("true");
        return HttpResponse.json(returned);
      })
    );

    await updateTestPatient(5, patientBody, undefined, true);
  });

  it("does not append saveWithAudit param when flag is false", async () => {
    server.use(
      http.put("*/api/v1/test-data/patients/5", ({ request }) => {
        expect(new URL(request.url).searchParams.has("saveWithAudit")).toBe(false);
        return HttpResponse.json(returned);
      })
    );

    await updateTestPatient(5, patientBody, undefined, false);
  });

  it("sends X-Admin-Key header when provided", async () => {
    server.use(
      http.put("*/api/v1/test-data/patients/5", async ({ request }) => {
        expect(request.headers.get("X-Admin-Key")).toBe("admin-key");
        return HttpResponse.json(returned);
      })
    );

    await updateTestPatient(5, patientBody, "admin-key");
  });
});
