import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState
} from "react";
import {
  clearAdminSession,
  getAdminSessionExpiresAtUtc,
  getAdminSessionToken,
  hydrateAdminSessionFromStorage,
  setAdminSession
} from "./adminSessionStore";
import { exchangeAdminSession } from "./api";

export interface AdminSessionContextValue {
  /** True when a non-expired session token is stored. */
  hasSession: boolean;
  /** ISO 8601 expiry from the server, or null. */
  expiresAtUtc: string | null;
  /** Exchange the static admin key for a short-lived session JWT. */
  // eslint-disable-next-line no-unused-vars -- parameter name documents the API for consumers
  signIn: (adminKey: string) => Promise<void>;
  signOut: () => void;
  /** Re-read session from storage (e.g. after tab focus). */
  refresh: () => void;
}

const AdminSessionContext = createContext<AdminSessionContextValue | null>(null);

export function AdminSessionProvider({ children }: { children: React.ReactNode }) {
  const [tick, setTick] = useState(0);

  const refresh = useCallback(() => {
    setTick((t) => t + 1);
  }, []);

  useEffect(() => {
    hydrateAdminSessionFromStorage();
    setTick((t) => t + 1);
  }, []);

  const hasSession = tick >= 0 && getAdminSessionToken() !== null;
  const expiresAtUtc = tick >= 0 ? getAdminSessionExpiresAtUtc() : null;

  const signIn = useCallback(
    async (adminKey: string) => {
      const result = await exchangeAdminSession(adminKey);
      setAdminSession(result.accessToken, new Date(result.expiresAtUtc));
      refresh();
    },
    [refresh]
  );

  const signOut = useCallback(() => {
    clearAdminSession();
    refresh();
  }, [refresh]);

  const value = useMemo(
    () => ({
      hasSession,
      expiresAtUtc,
      signIn,
      signOut,
      refresh
    }),
    [hasSession, expiresAtUtc, signIn, signOut, refresh]
  );

  return <AdminSessionContext.Provider value={value}>{children}</AdminSessionContext.Provider>;
}

/** Hook for admin session state; must be used under {@link AdminSessionProvider}. */
// eslint-disable-next-line react-refresh/only-export-components -- hook is paired with provider in this module
export function useAdminSession(): AdminSessionContextValue {
  const ctx = useContext(AdminSessionContext);
  if (!ctx) {
    throw new Error("useAdminSession must be used within AdminSessionProvider");
  }
  return ctx;
}

export default AdminSessionProvider;
