import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import App from "./App";
import { getApiStatus } from "./api";

vi.mock("./api", () => ({
  getApiStatus: vi.fn()
}));

const mockedGetApiStatus = vi.mocked(getApiStatus);

describe("App", () => {
  beforeEach(() => {
    mockedGetApiStatus.mockReset();
  });

  it("renders app title and stack overview", () => {
    render(<App />);

    expect(
      screen.getByRole("heading", { name: /mock health system/i })
    ).toBeInTheDocument();
    expect(screen.getByText(/stack overview/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /check api status/i })
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /admin access/i })).toBeInTheDocument();
  });

  it("shows API status when check succeeds", async () => {
    const user = userEvent.setup();
    mockedGetApiStatus.mockResolvedValue("Healthy and ready");

    render(<App />);
    await user.click(screen.getByRole("button", { name: /check api status/i }));

    await waitFor(() => {
      expect(screen.getByText("Healthy and ready")).toBeInTheDocument();
    });
    expect(mockedGetApiStatus).toHaveBeenCalledTimes(1);
  });

  it("shows error message when API check fails", async () => {
    const user = userEvent.setup();
    const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});
    mockedGetApiStatus.mockRejectedValue(new Error("Network error"));

    render(<App />);
    await user.click(screen.getByRole("button", { name: /check api status/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/unable to reach the api/i)
      ).toBeInTheDocument();
    });
    consoleSpy.mockRestore();
  });

  it("disables button and shows Checking... while loading", async () => {
    const user = userEvent.setup();
    mockedGetApiStatus.mockImplementation(
      () =>
        new Promise((resolve) =>
          setTimeout(() => resolve("OK"), 100)
        )
    );

    render(<App />);
    const button = screen.getByRole("button", { name: /check api status/i });
    await user.click(button);

    expect(button).toBeDisabled();
    expect(button).toHaveTextContent("Checking...");

    await waitFor(() => {
      expect(button).not.toBeDisabled();
      expect(button).toHaveTextContent("Check API status");
    });
  });
});
