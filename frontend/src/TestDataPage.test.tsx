import { describe, it, expect, beforeEach, vi } from "vitest";
import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import TestDataPage from "./TestDataPage";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";

const TAB_LABELS = [
  "Data Counts and Visualizations",
  "Data Generation",
  "Data Manipulation",
  "Information and Destruction"
];

describe("TestDataPage (orchestrator)", () => {
  beforeEach(() => {
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
      ),
      http.get("*/api/v1/test-data/soap/report-pkeys", () => HttpResponse.json({ pkeys: [] }))
    );
  });

  it("renders all four tab labels", async () => {
    renderWithAdminSession(<TestDataPage />);

    for (const label of TAB_LABELS) {
      expect(await screen.findByRole("button", { name: label })).toBeInTheDocument();
    }
  });

  it("shows only the active tab's content and hides the other three", async () => {
    const user = userEvent.setup();
    renderWithAdminSession(<TestDataPage />);

    // Counts tab is active by default.
    expect(await screen.findByRole("heading", { name: "Data Counts and Visualizations" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Data Generation" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Data Manipulation" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Information and Destruction" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Data Generation" }));
    expect(await screen.findByRole("heading", { name: "Data Generation" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Data Counts and Visualizations" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Data Manipulation" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Information and Destruction" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Data Manipulation" }));
    expect(await screen.findByRole("heading", { name: "Data Manipulation" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Data Generation" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Information and Destruction" }));
    expect(await screen.findByRole("heading", { name: "Information and Destruction" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Data Manipulation" })).not.toBeInTheDocument();
  });

  it("renders the tab row in demo mode", async () => {
    renderInDemoMode(<TestDataPage />);

    for (const label of TAB_LABELS) {
      expect(await screen.findByRole("button", { name: label })).toBeInTheDocument();
    }
    expect(await screen.findByRole("heading", { name: "Data Counts and Visualizations" })).toBeInTheDocument();
  });

  it("does not re-render the whole page on tab switch (AdminSessionBanner stays mounted)", async () => {
    const user = userEvent.setup();
    renderWithAdminSession(<TestDataPage />);

    await screen.findByRole("heading", { name: "Data Counts and Visualizations" });
    await user.click(screen.getByRole("button", { name: "Data Manipulation" }));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Data Manipulation" })).toBeInTheDocument();
    });
  });

  describe("unsaved patient-edit guard on tab switch", () => {
    beforeEach(() => {
      server.use(
        http.get("*/api/v1/test-data/patients/lookup", () =>
          HttpResponse.json({ id: 1, uid: "u-1", firstName: "Jane", lastName: "Doe", primaryEmailAddress: "jane@example.com" })
        )
      );
    });

    async function startEditingAPatient(user: ReturnType<typeof userEvent.setup>) {
      await user.click(screen.getByRole("button", { name: "Data Manipulation" }));
      const idInput = await screen.findByPlaceholderText(/e\.g\. 1/i);
      await user.type(idInput, "1");
      const form = idInput.closest("form") as HTMLElement;
      await user.click(within(form).getByRole("button", { name: /^lookup$/i }));
      await screen.findByText(/patient record \(all details\)/i);
      await user.click(screen.getByRole("button", { name: /^edit$/i }));
      await screen.findByRole("textbox", { name: "" });
    }

    it("asks for confirmation before switching tabs while editing, and stays put if canceled", async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(false);
      renderWithAdminSession(<TestDataPage />);

      await startEditingAPatient(user);
      await user.click(screen.getByRole("button", { name: "Data Counts and Visualizations" }));

      expect(confirmSpy).toHaveBeenCalledTimes(1);
      expect(screen.getByRole("heading", { name: "Data Manipulation" })).toBeInTheDocument();
      confirmSpy.mockRestore();
    });

    it("switches tabs (discarding the edit) if the user confirms", async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);
      renderWithAdminSession(<TestDataPage />);

      await startEditingAPatient(user);
      await user.click(screen.getByRole("button", { name: "Data Counts and Visualizations" }));

      expect(confirmSpy).toHaveBeenCalledTimes(1);
      expect(screen.getByRole("heading", { name: "Data Counts and Visualizations" })).toBeInTheDocument();
      confirmSpy.mockRestore();
    });

    it("does not prompt when switching tabs without an unsaved edit in progress", async () => {
      const user = userEvent.setup();
      const confirmSpy = vi.spyOn(window, "confirm");
      renderWithAdminSession(<TestDataPage />);

      await user.click(screen.getByRole("button", { name: "Data Manipulation" }));
      await screen.findByRole("heading", { name: "Data Manipulation" });
      await user.click(screen.getByRole("button", { name: "Data Counts and Visualizations" }));

      expect(confirmSpy).not.toHaveBeenCalled();
      confirmSpy.mockRestore();
    });
  });
});
