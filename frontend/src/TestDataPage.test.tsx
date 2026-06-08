import { describe, it, expect, vi, beforeEach } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import TestDataPage from "./TestDataPage";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";
import { DEMO_TEST_DATA_STATS } from "./demoData";

describe("TestDataPage", () => {
  beforeEach(() => {
    server.use(
      http.get("*/api/v1/test-data/soap/report-pkeys", () =>
        HttpResponse.json({ pkeys: ["PATIENT_COUNT"] })
      )
    );
  });

  it("loads stats and shows key summary values", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () =>
        HttpResponse.json({
          patientCount: 1000,
          duplicatePatientCount: 30,
          recentAuditEventCount: 5,
          totalStaffCount: 10,
          patientsBySite: [{ siteName: "Main Clinic", count: 800 }]
        })
      )
    );

    renderWithAdminSession(<TestDataPage />);

    await waitFor(() => {
      expect(screen.getByText("1000")).toBeInTheDocument();
      expect(screen.getByText("10")).toBeInTheDocument();
      expect(screen.getByText("PATIENT_COUNT")).toBeInTheDocument();
    });
  });

  it("submits patient generation and displays returned totals", async () => {
    const user = userEvent.setup();
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () =>
        HttpResponse.json({
          patientCount: 0,
          duplicatePatientCount: 0,
          recentAuditEventCount: 0,
          totalStaffCount: 0,
          patientsBySite: []
        })
      ),
      http.post("*/api/v1/test-data/patients/generate", () =>
        HttpResponse.json({
          totalRequested: 5000,
          totalBaseInserted: 5000,
          duplicateRequested: 150,
          duplicateInserted: 150,
          totalAfter: 5150
        })
      )
    );

    renderWithAdminSession(<TestDataPage />);
    await screen.findByRole("button", { name: /generate patients/i });
    await user.click(screen.getByRole("button", { name: /generate patients/i }));

    await waitFor(() => {
      expect(screen.getByText(/total after generation/i)).toBeInTheDocument();
      expect(screen.getByText("5150")).toBeInTheDocument();
    });
  });

  it("shows actionable error when generation request fails", async () => {
    const user = userEvent.setup();
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () =>
        HttpResponse.json({
          patientCount: 0,
          duplicatePatientCount: 0,
          recentAuditEventCount: 0,
          totalStaffCount: 0,
          patientsBySite: []
        })
      ),
      http.post("*/api/v1/test-data/patients/generate", () => HttpResponse.json({}, { status: 500 }))
    );

    renderWithAdminSession(<TestDataPage />);
    await screen.findByRole("button", { name: /generate patients/i });
    await user.click(screen.getByRole("button", { name: /generate patients/i }));

    await waitFor(() => {
      expect(screen.getByText(/unable to generate patients/i)).toBeInTheDocument();
    });
  });

  it("demo mode: renders stats from DEMO_TEST_DATA_STATS without calling the API", async () => {
    const getStatsSpy = vi.fn();
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () => {
        getStatsSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<TestDataPage />);

    await waitFor(() => {
      expect(screen.getByText(String(DEMO_TEST_DATA_STATS.patientCount))).toBeInTheDocument();
      expect(screen.getByText(String(DEMO_TEST_DATA_STATS.totalStaffCount))).toBeInTheDocument();
    });
    expect(getStatsSpy).not.toHaveBeenCalled();
  });

  it("demo mode: Generate Patients button is present and clickable without triggering an API call", async () => {
    const user = userEvent.setup();
    const postSpy = vi.fn();
    server.use(
      http.post("*/api/v1/test-data/patients/generate", () => {
        postSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<TestDataPage />);

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /generate patients/i })).toBeInTheDocument()
    );
    await user.click(screen.getByRole("button", { name: /generate patients/i }));

    expect(postSpy).not.toHaveBeenCalled();
  });
});
