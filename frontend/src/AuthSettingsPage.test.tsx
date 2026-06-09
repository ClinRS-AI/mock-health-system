import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import AuthSettingsPage from "./AuthSettingsPage";
import { server } from "./test/server";
import { renderWithAdminSession, renderInDemoMode } from "./test/renderWithAdminSession";
import { DEMO_AUTH_SETTINGS } from "./demoData";

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

    renderWithAdminSession(<AuthSettingsPage />);

    await waitFor(() => {
      expect(screen.getByText("PROTECTED")).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/shared bearer token/i)).toBeInTheDocument();
    });
  });

  it("shows load error when auth settings request fails", async () => {
    server.use(http.get(authSettingsPath, () => HttpResponse.json({}, { status: 403 })));

    renderWithAdminSession(<AuthSettingsPage />);

    await waitFor(() => {
      expect(
        screen.getByText(/unable to load authentication settings/i)
      ).toBeInTheDocument();
    });
  });

  it("demo mode: shows DEMO_AUTH_SETTINGS mode without calling the API", async () => {
    const getSettingsSpy = vi.fn();
    server.use(http.get(authSettingsPath, () => { getSettingsSpy(); return HttpResponse.json({}); }));

    renderInDemoMode(<AuthSettingsPage />);

    await waitFor(() => {
      expect(screen.getByText(DEMO_AUTH_SETTINGS.mode)).toBeInTheDocument();
    });
    expect(getSettingsSpy).not.toHaveBeenCalled();
  });

  it("demo mode: Save Settings button is present and clickable without triggering an API call", async () => {
    const user = userEvent.setup();
    const putSpy = vi.fn();
    server.use(http.put(authSettingsPath, () => { putSpy(); return HttpResponse.json({}); }));

    renderInDemoMode(<AuthSettingsPage />);

    await waitFor(() => expect(screen.getByRole("button", { name: /save settings/i })).toBeInTheDocument());
    await user.click(screen.getByRole("button", { name: /save settings/i }));

    expect(putSpy).not.toHaveBeenCalled();
  });

  it("renders Rate Limiting section with values from API response", async () => {
    server.use(
      http.get(authSettingsPath, () =>
        HttpResponse.json({
          mode: "None",
          bearerToken: null,
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false,
          rateLimitEnabled: false,
          rateLimitPerSecond: 10,
          rateLimitPerMinute: 300
        })
      )
    );

    renderWithAdminSession(<AuthSettingsPage />);

    await waitFor(() => {
      expect(screen.getByText("Rate Limiting")).toBeInTheDocument();
      expect(screen.getByText("Disabled")).toBeInTheDocument();
    });

    const perSecondInput = screen.getByLabelText(/requests per second/i);
    const perMinuteInput = screen.getByLabelText(/requests per minute/i);
    expect(perSecondInput).toHaveValue(10);
    expect(perMinuteInput).toHaveValue(300);
  });

  it("save includes rate limit fields in PUT payload", async () => {
    const user = userEvent.setup();
    let capturedBody: Record<string, unknown> | null = null;

    server.use(
      http.get(authSettingsPath, () =>
        HttpResponse.json({
          mode: "None",
          bearerToken: null,
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false,
          rateLimitEnabled: false,
          rateLimitPerSecond: 10,
          rateLimitPerMinute: 300
        })
      ),
      http.put(authSettingsPath, async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          mode: "None",
          bearerToken: null,
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false,
          rateLimitEnabled: true,
          rateLimitPerSecond: 10,
          rateLimitPerMinute: 300
        });
      })
    );

    renderWithAdminSession(<AuthSettingsPage />);
    await screen.findByText("Rate Limiting");

    // Enable rate limiting
    await user.click(screen.getByRole("checkbox"));
    await user.click(screen.getByRole("button", { name: /save settings/i }));

    await waitFor(() => {
      expect(capturedBody).not.toBeNull();
    });
    expect(capturedBody).toMatchObject({
      rateLimitEnabled: true,
      rateLimitPerSecond: 10,
      rateLimitPerMinute: 300
    });
  });

  it("demo mode: rate limit inputs are disabled", async () => {
    renderInDemoMode(<AuthSettingsPage />);

    await waitFor(() => {
      expect(screen.getByText("Rate Limiting")).toBeInTheDocument();
    });

    const perSecondInput = screen.getByLabelText(/requests per second/i);
    const perMinuteInput = screen.getByLabelText(/requests per minute/i);
    const checkbox = screen.getByRole("checkbox");

    expect(perSecondInput).toBeDisabled();
    expect(perMinuteInput).toBeDisabled();
    expect(checkbox).toBeDisabled();
  });

  it("save with rateLimitEnabled false sends false in payload", async () => {
    const user = userEvent.setup();
    let capturedBody: Record<string, unknown> | null = null;

    server.use(
      http.get(authSettingsPath, () =>
        HttpResponse.json({
          mode: "None",
          bearerToken: null,
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false,
          rateLimitEnabled: true,
          rateLimitPerSecond: 5,
          rateLimitPerMinute: 100
        })
      ),
      http.put(authSettingsPath, async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          mode: "None",
          bearerToken: null,
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false,
          rateLimitEnabled: false,
          rateLimitPerSecond: 5,
          rateLimitPerMinute: 100
        });
      })
    );

    renderWithAdminSession(<AuthSettingsPage />);
    await screen.findByText("Enabled");

    // Toggle off
    await user.click(screen.getByRole("checkbox"));
    await user.click(screen.getByRole("button", { name: /save settings/i }));

    await waitFor(() => {
      expect(capturedBody).not.toBeNull();
    });
    expect(capturedBody).toMatchObject({
      rateLimitEnabled: false,
      rateLimitPerSecond: 5,
      rateLimitPerMinute: 100
    });
  });

  it("disabling rate limit renders number inputs as disabled, not hidden", async () => {
    server.use(
      http.get(authSettingsPath, () =>
        HttpResponse.json({
          mode: "None",
          bearerToken: null,
          oAuthClientId: null,
          oAuthClientSecret: null,
          accessTokenLifetimeMinutes: 60,
          refreshTokenLifetimeDays: 30,
          hasAnyTokens: false,
          rateLimitEnabled: false,
          rateLimitPerSecond: 10,
          rateLimitPerMinute: 300
        })
      )
    );

    renderWithAdminSession(<AuthSettingsPage />);
    await screen.findByText("Rate Limiting");

    const perSecondInput = screen.getByLabelText(/requests per second/i);
    const perMinuteInput = screen.getByLabelText(/requests per minute/i);

    // Inputs are present in the DOM (not hidden) but disabled
    expect(perSecondInput).toBeInTheDocument();
    expect(perMinuteInput).toBeInTheDocument();
    expect(perSecondInput).toBeDisabled();
    expect(perMinuteInput).toBeDisabled();
    // Values are preserved
    expect(perSecondInput).toHaveValue(10);
    expect(perMinuteInput).toHaveValue(300);
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

    renderWithAdminSession(<AuthSettingsPage />);
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
