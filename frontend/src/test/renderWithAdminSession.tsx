import { render, type RenderOptions } from "@testing-library/react";
import React from "react";
import AdminSessionProvider from "../AdminSessionContext";

export function renderWithAdminSession(
  ui: React.ReactElement,
  options?: Omit<RenderOptions, "wrapper">
) {
  function Wrapper({ children }: { children: React.ReactNode }) {
    return <AdminSessionProvider>{children}</AdminSessionProvider>;
  }
  return render(ui, { wrapper: Wrapper, ...options });
}
