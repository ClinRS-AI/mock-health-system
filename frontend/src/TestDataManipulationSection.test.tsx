import { describe, it, expect, vi } from "vitest";
import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import TestDataManipulationSection from "./TestDataManipulationSection";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";

describe("TestDataManipulationSection", () => {
  it("shows full patient details on lookup, or a not-found message when there is no match (AC1)", async () => {
    const user = userEvent.setup();
    server.use(
      http.get("*/api/v1/test-data/patients/lookup", () =>
        HttpResponse.json({
          id: 1,
          uid: "550e8400-e29b-41d4-a716-446655440000",
          firstName: "Jane",
          lastName: "Doe",
          primaryEmailAddress: "jane.doe@example.com"
        })
      )
    );

    renderWithAdminSession(<TestDataManipulationSection />);
    const idInput = screen.getByPlaceholderText(/e\.g\. 1/i);
    await user.type(idInput, "1");
    const form = idInput.closest("form") as HTMLElement;
    await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

    await waitFor(() => {
      expect(screen.getByText(/patient record \(all details\)/i)).toBeInTheDocument();
      expect(screen.getByText(/jane\.doe@example\.com/)).toBeInTheDocument();
    });

    server.use(http.get("*/api/v1/test-data/patients/lookup", () => HttpResponse.json({}, { status: 404 })));
    await user.clear(idInput);
    await user.type(idInput, "999");
    await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

    await waitFor(() => {
      expect(screen.getByText(/no patient found/i)).toBeInTheDocument();
    });
  });

  it("edits and saves a patient record, persisting the change in the display (AC2)", async () => {
    const user = userEvent.setup();
    server.use(
      http.get("*/api/v1/test-data/patients/lookup", () =>
        HttpResponse.json({ id: 1, uid: "u-1", firstName: "Jane", lastName: "Doe", primaryEmailAddress: "jane@example.com" })
      ),
      http.put("*/api/v1/test-data/patients/1", async ({ request }) => {
        const body = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(body);
      })
    );

    renderWithAdminSession(<TestDataManipulationSection />);
    const idInput = screen.getByPlaceholderText(/e\.g\. 1/i);
    await user.type(idInput, "1");
    const form = idInput.closest("form") as HTMLElement;
    await user.click(within(form).getByRole("button", { name: /^lookup$/i }));
    await screen.findByText(/patient record \(all details\)/i);

    await user.click(screen.getByRole("button", { name: /^edit$/i }));
    const textarea = screen.getByRole("textbox", { name: "" }) as HTMLTextAreaElement;
    const updatedJson = JSON.stringify(
      { id: 1, uid: "u-1", firstName: "Janet", lastName: "Doe", primaryEmailAddress: "jane@example.com" },
      null,
      2
    );
    await user.clear(textarea);
    await user.paste(updatedJson);

    server.use(
      http.get("*/api/v1/test-data/patients/lookup", () =>
        HttpResponse.json({ id: 1, uid: "u-1", firstName: "Janet", lastName: "Doe", primaryEmailAddress: "jane@example.com" })
      )
    );
    await user.click(screen.getByRole("button", { name: /^save$/i }));

    await waitFor(() => {
      expect(screen.getByText(/Janet/)).toBeInTheDocument();
    });
  });

  it("shows full study details on lookup, or a not-found message when there is no match (AC3)", async () => {
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

    renderWithAdminSession(<TestDataManipulationSection />);
    const nameInput = await screen.findByPlaceholderText(/acme oncology/i);
    await user.type(nameInput, "Acme");
    const form = nameInput.closest("form") as HTMLElement;
    await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

    await waitFor(() => {
      expect(screen.getByText(/study record/i)).toBeInTheDocument();
      expect(screen.getByText(/Acme Oncology Study/)).toBeInTheDocument();
    });

    server.use(http.get("*/api/v1/test-data/studies/lookup", () => HttpResponse.json({}, { status: 404 })));
    await user.clear(nameInput);
    await user.type(nameInput, "Nonexistent");
    await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

    await waitFor(() => {
      expect(screen.getByText(/no study found/i)).toBeInTheDocument();
    });
  });

  it("returns a randomly selected patient or study via Get random (AC4)", async () => {
    const user = userEvent.setup();
    server.use(
      http.get("*/api/v1/test-data/patients/random", () =>
        HttpResponse.json({ id: 3, uid: "u-3", firstName: "Random", lastName: "Patient", primaryEmailAddress: "r@example.com" })
      ),
      http.get("*/api/v1/test-data/studies/random", () =>
        HttpResponse.json({
          id: 9,
          uid: "u-9",
          name: "Random Study",
          status: "Enrolling",
          sponsorTeam: { id: 1, name: "Team A" },
          contacts: [],
          createdOn: "2026-01-01T00:00:00Z",
          lastUpdatedOn: "2026-01-01T00:00:00Z"
        })
      )
    );

    renderWithAdminSession(<TestDataManipulationSection />);

    const patientIdInput = screen.getByPlaceholderText(/e\.g\. 1/i);
    const patientForm = patientIdInput.closest("form") as HTMLElement;
    await user.click(within(patientForm).getByRole("button", { name: /get random/i }));
    await waitFor(() => {
      expect(screen.getByText(/Random Patient|r@example\.com/)).toBeInTheDocument();
    });

    const studyNameInput = await screen.findByPlaceholderText(/acme oncology/i);
    const studyForm = studyNameInput.closest("form") as HTMLElement;
    await user.click(within(studyForm).getByRole("button", { name: /get random/i }));
    await waitFor(() => {
      expect(screen.getByText(/Random Study/)).toBeInTheDocument();
    });
  });

  it("renders no bulk-generation or reset control anywhere in this component (FR-012)", () => {
    renderWithAdminSession(<TestDataManipulationSection />);

    expect(screen.queryByRole("button", { name: /^generate/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /reset/i })).not.toBeInTheDocument();
  });

  it("demo mode: lookup/edit controls remain inert and no live API call fires (FR-018)", async () => {
    const user = userEvent.setup();
    const lookupSpy = vi.fn();
    server.use(
      http.get("*/api/v1/test-data/patients/lookup", () => {
        lookupSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<TestDataManipulationSection />);
    const idInput = screen.getByPlaceholderText(/e\.g\. 1/i);
    await user.type(idInput, "1");
    const form = idInput.closest("form") as HTMLElement;
    await user.click(within(form).getByRole("button", { name: /^lookup$/i }));

    expect(lookupSpy).not.toHaveBeenCalled();
  });
});
