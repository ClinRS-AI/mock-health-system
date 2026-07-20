import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import TestDataGenerationSection from "./TestDataGenerationSection";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";

describe("TestDataGenerationSection", () => {
  it("submits patient generation and displays returned totals (AC1)", async () => {
    const user = userEvent.setup();
    server.use(
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

    renderWithAdminSession(<TestDataGenerationSection />);
    await user.click(screen.getByRole("button", { name: /generate patients/i }));

    await waitFor(() => {
      expect(screen.getByText(/total after generation/i)).toBeInTheDocument();
      expect(screen.getByText("5150")).toBeInTheDocument();
    });
  });

  it("submits study generation and displays returned totals (AC2)", async () => {
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

    renderWithAdminSession(<TestDataGenerationSection />);
    await user.click(screen.getByRole("button", { name: /generate studies/i }));

    await waitFor(() => {
      expect(screen.getByText(/total after generation/i)).toBeInTheDocument();
      expect(screen.getAllByText("25").length).toBeGreaterThan(0);
    });
  });

  it("shows independent result summaries for staff and audit-event generation (AC3)", async () => {
    const user = userEvent.setup();
    server.use(
      http.post("*/api/v1/test-data/staff/generate", () =>
        HttpResponse.json({ inserted: 10, totalAfter: 20 })
      ),
      http.post("*/api/v1/test-data/audit-events/generate", () =>
        HttpResponse.json({ inserted: 25, totalAfter: 50, insertedByAuditType: [] })
      )
    );

    renderWithAdminSession(<TestDataGenerationSection />);

    await user.click(screen.getByRole("button", { name: /generate staff/i }));
    await waitFor(() => {
      expect(screen.getByText(/staff inserted/i)).toBeInTheDocument();
    });

    await user.click(screen.getByRole("button", { name: /generate recent audit events/i }));
    await waitFor(() => {
      expect(screen.getByText(/audit events inserted/i)).toBeInTheDocument();
    });
  });

  it("creates a manual patient and shows the returned id/UID (AC4)", async () => {
    const user = userEvent.setup();
    server.use(
      http.post("*/api/v1/test-data/patients/add", () =>
        HttpResponse.json({ id: 42, uid: "550e8400-e29b-41d4-a716-446655440000" })
      )
    );

    renderWithAdminSession(<TestDataGenerationSection />);
    await user.type(screen.getByLabelText(/first name/i), "Jane");
    await user.type(screen.getByLabelText(/last name/i), "Doe");
    await user.type(screen.getByLabelText(/email/i), "jane.doe@example.com");
    await user.click(screen.getByRole("button", { name: /^add patient$/i }));

    await waitFor(() => {
      expect(screen.getByText(/patient added/i)).toBeInTheDocument();
      expect(screen.getByText("42")).toBeInTheDocument();
    });
  });

  it("renders no reset/destructive control anywhere in this component (AC5)", () => {
    renderWithAdminSession(<TestDataGenerationSection />);

    expect(screen.queryByRole("button", { name: /reset/i })).not.toBeInTheDocument();
  });

  it("demo mode: generation controls remain inert and no live API call fires (FR-018)", async () => {
    const user = userEvent.setup();
    const postSpy = vi.fn();
    server.use(
      http.post("*/api/v1/test-data/patients/generate", () => {
        postSpy();
        return HttpResponse.json({});
      })
    );

    renderInDemoMode(<TestDataGenerationSection />);
    await user.click(screen.getByRole("button", { name: /generate patients/i }));

    expect(postSpy).not.toHaveBeenCalled();
  });
});
