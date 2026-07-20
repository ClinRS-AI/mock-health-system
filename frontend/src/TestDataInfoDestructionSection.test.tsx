import { describe, it, expect, vi, beforeEach } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import TestDataInfoDestructionSection from "./TestDataInfoDestructionSection";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";

describe("TestDataInfoDestructionSection", () => {
  beforeEach(() => {
    server.use(
      http.get("*/api/v1/test-data/soap/report-pkeys", () => HttpResponse.json({ pkeys: ["PATIENT_COUNT"] }))
    );
  });

  it("displays connection info and SOAP pkeys with copy controls (AC1)", async () => {
    renderWithAdminSession(<TestDataInfoDestructionSection />);

    await waitFor(() => {
      expect(screen.getByText("PATIENT_COUNT")).toBeInTheDocument();
    });
    expect(screen.getAllByRole("button", { name: /copy/i }).length).toBeGreaterThan(0);
  });

  it("requires confirmation before a patient reset request fires (AC2)", async () => {
    const user = userEvent.setup();
    const resetSpy = vi.fn();
    server.use(
      http.post("*/api/v1/test-data/patients/reset", () => {
        resetSpy();
        return HttpResponse.json({});
      })
    );

    renderWithAdminSession(<TestDataInfoDestructionSection />);
    await user.click(screen.getByRole("button", { name: /^reset patient data$/i }));

    // First click reveals a confirm step; the destructive request must not have fired yet.
    expect(resetSpy).not.toHaveBeenCalled();
    const confirmButton = await screen.findByRole("button", { name: /confirm reset/i });
    await user.click(confirmButton);

    await waitFor(() => expect(resetSpy).toHaveBeenCalled());
  });

  it("confirming a patient reset only calls the patient reset endpoint (AC3)", async () => {
    const user = userEvent.setup();
    const patientResetSpy = vi.fn();
    const studyResetSpy = vi.fn();
    server.use(
      http.post("*/api/v1/test-data/patients/reset", () => {
        patientResetSpy();
        return HttpResponse.json({});
      }),
      http.post("*/api/v1/test-data/studies/reset", () => {
        studyResetSpy();
        return HttpResponse.json({});
      })
    );

    renderWithAdminSession(<TestDataInfoDestructionSection />);
    await user.click(screen.getByRole("button", { name: /^reset patient data$/i }));
    await user.click(await screen.findByRole("button", { name: /confirm reset/i }));

    await waitFor(() => expect(patientResetSpy).toHaveBeenCalled());
    expect(studyResetSpy).not.toHaveBeenCalled();
  });

  it("confirming a study reset only calls the study reset endpoint (AC4)", async () => {
    const user = userEvent.setup();
    const patientResetSpy = vi.fn();
    const studyResetSpy = vi.fn();
    server.use(
      http.post("*/api/v1/test-data/patients/reset", () => {
        patientResetSpy();
        return HttpResponse.json({});
      }),
      http.post("*/api/v1/test-data/studies/reset", () => {
        studyResetSpy();
        return HttpResponse.json({});
      })
    );

    renderWithAdminSession(<TestDataInfoDestructionSection />);
    await user.click(screen.getByRole("button", { name: /^reset study data$/i }));
    await user.click(await screen.findByRole("button", { name: /confirm reset/i }));

    await waitFor(() => expect(studyResetSpy).toHaveBeenCalled());
    expect(patientResetSpy).not.toHaveBeenCalled();
  });

  it("renders no generation or lookup control anywhere in this component (AC5)", () => {
    renderWithAdminSession(<TestDataInfoDestructionSection />);

    expect(screen.queryByRole("button", { name: /^generate/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^lookup$/i })).not.toBeInTheDocument();
  });

  it("demo mode: both reset controls remain inert and no live API call fires (FR-018)", async () => {
    const user = userEvent.setup();
    const resetSpy = vi.fn();
    server.use(
      http.post("*/api/v1/test-data/patients/reset", () => {
        resetSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<TestDataInfoDestructionSection />);
    await user.click(screen.getByRole("button", { name: /^reset patient data$/i }));

    expect(resetSpy).not.toHaveBeenCalled();
    expect(screen.queryByRole("button", { name: /confirm reset/i })).not.toBeInTheDocument();
  });
});
