import { describe, it, expect } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import AuthSettingsPage from "./AuthSettingsPage";
import { server } from "./test/server";

const authSettingsPath = "*/api/v1/auth-settings";

describe("AuthSettingsPage", () => {
  it("loads settings and renders protected mode from API response", async () => {
    server.use(
      http.get(authSettingsPath, () =>
        HttpResponse.json({
          mode: "Bearer",
          bearerToken: "server-token",
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: true
        })
      )
    );

    render(<AuthSettingsPage />);

    await waitFor(() => {
      expect(screen.getByText("PROTECTED")).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/shared bearer token/i)).toBeInTheDocument();
    });
  });

  it("shows load error when auth settings request fails", async () => {
    server.use(http.get(authSettingsPath, () => HttpResponse.json({}, { status: 403 })));

    render(<AuthSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByText(/unable to load authentication settings/i)
      ).toBeInTheDocument();
    });
  });

  it("saves updated bearer settings and shows success feedback", async () => {
    const user = userEvent.setup();

    server.use(
      http.get(authSettingsPath, () =>
        HttpResponse.json({
          mode: "None",
          bearerToken: null,
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false
        })
      ),
      http.put(authSettingsPath, async ({ request }) => {
        const body = (await request.json()) as { mode: string; bearerToken?: string };
        expect(body.mode).toBe("Bearer");
        expect(body.bearerToken).toContain("my-bearer-token");
        return HttpResponse.json({
          mode: "Bearer",
          bearerToken: body.bearerToken ?? "",
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false
        });
      })
    );

    render(<AuthSettingsPage />);
    await screen.findByRole("button", { name: /^bearer/i });

    await user.click(screen.getByRole("button", { name: /^bearer/i }));
    await user.clear(screen.getByPlaceholderText(/shared bearer token/i));
    await user.type(screen.getByPlaceholderText(/shared bearer token/i), "my-bearer-token");
    await user.click(screen.getByRole("button", { name: /save settings/i }));

    await waitFor(() => {
      expect(screen.getByText(/authentication settings saved/i)).toBeInTheDocument();
    });
  });
});
