import { render, type RenderOptions } from "@testing-library/react";
import React from "react";
import { http, HttpResponse } from "msw";
import AdminSessionProvider from "../AdminSessionContext";
import { server } from "./server";

export function renderWithAdminSession(
  ui: React.ReactElement,
  options?: Omit<RenderOptions, "wrapper">
) {
  function Wrapper({ children }: { children: React.ReactNode }) {
    return <AdminSessionProvider>{children}</AdminSessionProvider>;
  }
  return render(ui, { wrapper: Wrapper, ...options });
}

/** Render with the admin-key probe returning 401 so isDemoMode becomes true. */
export function renderInDemoMode(
  ui: React.ReactElement,
  options?: Omit<RenderOptions, "wrapper">
) {
  server.use(
    http.head("*/api/v1/auth-settings", () => new HttpResponse(null, { status: 401 }))
  );
  return renderWithAdminSession(ui, options);
}
