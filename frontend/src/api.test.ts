import { describe, it, expect, vi, beforeEach } from "vitest";
import { getApiStatus } from "./api";

const { mockGet } = vi.hoisted(() => ({ mockGet: vi.fn() }));
vi.mock("axios", () => ({
  default: {
    create: () => ({ get: mockGet })
  }
}));

describe("getApiStatus", () => {
  beforeEach(() => {
    mockGet.mockReset();
  });

  it("returns string response from health endpoint", async () => {
    mockGet.mockResolvedValue({ data: "Healthy" });

    const result = await getApiStatus();

    expect(result).toBe("Healthy");
  });

  it("stringifies object response when backend returns object", async () => {
    mockGet.mockResolvedValue({
      data: { status: "ok", version: "1.0" }
    });

    const result = await getApiStatus();

    expect(result).toBe('{"status":"ok","version":"1.0"}');
  });
});
