import { describe, it, expect, afterEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import React from "react";
import { http, HttpResponse } from "msw";
import AdminSessionProvider, { useAdminSession } from "./AdminSessionContext";
import { server } from "./test/server";
import { clearAdminSession, setAdminSession } from "./adminSessionStore";

function ContextDisplay() {
  const { isDemoMode, isProbeSettled, hasSession } = useAdminSession();
  return (
    <div>
      <span data-testid="isDemoMode">{String(isDemoMode)}</span>
      <span data-testid="isProbeSettled">{String(isProbeSettled)}</span>
      <span data-testid="hasSession">{String(hasSession)}</span>
    </div>
  );
}

function renderContext() {
  return render(
    <AdminSessionProvider>
      <ContextDisplay />
    </AdminSessionProvider>
  );
}

describe("AdminSessionContext", () => {
  afterEach(() => {
    clearAdminSession();
  });

  describe("probe behavior", () => {
    it("HEAD 200 → isProbeSettled=true, isDemoMode=false", async () => {
      // Default server handler returns 200 for HEAD /api/v1/auth-settings
      renderContext();
      await waitFor(() => {
        expect(screen.getByTestId("isProbeSettled")).toHaveTextContent("true");
      });
      expect(screen.getByTestId("isDemoMode")).toHaveTextContent("false");
    });

    it("HEAD 401 → isProbeSettled=true, isDemoMode=true", async () => {
      server.use(
        http.head("*/api/v1/auth-settings", () => new HttpResponse(null, { status: 401 }))
      );
      renderContext();
      await waitFor(() => {
        expect(screen.getByTestId("isDemoMode")).toHaveTextContent("true");
      });
      expect(screen.getByTestId("isProbeSettled")).toHaveTextContent("true");
    });

    it("HEAD 403 → isDemoMode=true", async () => {
      server.use(
        http.head("*/api/v1/auth-settings", () => new HttpResponse(null, { status: 403 }))
      );
      renderContext();
      await waitFor(() => {
        expect(screen.getByTestId("isDemoMode")).toHaveTextContent("true");
      });
    });

    it("network error → isDemoMode=true", async () => {
      server.use(http.head("*/api/v1/auth-settings", () => HttpResponse.error()));
      renderContext();
      await waitFor(() => {
        expect(screen.getByTestId("isDemoMode")).toHaveTextContent("true");
      });
    });
  });

  describe("session state", () => {
    it("hasSession=false when no session token is set", async () => {
      renderContext();
      await waitFor(() => {
        expect(screen.getByTestId("isProbeSettled")).toHaveTextContent("true");
      });
      expect(screen.getByTestId("hasSession")).toHaveTextContent("false");
    });

    it("hasSession=false when token has already expired", async () => {
      setAdminSession("expired-token", new Date(Date.now() - 1000));
      renderContext();
      await waitFor(() => {
        expect(screen.getByTestId("isProbeSettled")).toHaveTextContent("true");
      });
      expect(screen.getByTestId("hasSession")).toHaveTextContent("false");
    });

    it("hasSession=true when a valid non-expired session is set", async () => {
      setAdminSession("valid-token", new Date(Date.now() + 60_000));
      renderContext();
      await waitFor(() => {
        expect(screen.getByTestId("isProbeSettled")).toHaveTextContent("true");
      });
      expect(screen.getByTestId("hasSession")).toHaveTextContent("true");
      // With a valid session, isDemoMode is false regardless of admin key requirement
      expect(screen.getByTestId("isDemoMode")).toHaveTextContent("false");
    });
  });
});
