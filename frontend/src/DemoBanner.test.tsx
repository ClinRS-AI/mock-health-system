import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { DemoBanner } from "./DemoBanner";

describe("DemoBanner", () => {
  it("renders the Demo Mode label", () => {
    render(<DemoBanner onNavigateToAdmin={() => {}} />);
    expect(screen.getByText(/demo mode/i)).toBeInTheDocument();
  });

  it("renders the description prompting the user to sign in", () => {
    render(<DemoBanner onNavigateToAdmin={() => {}} />);
    expect(screen.getByText(/read-only demo data/i)).toBeInTheDocument();
  });

  it("renders the Go to Admin access CTA button", () => {
    render(<DemoBanner onNavigateToAdmin={() => {}} />);
    expect(screen.getByRole("button", { name: /go to admin access/i })).toBeInTheDocument();
  });

  it("calls onNavigateToAdmin when the CTA button is clicked", async () => {
    const user = userEvent.setup();
    const onNavigateToAdmin = vi.fn();
    render(<DemoBanner onNavigateToAdmin={onNavigateToAdmin} />);

    await user.click(screen.getByRole("button", { name: /go to admin access/i }));

    expect(onNavigateToAdmin).toHaveBeenCalledTimes(1);
  });
});
