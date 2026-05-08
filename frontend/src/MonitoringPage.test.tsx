import { describe, it, expect } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import MonitoringPage from "./MonitoringPage";
import { server } from "./test/server";

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

    render(<MonitoringPage />);

    await waitFor(() => {
      expect(screen.getByText("/api/v1/health")).toBeInTheDocument();
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

    render(<MonitoringPage />);
    await screen.findByText("/api/v1/health");

    await user.click(screen.getByText("/api/v1/health"));

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

    render(<MonitoringPage />);

    await waitFor(() => {
      expect(screen.getByText(/unable to load monitored requests/i)).toBeInTheDocument();
    });
  });
});
