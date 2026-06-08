import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import MonitoringPage from "./MonitoringPage";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";
import { DEMO_MONITORING_DETAILS, DEMO_MONITORING_SUMMARIES } from "./demoData";

describe("MonitoringPage", () => {
  it("loads and displays monitoring requests and stats", async () => {
    server.use(
      http.get("*/api/v1/monitoring/requests", () =>
        HttpResponse.json([
          {
            id: 1,
            createdAtUtc: "2024-01-01T10:00:00Z",
            method: "GET",
            path: "/api/v1/health",
            statusCode: 200,
            durationMs: 12,
            origin: null
          }
        ])
      ),
      http.get("*/api/v1/monitoring/stats", () =>
        HttpResponse.json({
          statusBreakdown: [{ statusCode: 200, count: 1 }],
          requestCount: 1,
          averageDurationMs: 12,
          percentile95DurationMs: 12,
          maxDurationMs: 12
        })
      )
    );

    renderWithAdminSession(<MonitoringPage />);

    await waitFor(() => {
      // Path renders in two nodes (mobile subtitle + desktop cell); check at least one exists
      expect(screen.getAllByText("/api/v1/health").length).toBeGreaterThan(0);
      expect(screen.getByText("12.0 ms")).toBeInTheDocument();
    });
  });

  it("loads request detail when row is clicked", async () => {
    const user = userEvent.setup();

    server.use(
      http.get("*/api/v1/monitoring/requests/1", () =>
        HttpResponse.json({
          id: 1,
          createdAtUtc: "2024-01-01T10:00:00Z",
          method: "GET",
          path: "/api/v1/health",
          statusCode: 200,
          durationMs: 12,
          queryString: null,
          origin: null,
          referer: null,
          userAgent: "test-agent",
          remoteIp: "127.0.0.1",
          requestBody: null,
          responseBody: null,
          correlationId: "corr-1"
        })
      ),
      http.get("*/api/v1/monitoring/requests", () =>
        HttpResponse.json([
          {
            id: 1,
            createdAtUtc: "2024-01-01T10:00:00Z",
            method: "GET",
            path: "/api/v1/health",
            statusCode: 200,
            durationMs: 12,
            origin: null
          }
        ])
      ),
      http.get("*/api/v1/monitoring/stats", () =>
        HttpResponse.json({
          statusBreakdown: [{ statusCode: 200, count: 1 }],
          requestCount: 1,
          averageDurationMs: 12,
          percentile95DurationMs: 12,
          maxDurationMs: 12
        })
      )
    );

    renderWithAdminSession(<MonitoringPage />);
    // Path appears in two nodes; click the first match (the row's clickable area)
    await screen.findAllByText("/api/v1/health");

    await user.click(screen.getAllByText("/api/v1/health")[0]);

    await waitFor(() => {
      expect(screen.getByText(/correlation id: corr-1/i)).toBeInTheDocument();
    });
  });

  it("shows load error when monitoring API fails", async () => {
    server.use(
      http.get("*/api/v1/monitoring/requests", () => HttpResponse.json({}, { status: 500 })),
      http.get("*/api/v1/monitoring/stats", () =>
        HttpResponse.json({
          statusBreakdown: [],
          requestCount: 0,
          averageDurationMs: null,
          percentile95DurationMs: null,
          maxDurationMs: null
        })
      )
    );

    renderWithAdminSession(<MonitoringPage />);

    await waitFor(() => {
      expect(screen.getByText(/unable to load monitored requests/i)).toBeInTheDocument();
    });
  });

  it("demo mode: renders log entries from DEMO_MONITORING_SUMMARIES without calling the API", async () => {
    const getRequestsSpy = vi.fn();
    server.use(
      http.get("*/api/v1/monitoring/requests", () => {
        getRequestsSpy();
        return HttpResponse.json([]);
      })
    );

    renderInDemoMode(<MonitoringPage />);

    // Path renders in two nodes (mobile + desktop); verify at least one is present
    await waitFor(() => {
      expect(screen.getAllByText(DEMO_MONITORING_SUMMARIES[1].path).length).toBeGreaterThan(0);
    });
    expect(getRequestsSpy).not.toHaveBeenCalled();
  });

  it("demo mode: Refresh button does not trigger an API call", async () => {
    const user = userEvent.setup();
    const getRequestsSpy = vi.fn();
    server.use(
      http.get("*/api/v1/monitoring/requests", () => {
        getRequestsSpy();
        return HttpResponse.json([]);
      })
    );

    renderInDemoMode(<MonitoringPage />);

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /refresh/i })).toBeInTheDocument()
    );
    await user.click(screen.getByRole("button", { name: /refresh/i }));

    expect(getRequestsSpy).not.toHaveBeenCalled();
  });

  it("demo mode: renders stats from DEMO_MONITORING_STATS without calling the API", async () => {
    const getStatsSpy = vi.fn();
    server.use(
      http.get("*/api/v1/monitoring/stats", () => {
        getStatsSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<MonitoringPage />);

    // averageDurationMs=43 renders as "43.0 ms"; maxDurationMs=287 renders as "287 ms"
    await waitFor(() => {
      expect(screen.getByText("43.0 ms")).toBeInTheDocument();
    });
    expect(getStatsSpy).not.toHaveBeenCalled();
  });

  it("demo mode: clicking a row shows detail panel from DEMO_MONITORING_DETAILS without API call", async () => {
    const user = userEvent.setup();
    const getDetailSpy = vi.fn();
    server.use(
      http.get("*/api/v1/monitoring/requests/:id", () => {
        getDetailSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<MonitoringPage />);

    // Wait for the list to render, then click the first row
    const firstEntry = DEMO_MONITORING_SUMMARIES[0];
    await waitFor(() => expect(screen.getAllByText(firstEntry.path).length).toBeGreaterThan(0));
    await user.click(screen.getAllByText(firstEntry.path)[0]);

    // The detail panel should show the correlation ID from DEMO_MONITORING_DETAILS
    const expectedCorrelationId = DEMO_MONITORING_DETAILS[firstEntry.id].correlationId!;
    await waitFor(() => {
      expect(screen.getByText(new RegExp(expectedCorrelationId, "i"))).toBeInTheDocument();
    });
    expect(getDetailSpy).not.toHaveBeenCalled();
  });
});
