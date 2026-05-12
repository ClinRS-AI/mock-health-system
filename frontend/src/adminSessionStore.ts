const storageKeyToken = "mock-health-system.adminSessionToken";
const storageKeyExpires = "mock-health-system.adminSessionExpiresAtUtc";

let memoryToken: string | null = null;
let memoryExpiresAtUtc: string | null = null;

function readFromStorage(): void {
  if (typeof sessionStorage === "undefined") {
    return;
  }

  memoryToken = sessionStorage.getItem(storageKeyToken);
  memoryExpiresAtUtc = sessionStorage.getItem(storageKeyExpires);
}

function persist(token: string | null, expiresAtIso: string | null): void {
  memoryToken = token;
  memoryExpiresAtUtc = expiresAtIso;
  if (typeof sessionStorage === "undefined") {
    return;
  }

  if (token && expiresAtIso) {
    sessionStorage.setItem(storageKeyToken, token);
    sessionStorage.setItem(storageKeyExpires, expiresAtIso);
  } else {
    sessionStorage.removeItem(storageKeyToken);
    sessionStorage.removeItem(storageKeyExpires);
  }
}

/** Hydrate from sessionStorage (call once on app load). */
export function hydrateAdminSessionFromStorage(): void {
  readFromStorage();
}

export function getAdminSessionToken(): string | null {
  if (!memoryToken || !memoryExpiresAtUtc) {
    return null;
  }

  const expires = Date.parse(memoryExpiresAtUtc);
  if (Number.isNaN(expires) || Date.now() >= expires) {
    clearAdminSession();
    return null;
  }

  return memoryToken;
}

export function getAdminSessionExpiresAtUtc(): string | null {
  return memoryExpiresAtUtc;
}

export function setAdminSession(token: string, expiresAtUtc: Date): void {
  persist(token, expiresAtUtc.toISOString());
}

export function clearAdminSession(): void {
  persist(null, null);
}
