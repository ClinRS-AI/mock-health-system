import { describe, it, expect, vi, beforeEach } from "vitest";
import { screen, waitFor, within } from "@testing-library/react";
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

  describe("Studies section", () => {
    beforeEach(() => {
      server.use(
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
    });

    it("loads study stats and shows key summary values", async () => {
      server.use(
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

      renderWithAdminSession(<TestDataPage />);

      await waitFor(() => {
        expect(screen.getByText("42")).toBeInTheDocument();
        expect(screen.getByText("Enrolling")).toBeInTheDocument();
      });
    });

    it("submits study generation and displays returned totals", async () => {
      const user = userEvent.setup();
      server.use(
        http.post("*/api/v1/test-data/studies/generate", () =>
          HttpResponse.json({
            totalRequested: 25,
            totalInserted: 25,
            armsInserted: 61,
            visitsInserted: 143,
            milestonesInserted: 98,
            documentsInserted: 52,
            notesInserted: 40,
            totalAfter: 25
          })
        )
      );

      renderWithAdminSession(<TestDataPage />);
      await screen.findByRole("button", { name: /generate studies/i });
      await user.click(screen.getByRole("button", { name: /generate studies/i }));

      await waitFor(() => {
        expect(screen.getByText(/total after generation/i)).toBeInTheDocument();
      });
    });

    it("shows actionable error when study generation request fails", async () => {
      const user = userEvent.setup();
      server.use(
        http.post("*/api/v1/test-data/studies/generate", () => HttpResponse.json({}, { status: 500 }))
      );

      renderWithAdminSession(<TestDataPage />);
      await screen.findByRole("button", { name: /generate studies/i });
      await user.click(screen.getByRole("button", { name: /generate studies/i }));

      await waitFor(() => {
        expect(screen.getByText(/unable to generate studies/i)).toBeInTheDocument();
      });
    });

    it("resets study data when Reset study data is clicked", async () => {
      const user = userEvent.setup();
      const resetSpy = vi.fn();
      server.use(
        http.post("*/api/v1/test-data/studies/reset", () => {
          resetSpy();
          return HttpResponse.json({});
        })
      );

      renderWithAdminSession(<TestDataPage />);
      await screen.findByRole("button", { name: /reset study data/i });
      await user.click(screen.getByRole("button", { name: /reset study data/i }));

      await waitFor(() => expect(resetSpy).toHaveBeenCalled());
    });

    it("looks up a study by name fragment and displays the result", async () => {
      const user = userEvent.setup();
      server.use(
        http.get("*/api/v1/test-data/studies/lookup", () =>
          HttpResponse.json({
            id: 7,
            uid: "550e8400-e29b-41d4-a716-446655440000",
            name: "Acme Oncology Study",
            status: "Enrolling",
            sponsorTeam: { id: 1, name: "Team A" },
            contacts: [],
            createdOn: "2026-01-01T00:00:00Z",
            lastUpdatedOn: "2026-01-01T00:00:00Z"
          })
        )
      );

      renderWithAdminSession(<TestDataPage />);
      await user.click(screen.getByText(/lookup study/i));
      const nameInput = await screen.findByPlaceholderText(/acme oncology/i);
      await user.type(nameInput, "Acme");
      const form = nameInput.closest("form") as HTMLElement;
      await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

      await waitFor(() => {
        expect(screen.getByText(/study record/i)).toBeInTheDocument();
        expect(screen.getByText(/Acme Oncology Study/)).toBeInTheDocument();
      });
    });

    it("shows not-found message when study lookup has no match", async () => {
      const user = userEvent.setup();
      server.use(
        http.get("*/api/v1/test-data/studies/lookup", () => HttpResponse.json({}, { status: 404 }))
      );

      renderWithAdminSession(<TestDataPage />);
      await user.click(screen.getByText(/lookup study/i));
      const nameInput = await screen.findByPlaceholderText(/acme oncology/i);
      await user.type(nameInput, "Nonexistent");
      const form = nameInput.closest("form") as HTMLElement;
      await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

      await waitFor(() => {
        expect(screen.getByText(/no study found/i)).toBeInTheDocument();
      });
    });

    it("clears a stale lookup result when Get random fails with a non-404 error", async () => {
      const user = userEvent.setup();
      server.use(
        http.get("*/api/v1/test-data/studies/lookup", () =>
          HttpResponse.json({
            id: 7,
            uid: "550e8400-e29b-41d4-a716-446655440000",
            name: "Acme Oncology Study",
            status: "Enrolling",
            sponsorTeam: { id: 1, name: "Team A" },
            contacts: [],
            createdOn: "2026-01-01T00:00:00Z",
            lastUpdatedOn: "2026-01-01T00:00:00Z"
          })
        )
      );

      renderWithAdminSession(<TestDataPage />);
      await user.click(screen.getByText(/lookup study/i));
      const nameInput = await screen.findByPlaceholderText(/acme oncology/i);
      await user.type(nameInput, "Acme");
      const form = nameInput.closest("form") as HTMLElement;
      await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

      await waitFor(() => {
        expect(screen.getByText(/Acme Oncology Study/)).toBeInTheDocument();
      });

      server.use(
        http.get("*/api/v1/test-data/studies/random", () => HttpResponse.json({}, { status: 500 }))
      );
      await user.click(within(form).getByRole("button", { name: /get random/i }));

      await waitFor(() => {
        expect(screen.getByText(/unable to get a random study/i)).toBeInTheDocument();
        expect(screen.queryByText(/Acme Oncology Study/)).not.toBeInTheDocument();
      });
    });

    it("demo mode: Generate studies button is present and clickable without triggering an API call", async () => {
      const user = userEvent.setup();
      const postSpy = vi.fn();
      server.use(
        http.post("*/api/v1/test-data/studies/generate", () => {
          postSpy();
          return HttpResponse.json({});
        })
      );

      renderInDemoMode(<TestDataPage />);

      await waitFor(() =>
        expect(screen.getByRole("button", { name: /generate studies/i })).toBeInTheDocument()
      );
      await user.click(screen.getByRole("button", { name: /generate studies/i }));

      expect(postSpy).not.toHaveBeenCalled();
    });
  });
});
