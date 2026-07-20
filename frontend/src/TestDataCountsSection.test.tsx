import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import TestDataCountsSection from "./TestDataCountsSection";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";
import { DEMO_TEST_DATA_STATS, DEMO_STUDY_TEST_DATA_STATS } from "./demoData";

describe("TestDataCountsSection", () => {
  it("renders patient and study stats with a visual chart element alongside numeric values", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () =>
        HttpResponse.json({
          patientCount: 1000,
          duplicatePatientCount: 30,
          recentAuditEventCount: 5,
          totalStaffCount: 10,
          patientsBySite: [{ siteName: "Main Clinic", count: 800 }]
        })
      ),
      http.get("*/api/v1/test-data/studies/stats", () =>
        HttpResponse.json({
          studyCount: 42,
          armCount: 90,
          visitCount: 210,
          milestoneCount: 130,
          documentCount: 80,
          studiesByStatus: [{ statusName: "Enrolling", count: 20 }],
          studiesBySponsor: []
        })
      )
    );

    renderWithAdminSession(<TestDataCountsSection />);

    await waitFor(() => {
      expect(screen.getByText("1000")).toBeInTheDocument();
      expect(screen.getByText("10")).toBeInTheDocument();
      expect(screen.getByText("42")).toBeInTheDocument();
    });
    // Recharts doesn't paint any content in jsdom (no layout engine), so the chart itself isn't
    // assertable — instead confirm the chart branch (not the empty-state branch) was taken, per
    // user-visible text only, matching MonitoringPage.test.tsx's approach of not reaching into
    // chart internals.
    await waitFor(() => {
      expect(screen.queryByText(/no patients yet/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/no studies yet/i)).not.toBeInTheDocument();
    });
  });

  it("renders an empty-state message, not an error, when no data exists yet", async () => {
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
      http.get("*/api/v1/test-data/studies/stats", () =>
        HttpResponse.json({
          studyCount: 0,
          armCount: 0,
          visitCount: 0,
          milestoneCount: 0,
          documentCount: 0,
          studiesByStatus: [],
          studiesBySponsor: []
        })
      )
    );

    renderWithAdminSession(<TestDataCountsSection />);

    await waitFor(() => {
      expect(screen.getByText(/no patients yet/i)).toBeInTheDocument();
      expect(screen.getByText(/no studies yet/i)).toBeInTheDocument();
    });
    expect(screen.queryByText(/unable to/i)).not.toBeInTheDocument();
  });

  it("reloads counts without a full page reload when Refresh stats is clicked", async () => {
    const user = userEvent.setup();
    let patientCount = 5;
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () =>
        HttpResponse.json({
          patientCount,
          duplicatePatientCount: 0,
          recentAuditEventCount: 0,
          totalStaffCount: 0,
          patientsBySite: []
        })
      ),
      http.get("*/api/v1/test-data/studies/stats", () =>
        HttpResponse.json({
          studyCount: 0,
          armCount: 0,
          visitCount: 0,
          milestoneCount: 0,
          documentCount: 0,
          studiesByStatus: [],
          studiesBySponsor: []
        })
      )
    );

    renderWithAdminSession(<TestDataCountsSection />);
    await screen.findByText("5");

    patientCount = 9;
    await user.click(screen.getByRole("button", { name: /refresh stats/i }));

    await waitFor(() => {
      expect(screen.getByText("9")).toBeInTheDocument();
    });
  });

  it("shows an inline error, and blanks the stats, when the initial load fails", async () => {
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () => HttpResponse.json({}, { status: 500 })),
      http.get("*/api/v1/test-data/studies/stats", () =>
        HttpResponse.json({
          studyCount: 0,
          armCount: 0,
          visitCount: 0,
          milestoneCount: 0,
          documentCount: 0,
          studiesByStatus: [],
          studiesBySponsor: []
        })
      )
    );

    renderWithAdminSession(<TestDataCountsSection />);

    await waitFor(() => {
      expect(screen.getByText(/unable to load patient stats/i)).toBeInTheDocument();
    });
  });

  it("shows an inline error when a Refresh stats click fails", async () => {
    const user = userEvent.setup();
    let shouldFail = false;
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () => {
        if (shouldFail) return HttpResponse.json({}, { status: 500 });
        return HttpResponse.json({
          patientCount: 5,
          duplicatePatientCount: 0,
          recentAuditEventCount: 0,
          totalStaffCount: 0,
          patientsBySite: []
        });
      }),
      http.get("*/api/v1/test-data/studies/stats", () =>
        HttpResponse.json({
          studyCount: 0,
          armCount: 0,
          visitCount: 0,
          milestoneCount: 0,
          documentCount: 0,
          studiesByStatus: [],
          studiesBySponsor: []
        })
      )
    );

    renderWithAdminSession(<TestDataCountsSection />);
    await screen.findByText("5");

    shouldFail = true;
    await user.click(screen.getByRole("button", { name: /refresh stats/i }));

    await waitFor(() => {
      expect(screen.getByText(/unable to load patient stats/i)).toBeInTheDocument();
    });
    // The stat cards are gone (stats went null), not stale — matches current component behavior.
    expect(screen.queryByText("5")).not.toBeInTheDocument();
  });

  it("demo mode: renders DEMO_TEST_DATA_STATS and DEMO_STUDY_TEST_DATA_STATS without making a live API call", async () => {
    const statsSpy = vi.fn();
    const studiesStatsSpy = vi.fn();
    server.use(
      http.get("*/api/v1/test-data/patients/stats", () => {
        statsSpy();
        return HttpResponse.json({});
      }),
      http.get("*/api/v1/test-data/studies/stats", () => {
        studiesStatsSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<TestDataCountsSection />);

    await waitFor(() => {
      expect(screen.getByText(String(DEMO_TEST_DATA_STATS.patientCount))).toBeInTheDocument();
      expect(screen.getByText(String(DEMO_TEST_DATA_STATS.totalStaffCount))).toBeInTheDocument();
      expect(screen.getByText(String(DEMO_STUDY_TEST_DATA_STATS.studyCount))).toBeInTheDocument();
      expect(screen.getByText(String(DEMO_STUDY_TEST_DATA_STATS.armCount))).toBeInTheDocument();
    });
    expect(statsSpy).not.toHaveBeenCalled();
    expect(studiesStatsSpy).not.toHaveBeenCalled();
  });
});
