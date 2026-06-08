import { setupServer } from "msw/node";
import { http, HttpResponse } from "msw";

export const server = setupServer(
  // Default probe response: open mode (no admin key required).
  // Tests that want to exercise protected/demo mode override this with server.use().
  http.head("*/api/v1/auth-settings", () => new HttpResponse(null, { status: 200 }))
);
